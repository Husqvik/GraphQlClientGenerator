using System.CommandLine;

namespace GraphQlClientGenerator.Console;

internal static class GraphQlCSharpFileHelper
{
    public static async Task GenerateClientSourceCode(InvocationConfiguration invocationConfiguration, ProgramOptions options, CancellationToken cancellationToken)
    {
        var output = invocationConfiguration.Output;

        GraphQlSchema schema;

        if (String.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            var schemaJson = await File.ReadAllTextAsync(options.SchemaFileName, cancellationToken);
            await output.WriteLineAsync($"GraphQL schema file {options.SchemaFileName} loaded ({schemaJson.Length:N0} B). ");
            schema = GraphQlGenerator.DeserializeGraphQlSchema(schemaJson);
        }
        else
        {
            if (!KeyValueParameterParser.TryGetCustomHeaders(options.Header, out var headers, out var headerParsingErrorMessage))
                throw new InvalidOperationException(headerParsingErrorMessage);

            using var httpClientHandler = GraphQlGenerator.CreateDefaultHttpClientHandler();
            if (options.IgnoreServiceUrlCertificateErrors)
                httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            schema =
                await GraphQlGenerator.RetrieveSchema(
                    new HttpMethod(options.HttpMethod),
                    options.ServiceUrl,
                    headers,
                    httpClientHandler,
                    GraphQlWellKnownDirective.None,
                    cancellationToken);

            await output.WriteLineAsync($"GraphQL Schema retrieved from {options.ServiceUrl}. ");
        }
            
        var generatorConfiguration =
            new GraphQlGeneratorConfiguration
            {
                TargetNamespace = options.Namespace,
                CSharpVersion = options.CSharpVersion,
                ClassPrefix = options.ClassPrefix,
                ClassSuffix = options.ClassSuffix,
                CodeDocumentationType = options.CodeDocumentationType,
                GeneratePartialClasses = options.PartialClasses,
                MemberAccessibility = options.MemberAccessibility,
                IdTypeMapping = options.IdTypeMapping,
                FloatTypeMapping = options.FloatTypeMapping,
                IntegerTypeMapping = options.IntegerTypeMapping,
                BooleanTypeMapping = options.BooleanTypeMapping,
                JsonPropertyGeneration = options.JsonPropertyAttribute,
                EnumValueNaming = options.EnumValueNaming,
                DataClassMemberNullability = options.DataClassMemberNullability,
                GenerationOrder = options.GenerationOrder,
                InputObjectMode = options.InputObjectMode,
                IncludeDeprecatedFields = options.IncludeDeprecatedFields,
                EnableNullableReferences = options.NullableReferences,
                FileScopedNamespaces = options.FileScopedNamespaces
            };

        if (!KeyValueParameterParser.TryGetCustomClassMapping(options.ClassMapping, out var customMapping, out var customMappingParsingErrorMessage))
            throw new InvalidOperationException(customMappingParsingErrorMessage);

        foreach (var kvp in customMapping)
            generatorConfiguration.CustomClassNameMapping.Add(kvp);

        if (!String.IsNullOrEmpty(options.RegexScalarFieldTypeMappingConfigurationFile))
        {
            generatorConfiguration.ScalarFieldTypeMappingProvider =
                new RegexScalarFieldTypeMappingProvider(
                    RegexScalarFieldTypeMappingProvider.ParseRulesFromJson(await File.ReadAllTextAsync(options.RegexScalarFieldTypeMappingConfigurationFile, cancellationToken)));

            await output.WriteLineAsync($"Scalar field type mapping configuration file {options.RegexScalarFieldTypeMappingConfigurationFile} loaded. ");
        }

        var generator = new GraphQlGenerator(generatorConfiguration);

        if (options.OutputType is OutputType.SingleFile)
        {
            await File.WriteAllTextAsync(options.OutputPath, generator.GenerateFullClientCSharpFile(schema, output.WriteLine), cancellationToken);
            await output.WriteLineAsync($"File {options.OutputPath} generated successfully ({new FileInfo(options.OutputPath).Length:N0} B). ");
        }
        else
        {
            var projectFileInfo =
                options.OutputPath is not null && options.OutputPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                    ? new FileInfo(options.OutputPath)
                    : null;

            var codeFileEmitter = new FileSystemEmitter(projectFileInfo?.DirectoryName ?? options.OutputPath);
            var multipleFileGenerationContext =
                new MultipleFileGenerationContext(schema, codeFileEmitter, projectFileInfo?.Name)
                {
                    LogMessage = output.WriteLine
                };

            generator.Generate(multipleFileGenerationContext);
        }
    }
}