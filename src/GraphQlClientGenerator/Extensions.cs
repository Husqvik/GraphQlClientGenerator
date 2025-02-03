namespace GraphQlClientGenerator;

public static class Extensions
{
    public static GraphQlFieldType UnwrapIfNonNull(this GraphQlFieldType graphQlType) =>
        graphQlType.Kind == GraphQlTypeKind.NonNull ? graphQlType.OfType : graphQlType;

    internal static bool IsComplex(this GraphQlTypeKind graphQlTypeKind) =>
        graphQlTypeKind is GraphQlTypeKind.Object or GraphQlTypeKind.Interface or GraphQlTypeKind.Union;

    internal static IEnumerable<GraphQlType> GetComplexTypes(this GraphQlSchema schema) =>
        schema.Types.Where(t => t.Kind.IsComplex() && !t.IsBuiltIn());

    internal static IEnumerable<GraphQlType> GetInputObjectTypes(this GraphQlSchema schema) =>
        schema.Types.Where(t => t.Kind == GraphQlTypeKind.InputObject && !t.IsBuiltIn());

    internal static bool IsBuiltIn(this GraphQlType graphQlType) => graphQlType.Name is not null && graphQlType.Name.StartsWith("__");

    internal static string ToSetterAccessibilityPrefix(this PropertyAccessibility accessibility) =>
        accessibility switch
        {
            PropertyAccessibility.Public => null,
            PropertyAccessibility.Protected => "protected ",
            PropertyAccessibility.Internal => "internal ",
            PropertyAccessibility.ProtectedInternal => "protected internal ",
            PropertyAccessibility.Private => "private ",
            _ => throw new NotSupportedException()
        };

    internal static string EscapeXmlElementText(this string text) => text?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}