namespace GraphQlClientGenerator;

public interface IScalarFieldTypeMappingProvider
{
    ScalarFieldTypeDescription GetCustomScalarFieldType(GraphQlGeneratorConfiguration configuration, GraphQlType baseType, GraphQlTypeBase valueType, string valueName);
}

public struct ScalarFieldTypeDescription
{
    public string NetTypeName { get; set; }
    public string FormatMask { get; set; }

    public static ScalarFieldTypeDescription FromNetTypeName(string netTypeName) => new() { NetTypeName = netTypeName };
}

public sealed class DefaultScalarFieldTypeMappingProvider : IScalarFieldTypeMappingProvider
{
    public static readonly DefaultScalarFieldTypeMappingProvider Instance = new();

    public ScalarFieldTypeDescription GetCustomScalarFieldType(GraphQlGeneratorConfiguration configuration, GraphQlType baseType, GraphQlTypeBase valueType, string valueName)
    {
        valueName = NamingHelper.ToPascalCase(valueName);

        if (valueName is "From" or "ValidFrom" or "To" or "ValidTo" or "CreatedAt" or "UpdatedAt" or "ModifiedAt" or "DeletedAt" || valueName.EndsWith("Timestamp"))
            return new ScalarFieldTypeDescription { NetTypeName = "DateTimeOffset?" };

        return GetFallbackFieldType(configuration, valueType);
    }

    public static ScalarFieldTypeDescription GetFallbackFieldType(GraphQlGeneratorConfiguration configuration, GraphQlTypeBase valueType)
    {
        valueType = (valueType as GraphQlFieldType)?.UnwrapIfNonNull() ?? valueType;
        if (valueType.Kind == GraphQlTypeKind.Enum)
            return new ScalarFieldTypeDescription { NetTypeName = configuration.ClassPrefix + NamingHelper.ToPascalCase(valueType.Name) + configuration.ClassSuffix + "?" };

        var dataType = valueType.Name == GraphQlTypeBase.GraphQlTypeScalarString ? "string" : "object";
        return new ScalarFieldTypeDescription { NetTypeName = GraphQlGenerator.AddQuestionMarkIfNullableReferencesEnabled(configuration, dataType) };
    }
}