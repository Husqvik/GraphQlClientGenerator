namespace GraphQlClientGenerator
{
    public delegate string GetCustomScalarFieldTypeDelegate(GraphQlType baseType, GraphQlTypeBase valueType, string valueName);

    public static class GraphQlGeneratorConfiguration
    {
        public static CSharpVersion CSharpVersion { get; set; }

        public static string ClassPostfix { get; set; }

        public static bool GenerateComments { get; set; }

        public static bool IncludeDeprecatedFields { get; set; }

        public static GetCustomScalarFieldTypeDelegate CustomScalarFieldTypeMapping { get; set; } = DefaultScalarFieldTypeMapping;

        public static void Reset()
        {
            ClassPostfix = null;
            CSharpVersion = CSharpVersion.Compatible;
            CustomScalarFieldTypeMapping = DefaultScalarFieldTypeMapping;
            GenerateComments = false;
            IncludeDeprecatedFields = false;
        }

        public static string DefaultScalarFieldTypeMapping(GraphQlType baseType, GraphQlTypeBase valueType, string valueName)
        {
            valueName = NamingHelper.ToPascalCase(valueName);
            if (valueName == "From" || valueName == "ValidFrom" || valueName == "CreatedAt" ||
                valueName == "To" || valueName == "ValidTo" || valueName == "ModifiedAt" || valueName.EndsWith("Timestamp"))
                return "DateTimeOffset?";

            return valueType.Name == GraphQlTypeBase.GraphQlTypeScalarString ? "string" : "object";
        }
    }

    public enum CSharpVersion
    {
        Compatible,
        Newest
    }
}