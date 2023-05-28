using CommandLine;

namespace SC2APICall
{
    [Verb("join_game")]
    public class JoinGame
    {
        [Option]
        public int game_port { get; set; }
        [Option]
        public int start_port { get; set; }
        [Option]
        public int race { get; set; } = (int)SC2APIProtocol.Race.Random;
    }

    [Verb("create_game")]
    public class CreateGame
    {
        [Option]
        public string map { get; set; }
        [Option]
        public int game_port { get; set; }
        [Option]
        public bool realtime { get; set; }
    }
}
