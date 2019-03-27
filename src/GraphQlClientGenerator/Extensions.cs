namespace GraphQlClientGenerator
{
    public static class Extensions
    {
        public static GraphQlFieldType UnwrapIfNonNull(this GraphQlFieldType graphQlType) =>
            graphQlType.Kind == GraphQlGenerator.GraphQlTypeKindNonNull ? graphQlType.OfType : graphQlType;
    }
}