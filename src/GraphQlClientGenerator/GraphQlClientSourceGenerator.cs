using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Text;

namespace GraphQlClientGenerator;

[Generator]
public class GraphQlClientSourceGenerator : IIncrementalGenerator
{
    private const string ApplicationCode = "GRAPHQLGEN";
    private const string FileNameGraphQlClientSource = "GraphQlClient.cs";
    private const string FileNameRegexScalarFieldTypeMappingProviderConfiguration = "RegexScalarFieldTypeMappingProvider.gql.config.json";
    private const string BuildPropertyKeyPrefix = "build_property.GraphQlClientGenerator_";

    private static readonly DiagnosticDescriptor DescriptorParameterError = CreateDiagnosticDescriptor(DiagnosticSeverity.Error, 1000);
    private static readonly DiagnosticDescriptor DescriptorGenerationError = CreateDiagnosticDescriptor(DiagnosticSeverity.Error, 1001);
    private static readonly DiagnosticDescriptor DescriptorWarning = CreateDiagnosticDescriptor(DiagnosticSeverity.Warning, 2000);
    private static readonly DiagnosticDescriptor DescriptorInfo = CreateDiagnosticDescriptor(DiagnosticSeverity.Info, 3000);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptionsProvider = context.AnalyzerConfigOptionsProvider.Select((op, _) => op.GlobalOptions);
        var schemaFilesProvider =
            context.AdditionalTextsProvider
                .Where(at => at.Path.EndsWith(".gql.schema.json", StringComparison.OrdinalIgnoreCase))
                .Collect();

        var regexConfigFileProvider =
            context.AdditionalTextsProvider
                .Where(at => Path.GetFileName(at.Path).Equals(FileNameRegexScalarFieldTypeMappingProviderConfiguration, StringComparison.OrdinalIgnoreCase))
                .Collect();

        var sourceState =
            globalOptionsProvider
                .Combine(schemaFilesProvider)
                .Combine(regexConfigFileProvider)
                .Combine(context.CompilationProvider)
                .Select((data, _) =>
                {
                    var (((options, schemaFiles), regexScalarFieldTypeMappingProviderFiles), compilation) = data;
                    var diagnostics = new List<Diagnostic>();
                    var setup = ResolveGenerationSetup(options, schemaFiles, diagnostics.Add);
                    var configuration = ResolveGeneratorConfiguration(options, compilation, regexScalarFieldTypeMappingProviderFiles.FirstOrDefault(), diagnostics.Add);
                    return (setup, configuration, diagnostics);
                });

