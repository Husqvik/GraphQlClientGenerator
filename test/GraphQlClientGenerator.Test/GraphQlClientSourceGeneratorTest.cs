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
        private const string TestFileName = "GraphQlClientSourceGeneratorTest";

        private readonly GraphQlSchemaFile _schemaFile;

        public GraphQlClientSourceGeneratorTest()
        {
            var resourceStream = typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream("GraphQlClientGenerator.Test.TestSchema3");
            var fileName = Path.Combine(Path.GetTempPath(), TestFileName + ".json");
            using var fileStream = File.Create(fileName);
            resourceStream.CopyTo(fileStream);
            _schemaFile = new GraphQlSchemaFile(fileName);
        }

        public void Dispose()
        {
            if (File.Exists(_schemaFile.Path))
                File.Delete(_schemaFile.Path);
        }

        [Fact]
        public void SourceGeneration()
        {
            var sourceGenerator = new GraphQlClientSourceGenerator();
            sourceGenerator.Initialize(new GeneratorInitializationContext());

            var compilerAnalyzerConfigOptionsProvider =
                new CompilerAnalyzerConfigOptionsProvider(
                    new CompilerAnalyzerConfigOptions(
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
                            { "build_property.GraphQlClientGenerator_Headers", "Authorization:Basic XXX|X-REQUEST-ID:123456789" }
                        }));

            var compilation = CompilationHelper.CreateCompilation(null, "SourceGeneratorTestAssembly");

            var generatorExecutionContextType = typeof(GeneratorExecutionContext);
            var constructorInfo = generatorExecutionContextType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var executionContext =
                (GeneratorExecutionContext)constructorInfo.Invoke(
                    new object[]
                    {
                        compilation,
                        new CSharpParseOptions(LanguageVersion.CSharp9),
                        new AdditionalText [] { _schemaFile }.ToImmutableArray(),
                        compilerAnalyzerConfigOptionsProvider,
                        null,
                        CancellationToken.None
                    });

            var additionalSourceFiles = generatorExecutionContextType.GetField("_additionalSources", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(executionContext);
            
            sourceGenerator.Execute(executionContext);

            var sourcesAdded = ((IEnumerable)additionalSourceFiles.GetType().GetField("_sourcesAdded", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(additionalSourceFiles)).GetEnumerator();
            sourcesAdded.MoveNext().ShouldBeTrue();
            var generatedSource = (SourceText)sourcesAdded.Current.GetType().GetProperty("Text").GetValue(sourcesAdded.Current);
            sourcesAdded.MoveNext().ShouldBeFalse();
            generatedSource.Encoding.ShouldBe(Encoding.UTF8);
            var sourceCode = generatedSource.ToString();

            using var reader = new StreamReader(typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream("GraphQlClientGenerator.Test.ExpectedSourceGeneratorResult"));
            var expectedSourceCode = reader.ReadToEnd();
            sourceCode.ShouldBe(expectedSourceCode);
        }

        private class GraphQlSchemaFile : AdditionalText
        {
            public GraphQlSchemaFile(string path) => Path = path;

            public override SourceText GetText(CancellationToken cancellationToken = default) =>
                SourceText.From(File.ReadAllText(Path));

            public override string Path { get; }
        }

        private class CompilerAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private static readonly CompilerAnalyzerConfigOptions DummyOptions = new CompilerAnalyzerConfigOptions(new Dictionary<string, string>());

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