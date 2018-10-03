namespace GraphQlClientGenerator
{
    public delegate string GetCustomScalarFieldTypeDelegate(GraphQlType baseType, string valueName);

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

        public static string DefaultScalarFieldTypeMapping(GraphQlType baseType, string valueName)
        {
            var propertyName = NamingHelper.CapitalizeFirst(valueName);
            if (propertyName == "From" || propertyName == "ValidFrom" || propertyName == "CreatedAt" ||
                propertyName == "To" || propertyName == "ValidTo" || propertyName == "ModifiedAt" || propertyName.EndsWith("Timestamp"))
                return "DateTimeOffset?";

            return "string";
        }
    }

    public enum CSharpVersion
    {
        Compatible,
        Newest
    }
}