namespace GraphQlClientGenerator;

public class GraphQlGeneratorConfiguration
{
    private string _targetNamespace = "GraphQlApi";

    public CSharpVersion CSharpVersion { get; set; }

    public string ClassPrefix { get; set; }

    public string ClassSuffix { get; set; }

    public string TargetNamespace

    {
        get => _targetNamespace;
        set
        {
            if (!CSharpHelper.IsValidNamespace(value))
                throw new ArgumentException($"namespace \"{value}\" is not valid. ");

            _targetNamespace = value;
        }
    }

    /// <summary>
    /// Allows to define custom class names for specific GraphQL types.
    /// </summary>
    public IDictionary<string, string> CustomClassNameMapping { get; } = new Dictionary<string, string>();

    public CodeDocumentationType CodeDocumentationType { get; set; }

    public bool IncludeDeprecatedFields { get; set; }

    public bool EnableNullableReferences { get; set; }

    public bool GeneratePartialClasses { get; set; } = true;

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
    /// This property is used for mapping GraphQL scalar type into specific .NET type. By default, any custom GraphQL scalar type is mapped into <see cref="object"/>.
    /// </summary>
    public IScalarFieldTypeMappingProvider ScalarFieldTypeMappingProvider { get; set; }

    public bool FileScopedNamespaces { get; set; }

    public DataClassMemberNullability DataClassMemberNullability { get; set; }

    public GenerationOrder GenerationOrder { get; set; }

    public GraphQlGeneratorConfiguration() => Reset();

    public void Reset()
    {
        ClassPrefix = null;
        ClassSuffix = null;
        CustomClassNameMapping.Clear();
        CSharpVersion = CSharpVersion.Compatible;
        ScalarFieldTypeMappingProvider = DefaultScalarFieldTypeMappingProvider.Instance;
        CodeDocumentationType = CodeDocumentationType.Disabled;
        IncludeDeprecatedFields = false;
        EnableNullableReferences = false;
        FloatTypeMapping = FloatTypeMapping.Decimal;
        BooleanTypeMapping = BooleanTypeMapping.Boolean;
        IntegerTypeMapping = IntegerTypeMapping.Int32;
        IdTypeMapping = IdTypeMapping.Guid;
        GeneratePartialClasses = true;
        MemberAccessibility = MemberAccessibility.Public;
        JsonPropertyGeneration = JsonPropertyGenerationOption.CaseInsensitive;
        PropertyGeneration = PropertyGenerationOption.AutoProperty;
        EnumValueNaming = EnumValueNamingOption.CSharp;
        FileScopedNamespaces = false;
        DataClassMemberNullability = DataClassMemberNullability.AlwaysNullable;
        GenerationOrder = GenerationOrder.DefinedBySchema;
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
    CSharp6,
    CSharp12
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
public enum CodeDocumentationType
{
    Disabled = 0,
    XmlSummary = 1,
    DescriptionAttribute = 2
}

public enum DataClassMemberNullability
{
    AlwaysNullable,
    DefinedBySchema
}

public enum GenerationOrder
{
    DefinedBySchema,
    Alphabetical
}