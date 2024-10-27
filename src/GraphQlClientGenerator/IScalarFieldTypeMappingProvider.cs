namespace GraphQlClientGenerator;

public interface IScalarFieldTypeMappingProvider
{
    ScalarFieldTypeDescription GetCustomScalarFieldType(ScalarFieldTypeProviderContext context);
}

public sealed class DefaultScalarFieldTypeMappingProvider : IScalarFieldTypeMappingProvider
{
    public static readonly DefaultScalarFieldTypeMappingProvider Instance = new();

    public ScalarFieldTypeDescription GetCustomScalarFieldType(ScalarFieldTypeProviderContext context)
    {
        var propertyName = NamingHelper.ToPascalCase(context.FieldName);

        if (propertyName is "From" or "ValidFrom" or "To" or "ValidTo" or "CreatedAt" or "UpdatedAt" or "ModifiedAt" or "DeletedAt" || propertyName.EndsWith("Timestamp"))
            return new ScalarFieldTypeDescription { NetTypeName = "DateTimeOffset?" };

        return GetFallbackFieldType(context);
    }

    public static ScalarFieldTypeDescription GetFallbackFieldType(ScalarFieldTypeProviderContext context)
    {
        var fieldType = context.FieldType.UnwrapIfNonNull();
        if (fieldType.Kind == GraphQlTypeKind.Enum)
            return GenerationContext.GetDefaultEnumNetType(context);

        var dataType = fieldType.Name == GraphQlTypeBase.GraphQlTypeScalarString ? "string" : "object";
        return GenerationContext.GetReferenceNetType(context, dataType);
    }
}