        context.RegisterSourceOutput(
            sourceState,
            (sourceProductionContext, state) =>
            {
                try
                {
                    var (setup, configuration, diagnostics) = state;

                    foreach (var diagnostic in diagnostics)
                        sourceProductionContext.ReportDiagnostic(diagnostic);

                    if (setup is null || configuration is null)
                        return;

                    ExecuteGeneration(setup, configuration, sourceProductionContext.AddSource, sourceProductionContext.ReportDiagnostic);
                }
                catch (Exception exception)
                {
                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(DescriptorGenerationError, Location.None, exception.ToString()));
                }
            });
    }

    private static GenerationSetup ResolveGenerationSetup(AnalyzerConfigOptions globalOptions, IReadOnlyCollection<AdditionalText> graphQlSchemaFiles, Action<Diagnostic> reportDiagnostic)
    {
        globalOptions.TryGetValue(BuildPropertyKey("ServiceUrl"), out var serviceUrl);
        var isServiceUrlMissing = String.IsNullOrWhiteSpace(serviceUrl);

        var isSchemaFileSpecified = graphQlSchemaFiles.Count > 0;
        if (isServiceUrlMissing && !isSchemaFileSpecified)
        {
            reportDiagnostic(
                Diagnostic.Create(
                    DescriptorInfo,
                    Location.None,
                    "Neither \"GraphQlClientGenerator_ServiceUrl\" parameter nor GraphQL JSON schema additional file specified; terminating. "));

            return null;
        }

        if (!isServiceUrlMissing && isSchemaFileSpecified)
        {
            reportDiagnostic(
                Diagnostic.Create(
                    DescriptorParameterError,
                    Location.None,
                    "\"GraphQlClientGenerator_ServiceUrl\" parameter and GraphQL JSON schema additional file are mutually exclusive. "));

            return null;
        }

        var httpMethod =
            globalOptions.TryGetValue(BuildPropertyKey("HttpMethod"), out var httpMethodRaw)
                ? new HttpMethod(httpMethodRaw)
                : HttpMethod.Post;

        globalOptions.TryGetValue(BuildPropertyKey("Headers"), out var headersRaw);
        if (!KeyValueParameterParser.TryGetCustomHeaders(
                headersRaw?.Split(['|'], StringSplitOptions.RemoveEmptyEntries),
                out var headers,
                out var headerParsingErrorMessage))
        {
            reportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, headerParsingErrorMessage));
            return null;
        }

        var outputType = OutputType.SingleFile;
        SetConfigurationEnumValue(globalOptions, nameof(OutputType), OutputType.SingleFile, v => outputType = v);

        var ignoreServiceUrlCertificateErrors =
            globalOptions.TryGetValue(BuildPropertyKey("IgnoreServiceUrlCertificateErrors"), out var ignoreServiceUrlCertificateErrorsRaw) &&
            !String.IsNullOrWhiteSpace(ignoreServiceUrlCertificateErrorsRaw) && Convert.ToBoolean(ignoreServiceUrlCertificateErrorsRaw);

        return
            new GenerationSetup
            {
                HttpMethod = httpMethod,
                OutputType = outputType,
                ServiceUrl = serviceUrl,
                HttpHeaders = headers,
                IgnoreServiceUrlCertificateErrors = ignoreServiceUrlCertificateErrors,
                SchemaFiles = graphQlSchemaFiles
            };
    }

    private static GraphQlGeneratorConfiguration ResolveGeneratorConfiguration(
        AnalyzerConfigOptions globalOptions,
        Compilation compilation,
        AdditionalText regexScalarFieldTypeMappingProviderConfigurationFile,
        Action<Diagnostic> reportDiagnostic)
    {
        if (compilation is not CSharpCompilation cSharpCompilation)
        {
            reportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, $"incompatible language: {compilation.Language}"));
            return null;
        }

        globalOptions.TryGetValue(BuildPropertyKey("Namespace"), out var @namespace);
        if (String.IsNullOrWhiteSpace(@namespace))
        {
            var root = (CompilationUnitSyntax)cSharpCompilation.SyntaxTrees.FirstOrDefault()?.GetRoot();
            var namespaceIdentifier = (IdentifierNameSyntax)root?.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name;
            if (namespaceIdentifier is null)
            {
                reportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, "\"GraphQlClientGenerator_Namespace\" required"));
                return null;
            }

            @namespace = namespaceIdentifier.Identifier.ValueText;

            reportDiagnostic(Diagnostic.Create(DescriptorInfo, Location.None, $"\"GraphQlClientGenerator_Namespace\" not specified; using \"{@namespace}\""));
        }

        var configuration = new GraphQlGeneratorConfiguration { TargetNamespace = @namespace };

        globalOptions.TryGetValue(BuildPropertyKey(nameof(configuration.ClassPrefix)), out var classPrefix);
        configuration.ClassPrefix = classPrefix;

        globalOptions.TryGetValue(BuildPropertyKey(nameof(configuration.ClassSuffix)), out var classSuffix);
        configuration.ClassSuffix = classSuffix;

        if (cSharpCompilation.LanguageVersion >= LanguageVersion.CSharp12)
            configuration.CSharpVersion = CSharpVersion.CSharp12;
        else if (cSharpCompilation.LanguageVersion >= LanguageVersion.CSharp6)
            configuration.CSharpVersion = CSharpVersion.CSharp6;

        globalOptions.TryGetValue(BuildPropertyKey(nameof(configuration.IncludeDeprecatedFields)), out var includeDeprecatedFieldsRaw);
        configuration.IncludeDeprecatedFields = Boolean.TryParse(includeDeprecatedFieldsRaw, out var includeDeprecatedFields) && includeDeprecatedFields;

        globalOptions.TryGetValue(BuildPropertyKey(nameof(configuration.EnableNullableReferences)), out var enableNullableReferencesRaw);
        configuration.EnableNullableReferences = Boolean.TryParse(enableNullableReferencesRaw, out var enableNullableReferences) && enableNullableReferences;

        if (configuration.EnableNullableReferences && cSharpCompilation.Options.NullableContextOptions is NullableContextOptions.Disable)
        {
            reportDiagnostic(Diagnostic.Create(DescriptorInfo, Location.None, "compilation nullable references disabled"));
            configuration.EnableNullableReferences = false;
        }

        SetConfigurationEnumValue(globalOptions, nameof(CodeDocumentationType), CodeDocumentationType.XmlSummary, v => configuration.CodeDocumentationType = v);
        SetConfigurationEnumValue(globalOptions, nameof(FloatTypeMapping), FloatTypeMapping.Decimal, v => configuration.FloatTypeMapping = v);
        SetConfigurationEnumValue(globalOptions, nameof(BooleanTypeMapping), BooleanTypeMapping.Boolean, v => configuration.BooleanTypeMapping = v);
        SetConfigurationEnumValue(globalOptions, nameof(IdTypeMapping), IdTypeMapping.Guid, v => configuration.IdTypeMapping = v);
        SetConfigurationEnumValue(globalOptions, "JsonPropertyGeneration", JsonPropertyGenerationOption.CaseInsensitive, v => configuration.JsonPropertyGeneration = v);
        SetConfigurationEnumValue(globalOptions, "EnumValueNaming", EnumValueNamingOption.CSharp, v => configuration.EnumValueNaming = v);
        SetConfigurationEnumValue(globalOptions, nameof(DataClassMemberNullability), DataClassMemberNullability.AlwaysNullable, v => configuration.DataClassMemberNullability = v);
        SetConfigurationEnumValue(globalOptions, nameof(GenerationOrder), GenerationOrder.DefinedBySchema, v => configuration.GenerationOrder = v);
        SetConfigurationEnumValue(globalOptions, nameof(InputObjectMode), InputObjectMode.Rich, v => configuration.InputObjectMode = v);

        globalOptions.TryGetValue(BuildPropertyKey("CustomClassMapping"), out var customClassMappingRaw);
        if (!KeyValueParameterParser.TryGetCustomClassMapping(
                customClassMappingRaw?.Split(['|', ';', ' '], StringSplitOptions.RemoveEmptyEntries),
                out var customMapping,
                out var customMappingParsingErrorMessage))
        {
            reportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, customMappingParsingErrorMessage));
            return null;
        }

        foreach (var kvp in customMapping)
            configuration.CustomClassNameMapping.Add(kvp);

        var regexScalarFieldTypeMappingProviderConfigurationJson =
            regexScalarFieldTypeMappingProviderConfigurationFile
                ?.GetText()
                ?.ToString();

        var regexScalarFieldTypeMappingProviderRules =
            regexScalarFieldTypeMappingProviderConfigurationJson is not null
                ? RegexScalarFieldTypeMappingProvider.ParseRulesFromJson(regexScalarFieldTypeMappingProviderConfigurationJson)
                : null;

        if (globalOptions.TryGetValue(BuildPropertyKey("ScalarFieldTypeMappingProvider"), out var scalarFieldTypeMappingProviderName))
        {
            if (regexScalarFieldTypeMappingProviderRules is not null)
            {
                reportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, "\"GraphQlClientGenerator_ScalarFieldTypeMappingProvider\" and RegexScalarFieldTypeMappingProviderConfiguration are mutually exclusive"));
                return null;
            }

            if (String.IsNullOrWhiteSpace(scalarFieldTypeMappingProviderName))
            {
                reportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, "\"GraphQlClientGenerator_ScalarFieldTypeMappingProvider\" value missing"));
                return null;
            }

            var scalarFieldTypeMappingProviderType = Type.GetType(scalarFieldTypeMappingProviderName);
            if (scalarFieldTypeMappingProviderType is null)
            {
                reportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, $"ScalarFieldTypeMappingProvider \"{scalarFieldTypeMappingProviderName}\" not found"));
                return null;
            }

            var scalarFieldTypeMappingProvider = (IScalarFieldTypeMappingProvider)Activator.CreateInstance(scalarFieldTypeMappingProviderType);
            configuration.ScalarFieldTypeMappingProvider = scalarFieldTypeMappingProvider;
        }
        else if (regexScalarFieldTypeMappingProviderRules?.Count > 0)
            configuration.ScalarFieldTypeMappingProvider = new RegexScalarFieldTypeMappingProvider(regexScalarFieldTypeMappingProviderRules);

        globalOptions.TryGetValue(BuildPropertyKey(nameof(configuration.FileScopedNamespaces)), out var fileScopedNamespacesRaw);
        configuration.FileScopedNamespaces = !String.IsNullOrWhiteSpace(fileScopedNamespacesRaw) && Convert.ToBoolean(fileScopedNamespacesRaw);

        return configuration;
    }

    private static void ExecuteGeneration(GenerationSetup setup, GraphQlGeneratorConfiguration configuration, AddSourceDelegate addSource, Action<Diagnostic> reportDiagnostic)
    {
        var graphQlSchemas = new List<(string TargetFileName, GraphQlSchema Schema)>();
        if (setup.SchemaFiles.Count == 0)
        {
            using var httpClientHandler = GraphQlHttpUtilities.CreateDefaultHttpClientHandler();

            if (setup.IgnoreServiceUrlCertificateErrors)
                httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            var graphQlSchema =
                GraphQlHttpUtilities.RetrieveSchema(setup.HttpMethod, setup.ServiceUrl, setup.HttpHeaders, httpClientHandler, GraphQlWellKnownDirective.None)
                    .GetAwaiter()
                    .GetResult();

            graphQlSchemas.Add((FileNameGraphQlClientSource, graphQlSchema));
            reportDiagnostic(Diagnostic.Create(DescriptorInfo, Location.None, $"GraphQl schema fetched successfully using HTTP {setup.HttpMethod} {setup.ServiceUrl}. "));
        }
        else
        {
            foreach (var schemaFile in setup.SchemaFiles)
            {
                var schemaJsom = schemaFile.GetText()?.ToString();
                if (String.IsNullOrEmpty(schemaJsom))
                {
                    reportDiagnostic(Diagnostic.Create(DescriptorWarning, Location.None, $"Schema file {schemaFile.Path} is empty; ignored. "));
                    continue;
                }

                var targetFileName = $"{Path.GetFileNameWithoutExtension(schemaFile.Path)}.cs";
                graphQlSchemas.Add((targetFileName, GraphQlHttpUtilities.DeserializeGraphQlSchema(schemaJsom)));
            }
        }

        var generator = new GraphQlGenerator(configuration);

        foreach (var (targetFileName, schema) in graphQlSchemas)
        {
            if (setup.OutputType is OutputType.SingleFile)
            {
                var builder = new StringBuilder();
                using (var writer = new StringWriter(builder))
                    generator.WriteFullClientCSharpFile(schema, writer);

                addSource(targetFileName, SourceText.From(builder.ToString(), Encoding.UTF8));
            }
            else
            {
                var multipleFileGenerationContext = new MultipleFileGenerationContext(schema, new SourceGeneratorFileEmitter(addSource));
                generator.Generate(multipleFileGenerationContext);
            }
        }

        reportDiagnostic(Diagnostic.Create(DescriptorInfo, Location.None, "GraphQlClientGenerator task completed successfully. "));
    }

    private static void SetConfigurationEnumValue<TEnum>(
        AnalyzerConfigOptions globalOptions,
        string parameterName,
        TEnum defaultValue,
        Action<TEnum> valueSetter) where TEnum : Enum
    {
        globalOptions.TryGetValue(BuildPropertyKey(parameterName), out var enumStringValue);

        var value =
            String.IsNullOrWhiteSpace(enumStringValue)
                ? defaultValue
                : (TEnum)Enum.Parse(typeof(TEnum), enumStringValue, true);

        valueSetter(value);
    }

    private static string BuildPropertyKey(string parameterName) => $"{BuildPropertyKeyPrefix}{parameterName}";

    private static DiagnosticDescriptor CreateDiagnosticDescriptor(DiagnosticSeverity severity, int code) =>
        new(
            $"{ApplicationCode}{code}",
            $"{severity} {ApplicationCode}{code}",
            "{0}",
            "GraphQlClientGenerator",
            severity,
            true);

    private class GenerationSetup
    {
        public OutputType OutputType { get; set; }
        public string ServiceUrl { get; set; }
        public HttpMethod HttpMethod { get; set; }
        public bool IgnoreServiceUrlCertificateErrors { get; set; }
        public ICollection<KeyValuePair<string, string>> HttpHeaders { get; set; }
        public IReadOnlyCollection<AdditionalText> SchemaFiles { get; set; }
    }
}

public delegate void AddSourceDelegate(string hintName, SourceText sourceText);

public class SourceGeneratorFileEmitter(AddSourceDelegate addSource) : ICodeFileEmitter
{
    private readonly AddSourceDelegate _addSource = addSource ?? throw new ArgumentNullException(nameof(addSource));

    public CodeFile CreateFile(string fileName) => new(fileName, new MemoryStream());

    public CodeFileInfo CollectFileInfo(CodeFile codeFile)
    {
        if (codeFile.Stream is not MemoryStream memoryStream)
            throw new ArgumentException($"File was not created by {nameof(SourceGeneratorFileEmitter)}.", nameof(codeFile));

        codeFile.Writer.Flush();
        _addSource(codeFile.FileName, SourceText.From(Encoding.UTF8.GetString(memoryStream.ToArray()), Encoding.UTF8));
        var fileSize = (int)codeFile.Stream.Length;
        codeFile.Dispose();
        return new CodeFileInfo { FileName = codeFile.FileName, Length = fileSize };
    }
}