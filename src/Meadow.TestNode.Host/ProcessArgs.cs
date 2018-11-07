using McMaster.Extensions.CommandLineUtils;

namespace Meadow.TestNode.Host
{

    class ProcessArgs
    {
        [Option("-p|--port", "TODO.", CommandOptionType.SingleValue)]
        public uint Port { get; } = 8747;

        [Option("-h|--host", "TODO", CommandOptionType.SingleValue)]
        public string Host { get; } = "127.0.0.1";

        [Option("-a|--account_count", "TODO", CommandOptionType.SingleValue)]
        public uint AccountCount { get; } = 100;

        [Option("-b|--account_balance", "TODO", CommandOptionType.SingleValue)]
        public uint AccountBalance { get; } = 1000;

        [Option("-m|--mnemonic", "TODO", CommandOptionType.SingleValue)]
        public string Mnemonic { get; }

        [Option("--proxy_node", "TODO", CommandOptionType.SingleValue)]
        public string Proxy { get; }

        public static ProcessArgs Parse(string[] args)
        {
            var app = new CommandLineApplication<ProcessArgs>(throwOnUnexpectedArg: true);
            app.Conventions.UseDefaultConventions();
            app.Parse(args);
            return app.Model;
        }
    }

}
