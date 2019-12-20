namespace GraphQlClientGenerator
{
    public static class Extensions
    {
        public static GraphQlFieldType UnwrapIfNonNull(this GraphQlFieldType graphQlType) =>
            graphQlType.Kind == GraphQlTypeKind.NonNull ? graphQlType.OfType : graphQlType;
    }
}