using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;

namespace GraphQlClientGenerator.Console;

internal static class Commands
{
    public static readonly Parser Parser =
        new CommandLineBuilder(SetupGenerateCommand())
            .UseDefaults()
            .UseExceptionHandler((exception, invocationContext) =>
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                invocationContext.Console.Error.WriteLine($"An error occurred: {exception}");
                System.Console.ResetColor();
                invocationContext.ExitCode = 2;
            })
            .Build();

    private static RootCommand SetupGenerateCommand()
    {
        var serviceUrlOption = new Option<string>(["--serviceUrl", "-u"], "GraphQL service URL used for retrieving schema metadata");
        var schemaFileOption = new Option<string>(["--schemaFileName", "-s"], "Path to schema metadata file in JSON format");

        var classMappingOption =
            new Option<string[]>(
                "--classMapping",
                "Format: {GraphQlTypeName}:{C#ClassName}; allows to define custom class names for specific GraphQL types");

        classMappingOption.AddValidator(
            option =>
                option.ErrorMessage =
                    KeyValueParameterParser.TryGetCustomClassMapping(option.Tokens.Select(t => t.Value), out _, out var errorMessage)
                        ? null
                        : errorMessage);

        var headerOption = new Option<string[]>("--header", "Format: {Header}:{Value}; allows to enter custom headers required to fetch GraphQL metadata");
        headerOption.AddValidator(
            option =>
                option.ErrorMessage =
                    KeyValueParameterParser.TryGetCustomHeaders(option.Tokens.Select(t => t.Value), out _, out var errorMessage)
                        ? null
                        : errorMessage);

        var regexScalarFieldTypeMappingConfigurationOption =
            new Option<string>("--regexScalarFieldTypeMappingConfigurationFile", $"File name specifying rules for \"{nameof(RegexScalarFieldTypeMappingProvider)}\"");

        var command =
            new RootCommand
            {
                new Option<string>(["--outputPath", "-o"], "Output path; include file name for single file output type; folder name for one class per file output type") { IsRequired = true },
                new Option<string>(["--namespace", "-n"], "Root namespace all classes and other members are generated into") { IsRequired = true },
                serviceUrlOption,
                schemaFileOption,
                new Option<string>("--httpMethod", () => "POST", "GraphQL schema metadata retrieval HTTP method"),
                headerOption,
                new Option<string>("--classPrefix", "Class prefix; value \"Test\" extends class name to \"TestTypeName\""),
                new Option<string>("--classSuffix", "Class suffix, for instance for version control; value \"V2\" extends class name to \"TypeNameV2\""),
                new Option<CSharpVersion>("--csharpVersion", () => CSharpVersion.Compatible, "C# version compatibility"),
                new Option<CodeDocumentationType>("--codeDocumentationType", () => CodeDocumentationType.Disabled, "Specifies code documentation generation option"),
                new Option<MemberAccessibility>("--memberAccessibility", () => MemberAccessibility.Public, "Class and interface access level"),
                new Option<OutputType>("--outputType", () => OutputType.SingleFile, "Specifies generated classes organization"),
                new Option<bool>("--partialClasses", () => false, "Mark classes as \"partial\""),
                classMappingOption,
                new Option<BooleanTypeMapping>("--booleanTypeMapping", () => BooleanTypeMapping.Boolean, "Specifies the .NET type generated for GraphQL built-in Boolean data type"),
                new Option<FloatTypeMapping>("--floatTypeMapping", () => FloatTypeMapping.Decimal, "Specifies the .NET type generated for GraphQL built-in Float data type"),
                new Option<IdTypeMapping>("--idTypeMapping", () => IdTypeMapping.Guid, "Specifies the .NET type generated for GraphQL built-in ID data type"),
                new Option<IntegerTypeMapping>("--integerTypeMapping", () => IntegerTypeMapping.Int32, "Specifies the .NET type generated for GraphQL built-in Integer data type"),
                new Option<JsonPropertyGenerationOption>("--jsonPropertyAttribute", () => JsonPropertyGenerationOption.CaseInsensitive, "Specifies the condition for using \"JsonPropertyAttribute\""),
                new Option<EnumValueNamingOption>("--enumValueNaming", () => EnumValueNamingOption.CSharp, "Use \"Original\" to avoid pretty C# name conversion for maximum deserialization compatibility"),
                new Option<DataClassMemberNullability>("--dataClassMemberNullability", () => DataClassMemberNullability.AlwaysNullable, "Specifies whether data class scalar properties generated always nullable (for better type reuse) or respect the GraphQL schema"),
                new Option<GenerationOrder>("--generationOrder", () => GenerationOrder.DefinedBySchema, "Specifies whether order of generated C# classes/enums respect the GraphQL schema or is enforced to alphabetical for easier change tracking"),
                new Option<InputObjectMode>("--inputObjectMode", () => InputObjectMode.Rich, "Specifies whether input objects are generated as POCOs or they have support of GraphQL parameter references and explicit null values"),
                new Option<bool>("--includeDeprecatedFields", () => false, "Includes deprecated fields in generated query builders and data classes"),
                new Option<bool>("--nullableReferences", () => false, "Enables nullable references"),
                new Option<bool>("--fileScopedNamespaces", () => false, "Specifies whether file-scoped namespaces should be used in generated files (C# 10+)"),
                new Option<bool>("--ignoreServiceUrlCertificateErrors", () => false, "Ignores HTTPS errors when retrieving GraphQL metadata from an URL; typically when using self signed certificates"),
                regexScalarFieldTypeMappingConfigurationOption
            };

        command.TreatUnmatchedTokensAsErrors = true;
        command.Name = "GraphQlClientGenerator.Console";
        command.Description = "A tool for generating C# GraphQL query builders and data classes";
        command.Handler = CommandHandler.Create<IConsole, ProgramOptions>(GraphQlCSharpFileHelper.GenerateClientSourceCode);
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
                var regexScalarFieldTypeMappingConfigurationFileName = option.FindResultFor(regexScalarFieldTypeMappingConfigurationOption)?.GetValueOrDefault<string>();
                if (regexScalarFieldTypeMappingConfigurationFileName is null)
                    return;

                try
                {
                    RegexScalarFieldTypeMappingProvider.ParseRulesFromJson(File.ReadAllText(regexScalarFieldTypeMappingConfigurationFileName));
                }
                catch (Exception exception)
                {
                    option.ErrorMessage = exception.Message;
                }
            });

        return command;
    }
}