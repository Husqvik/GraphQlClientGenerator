using System;
using System.Collections.Generic;

namespace GraphQlClientGenerator
{
    public delegate string GetCustomScalarFieldTypeDelegate(GraphQlType baseType, GraphQlTypeBase valueType, string valueName);

    public static class GraphQlGeneratorConfiguration
    {
        public static CSharpVersion CSharpVersion { get; set; }

        public static string ClassPostfix { get; set; }

        public static IDictionary<string, string> CustomClassNameMapping { get; } = new Dictionary<string, string>();

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
            CustomClassNameMapping.Clear();
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
            if (valueName == "From" || valueName == "ValidFrom" || valueName == "To" || valueName == "ValidTo" ||
                valueName == "CreatedAt" || valueName == "UpdatedAt" || valueName == "ModifiedAt" || valueName == "DeletedAt" ||
                valueName.EndsWith("Timestamp"))
                return "DateTimeOffset?";

            var dataType = valueType.Name == GraphQlTypeBase.GraphQlTypeScalarString ? "string" : "object";
            return GraphQlGenerator.AddQuestionMarkIfNullableReferencesEnabled(dataType);
        }
    }

    public enum CSharpVersion
    {
        Compatible,
        Newest,
        NewestWithNullableReferences
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
        Object,
        Custom
    }

    [Flags]
    public enum CommentGenerationOption
    {
        Disabled = 0,
        CodeSummary = 1,
        DescriptionAttribute = 2
    }
}