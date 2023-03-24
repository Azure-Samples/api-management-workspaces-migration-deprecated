using CommandLine;

namespace MigrationTool.Migration.Domain
{
    public class MigrationProgramConfig
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('r', "resourceGroup", Required = true, HelpText = "Set resource group.")]
        public string ResourceGroup { get; set; }

        [Option('s', "serviceName", Required = true, HelpText = "Set service name.")]
        public string ServiceName { get; set; }
    }

}
