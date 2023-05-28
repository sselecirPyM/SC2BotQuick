using CommandLine;
using SC2APIProtocol;
namespace SC2APICall
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //args = new[]
            //{
            //    "create_game",
            //    "--map","D:/StarCraft II/Maps/BerlingradAIE.SC2Map",
            //    "--game_port","5680",
            //    "--realtime","1"
            //};
            //args = new[]
            //{
            //    "join_game",
            //    "--game_port","5680",
            //    "--start_port","5678",
            //    "--race", "3"
            //};

            Parser.Default.ParseArguments<CreateGame, JoinGame>(args).
                MapResult(
                    (CreateGame c) => _CreateGame(c),
                    (JoinGame j) => _JoinGame(j),
                    errs => 1);
        }

        static int _CreateGame(CreateGame args)
        {
            GameConnection gameConnection = new GameConnection();
            gameConnection.Connect("127.0.0.1", args.game_port);
            gameConnection.Request(new Request()
            {
                CreateGame = new RequestCreateGame()
                {
                    LocalMap = new LocalMap()
                    {
                        MapPath = args.map,
                    },
                    Realtime = args.realtime,
                    PlayerSetups =
                            {
                                new PlayerSetup()
                                {
                                    Type = PlayerType.Participant,
                                },
                                new PlayerSetup()
                                {
                                    Type = PlayerType.Participant,
                                }
                            }
                }
            });
            gameConnection.Dispose();
            return 0;
        }
        static int _JoinGame(JoinGame args)
        {
            GameConnection gameConnection = new GameConnection();

            gameConnection.Connect("127.0.0.1", args.game_port);
            gameConnection.Request(new Request()
            {
                JoinGame = new RequestJoinGame()
                {
                    Race = (Race)args.race,
                    SharedPort = args.start_port + 1,
                    ServerPorts = new PortSet
                    {
                        GamePort = args.start_port + 2,
                        BasePort = args.start_port + 3
                    },
                    ClientPorts =
                        {
                            new PortSet()
                            {
                                GamePort = args.start_port + 4,
                                BasePort = args.start_port + 5
                            }
                        }
                }
            });
            gameConnection.Dispose();
            return 0;
        }
    }
}