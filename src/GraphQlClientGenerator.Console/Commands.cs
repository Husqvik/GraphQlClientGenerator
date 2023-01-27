using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace GraphQlClientGenerator.Console;

internal static class Commands
{
    public static readonly RootCommand GenerateCommand = SetupGenerateCommand();

    private static RootCommand SetupGenerateCommand()
    {
        var serviceUrlOption = new Option<string>(new[] { "--serviceUrl", "-u" }, "GraphQL service URL used for retrieving schema metadata");
        var schemaFileOption = new Option<string>(new[] { "--schemaFileName", "-s" }, "Path to schema metadata file in JSON format");

        var classMappingOption =
            new Option<string[]>(
                "--classMapping",
                "Format: {GraphQlTypeName}:{C#ClassName}; allows to define custom class names for specific GraphQL types; common reason for this is to avoid property of the same name as its parent class");

        classMappingOption.AddValidator(
            static option =>
                option.ErrorMessage =
                    KeyValueParameterParser.TryGetCustomClassMapping(option.Tokens.Select(t => t.Value), out _, out var errorMessage)
                        ? null
                        : errorMessage);

        var headerOption = new Option<string[]>("--header", "Format: {Header}:{Value}; allows to enter custom headers required to fetch GraphQL metadata");
        headerOption.AddValidator(
            static option =>
                option.ErrorMessage =
                    KeyValueParameterParser.TryGetCustomHeaders(option.Tokens.Select(t => t.Value), out _, out var errorMessage)
                        ? null
                        : errorMessage);

        var regexScalarFieldTypeMappingConfigurationOption =
            new Option<string>("--regexScalarFieldTypeMappingConfigurationFile", $"File name specifying rules for \"{nameof(RegexScalarFieldTypeMappingProvider)}\"");

        var command =
            new RootCommand
            {
                new Option<string>(new[] { "--outputPath", "-o" }, "Output path; include file name for single file output type; folder name for one class per file output type") { IsRequired = true },
                new Option<string>(new[] { "--namespace", "-n" }, "Root namespace all classes and other members are generated to") { IsRequired = true },
                serviceUrlOption,
                schemaFileOption,
                new Option<string>("--httpMethod", () => "POST", "GraphQL schema metadata retrieval HTTP method"),
                headerOption,
                new Option<string>("--classPrefix", "Class prefix; value \"Test\" extends class name to \"TestTypeName\""),
                new Option<string>("--classSuffix", "Class suffix, for instance for version control; value \"V2\" extends class name to \"TypeNameV2\""),
                new Option<CSharpVersion>("--csharpVersion", () => CSharpVersion.Compatible, "C# version compatibility"),
                new Option<MemberAccessibility>("--memberAccessibility", () => MemberAccessibility.Public, "Class and interface access level"),
                new Option<OutputType>("--outputType", () => OutputType.SingleFile, "Specifies generated classes organization"),
                new Option<bool>("--partialClasses", () => false, "Mark classes as \"partial\""),
                classMappingOption,
                new Option<IdTypeMapping>("--idTypeMapping", () => IdTypeMapping.Guid, "Specifies the .NET type generated for GraphQL ID data type"),
                new Option<FloatTypeMapping>("--floatTypeMapping", () => FloatTypeMapping.Decimal, "Specifies the .NET type generated for GraphQL Float data type"),
                new Option<IntegerTypeMapping>("--integerTypeMapping", () => IntegerTypeMapping.Int32, "Specifies the .NET type generated for GraphQL Integer data type"),
                new Option<BooleanTypeMapping>("--booleanTypeMapping", () => BooleanTypeMapping.Boolean, "Specifies the .NET type generated for GraphQL Boolean data type"),
                new Option<JsonPropertyGenerationOption>("--jsonPropertyAttribute", () => JsonPropertyGenerationOption.CaseInsensitive, "Specifies the condition for using \"JsonPropertyAttribute\""),
                new Option<EnumValueNamingOption>("--enumValueNaming", "Use \"Original\" to avoid pretty C# name conversion for maximum deserialization compatibility"),
                new Option<bool>("--includeDeprecatedFields", () => false, "Generate deprecated fields"),
                new Option<bool>("--fileScopedNamespaces", () => false, "Specifies if file-scoped namespaces should be used in generated files (C# 10+)"),
                regexScalarFieldTypeMappingConfigurationOption
            };

        command.TreatUnmatchedTokensAsErrors = true;
        command.Name = "GraphQlClientGenerator.Console";
        command.Description = "A tool for generating strongly typed GraphQL query builders and data classes";
        command.Handler = CommandHandler.Create<IConsole, ProgramOptions>(GraphQlCSharpFileHelper.GenerateGraphQlClientSourceCode);
        command.AddValidator(
            option =>
                option.ErrorMessage =
                    option.FindResultFor(serviceUrlOption) is not null && option.FindResultFor(schemaFileOption) is not null
                        ? "\"serviceUrl\" and \"schemaFileName\" parameters are mutually exclusive. "
                        : null);

        command.AddValidator(
            option =>
                option.ErrorMessage =
                    option.FindResultFor(serviceUrlOption) is null && option.FindResultFor(schemaFileOption) is null
                        ? "Either \"serviceUrl\" or \"schemaFileName\" parameter must be specified. "
                        : null);

        command.AddValidator(
            option =>
            {
                var result = option.FindResultFor(regexScalarFieldTypeMappingConfigurationOption);
                if (result is null)
                    return;

                try
                {
                    RegexScalarFieldTypeMappingProvider.ParseRulesFromJson(File.ReadAllText(result.GetValueOrDefault<string>()));
                }
                catch (Exception exception)
                {
                    option.ErrorMessage = exception.Message;
                }
            });

        return command;
    }
}