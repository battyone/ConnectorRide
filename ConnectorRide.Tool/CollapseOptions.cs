using CommandLine;

namespace Knapcode.ConnectorRide.Tool
{
    [Verb("collapse", HelpText = "Collapse duplicate and adjacent blobs.")]
    public class CollapseOptions
    {
        [Option('s', "connection-string", Required = true, HelpText = "The connection string for Azure Storage.")]
        public string ConnectionString { get; set; }

        [Option('c', "container", Required = true, HelpText = "The container name.")]
        public string Container { get; set; }

        [Option('f', "path-format", Required = true, HelpText = "The format to use when matching blobs to collapse.")]
        public string PathFormat { get; set; }

        [Option('t', "comparison-type", Required = true, HelpText = "The type of comparison to make on matched blobs.")]
        public ComparisonType ComparisonType { get; set; }
    }
}
