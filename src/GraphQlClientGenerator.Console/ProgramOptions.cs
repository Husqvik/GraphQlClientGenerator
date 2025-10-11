namespace GraphQlClientGenerator.Console;

public class ProgramOptions
{
    public required string OutputPath { get; init; }
    public required string Namespace { get; init; }
    public required bool FileScopedNamespaces { get; init; }
    public required bool IgnoreServiceUrlCertificateErrors { get; init; }
    public required string ServiceUrl { get; init; }
    public required FileInfo SchemaFile { get; init; }
    public required string HttpMethod { get; init; }
    public required ICollection<string> Header { get; init; }
    public required string ClassPrefix { get; init; }
    public required string ClassSuffix { get; init; }
    public required CSharpVersion CSharpVersion { get; init; }
    public required CodeDocumentationType CodeDocumentationType { get; init; }
    public required MemberAccessibility MemberAccessibility { get; init; }
    public required OutputType OutputType { get; init; }
    public required bool PartialClasses { get; init; }
    public required ICollection<string> ClassMapping { get; init; }
    public required string RegexScalarFieldTypeMappingConfigurationFile { get; init; }
    public required IdTypeMapping IdTypeMapping { get; init; }
    public required FloatTypeMapping FloatTypeMapping { get; init; }
    public required IntegerTypeMapping IntegerTypeMapping { get; init; }
    public required BooleanTypeMapping BooleanTypeMapping { get; init; }
    public required JsonPropertyGenerationOption JsonPropertyAttribute { get; init; }
    public required EnumValueNamingOption EnumValueNaming { get; init; }
    public required DataClassMemberNullability DataClassMemberNullability { get; init; }
    public required GenerationOrder GenerationOrder { get; init; }
    public required InputObjectMode InputObjectMode { get; init; }
    public required bool IncludeDeprecatedFields { get; init; }
    public required bool NullableReferences { get; init; }
}