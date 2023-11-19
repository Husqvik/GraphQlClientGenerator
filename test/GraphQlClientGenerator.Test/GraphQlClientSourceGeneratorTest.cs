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
    [InlineData("GraphQlClientGenerator.DefaultScalarFieldTypeMappingProvider, GraphQlClientGenerator", false, "SourceGeneratorResult")]
    [InlineData(null, true, "SourceGeneratorResultWithFileScopedNamespaces")]
    public void SourceGeneration(string scalarFieldTypeMappingProviderTypeName, bool useFileScopedNamespace, string expectedResultResourceName)
    {
        var generatedSource = GenerateSource(SetupGeneratorOptions(OutputType.SingleFile, useFileScopedNamespace, scalarFieldTypeMappingProviderTypeName), null);

        generatedSource.Encoding.ShouldBe(Encoding.UTF8);
        var sourceCode = generatedSource.ToString();

        var expectedSourceCode = GetExpectedSourceText(expectedResultResourceName);
        sourceCode.ShouldBe(expectedSourceCode);
    }

    [Fact]
    public void SourceGenerationWithRegexCustomScalarFieldTypeMappingProvider()
    {
        var generatedSource = GenerateSource(SetupGeneratorOptions(OutputType.SingleFile, false, null), _fileMappingRules);
        var sourceCode = generatedSource.ToString();

        var expectedSourceCode = GetExpectedSourceText("SourceGeneratorResult").Replace("typeof(DateTimeOffset)", "typeof(DateTime)").Replace("DateTimeOffset?", "DateTime?");
        sourceCode.ShouldBe(expectedSourceCode);
    }

    [Fact]
    public void SourceGenerationWithOneClassPerFile()
    {
        var result = RunGenerator(SetupGeneratorOptions(OutputType.OneClassPerFile, true, null), null);
        result.GeneratedSources.Length.ShouldBe(70);
        var fileSizes = result.GeneratedSources.Where(s => s.HintName != "BaseClasses.cs").Select(s => s.SourceText.ToString().ReplaceLineEndings().Length).ToArray();
        fileSizes.ShouldBe(
            [3550, 744, 578, 791, 1404, 485, 620, 926, 642, 659, 1505, 3110, 11684, 3742, 4952, 1481, 4167, 3842, 3927, 2899, 2653, 6104, 4392, 1700, 2623, 5018, 4797, 1730, 2602, 4551, 3904, 1719, 1214, 4294, 1939, 1682, 1921, 9664, 1924, 5903, 2053, 643, 1374, 4359, 1557, 2584, 853, 2373, 1892, 1398, 1765, 943, 2586, 703, 840, 938, 3168, 2098, 704, 934, 2927, 1743, 702, 684, 1234, 812, 725, 705, 6927]);
    }

    private static string GetExpectedSourceText(string expectedResultsFile)
    {
        using var reader = new StreamReader(typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream($"GraphQlClientGenerator.Test.ExpectedSingleFileGenerationContext.{expectedResultsFile}"));
        return reader.ReadToEnd();
    }

    private static Dictionary<string, string> SetupGeneratorOptions(OutputType outputType, bool useFileScopedNamespaces, string scalarFieldTypeMappingProviderTypeName)
    {
        var configurationOptions =
            new Dictionary<string, string>
            {
                { "build_property.GraphQlClientGenerator_ClassPrefix", "SourceGenerated" },
                { "build_property.GraphQlClientGenerator_ClassSuffix", "V2" },
                { "build_property.GraphQlClientGenerator_IncludeDeprecatedFields", "true" },
                { "build_property.GraphQlClientGenerator_CommentGeneration", nameof(CommentGenerationOption.CodeSummary) },
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

    private SourceText GenerateSource(Dictionary<string, string> options, AdditionalText additionalFile)
    {
        var result = RunGenerator(options, additionalFile);
        result.GeneratedSources.Length.ShouldBe(1);
        return result.GeneratedSources[0].SourceText;
    }

    private GeneratorRunResult RunGenerator(Dictionary<string, string> options, AdditionalText additionalFile)
    {
        var compilerAnalyzerConfigOptionsProvider = new CompilerAnalyzerConfigOptionsProvider(new CompilerAnalyzerConfigOptions(options));

        var compilation = CompilationHelper.CreateCompilation(null, "SourceGeneratorTestAssembly");

        var additionalFiles = new List<AdditionalText> { _fileGraphQlSchema };

        if (additionalFile is not null)
            additionalFiles.Add(additionalFile);

        var sourceGenerator = new GraphQlClientSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(new[] { sourceGenerator }, additionalFiles, optionsProvider: compilerAnalyzerConfigOptionsProvider);
        var csharpDriver = driver.RunGenerators(compilation);
        var runResult = csharpDriver.GetRunResult();
        runResult.Results.Length.ShouldBe(1);
        return runResult.Results[0];
    }

    private class AdditionalFile : AdditionalText
    {
        public AdditionalFile(string path) => Path = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(File.ReadAllText(Path));

        public override string Path { get; }
    }

    private class CompilerAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private static readonly CompilerAnalyzerConfigOptions DummyOptions = new(new Dictionary<string, string>());

        public CompilerAnalyzerConfigOptionsProvider(AnalyzerConfigOptions globalOptions) => GlobalOptions = globalOptions;

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => DummyOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => DummyOptions;
    }

    private class CompilerAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options;

        public CompilerAnalyzerConfigOptions(Dictionary<string, string> options) => _options = options;

        public override bool TryGetValue(string key, out string value) => _options.TryGetValue(key, out value);
    }
}