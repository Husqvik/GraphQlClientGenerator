using System.CommandLine;
using System.CommandLine.Parsing;

namespace GraphQlClientGenerator.Console;

internal static class Commands
{
    public static RootCommand GenerateCommand()
    {
        var serviceUrlOption = new Option<string>("--serviceUrl", "-u") { Description = "GraphQL service URL used for retrieving schema metadata" };
        var schemaFileOption = new Option<FileInfo>("--schemaFileName", "-s") { Description = "Path to schema metadata file in JSON format" };

        var regexScalarFieldTypeMappingConfigurationOption =
            new Option<string>("--regexScalarFieldTypeMappingConfigurationFile")
            {
                Description = $"File name specifying rules for \"{nameof(RegexScalarFieldTypeMappingProvider)}\""
            };

        var outputPathOption =
            new Option<string>("--outputPath", "-o")
            {
                Description = "Output path; include file name for single file output type; folder name for one class per file output type",
                Required = true
            };

        var namespaceOption =
            new Option<string>("--namespace", "-n")
            {
                Description = "Root namespace all classes and other members are generated into",
                Required = true
            };

        var httpMethodOption =
            new Option<string>("--httpMethod")
            {
                Description = "GraphQL schema metadata retrieval HTTP method",
                DefaultValueFactory = _ => HttpMethod.Post.Method
            };

        var headerOption =
            new Option<string[]>("--header")
            {
                Description = "Format: {Header}:{Value}; allows to enter custom headers required to fetch GraphQL metadata",
                Validators = { result => result.AddKeyValueErrorIfFound(KeyValueParameterParser.TryGetCustomHeaders) }
            };

        var classPrefixOption = new Option<string>("--classPrefix") { Description = "Class prefix; value \"Test\" extends class name to \"TestTypeName\"" };
        var classSuffixOption = new Option<string>("--classSuffix") { Description = "Class suffix, for instance for version control; value \"V2\" extends class name to \"TypeNameV2\"" };

        var classMappingOption =
            new Option<string[]>("--classMapping")
            {
                Description = "Format: {GraphQlTypeName}:{C#ClassName}; allows to define custom class names for specific GraphQL types",
                Validators = { result => result.AddKeyValueErrorIfFound(KeyValueParameterParser.TryGetCustomClassMapping) }
            };

        var csharpVersionOption =
            new Option<CSharpVersion>("--csharpVersion")
            {
                Description = "C# version compatibility",
                DefaultValueFactory = _ => CSharpVersion.Compatible
            };

        var codeDocumentationOption =
            new Option<CodeDocumentationType>("--codeDocumentationType")
            {
                Description = "Specifies code documentation generation option",
                DefaultValueFactory = _ => CodeDocumentationType.Disabled
            };

        var memberAccessibilityOption =
            new Option<MemberAccessibility>("--memberAccessibility")
            {
                Description = "Class and interface access level",
                DefaultValueFactory = _ => MemberAccessibility.Public
            };

        var outputTypeOption =
            new Option<OutputType>("--outputType")
            {
                Description = "Specifies generated classes organization",
                DefaultValueFactory = _ => OutputType.SingleFile
            };

        var partialClassesOption = new Option<bool>("--partialClasses") { Description = "Mark classes as \"partial\"", DefaultValueFactory = _ => false };

        var booleanTypeMappingOption =
            new Option<BooleanTypeMapping>("--booleanTypeMapping")
            {
                Description = "Specifies the .NET type generated for GraphQL built-in Boolean data type",
                DefaultValueFactory = _ => BooleanTypeMapping.Boolean
            };

        var floatTypeMappingOption =
            new Option<FloatTypeMapping>("--floatTypeMapping")
            {
                Description = "Specifies the .NET type generated for GraphQL built-in Float data type",
                DefaultValueFactory = _ => FloatTypeMapping.Decimal
            };

        var idTypeMappingOption =
            new Option<IdTypeMapping>("--idTypeMapping")
            {
                Description = "Specifies the .NET type generated for GraphQL built-in ID data type",
                DefaultValueFactory = _ => IdTypeMapping.Guid
            };

        var integerTypeMappingOption =
            new Option<IntegerTypeMapping>("--integerTypeMapping")
            {
                Description = "Specifies the .NET type generated for GraphQL built-in Integer data type",
                DefaultValueFactory = _ => IntegerTypeMapping.Int32
            };

        var jsonPropertyAttributeOption =
            new Option<JsonPropertyGenerationOption>("--jsonPropertyAttribute")
            {
                Description = "Specifies the condition for using \"JsonPropertyAttribute\"",
                DefaultValueFactory = _ => JsonPropertyGenerationOption.CaseInsensitive
            };

        var enumValueMappingOption =
            new Option<EnumValueNamingOption>("--enumValueNaming")
            {
                Description = "Use \"Original\" to avoid pretty C# name conversion for maximum deserialization compatibility",
                DefaultValueFactory = _ => EnumValueNamingOption.CSharp
            };

        var dataClassMemberNullabilityOption =
            new Option<DataClassMemberNullability>("--dataClassMemberNullability")
            {
                Description = "Specifies whether data class scalar properties generated always nullable (for better type reuse) or respect the GraphQL schema",
                DefaultValueFactory = _ => DataClassMemberNullability.AlwaysNullable
            };

        var generationOrderOption =
            new Option<GenerationOrder>("--generationOrder")
            {
                Description = "Specifies whether order of generated C# classes/enums respect the GraphQL schema or is enforced to alphabetical for easier change tracking",
                DefaultValueFactory = _ => GenerationOrder.DefinedBySchema
            };

        var inputObjectModeOption =
            new Option<InputObjectMode>("--inputObjectMode")
            {
                Description = "Specifies whether input objects are generated as POCOs or they have support of GraphQL parameter references and explicit null values",
                DefaultValueFactory = _ => InputObjectMode.Rich
            };

        var includeDeprecatedFieldOption =
            new Option<bool>("--includeDeprecatedFields")
            {
                Description = "Includes deprecated fields in generated query builders and data classes",
                DefaultValueFactory = _ => false
            };

        var nullableReferenceOption = new Option<bool>("--nullableReferences") { Description = "Enables nullable references", DefaultValueFactory = _ => false };

        var fileScopeNamespaceOption =
            new Option<bool>("--fileScopedNamespaces")
            {
                Description = "Specifies whether file-scoped namespaces should be used in generated files (C# 10 or later)",
                DefaultValueFactory = _ => false
            };

        var ignoreServiceUrlCertificateErrorOption =
            new Option<bool>("--ignoreServiceUrlCertificateErrors")
            {
                Description = "Ignores HTTPS errors when retrieving GraphQL metadata from an URL; typically when using self signed certificates",
                DefaultValueFactory = _ => false
            };

        var command =
            new RootCommand("A tool for generating C# GraphQL query builders and data classes")
            {
                TreatUnmatchedTokensAsErrors = true,
                Options =
                {
                    outputPathOption,
                    namespaceOption,
                    serviceUrlOption,
                    schemaFileOption,
                    httpMethodOption,
                    headerOption,
                    classPrefixOption,
                    classSuffixOption,
                    classMappingOption,
                    csharpVersionOption,
                    codeDocumentationOption,
                    memberAccessibilityOption,
                    outputTypeOption,
                    partialClassesOption,
                    booleanTypeMappingOption,
                    floatTypeMappingOption,
                    idTypeMappingOption,
                    integerTypeMappingOption,
                    jsonPropertyAttributeOption,
                    enumValueMappingOption,
                    dataClassMemberNullabilityOption,
                    generationOrderOption,
                    inputObjectModeOption,
                    includeDeprecatedFieldOption,
                    nullableReferenceOption,
                    fileScopeNamespaceOption,
                    ignoreServiceUrlCertificateErrorOption,
                    regexScalarFieldTypeMappingConfigurationOption
                },
                Validators =
                {
                    result =>
                    {
                        var errorMessage =
                            (ServiceUrl: result.GetResult(serviceUrlOption), SchemaFile: result.GetResult(schemaFileOption)) switch
                            {
                                { ServiceUrl: not null, SchemaFile: not null } => "\"serviceUrl\" and \"schemaFileName\" parameters are mutually exclusive. ",
                                { ServiceUrl: null, SchemaFile: null } => "Either \"serviceUrl\" or \"schemaFileName\" parameter must be specified. ",
                                _ => null
                            };

                        if (errorMessage is not null)
                            result.AddError(errorMessage);
                    },
                    result =>
                    {
                        var regexScalarFieldTypeMappingConfigurationFileName = result.GetValue(regexScalarFieldTypeMappingConfigurationOption);
                        if (String.IsNullOrEmpty(regexScalarFieldTypeMappingConfigurationFileName))
                            return;

                        try
                        {
                            RegexScalarFieldTypeMappingProvider.ParseRulesFromJson(File.ReadAllText(regexScalarFieldTypeMappingConfigurationFileName));
                        }
                        catch (Exception exception)
                        {
                            result.AddError(exception.Message);
                        }
                    }
                }
            };

        command.SetAction(async (result, cancellationToken) =>
        {
            var regexScalarFieldTypeMappingConfigurationFile = result.GetValue(regexScalarFieldTypeMappingConfigurationOption);

            RegexScalarFieldTypeMappingProvider scalarFieldTypeMappingProvider = null;

            if (!String.IsNullOrEmpty(regexScalarFieldTypeMappingConfigurationFile))
            {
                var configurationJson = await File.ReadAllTextAsync(regexScalarFieldTypeMappingConfigurationFile, cancellationToken);

                scalarFieldTypeMappingProvider =
                    new RegexScalarFieldTypeMappingProvider(
                        RegexScalarFieldTypeMappingProvider.ParseRulesFromJson(configurationJson));

                await result.InvocationConfiguration.Output.WriteLineAsync($"Scalar field type mapping configuration file {regexScalarFieldTypeMappingConfigurationFile} loaded. ");
            }

            var options =
                new ProgramOptions
                {
                    OutputPath = result.GetValue(outputPathOption),
                    IgnoreServiceUrlCertificateErrors = result.GetValue(ignoreServiceUrlCertificateErrorOption),
                    ServiceUrl = result.GetValue(serviceUrlOption),
                    SchemaFile = result.GetValue(schemaFileOption),
                    HttpMethod = result.GetValue(httpMethodOption),
                    Header = result.GetValue(headerOption),
                    OutputType = result.GetValue(outputTypeOption),
                    GeneratorConfiguration =
                        new()
                        {
                            CSharpVersion = result.GetValue(csharpVersionOption),
                            ClassPrefix = result.GetValue(classPrefixOption),
                            ClassSuffix = result.GetValue(classSuffixOption),
                            TargetNamespace = result.GetValue(namespaceOption),
                            CodeDocumentationType = result.GetValue(codeDocumentationOption),
                            IncludeDeprecatedFields = result.GetValue(includeDeprecatedFieldOption),
                            EnableNullableReferences = result.GetValue(nullableReferenceOption),
                            GeneratePartialClasses = result.GetValue(partialClassesOption),
                            IntegerTypeMapping = result.GetValue(integerTypeMappingOption),
                            FloatTypeMapping = result.GetValue(floatTypeMappingOption),
                            BooleanTypeMapping = result.GetValue(booleanTypeMappingOption),
                            IdTypeMapping = result.GetValue(idTypeMappingOption),
                            JsonPropertyGeneration = result.GetValue(jsonPropertyAttributeOption),
                            EnumValueNaming = result.GetValue(enumValueMappingOption),
                            MemberAccessibility = result.GetValue(memberAccessibilityOption),
                            FileScopedNamespaces = result.GetValue(fileScopeNamespaceOption),
                            DataClassMemberNullability = result.GetValue(dataClassMemberNullabilityOption),
                            GenerationOrder = result.GetValue(generationOrderOption),
                            InputObjectMode = result.GetValue(inputObjectModeOption),
                            ScalarFieldTypeMappingProvider = scalarFieldTypeMappingProvider
                        }
                };

            if (!KeyValueParameterParser.TryGetCustomClassMapping(result.GetValue(classMappingOption), out var customMapping, out var customMappingParsingErrorMessage))
                throw new InvalidOperationException(customMappingParsingErrorMessage);

            customMapping.ForEach(options.GeneratorConfiguration.CustomClassNameMapping.Add);

            await GraphQlCSharpFileHelper.GenerateClientSourceCode(result.InvocationConfiguration, options, cancellationToken);
            return 0;
        });

        return command;
    }

    private static void AddKeyValueErrorIfFound(this OptionResult result, TryGetKeyValuePairs tryGetKeyValuePairs)
    {
        if (!tryGetKeyValuePairs(result.Tokens.Select(t => t.Value), out _, out var errorMessage))
            result.AddError(errorMessage);
    }

    private delegate bool TryGetKeyValuePairs(IEnumerable<string> sources, out List<KeyValuePair<string, string>> keyValuePairs, out string errorMessage);
}