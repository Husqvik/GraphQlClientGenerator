using System.Collections.Generic;
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
        [Option('o', "outputPath", Required = true, HelpText = "Output path; include file name for single file output type; folder name for one class per file output type")]
        public string OutputPath { get; set; }

        [Option('n', "namespace", Required = true, HelpText = "Root namespace all classes and other members are generated to")]
        public string Namespace { get; set; }

        [Option('u', "serviceUrl", HelpText = "GraphQL service URL used for retrieving schema metadata")]
        public string ServiceUrl { get; set; }

        [Option('s', "schemaFileName", HelpText = "Path to schema metadata file in JSON format")]
        public string SchemaFileName { get; set; }

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
        
        [Option("classMapping", Required = false, HelpText = "Format: {GraphQlTypeName}:{C#ClassName}; allows to define custom class names for specific GraphQL types; common reason for this is to avoid property of the same name as its parent class")]
        public IEnumerable<string> ClassMapping { get; set; }

        [Option("idTypeMapping", Required = false, HelpText = "Specifies the .NET type generated for GraphQL ID data type; allowed values: " + nameof(IdTypeMapping.Guid) + " (default), " + nameof(IdTypeMapping.String) + ", " + nameof(IdTypeMapping.Object))]
        public IdTypeMapping IdTypeMapping { get; set; }

        [Option("floatTypeMapping", Required = false, HelpText = "Specifies the .NET type generated for GraphQL Float data type; allowed values: " + nameof(FloatTypeMapping.Decimal) + " (default), " + nameof(FloatTypeMapping.Double) + ", " + nameof(FloatTypeMapping.Float))]
        public FloatTypeMapping FloatTypeMapping { get; set; }

        [Option("jsonPropertyAttribute", Required = false, HelpText = "Specifies the condition for using \"JsonPropertyAttribute\"; allowed values: " + nameof(JsonPropertyGenerationOption.CaseInsensitive) + " (default), " + nameof(JsonPropertyGenerationOption.CaseSensitive) + ", " + nameof(JsonPropertyGenerationOption.Always) + ", " + nameof(JsonPropertyGenerationOption.Never) + ", " + nameof(JsonPropertyGenerationOption.UseDefaultAlias))]
        public JsonPropertyGenerationOption JsonPropertyAttribute { get; set; }
    }
}