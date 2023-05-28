using SC2APIProtocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SC2BotQuick
{
    public enum FakeServerState
    {
        Init = 0,
        CreateGame,
        Loop,
    }

    public class FakeServer
    {
        public GameConnection gameConnection;


        int receiveLoop;
        int currentLoop;

        int resolution;
        int delay;

        public Response ObservationCache;
        public Response GameInfoCache;
        public Response GameDataCache;


        FakeServerState state;

        Queue<Request> waitForPorcess = new Queue<Request>();
        ConcurrentQueue<Response> receiveQueue = new ConcurrentQueue<Response>();

        public FakeServer(GameConnection gameConnection, int resolution, int delay)
        {
            this.gameConnection = gameConnection;
            this.resolution = resolution;
            this.delay = delay;
        }

        void ReceiveTask()
        {
            while (true)
            {
                var response = gameConnection.ReceiveMessage();
                if (response.Step != null)
                {
                    receiveLoop++;
                }
                receiveQueue.Enqueue(response);
            }
        }

        Response ReceiveRequest(Request request)
        {
            //return gameConnection.Request(request);
            if (request.Action != null)
            {
                gameConnection.NoReturnRequest(request);
                return new Response()
                {
                    Action = new ResponseAction()
                };
            }
            else if (request.Observation != null)
            {
                Debug.Assert(ObservationCache.Observation != null);
                return ObservationCache;
            }
            else if (request.Data != null)
            {
                Debug.Assert(GameDataCache.Data != null);
                return GameDataCache;
            }
            else if (request.GameInfo != null)
            {
                Debug.Assert(GameInfoCache.GameInfo != null);
                return GameInfoCache;
            }
            else if (request.Step != null)
            {
                GoNextFrame();
                return new Response()
                {
                    Step = new ResponseStep()
                    {
                        SimulationLoop = (uint)currentLoop
                    }
                };
            }
            else if (request.JoinGame != null)
            {
                Debug.Assert(state != FakeServerState.Loop);
                state = FakeServerState.Loop;
                var response = gameConnection.Request(request);

                Task.Run(ReceiveTask);
                AfterJoinGame();
                GoNextFrame();
                return response;
            }
            else if (request.CreateGame != null)
            {
                Debug.Assert(state != FakeServerState.Loop);

                state = FakeServerState.CreateGame;
                return gameConnection.Request(request);
            }
            else if (request.Debug != null)
            {
                gameConnection.NoReturnRequest(request);
                return new Response();
            }
            else if (request.Quit != null)
            {
                gameConnection.NoReturnRequest(request);
                return new Response();
            }
            else if (request.LeaveGame != null)
            {
                gameConnection.NoReturnRequest(request);
                return new Response();
            }
            else if (request.AvailableMaps != null)
            {
                Debug.Assert(state != FakeServerState.Loop);
                gameConnection.Request(request);
            }
            else if (request.Query != null)
            {
                if (request.Query.Abilities != null)
                {
                    var responseQuery = new ResponseQuery()
                    {

                    };
                    var response = new Response()
                    {
                        Query = responseQuery
                    };
                    foreach (var abil in responseQuery.Abilities)
                    {
                        responseQuery.Abilities.Add(new ResponseQueryAvailableAbilities()
                        {
                            UnitTag = abil.UnitTag,
                        });
                    }
                    return response;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (request.Ping != null)
            {
                return new Response();
            }
            else
            {
                Debug.Assert(false);
                //return gameConnection.Request(request);
            }
            return new Response();
        }

        void AfterJoinGame()
        {
            var requestGameInfo = new Request
            {
                GameInfo = new RequestGameInfo()
            };
            AsyncMessage(requestGameInfo);


            var requestGameData = new Request
            {
                Data = new RequestData()
                {
                    AbilityId = true,
                    BuffId = true,
                    EffectId = true,
                    UnitTypeId = true,
                    UpgradeId = true
                }
            };
            AsyncMessage(requestGameData);


            for (int i = 0; i < delay; i++)
            {
                StepObservation();
            }
        }

        int t = 0;

        void StepObservation()
        {
            var requestStep = new Request()
            {
                Step = new RequestStep
                {
                    Count = 1
                }
            };
            AsyncMessage(requestStep);

            if (t == 0)
            {
                var requestObservation = new Request
                {
                    Observation = new RequestObservation()
                };
                AsyncMessage(requestObservation);
            }
            t = (t + 1) % resolution;
        }

        int loop = 0;
        void GoNextFrame()
        {
            while (currentLoop == receiveLoop)
            {
                Thread.Sleep(1);
            }
            currentLoop++;

            int stepCount = 0;
            while (true)
            {
                var request = waitForPorcess.Peek();

                if (receiveQueue.TryPeek(out var response))
                {
                    if (request.Step != null)
                    {
                        stepCount++;
                    }
                    if (stepCount == 2)
                    {
                        break;
                    }
                    if (request.Step != null)
                    {
                        StepObservation();
                    }
                    receiveQueue.TryDequeue(out var useless);
                    waitForPorcess.Dequeue();
                    CacheRequest(response);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
            if (loop == ObservationCache.Observation.Observation.GameLoop)
            {
                ObservationCache.Observation.Observation.GameLoop++;
                loop++;
            }
            else
            {
                loop = (int)ObservationCache.Observation.Observation.GameLoop;
            }
        }

        void AsyncMessage(Request request)
        {
            waitForPorcess.Enqueue(request);
            gameConnection.SendMessage2(request);
        }

        void CacheRequest(Response response)
        {
            if (response.Observation != null)
            {
                ObservationCache = response;
            }
            else if (response.GameInfo != null)
            {
                GameInfoCache = response;
            }
            else if (response.Data != null)
            {
                GameDataCache = response;
            }
        }

        MemoryStream memoryStream = new MemoryStream();
        public byte[] ReceiveBytes(byte[] bytes)
        {
            lock (memoryStream)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                var request = ProtoBuf.Serializer.Deserialize<Request>(bytes.AsSpan());
                var response = ReceiveRequest(request);
                ProtoBuf.Serializer.Serialize<Response>(memoryStream, response);
                byte[] responseBytes = memoryStream.GetBuffer()[0..(int)memoryStream.Position];

                return responseBytes;
            }
        }
        //MemoryStream memoryStream = new MemoryStream();
        //public byte[] ReceiveBytes(byte[] bytes)
        //{
        //    lock (memoryStream)
        //    {
        //        memoryStream.Seek(0, SeekOrigin.Begin);
        //        var request = ProtoBuf.Serializer.Deserialize<Request>(bytes.AsSpan());
        //        //var response = ReceiveRequest(request);
        //        var response = gameConnection.Request(request);
        //        ProtoBuf.Serializer.Serialize<Response>(memoryStream, response);
        //        byte[] responseBytes = memoryStream.GetBuffer()[0..(int)memoryStream.Position];
        //        if (response.Errors != null)
        //            foreach (var error in response.Errors)
        //                Console.WriteLine(error);

        //        return responseBytes;

        //    }
        //}
    }
}
