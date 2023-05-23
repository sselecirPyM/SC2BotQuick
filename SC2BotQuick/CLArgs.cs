using CommandLine;

namespace SC2BotQuick
{
    public class CLArgs
    {
        [Option('c',"client-port")]
        public int ClientPort { get; set; } = 5678;
        [Option('s', "server-port")]
        public int ServerPort { get; set; } = 5677;

        [Option('d', "delay")]
        public int Delay { get; set; } = 3;

        [Option('r', "resolution")]
        public int Resolution { get; set; } = 2;

    }
}
