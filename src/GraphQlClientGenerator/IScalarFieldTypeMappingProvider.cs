using System.Numerics;

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

        if (propertyName is "From" or "ValidFrom" or "To" or "ValidTo" or "CreatedAt" or "UpdatedAt" or "ModifiedAt" or "DeletedAt" || propertyName.EndsWith("Timestamp")) // TODO: just ad hoc, will be removed
            return ScalarFieldTypeDescription.FromNetTypeName(GenerationContext.GetNullableNetTypeName(context, nameof(DateTimeOffset), false));

        return GetFallbackFieldType(context);
    }

    public static ScalarFieldTypeDescription GetFallbackFieldType(ScalarFieldTypeProviderContext context)
    {
        var fieldType = context.FieldType.UnwrapIfNonNull();
        if (fieldType.Kind is GraphQlTypeKind.Enum)
            return GenerationContext.GetDefaultEnumNetType(context);

        var (netType, isReference, formatMask) =
            fieldType.Name switch
            {
                "BigInt" => (nameof(BigInteger), false, null),
                "Byte" => ("byte", false, null),
                "Date" => (nameof(DateTime), false, "yyyy-MM-dd"),
                "DateOnly" => ("DateOnly", false, "yyyy-MM-dd"),
                "DateTime" => (nameof(DateTime), false, null),
                "DateTimeOffset" => (nameof(DateTimeOffset), false, null),
                "Decimal" => ("decimal", false, null),
                "Guid" => (nameof(Guid), false, null),
                "Half" => ("Half", false, null),
                "Long" => ("long", false, null),
                "SByte" => (nameof(SByte), false, null),
                "Short" => ("short", false, null),
                "TimeOnly" => ("TimeOnly", false, null),
                "UShort" => ("ushort", false, null),
                "UInt" => ("uint", false, null),
                "ULong" => ("ulong", false, null),
                "Uri" => (nameof(Uri), true, null),
                GraphQlTypeBase.GraphQlTypeScalarString => ("string", true, null),
                _ => ("object", true, null)
            };

        return
            new ScalarFieldTypeDescription
            {
                NetTypeName = GenerationContext.GetNullableNetTypeName(context, netType, isReference),
                FormatMask = formatMask
            };
    }
}