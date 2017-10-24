using System;

namespace GraphQlClientGenerator
{
    public static class GraphQlGeneratorConfiguration
    {
        public static CSharpVersion CSharpVersion { get; set; }

        public static string ClassPostfix { get; set; }

        public static Func<GraphQlField, string> CustomScalarFieldMapping { get; set; } = DefaultScalarFieldMapping;

        public static void Reset()
        {
            ClassPostfix = null;
            CSharpVersion = CSharpVersion.Compatible;
            CustomScalarFieldMapping = DefaultScalarFieldMapping;
        }

        private static string DefaultScalarFieldMapping(GraphQlField field)
        {
            var propertyName = NamingHelper.CapitalizeFirst(field.Name);
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