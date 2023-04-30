namespace GraphQlClientGenerator;

public static class Extensions
{
    public static GraphQlFieldType UnwrapIfNonNull(this GraphQlFieldType graphQlType) =>
        graphQlType.Kind == GraphQlTypeKind.NonNull ? graphQlType.OfType : graphQlType;

    public static bool IsComplex(this GraphQlTypeKind graphQlTypeKind) =>
        graphQlTypeKind is GraphQlTypeKind.Object or GraphQlTypeKind.Interface or GraphQlTypeKind.Union;

    public static IEnumerable<GraphQlType> GetComplexTypes(this GraphQlSchema schema) =>
        schema.Types.Where(t => t.Kind.IsComplex() && !t.IsBuiltIn());

    public static IEnumerable<GraphQlType> GetInputObjectTypes(this GraphQlSchema schema) =>
        schema.Types.Where(t => t.Kind == GraphQlTypeKind.InputObject && !t.IsBuiltIn());

    public static bool IsBuiltIn(this GraphQlType graphQlType) => graphQlType.Name is not null && graphQlType.Name.StartsWith("__");
}