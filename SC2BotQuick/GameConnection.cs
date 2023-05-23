using SC2APIProtocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;

namespace SC2BotQuick
{
    public class GameConnection
    {
        public ClientWebSocket clientWebSocket;
        public int connectTimeout = 100000;
        public int readWriteTimeout = 120000;

        const int bufferLength = 1024 * 1024;

        Queue<bool> isEmpty = new Queue<bool>();
        public void Connect(string address, int port)
        {
            int maxTryCount = 60;
            int count = 0;
            while (count < maxTryCount)
            {
                try
                {
                    clientWebSocket = new ClientWebSocket();
                    // Disable PING control frames (https://tools.ietf.org/html/rfc6455#section-5.5.2).
                    // It seems SC2 built in websocket server does not do PONG but tries to process ping as
                    // request and then sends empty response to client. 
                    clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromDays(30);
                    var adr = string.Format("ws://{0}:{1}/sc2api", address, port);
                    var uri = new Uri(adr);
                    CancellationTokenSource cancellationSource = new CancellationTokenSource();
                    {
                        cancellationSource.CancelAfter(connectTimeout);
                    }
                    clientWebSocket.ConnectAsync(uri, cancellationSource.Token).Wait();
                    break;

                }
                catch { Thread.Sleep(100); count++; }
            }
            if (count >= maxTryCount)
            {
                throw new Exception("The maximum number of attempts has been reached");
            }
        }

        public Response[] Request(params Request[] requests)
        {
            Response[] responses = new Response[requests.Length];
            for (int i = 0; i < requests.Length; i++)
            {
                Request request = requests[i];
                SendMessage1(request);
                isEmpty.Enqueue(false);
            }
            for (int i = 0; i < requests.Length; i++)
            {
                responses[i] = ReceiveMessage();
            }
            return responses;
        }

        public Response Request(Request request)
        {
            SendMessage1(request);
            isEmpty.Enqueue(false);
            return ReceiveMessage();
        }

        public void NoReturnRequest(Request request)
        {
            SendMessage1(request);
            isEmpty.Enqueue(true);
        }

        public void SendMessage2(Request request)
        {
            SendMessage1(request);
            isEmpty.Enqueue(false);
        }


        void SendMessage1(Request request)
        {
            var sendBuf = ArrayPool<byte>.Shared.Rent(bufferLength);
            var outStream = new MemoryStream(sendBuf);
            ProtoBuf.Serializer.Serialize(outStream, request);
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                cancellationSource.CancelAfter(readWriteTimeout);
                clientWebSocket.SendAsync(new ArraySegment<byte>(sendBuf, 0, (int)outStream.Position),
                    WebSocketMessageType.Binary, true, cancellationSource.Token).Wait();
            }
            ArrayPool<byte>.Shared.Return(sendBuf);
        }

        public Response ReceiveMessage()
        {
            while (true)
            {
                var response = ReceiveMessage1();
                if (!isEmpty.Dequeue())
                {
                    return response;
                }
            }
        }

        Response ReceiveMessage1()
        {
            var receiveBuf = ArrayPool<byte>.Shared.Rent(bufferLength);
            var finished = false;
            var currentPosition = 0;
            while (!finished)
            {
                var left = receiveBuf.Length - currentPosition;
                if (left <= 0)
                {
                    // No space left in the array, enlarge the array by doubling its size.
                    var temp = new byte[receiveBuf.Length * 2];
                    Array.Copy(receiveBuf, temp, receiveBuf.Length);
                    receiveBuf = temp;
                    left = receiveBuf.Length - currentPosition;
                }
                using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
                {
                    cancellationSource.CancelAfter(readWriteTimeout);
                    var task = clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuf, currentPosition, left), cancellationSource.Token);
                    task.Wait();
                    var result = task.Result;
                    if (result.MessageType != WebSocketMessageType.Binary)
                        throw new Exception("Expected Binary message type.");

                    currentPosition += result.Count;
                    finished = result.EndOfMessage;
                }
            }
            var response = ProtoBuf.Serializer.Deserialize<Response>(new ReadOnlySpan<byte>(receiveBuf, 0, currentPosition));
            ArrayPool<byte>.Shared.Return(receiveBuf);
            return response;
        }
    }
}
