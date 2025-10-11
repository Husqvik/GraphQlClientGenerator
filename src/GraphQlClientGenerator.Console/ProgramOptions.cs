namespace GraphQlClientGenerator.Console;

public class ProgramOptions
{
    public required string OutputPath { get; init; }
    public required bool IgnoreServiceUrlCertificateErrors { get; init; }
    public required string ServiceUrl { get; init; }
    public required FileInfo SchemaFile { get; init; }
    public required string HttpMethod { get; init; }
    public required ICollection<string> Header { get; init; }
    public required OutputType OutputType { get; init; }
    public required GraphQlGeneratorConfiguration GeneratorConfiguration { get; init; }
}