using CommandLine;

namespace GraphQlClientGenerator.Console
{
    public enum OutputType
    {
        SingleFile,
        OneClassPerFile
    }

    public class ProgramOptions
    {
        [Option('u', "serviceUrl", Required = true, HelpText = "GraphQL service URL used for retrieving schema metadata")]
        public string ServiceUrl { get; set; }

        [Option('o', "outputPath", Required = true, HelpText = "Output path")]
        public string OutputPath { get; set; }

        [Option('n', "namespace", Required = true, HelpText = "Root namespace all classes and other members are generated to")]
        public string Namespace { get; set; }

        [Option("authorization", Required = false, HelpText = "Authorization header value")]
        public string Authorization { get; set; }

        [Option("classPostfix", Required = false, HelpText = "Class postfix, for instance for version control; value \"V2\" extends class name to \"TypeNameV2\"")]
        public string ClassPostfix { get; set; }

        [Option("csharpVersion", Required = false, HelpText = "C# version compatibility; allowed values: " + nameof(CSharpVersion.Compatible) + " (default), " + nameof(CSharpVersion.Newest) + ", " + nameof(CSharpVersion.NewestWithNullableReferences))]
        public CSharpVersion CSharpVersion { get; set; }

        [Option("memberAccessibility", Required = false, HelpText = "Class and interface access level; allowed values: " + nameof(MemberAccessibility.Public) + " (default), " + nameof(MemberAccessibility.Internal))]
        public MemberAccessibility MemberAccessibility { get; set; }

        [Option("outputType", Required = false, HelpText = "Specifies generated classes organization; allowed values: " + nameof(OutputType.SingleFile) + " (default), " + nameof(OutputType.OneClassPerFile))]
        public OutputType OutputType { get; set; }

        [Option("partialClasses", Required = false, HelpText = "Mark classes as \"partial\"")]
        public bool PartialClasses { get; set; }

        [Option("idTypeMapping", Required = false, HelpText = "Determines the .NET type generated for GraphQL ID data type.")]
        public IdTypeMapping IdTypeMapping { get; set; }
    }
}