using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace GraphQlClientGenerator.Test;

public class GraphQlClientSourceGeneratorTest : IDisposable
{
    private const string FileNameTestSchema = "GraphQlClientSourceGeneratorTest";

    private readonly AdditionalFile _fileGraphQlSchema =
        CreateAdditionalFile("GraphQlClientGenerator.Test.TestSchemas.TestSchema3", $"{FileNameTestSchema}.GQL.Schema.Json");

    private readonly AdditionalFile _fileMappingRules =
        CreateAdditionalFile("GraphQlClientGenerator.Test.RegexCustomScalarFieldTypeMappingRules", "RegexScalarFieldTypeMappingProvider.gql.config.JSON");

    private static AdditionalFile CreateAdditionalFile(string resourceName, string fileName)
    {
        var resourceStream =
            typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"resource \"{resourceName}\" not found");

        var fullFileName = Path.Combine(Path.GetTempPath(), fileName);
        using var fileStream = File.Create(fullFileName);
        resourceStream.CopyTo(fileStream);
        return new AdditionalFile(fullFileName);
    }

    public void Dispose()
    {
        if (File.Exists(_fileGraphQlSchema.Path))
            File.Delete(_fileGraphQlSchema.Path);

        if (File.Exists(_fileMappingRules.Path))
            File.Delete(_fileMappingRules.Path);
    }

    [Theory]
    [InlineData("GraphQlClientGenerator.DefaultScalarFieldTypeMappingProvider, GraphQlClientGenerator", false)]
    [InlineData(null, true)]
    public Task SourceGeneration(string scalarFieldTypeMappingProviderTypeName, bool useFileScopedNamespace)
    {
        var generatedSource = GenerateSource(SetupGeneratorOptions(OutputType.SingleFile, useFileScopedNamespace, scalarFieldTypeMappingProviderTypeName), null);

        generatedSource.Encoding.ShouldBe(Encoding.UTF8);
        var sourceCode = generatedSource.ToString();
        return Verify(sourceCode).UseParameters(useFileScopedNamespace);
    }

    [Fact]
    public Task SourceGenerationWithRegexCustomScalarFieldTypeMappingProvider()
    {
        var options = SetupGeneratorOptions(OutputType.SingleFile, false, null);
        options.Add("build_property.GraphQlClientGenerator_DataClassMemberNullability", nameof(DataClassMemberNullability.DefinedBySchema));
        options.Add("build_property.GraphQlClientGenerator_GenerationOrder", nameof(GenerationOrder.Alphabetical));
        options.Add("build_property.GraphQlClientGenerator_EnableNullableReferences", "true");
        options.Add("build_property.GraphQlClientGenerator_InputObjectMode", "POCO");

        var generatedSource = GenerateSource(options, _fileMappingRules, NullableContextOptions.Enable);
        var sourceCode = generatedSource.ToString();
        return Verify(sourceCode);
    }

    [Fact]
    public void SourceGenerationWithOneClassPerFile()
    {
        var result = RunGenerator(SetupGeneratorOptions(OutputType.OneClassPerFile, true, null), null, NullableContextOptions.Disable);
        result.GeneratedSources.Length.ShouldBe(70);
        var fileSizes = result.GeneratedSources.Where(s => s.HintName != "BaseClasses.cs").Select(s => s.SourceText.ToString().ReplaceLineEndings().Length).ToArray();
        fileSizes.ShouldBe(
            [3501, 813, 647, 860, 1473, 554, 689, 995, 616, 634, 1366, 2772, 10519, 3295, 4417, 1298, 3720, 3460, 3540, 2540, 2426, 5481, 3979, 1517, 2396, 4504, 4304, 1547, 2375, 4060, 3457, 1536, 1075, 3929, 1712, 1499, 1759, 8645, 2670, 8417, 2829, 718, 2064, 7141, 2724, 4067, 1081, 3587, 2904, 1936, 2609, 1326, 4737, 778, 1067, 1321, 5013, 3581, 931, 1317, 4615, 2913, 929, 779, 1645, 1194, 975, 789, 10249]);
    }

    private static Dictionary<string, string> SetupGeneratorOptions(OutputType outputType, bool useFileScopedNamespaces, string scalarFieldTypeMappingProviderTypeName)
    {
        var configurationOptions =
            new Dictionary<string, string>
            {
                { "build_property.GraphQlClientGenerator_ClassPrefix", "SourceGenerated" },
                { "build_property.GraphQlClientGenerator_ClassSuffix", "V2" },
                { "build_property.GraphQlClientGenerator_IncludeDeprecatedFields", "true" },
                { "build_property.GraphQlClientGenerator_CodeDocumentationType", nameof(CodeDocumentationType.XmlSummary) },
                { "build_property.GraphQlClientGenerator_FloatTypeMapping", nameof(FloatTypeMapping.Double) },
                { "build_property.GraphQlClientGenerator_BooleanTypeMapping", nameof(BooleanTypeMapping.Boolean) },
                { "build_property.GraphQlClientGenerator_IdTypeMapping", nameof(IdTypeMapping.String) },
                { "build_property.GraphQlClientGenerator_JsonPropertyGeneration", nameof(JsonPropertyGenerationOption.Always) },
                { "build_property.GraphQlClientGenerator_CustomClassMapping", "Query:Tibber|RootMutation:TibberMutation Consumption:ConsumptionEntry;Production:ProductionEntry" },
                { "build_property.GraphQlClientGenerator_Headers", "Authorization:Basic XXX|X-REQUEST-ID:123456789" },
                { "build_property.GraphQlClientGenerator_HttpMethod", "GET" },
                { "build_property.GraphQlClientGenerator_EnumValueNaming", nameof(EnumValueNamingOption.CSharp) },
                { "build_property.GraphQlClientGenerator_OutputType", outputType.ToString() }
            };

        if (scalarFieldTypeMappingProviderTypeName is not null)
            configurationOptions.Add("build_property.GraphQlClientGenerator_ScalarFieldTypeMappingProvider", scalarFieldTypeMappingProviderTypeName);

        if (useFileScopedNamespaces)
            configurationOptions.Add("build_property.GraphQlClientGenerator_FileScopedNamespaces", "true");

        return configurationOptions;
    }

    private SourceText GenerateSource(Dictionary<string, string> options, AdditionalText additionalFile, NullableContextOptions nullableContextOptions = NullableContextOptions.Disable)
    {
        var result = RunGenerator(options, additionalFile, nullableContextOptions);
        result.GeneratedSources.Length.ShouldBe(1);
        return result.GeneratedSources[0].SourceText;
    }

    private GeneratorRunResult RunGenerator(Dictionary<string, string> options, AdditionalText additionalFile, NullableContextOptions nullableContextOptions)
    {
        var compilerAnalyzerConfigOptionsProvider = new CompilerAnalyzerConfigOptionsProvider(new CompilerAnalyzerConfigOptions(options));

        var compilation = CompilationHelper.CreateCompilation(null, "SourceGeneratorTestAssembly", nullableContextOptions);

        var additionalFiles = new List<AdditionalText> { _fileGraphQlSchema };

        if (additionalFile is not null)
            additionalFiles.Add(additionalFile);

        var sourceGenerator = new GraphQlClientSourceGenerator();
        var driver =
            CSharpGeneratorDriver.Create(sourceGenerator)
                .WithUpdatedAnalyzerConfigOptions(compilerAnalyzerConfigOptionsProvider)
                .AddAdditionalTexts([.. additionalFiles]);

        var csharpDriver = driver.RunGenerators(compilation);
        var runResult = csharpDriver.GetRunResult();
        runResult.Results.Length.ShouldBe(1);
        return runResult.Results[0];
    }

    private class AdditionalFile(string path) : AdditionalText
    {
        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(File.ReadAllText(Path), Encoding.UTF8);

        public override string Path { get; } = path;
    }

    private class CompilerAnalyzerConfigOptionsProvider(AnalyzerConfigOptions globalOptions) : AnalyzerConfigOptionsProvider
    {
        private static readonly CompilerAnalyzerConfigOptions DummyOptions = new([]);

        public override AnalyzerConfigOptions GlobalOptions { get; } = globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => DummyOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => DummyOptions;
    }

    private class CompilerAnalyzerConfigOptions(Dictionary<string, string> options) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value) => options.TryGetValue(key, out value);
    }
}