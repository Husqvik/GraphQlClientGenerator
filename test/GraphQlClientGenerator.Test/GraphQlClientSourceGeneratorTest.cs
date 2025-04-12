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
            [3581, 745, 579, 792, 1405, 486, 621, 927, 696, 714, 1446, 2852, 10599, 3375, 4497, 1378, 3800, 3519, 3620, 2620, 2506, 5561, 4038, 1597, 2476, 4563, 4342, 1627, 2455, 4140, 3537, 1616, 1155, 4009, 1792, 1579, 1839, 8725, 2071, 6394, 2207, 709, 1699, 5619, 2092, 3257, 983, 2955, 2361, 1660, 2155, 1139, 3571, 704, 969, 1134, 4025, 2771, 833, 1130, 3716, 2281, 831, 770, 1458, 1007, 877, 780, 8460]);
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
        var driver = CSharpGeneratorDriver.Create([sourceGenerator], additionalFiles, optionsProvider: compilerAnalyzerConfigOptionsProvider);
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