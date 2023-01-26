using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace GraphQlClientGenerator.Test;

public class GraphQlClientSourceGeneratorTest : IDisposable
{
    private const string FileNameTestSchema = "GraphQlClientSourceGeneratorTest";

    private readonly AdditionalFile _fileGraphQlSchema;
    private readonly AdditionalFile _fileMappingRules;

    public GraphQlClientSourceGeneratorTest()
    {
        _fileGraphQlSchema = CreateAdditionalFile("GraphQlClientGenerator.Test.TestSchemas.TestSchema3", $"{FileNameTestSchema}.GQL.Schema.Json");
        _fileMappingRules = CreateAdditionalFile("GraphQlClientGenerator.Test.RegexCustomScalarFieldTypeMappingRules", "RegexScalarFieldTypeMappingProvider.gql.config.JSON");
    }

    private static AdditionalFile CreateAdditionalFile(string resourceName, string fileName)
    {
        var resourceStream = typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"resource \"{resourceName}\" not found");
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

    [Fact]
    public void SourceGeneration()
    {
        var generatedSource = GenerateSource(null, "GraphQlClientGenerator.DefaultScalarFieldTypeMappingProvider, GraphQlClientGenerator", false);

        generatedSource.Encoding.ShouldBe(Encoding.UTF8);
        var sourceCode = generatedSource.ToString();

        var expectedSourceCode = GetExpectedSourceText("SourceGeneratorResult");
        sourceCode.ShouldBe(expectedSourceCode);
    }

    [Fact]
    public void SourceGeneration_UseFileScopedNamespaces()
    {
        var generatedSource = GenerateSource(null, null, true);

        generatedSource.Encoding.ShouldBe(Encoding.UTF8);
        var sourceCode = generatedSource.ToString();

        var expectedSourceCode = GetExpectedSourceText("SourceGeneratorResultWithFileScopedNamespaces");
        sourceCode.ShouldBe(expectedSourceCode);
    }

    [Fact]
    public void SourceGenerationWithRegexCustomScalarFieldTypeMappingProvider()
    {
        var generatedSource = GenerateSource(_fileMappingRules, null, false);
        var sourceCode = generatedSource.ToString();

        var expectedSourceCode = GetExpectedSourceText("SourceGeneratorResult").Replace("typeof(DateTimeOffset)", "typeof(DateTime)").Replace("DateTimeOffset?", "DateTime?");
        sourceCode.ShouldBe(expectedSourceCode);
    }

    private static string GetExpectedSourceText(string expectedResultsFile)
    {
        using var reader = new StreamReader(typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream($"GraphQlClientGenerator.Test.ExpectedSingleFileGenerationContext.{expectedResultsFile}"));
        return reader.ReadToEnd();
    }

    private SourceText GenerateSource(AdditionalText additionalFile, string scalarFieldTypeMappingProviderTypeName, bool useFileScopedNamespaces)
    {
        var configurationOptions =
            new Dictionary<string, string>
            {
                { "build_property.GraphQlClientGenerator_ClassPrefix", "SourceGenerated" },
                { "build_property.GraphQlClientGenerator_ClassSuffix", "V2" },
                { "build_property.GraphQlClientGenerator_IncludeDeprecatedFields", "true" },
                { "build_property.GraphQlClientGenerator_CommentGeneration", "CodeSummary" },
                { "build_property.GraphQlClientGenerator_FloatTypeMapping", "Double" },
                { "build_property.GraphQlClientGenerator_BooleanTypeMapping", "Boolean" },
                { "build_property.GraphQlClientGenerator_IdTypeMapping", "String" },
                { "build_property.GraphQlClientGenerator_JsonPropertyGeneration", "Always" },
                { "build_property.GraphQlClientGenerator_CustomClassMapping", "Query:Tibber|RootMutation:TibberMutation Consumption:ConsumptionEntry;Production:ProductionEntry" },
                { "build_property.GraphQlClientGenerator_Headers", "Authorization:Basic XXX|X-REQUEST-ID:123456789" },
                { "build_property.GraphQlClientGenerator_HttpMethod", "GET" },
                { "build_property.GraphQlClientGenerator_EnumValueNaming", "CSharp" }
            };

        if (scalarFieldTypeMappingProviderTypeName is not null)
            configurationOptions.Add("build_property.GraphQlClientGenerator_ScalarFieldTypeMappingProvider", scalarFieldTypeMappingProviderTypeName);


        if (useFileScopedNamespaces)
            configurationOptions.Add("build_property.GraphQlClientGenerator_FileScopedNamespaces", "true");

        var compilerAnalyzerConfigOptionsProvider = new CompilerAnalyzerConfigOptionsProvider(new CompilerAnalyzerConfigOptions(configurationOptions));

        var compilation = CompilationHelper.CreateCompilation(null, "SourceGeneratorTestAssembly");

        var additionalFiles = new List<AdditionalText> { _fileGraphQlSchema };

        if (additionalFile is not null)
            additionalFiles.Add(additionalFile);

        var sourceGenerator = new GraphQlClientSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(new [] { sourceGenerator}, additionalFiles, optionsProvider: compilerAnalyzerConfigOptionsProvider);
        var csharpDriver = driver.RunGenerators(compilation);
        var runResult = csharpDriver.GetRunResult();
        var results = runResult.Results;
        results.Length.ShouldBe(1);
        results[0].GeneratedSources.Length.ShouldBe(1);
        return results[0].GeneratedSources[0].SourceText;
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