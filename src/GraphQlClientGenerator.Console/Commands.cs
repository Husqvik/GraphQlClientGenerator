using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;

namespace GraphQlClientGenerator.Console;

internal static class Commands
{
    public static readonly RootCommand GenerateCommand = SetupRootCommand();

    private static RootCommand SetupRootCommand()
    {
        var serviceUrlOption = new Option<string>("--serviceUrl", "-u") { Description = "GraphQL service URL used for retrieving schema metadata" };
        var schemaFileOption = new Option<string>("--schemaFileName", "-s") { Description = "Path to schema metadata file in JSON format" };

        var regexScalarFieldTypeMappingConfigurationOption =
            new Option<string>("--regexScalarFieldTypeMappingConfigurationFile")
            {
                Description = $"File name specifying rules for \"{nameof(RegexScalarFieldTypeMappingProvider)}\""
            };

        var command =
            new RootCommand("A tool for generating C# GraphQL query builders and data classes")
            {
                TreatUnmatchedTokensAsErrors = true,
                Options =
                {
                    new Option<string>("--outputPath", "-o")
                    {
                        Description = "Output path; include file name for single file output type; folder name for one class per file output type",
                        Required = true
                    },
                    new Option<string>("--namespace", "-n")
                    {
                        Description = "Root namespace all classes and other members are generated into",
                        Required = true
                    },
                    serviceUrlOption,
                    schemaFileOption,
                    new Option<string>("--httpMethod")
                    {
                        Description = "GraphQL schema metadata retrieval HTTP method",
                        DefaultValueFactory = _ => "POST",
                    },
                    new Option<string[]>("--header")
                    {
                        Description = "Format: {Header}:{Value}; allows to enter custom headers required to fetch GraphQL metadata",
                        Validators =
                        {
                            r => r.AddErrorIfSpecified(
                                !KeyValueParameterParser.TryGetCustomHeaders(r.Tokens.Select(t => t.Value), out _, out var errorMessage)
                                    ? errorMessage
                                    : null)
                        }
                    },
                    new Option<string>("--classPrefix")
                    {
                        Description = "Class prefix; value \"Test\" extends class name to \"TestTypeName\""
                    },
                    new Option<string>("--classSuffix")
                    {
                        Description = "Class suffix, for instance for version control; value \"V2\" extends class name to \"TypeNameV2\""
                    },
                    new Option<string[]>("--classMapping")
                    {
                        Description = "Format: {GraphQlTypeName}:{C#ClassName}; allows to define custom class names for specific GraphQL types",
                        Validators =
                        {
                            r => r.AddErrorIfSpecified(
                                !KeyValueParameterParser.TryGetCustomClassMapping(r.Tokens.Select(t => t.Value), out _, out var errorMessage)
                                    ? errorMessage
                                    : null)
                        }
                    },
                    new Option<CSharpVersion>("--csharpVersion")
                    {
                        Description = "C# version compatibility",
                        DefaultValueFactory = _ => CSharpVersion.Compatible,
                    },
                    new Option<CodeDocumentationType>("--codeDocumentationType")
                    {
                        Description = "Specifies code documentation generation option",
                        DefaultValueFactory = _ => CodeDocumentationType.Disabled,
                    },
                    new Option<MemberAccessibility>("--memberAccessibility")
                    {
                        Description = "Class and interface access level",
                        DefaultValueFactory = _ => MemberAccessibility.Public,
                    },
                    new Option<OutputType>("--outputType")
                    {
                        Description = "Specifies generated classes organization",
                        DefaultValueFactory = _ => OutputType.SingleFile
                    },
                    new Option<bool>("--partialClasses")
                    {
                        Description = "Mark classes as \"partial\"",
                        DefaultValueFactory = _ => false
                    },
                    new Option<BooleanTypeMapping>("--booleanTypeMapping")
                    {
                        Description = "Specifies the .NET type generated for GraphQL built-in Boolean data type",
                        DefaultValueFactory = _ => BooleanTypeMapping.Boolean
                    },
                    new Option<FloatTypeMapping>("--floatTypeMapping")
                    {
                        Description = "Specifies the .NET type generated for GraphQL built-in Float data type",
                        DefaultValueFactory = _ => FloatTypeMapping.Decimal
                    },
                    new Option<IdTypeMapping>("--idTypeMapping")
                    {
                        Description = "Specifies the .NET type generated for GraphQL built-in ID data type",
                        DefaultValueFactory = _ => IdTypeMapping.Guid
                    },
                    new Option<IntegerTypeMapping>("--integerTypeMapping")
                    {
                        Description = "Specifies the .NET type generated for GraphQL built-in Integer data type",
                        DefaultValueFactory = _ => IntegerTypeMapping.Int32
                    },
                    new Option<JsonPropertyGenerationOption>("--jsonPropertyAttribute")
                    {
                        Description = "Specifies the condition for using \"JsonPropertyAttribute\"",
                        DefaultValueFactory = _ => JsonPropertyGenerationOption.CaseInsensitive
                    },
                    new Option<EnumValueNamingOption>("--enumValueNaming")
                    {
                        Description = "Use \"Original\" to avoid pretty C# name conversion for maximum deserialization compatibility",
                        DefaultValueFactory = _ => EnumValueNamingOption.CSharp
                    },
                    new Option<DataClassMemberNullability>("--dataClassMemberNullability")
                    {
                        Description = "Specifies whether data class scalar properties generated always nullable (for better type reuse) or respect the GraphQL schema",
                        DefaultValueFactory = _ => DataClassMemberNullability.AlwaysNullable
                    },
                    new Option<GenerationOrder>("--generationOrder")
                    {
                        Description = "Specifies whether order of generated C# classes/enums respect the GraphQL schema or is enforced to alphabetical for easier change tracking",
                        DefaultValueFactory = _ => GenerationOrder.DefinedBySchema
                    },
                    new Option<InputObjectMode>("--inputObjectMode")
                    {
                        Description = "Specifies whether input objects are generated as POCOs or they have support of GraphQL parameter references and explicit null values",
                        DefaultValueFactory = _ => InputObjectMode.Rich
                    },
                    new Option<bool>("--includeDeprecatedFields")
                    {
                        Description = "Includes deprecated fields in generated query builders and data classes",
                        DefaultValueFactory = _ => false
                    },
                    new Option<bool>("--nullableReferences")
                    {
                        Description = "Enables nullable references",
                        DefaultValueFactory = _ => false
                    },
                    new Option<bool>("--fileScopedNamespaces")
                    {
                        Description = "Specifies whether file-scoped namespaces should be used in generated files (C# 10 or later)",
                        DefaultValueFactory = _ => false
                    },
                    new Option<bool>("--ignoreServiceUrlCertificateErrors")
                    {
                        Description = "Ignores HTTPS errors when retrieving GraphQL metadata from an URL; typically when using self signed certificates",
                        DefaultValueFactory = _ => false
                    },
                    regexScalarFieldTypeMappingConfigurationOption
                },
                Validators =
                {
                    r =>
                        r.AddErrorIfSpecified(
                            (ServiceUrl: r.GetResult(serviceUrlOption), SchemaFile: r.GetResult(schemaFileOption)) switch
                            {
                                { ServiceUrl: not null, SchemaFile: not null } => "\"serviceUrl\" and \"schemaFileName\" parameters are mutually exclusive. ",
                                { ServiceUrl: null, SchemaFile: null } => "Either \"serviceUrl\" or \"schemaFileName\" parameter must be specified. ",
                                _ => null
                            }),
                    r =>
                    {
                        var regexScalarFieldTypeMappingConfigurationFileName = r.GetValue(regexScalarFieldTypeMappingConfigurationOption);
                        if (String.IsNullOrEmpty(regexScalarFieldTypeMappingConfigurationFileName))
                            return;

                        try
                        {
                            RegexScalarFieldTypeMappingProvider.ParseRulesFromJson(File.ReadAllText(regexScalarFieldTypeMappingConfigurationFileName));
                        }
                        catch (Exception exception)
                        {
                            r.AddError(exception.Message);
                        }
                    }
                }
            };

        command.SetAction(async (result, cancellationToken) =>
        {
            var bindingContext = CommandHandler.Create<ProgramOptions>(delegate { }).GetBindingContext(result);
            var options = (ProgramOptions)new ModelBinder<ProgramOptions>().CreateInstance(bindingContext);
            await GraphQlCSharpFileHelper.GenerateClientSourceCode(result.Configuration, options, cancellationToken);
            return 0;
        });

        return command;
    }

    private static void AddErrorIfSpecified(this SymbolResult result, string errorMessage)
    {
        if (!String.IsNullOrEmpty(errorMessage))
            result.AddError(errorMessage);
    }
}