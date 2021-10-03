using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Shouldly;
using Xunit;

namespace GraphQlClientGenerator.Test
{
    public class GraphQlClientSourceGeneratorTest : IDisposable
    {
        private const string FileNameTestSchema = "GraphQlClientSourceGeneratorTest";

        private readonly AdditionalFile _fileGraphQlSchema;
        private readonly AdditionalFile _fileMappingRules;

        public GraphQlClientSourceGeneratorTest()
        {
            _fileGraphQlSchema = CreateAdditionalFile("GraphQlClientGenerator.Test.TestSchemas.TestSchema3", FileNameTestSchema + ".json");
            _fileMappingRules = CreateAdditionalFile("GraphQlClientGenerator.Test.RegexCustomScalarFieldTypeMappingRules", "RegexScalarFieldTypeMappingProviderConfiguration.json");
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
            var generatedSource = GenerateSource(null, "GraphQlClientGenerator.DefaultScalarFieldTypeMappingProvider, GraphQlClientGenerator");

            generatedSource.Encoding.ShouldBe(Encoding.UTF8);
            var sourceCode = generatedSource.ToString();

            var expectedSourceCode = GetExpectedSourceText();
            sourceCode.ShouldBe(expectedSourceCode);
        }

        [Fact]
        public void SourceGenerationWithRegexCustomScalarFieldTypeMappingProvider()
        {
            var generatedSource = GenerateSource(_fileMappingRules, null);
            var sourceCode = generatedSource.ToString();

            var expectedSourceCode = GetExpectedSourceText();
            sourceCode.ShouldBe(expectedSourceCode);
        }

        private static string GetExpectedSourceText()
        {
            using var reader = new StreamReader(typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream("GraphQlClientGenerator.Test.ExpectedSingleFileGenerationContext.SourceGeneratorResult"));
            return reader.ReadToEnd();
        }

        private SourceText GenerateSource(AdditionalText additionalFile, string scalarFieldTypeMappingProviderTypeName)
        {
            var sourceGenerator = new GraphQlClientSourceGenerator();
            sourceGenerator.Initialize(new GeneratorInitializationContext());

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
                    { "build_property.GraphQlClientGenerator_HttpMethod", "GET" }
                };

            if (scalarFieldTypeMappingProviderTypeName != null)
                configurationOptions.Add("build_property.GraphQlClientGenerator_ScalarFieldTypeMappingProvider", scalarFieldTypeMappingProviderTypeName);

            var compilerAnalyzerConfigOptionsProvider = new CompilerAnalyzerConfigOptionsProvider(new CompilerAnalyzerConfigOptions(configurationOptions));

            var compilation = CompilationHelper.CreateCompilation(null, "SourceGeneratorTestAssembly");

            var generatorExecutionContextType = typeof(GeneratorExecutionContext);
            var constructorInfo = generatorExecutionContextType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var additionalFiles = new List<AdditionalText> { _fileGraphQlSchema };

            if (additionalFile != null)
                additionalFiles.Add(additionalFile);

            var additionalSourcesCollectionType = Type.GetType("Microsoft.CodeAnalysis.AdditionalSourcesCollection, Microsoft.CodeAnalysis");
            var additionalSourcesCollection = additionalSourcesCollectionType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string) }, null).Invoke(new object[] { ".cs" });

            var executionContext =
                (GeneratorExecutionContext)constructorInfo.Invoke(
                    new[]
                    {
                        compilation,
                        new CSharpParseOptions(LanguageVersion.CSharp9),
                        additionalFiles.ToImmutableArray(),
                        compilerAnalyzerConfigOptionsProvider,
                        null,
                        additionalSourcesCollection,
                        CancellationToken.None
                    });

            var additionalSourceFiles = generatorExecutionContextType.GetField("_additionalSources", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executionContext);

            sourceGenerator.Execute(executionContext);

            var sourcesAdded = ((IEnumerable)additionalSourceFiles.GetType().GetField("_sourcesAdded", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(additionalSourceFiles)).GetEnumerator();
            sourcesAdded.MoveNext().ShouldBeTrue();
            var sourceText = (SourceText)sourcesAdded.Current.GetType().GetProperty("Text").GetValue(sourcesAdded.Current);
            sourcesAdded.MoveNext().ShouldBeFalse();
            return sourceText;
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
}