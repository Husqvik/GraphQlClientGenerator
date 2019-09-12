using System;

namespace GraphQlClientGenerator
{
    public delegate string GetCustomScalarFieldTypeDelegate(GraphQlType baseType, GraphQlTypeBase valueType, string valueName);

    public static class GraphQlGeneratorConfiguration
    {
        public static CSharpVersion CSharpVersion { get; set; }

        public static string ClassPostfix { get; set; }

        public static CommentGenerationOption CommentGeneration { get; set; }

        public static bool IncludeDeprecatedFields { get; set; }

        public static bool GeneratePartialClasses { get; set; } = true;

        /// <summary>
        /// Determines whether unknown type scalar fields will be automatically requested when <code>WithAllScalarFields</code> issued.
        /// </summary>
        public static bool TreatUnknownObjectAsScalar { get; set; }

        public static IntegerType IntegerType { get; set; } = IntegerType.Int32;

        public static FloatType FloatType { get; set; }

        public static IdType IdType { get; set; } = IdType.Guid;

        /// <summary>
        /// This property is used for mapping GraphQL scalar type into specific .NET type. By default any custom GraphQL scalar type is mapped into <see cref="System.Object"/>.
        /// </summary>
        public static GetCustomScalarFieldTypeDelegate CustomScalarFieldTypeMapping { get; set; } = DefaultScalarFieldTypeMapping;

        public static void Reset()
        {
            ClassPostfix = null;
            CSharpVersion = CSharpVersion.Compatible;
            CustomScalarFieldTypeMapping = DefaultScalarFieldTypeMapping;
            CommentGeneration = CommentGenerationOption.Disabled;
            IncludeDeprecatedFields = false;
            FloatType = FloatType.Decimal;
            IntegerType = IntegerType.Int32;
            IdType = IdType.Guid;
            TreatUnknownObjectAsScalar = false;
            GeneratePartialClasses = true;
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

    public enum FloatType
    {
        Decimal,
        Float,
        Double
    }

    public enum IntegerType
    {
        Int16,
        Int32,
        Int64
    }

    public enum IdType
    {
        String,
        Guid,
        Object
    }

    [Flags]
    public enum CommentGenerationOption
    {
        Disabled = 0,
        CodeSummary = 1,
        DescriptionAttribute = 2
    }
}