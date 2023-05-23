using CommandLine;
using Fleck;
using System;
using System.Threading;

namespace SC2BotQuick
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //if (args.Length == 0)
            //{
            //    args = new string[]
            //    {
            //        //"-c","5678",
            //        //"-s", "5677",
            //        "-c","5677",
            //        "-s", "5678",
            //    };
            //}
            var parser = new Parser(settings => settings.CaseSensitive = false);
            parser.ParseArguments<CLArgs>(args).WithParsed(Run);
        }

        static void Run(CLArgs args)
        {
            Console.WriteLine("client:{0} server:{1}", args.ClientPort, args.ServerPort);

            GameConnection gameConnection = new GameConnection();
            gameConnection.Connect("127.0.0.1", args.ServerPort);
            Console.WriteLine("Connection to the server succeeded.\r\n");

            var server = new WebSocketServer(string.Format("ws://127.0.0.1:{0}/sc2api", args.ClientPort));

            var fakeServer = new FakeServer(gameConnection, args.Resolution, args.Delay);

            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("a client is connected");
                };

                socket.OnClose = () =>
                {
                    Console.WriteLine("client connection closed");
                };

                socket.OnBinary = binary =>
                {
                    socket.Send(fakeServer.ReceiveBytes(binary));
                };
            });

            while (true)
            {
                Thread.Sleep(1);
            }
        }
    }
}