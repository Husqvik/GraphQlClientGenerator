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
            return ScalarFieldTypeDescription.FromNetTypeName(GenerationContext.GetNullableNetTypeName(context, nameof(DateTimeOffset), false));

        return GetFallbackFieldType(context);
    }

    public static ScalarFieldTypeDescription GetFallbackFieldType(ScalarFieldTypeProviderContext context)
    {
        var fieldType = context.FieldType.UnwrapIfNonNull();
        if (fieldType.Kind is GraphQlTypeKind.Enum)
            return GenerationContext.GetDefaultEnumNetType(context);

        var dataType = fieldType.Name is GraphQlTypeBase.GraphQlTypeScalarString ? "string" : "object";
        return ScalarFieldTypeDescription.FromNetTypeName(GenerationContext.GetNullableNetTypeName(context, dataType, true));
    }
}