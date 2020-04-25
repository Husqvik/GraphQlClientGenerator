using CommandLine;

namespace GraphQlClientGenerator.Console
{
    public class ProgramOptions
    {
        [Option('u', "serviceUrl", Required = true, HelpText = "GraphQL service URL used for retrieving schema metadata")]
        public string ServiceUrl { get; set; }

        [Option('o', "outputFileName", Required = true, HelpText = "Output file name")]
        public string OutputFileName { get; set; }

        [Option('n', "namespace", Required = true, HelpText = "Root namespace")]
        public string Namespace { get; set; }
    }
}