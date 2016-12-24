using System.IO;
using System.Text;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace GraphQlClientGenerator.Test
{
    public class GraphQlGeneratorTest
    {
        private static readonly GraphQlSchema TestSchema;

        static GraphQlGeneratorTest()
        {
            TestSchema = JsonConvert.DeserializeObject<GraphQlResult>(GetTestResource("TestSchema"), GraphQlGenerator.SerializerSettings).Data.Schema;
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

        private static string GetTestResource(string name)
        {
            using (var reader = new StreamReader(typeof(GraphQlGeneratorTest).Assembly.GetManifestResourceStream($"GraphQlClientGenerator.Test.{name}")))
                return reader.ReadToEnd();
        }
    }
}
