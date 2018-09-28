using System.IO;
using System.Text;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace GraphQlClientGenerator.Test
{
    public class GraphQlGeneratorTest
    {
        private static readonly GraphQlSchema TestSchema = DeserializeTestSchema("TestSchema");

        private static GraphQlSchema DeserializeTestSchema(string resourceName) =>
            JsonConvert.DeserializeObject<GraphQlResult>(
                    GetTestResource(resourceName),
                    GraphQlGenerator.SerializerSettings)
                .Data.Schema;

        public GraphQlGeneratorTest() => GraphQlGeneratorConfiguration.Reset();

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
            stringBuilder.ToString().ShouldBe(expectedOutput);
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
            File.WriteAllText(@"D:\ExpectedDeprecatedAttributes", stringBuilder.ToString());
            stringBuilder.ToString().ShouldBe(expectedOutput);
        }

        private static string GetTestResource(string name)
        {
            using (var reader = new StreamReader(typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream($"GraphQlClientGenerator.Test.{name}")))
                return reader.ReadToEnd();
        }
    }
}
