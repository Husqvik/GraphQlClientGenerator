namespace GraphQlClientGenerator.Test;

public class GraphQlIntrospectionSchemaTest
{
    public class GraphQlFieldTypeTest
    {
        [Theory]
        [MemberData(nameof(EqualsTestData))]
        public void Equality(GraphQlFieldType o1, GraphQlFieldType o2, bool expectedResult)
        {
            o1.Equals(o2).ShouldBe(expectedResult);
        }

        public static IEnumerable<object[]> EqualsTestData =>
        [
            [new GraphQlFieldType(), new GraphQlFieldType(), true],
            [new GraphQlFieldType { Name = "" }, new GraphQlFieldType(), false],
            [new GraphQlFieldType(), new GraphQlFieldType { Kind = GraphQlTypeKind.Object }, false],
            [new GraphQlFieldType { Name = "TestType", Kind = GraphQlTypeKind.Interface }, new GraphQlFieldType { Name = "TestType", Kind = GraphQlTypeKind.Interface }, true],
            [new GraphQlFieldType(), new GraphQlFieldType { OfType = new GraphQlFieldType() }, false],
            [new GraphQlFieldType { OfType = new GraphQlFieldType() }, new GraphQlFieldType { OfType = new GraphQlFieldType() }, true]
        ];
    }
}