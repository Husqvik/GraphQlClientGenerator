using System.Text;

namespace GraphQlClientGenerator;

public delegate string GetDataPropertyAccessorBodiesDelegate(string backingFieldName, ScalarFieldTypeDescription backingFieldType);

public class GraphQlGeneratorConfiguration
{
    public CSharpVersion CSharpVersion { get; set; }

    public string ClassPrefix { get; set; }

    public string ClassSuffix { get; set; }

    /// <summary>
    /// Allows to define custom class names for specific GraphQL types. One common reason for this is to avoid property of the same name as its parent class.
    /// </summary>
    public IDictionary<string, string> CustomClassNameMapping { get; } = new Dictionary<string, string>();

    public CommentGenerationOption CommentGeneration { get; set; }

    public bool IncludeDeprecatedFields { get; set; }

    public bool GeneratePartialClasses { get; set; } = true;

    /// <summary>
    /// Determines whether unknown type scalar fields will be automatically requested when <code>WithAllScalarFields</code> issued.
    /// </summary>
    public bool TreatUnknownObjectAsScalar { get; set; }

    /// <summary>
    /// Determines the .NET type generated for GraphQL Integer data type.
    /// </summary>
    /// <remarks>For using custom .NET data type <code>Custom</code> option must be used. </remarks>
    public IntegerTypeMapping IntegerTypeMapping { get; set; } = IntegerTypeMapping.Int32;

    /// <summary>
    /// Determines the .NET type generated for GraphQL Float data type.
    /// </summary>
    /// <remarks>For using custom .NET data type <code>Custom</code> option must be used. </remarks>
    public FloatTypeMapping FloatTypeMapping { get; set; }

    /// <summary>
    /// Determines the .NET type generated for GraphQL Boolean data type.
    /// </summary>
    /// <remarks>For using custom .NET data type <code>Custom</code> option must be used. </remarks>
    public BooleanTypeMapping BooleanTypeMapping { get; set; }

    /// <summary>
    /// Determines the .NET type generated for GraphQL ID data type.
    /// </summary>
    /// <remarks>For using custom .NET data type <code>Custom</code> option must be used. </remarks>
    public IdTypeMapping IdTypeMapping { get; set; } = IdTypeMapping.Guid;
        
    public PropertyGenerationOption PropertyGeneration { get; set; } = PropertyGenerationOption.AutoProperty;

    public JsonPropertyGenerationOption JsonPropertyGeneration { get; set; } = JsonPropertyGenerationOption.CaseInsensitive;

    public EnumValueNamingOption EnumValueNaming { get; set; }

    /// <summary>
    /// Determines builder class, data class and interfaces accessibility level.
    /// </summary>
    public MemberAccessibility MemberAccessibility { get; set; }

    /// <summary>
    /// This property is used for mapping GraphQL scalar type into specific .NET type. By default any custom GraphQL scalar type is mapped into <see cref="System.Object"/>.
    /// </summary>
    public IScalarFieldTypeMappingProvider ScalarFieldTypeMappingProvider { get; set; }

    /// <summary>
    /// Used for custom data property accessor bodies generation; applicable only when <code>PropertyGeneration = PropertyGenerationOption.BackingField</code>.
    /// </summary>
    public GetDataPropertyAccessorBodiesDelegate PropertyAccessorBodyWriter { get; set; }

    public bool FileScopedNamespaces { get; set; }

    public GraphQlGeneratorConfiguration() => Reset();

    public void Reset()
    {
        ClassPrefix = null;
        ClassSuffix = null;
        CustomClassNameMapping.Clear();
        CSharpVersion = CSharpVersion.Compatible;
        ScalarFieldTypeMappingProvider = DefaultScalarFieldTypeMappingProvider.Instance;
        PropertyAccessorBodyWriter = GeneratePropertyAccessors;
        CommentGeneration = CommentGenerationOption.Disabled;
        IncludeDeprecatedFields = false;
        FloatTypeMapping = FloatTypeMapping.Decimal;
        BooleanTypeMapping = BooleanTypeMapping.Boolean;
        IntegerTypeMapping = IntegerTypeMapping.Int32;
        IdTypeMapping = IdTypeMapping.Guid;
        TreatUnknownObjectAsScalar = false;
        GeneratePartialClasses = true;
        MemberAccessibility = MemberAccessibility.Public;
        JsonPropertyGeneration = JsonPropertyGenerationOption.CaseInsensitive;
        PropertyGeneration = PropertyGenerationOption.AutoProperty;
        EnumValueNaming = EnumValueNamingOption.CSharp;
        FileScopedNamespaces = false;
    }

    public string GeneratePropertyAccessors(string backingFieldName, ScalarFieldTypeDescription backingFieldType)
    {
        var useCompatibleVersion = CSharpVersion == CSharpVersion.Compatible;
        var builder = new StringBuilder();
        builder.Append(" { get");
        builder.Append(useCompatibleVersion ? " { return " : " => ");
        builder.Append(backingFieldName);
        builder.Append(";");

        if (useCompatibleVersion)
            builder.Append(" }");

        builder.Append(" set");
        builder.Append(useCompatibleVersion ? " { " : " => ");
        builder.Append(backingFieldName);
        builder.Append(" = value;");

        if (useCompatibleVersion)
            builder.Append(" }");

        builder.Append(" }");

        return builder.ToString();
    }
}

public enum EnumValueNamingOption
{
    CSharp,
    Original
}

public enum CSharpVersion
{
    Compatible,
    Newest,
    NewestWithNullableReferences
}

public enum FloatTypeMapping
{
    Decimal,
    Float,
    Double,
    Custom
}

public enum BooleanTypeMapping
{
    Boolean,
    Custom
}

public enum IntegerTypeMapping
{
    Int16,
    Int32,
    Int64,
    Custom
}

public enum IdTypeMapping
{
    Guid,
    String,
    Object,
    Custom
}

public enum MemberAccessibility
{
    Public,
    Internal
}

public enum JsonPropertyGenerationOption
{
    Never,
    Always,
    UseDefaultAlias,
    CaseInsensitive,
    CaseSensitive
}

public enum PropertyGenerationOption
{
    AutoProperty,
    BackingField
}

[Flags]
public enum CommentGenerationOption
{
    Disabled = 0,
    CodeSummary = 1,
    DescriptionAttribute = 2
}