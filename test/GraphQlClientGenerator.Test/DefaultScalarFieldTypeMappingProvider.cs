namespace GraphQlClientGenerator.Test;

public class DefaultScalarFieldTypeMappingProviderTest
{
    [Theory]
    [InlineData("BigInt", "BigInteger?", null)]
    [InlineData(nameof(Byte), "byte?", null)]
    [InlineData("Date", "DateTime?", "yyyy-MM-dd")]
    [InlineData(nameof(DateOnly), "DateOnly?", "yyyy-MM-dd")]
    [InlineData(nameof(DateTime), "DateTime?", null)]
    [InlineData(nameof(DateTimeOffset), "DateTimeOffset?", null)]
    [InlineData(nameof(Decimal), "decimal?", null)]
    [InlineData(nameof(Guid), "Guid?", null)]
    [InlineData(nameof(Half), "Half?", null)]
    [InlineData("Long", "long?", null)]
    [InlineData(nameof(SByte), "SByte?", null)]
    [InlineData("Short", "short?", null)]
    [InlineData(nameof(TimeOnly), "TimeOnly?", null)]
    [InlineData("UShort", "ushort?", null)]
    [InlineData("UInt", "uint?", null)]
    [InlineData("ULong", "ulong?", null)]
    [InlineData(nameof(Uri), nameof(Uri), null)]
    [InlineData(nameof(String), "string", null)]
    [InlineData("unknown", "object", null)]
    public void GetFallbackFieldType(string graphQlTypeName, string expectedNetType, string expectedFormatMask)
    {
        var netTypeDescription =
            DefaultScalarFieldTypeMappingProvider.GetFallbackFieldType(
                new()
                {
                    ComponentType = ClientComponentType.DataClassProperty,
                    Configuration = new(),
                    FieldType = new() { Kind = GraphQlTypeKind.Scalar, Name = graphQlTypeName }
                });

        netTypeDescription.NetTypeName.ShouldBe(expectedNetType);
        netTypeDescription.FormatMask.ShouldBe(expectedFormatMask);
    }
}