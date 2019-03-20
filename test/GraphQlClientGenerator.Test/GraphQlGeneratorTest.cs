using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace GraphQlClientGenerator.Test
{
    public class GraphQlGeneratorTest
    {
        private static readonly GraphQlSchema TestSchema = DeserializeTestSchema("TestSchema");

        private readonly ITestOutputHelper _outputHelper;

        private static GraphQlSchema DeserializeTestSchema(string resourceName) =>
            JsonConvert.DeserializeObject<GraphQlResult>(
                    GetTestResource(resourceName),
                    GraphQlGenerator.SerializerSettings)
                .Data.Schema;

        public GraphQlGeneratorTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            GraphQlGeneratorConfiguration.Reset();
        }

        [Fact]
        public void GenerateQueryBuilder()
        {
            var stringBuilder = new StringBuilder();
            GraphQlGenerator.GenerateQueryBuilder(TestSchema, stringBuilder);

            var expectedQueryBuilders = GetTestResource("ExpectedQueryBuilders");
            stringBuilder.ToString().ShouldBe(expectedQueryBuilders);
        }

        [Fact]
        public void GenerateDataClasses()
        {
            var stringBuilder = new StringBuilder();
            GraphQlGenerator.GenerateDataClasses(TestSchema, stringBuilder);

            var expectedDataClasses = GetTestResource("ExpectedDataClasses");
            stringBuilder.ToString().ShouldBe(expectedDataClasses);
        }

        [Fact]
        public void NewCSharpSyntaxWithClassPostfix()
        {
            GraphQlGeneratorConfiguration.CSharpVersion = CSharpVersion.Newest;
            GraphQlGeneratorConfiguration.ClassPostfix = "V1";
            var schema = DeserializeTestSchema("TestSchema2");

            var stringBuilder = new StringBuilder();
            GraphQlGenerator.GenerateQueryBuilder(schema, stringBuilder);
            GraphQlGenerator.GenerateDataClasses(schema, stringBuilder);

            var expectedOutput = GetTestResource("ExpectedNewCSharpSyntaxWithClassPostfix");
            var generatedSourceCode = stringBuilder.ToString();
            generatedSourceCode.ShouldBe(expectedOutput);

            var syntaxTree =
                SyntaxFactory.ParseSyntaxTree(
                    $@"{GraphQlGenerator.RequiredNamespaces}

namespace GraphQLTestAssembly
{{
{generatedSourceCode}
}}",
                    CSharpParseOptions.Default.WithLanguageVersion(Enum.GetValues(typeof(LanguageVersion)).Cast<LanguageVersion>().Max()));

            var compilationOptions =
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithPlatform(Platform.AnyCpu)
                    .WithOverflowChecks(true)
                    .WithOptimizationLevel(OptimizationLevel.Release);

            var systemReference = MetadataReference.CreateFromFile(typeof(DateTimeOffset).Assembly.Location);
            var runtimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);
            var netStandardReference = MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location);
            var linqReference = MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location);
            var runtimeSerializationReference = MetadataReference.CreateFromFile(typeof(EnumMemberAttribute).Assembly.Location);

            var compilation =
                CSharpCompilation.Create(
                    "GraphQLTestAssembly",
                    new [] { syntaxTree },
                    new [] { systemReference, runtimeSerializationReference, runtimeReference, linqReference, netStandardReference }, compilationOptions);

            var assemblyFileName = Path.GetTempFileName();
            var result = compilation.Emit(assemblyFileName);
            var errorReport = String.Join(Environment.NewLine, result.Diagnostics.Select(l => l.ToString()));
            errorReport.ShouldBeNullOrEmpty();

            Assembly.LoadFrom(assemblyFileName);
            Type.GetType("GraphQLTestAssembly.GraphQlQueryBuilder,GraphQLTestAssembly").ShouldNotBeNull();
        }

        [Fact]
        public void DeprecatedAttributes()
        {
            GraphQlGeneratorConfiguration.CSharpVersion = CSharpVersion.Newest;
            GraphQlGeneratorConfiguration.GenerateComments = true;
            GraphQlGeneratorConfiguration.IncludeDeprecatedFields = true;
            var schema = DeserializeTestSchema("TestSchemaWithDeprecatedFields");

            var stringBuilder = new StringBuilder();
            GraphQlGenerator.GenerateDataClasses(schema, stringBuilder);

            var expectedOutput = GetTestResource("ExpectedDeprecatedAttributes");
            stringBuilder.ToString().ShouldBe(expectedOutput);
        }

        private static string GetTestResource(string name)
        {
            using (var reader = new StreamReader(typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream($"GraphQlClientGenerator.Test.{name}")))
                return reader.ReadToEnd();
        }
    }
}
