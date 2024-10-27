using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GraphQlClientGenerator;

[Generator]
public class GraphQlClientSourceGenerator : ISourceGenerator
{
    private const string ApplicationCode = "GRAPHQLGEN";
    private const string FileNameGraphQlClientSource = "GraphQlClient.cs";
    private const string FileNameRegexScalarFieldTypeMappingProviderConfiguration = "RegexScalarFieldTypeMappingProvider.gql.config.json";
    private const string BuildPropertyKeyPrefix = "build_property.GraphQlClientGenerator_";

    private static readonly DiagnosticDescriptor DescriptorParameterError = CreateDiagnosticDescriptor(DiagnosticSeverity.Error, 1000);
    private static readonly DiagnosticDescriptor DescriptorGenerationError = CreateDiagnosticDescriptor(DiagnosticSeverity.Error, 1001);
    private static readonly DiagnosticDescriptor DescriptorInfo = CreateDiagnosticDescriptor(DiagnosticSeverity.Info, 3000);

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.Compilation is not CSharpCompilation compilation)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DescriptorParameterError,
                    Location.None,
                    $"incompatible language: {context.Compilation.Language}"));

            return;
        }

        try
        {
            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("ServiceUrl"), out var serviceUrl);
            var isServiceUrlMissing = String.IsNullOrWhiteSpace(serviceUrl);
            var graphQlSchemaFiles = context.AdditionalFiles.Where(f => Path.GetFileName(f.Path).EndsWith(".gql.schema.json", StringComparison.OrdinalIgnoreCase)).ToList();
            var regexScalarFieldTypeMappingProviderConfigurationJson =
                context.AdditionalFiles
                    .SingleOrDefault(f => String.Equals(Path.GetFileName(f.Path), FileNameRegexScalarFieldTypeMappingProviderConfiguration, StringComparison.OrdinalIgnoreCase))
                    ?.GetText()
                    ?.ToString();

            var regexScalarFieldTypeMappingProviderRules =
                regexScalarFieldTypeMappingProviderConfigurationJson is not null
                    ? RegexScalarFieldTypeMappingProvider.ParseRulesFromJson(regexScalarFieldTypeMappingProviderConfigurationJson)
                    : null;

            var isSchemaFileSpecified = graphQlSchemaFiles.Any();
            if (isServiceUrlMissing && !isSchemaFileSpecified)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DescriptorInfo,
                        Location.None,
                        "Neither \"GraphQlClientGenerator_ServiceUrl\" parameter nor GraphQL JSON schema additional file specified; terminating. "));

                return;
            }

            if (!isServiceUrlMissing && isSchemaFileSpecified)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DescriptorParameterError,
                        Location.None,
                        "\"GraphQlClientGenerator_ServiceUrl\" parameter and GraphQL JSON schema additional file are mutually exclusive. "));

                return;
            }

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("Namespace"), out var @namespace);
            if (String.IsNullOrWhiteSpace(@namespace))
            {
                var root = (CompilationUnitSyntax)compilation.SyntaxTrees.FirstOrDefault()?.GetRoot();
                var namespaceIdentifier = (IdentifierNameSyntax)root?.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name;
                if (namespaceIdentifier is null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DescriptorParameterError,
                            Location.None,
                            "\"GraphQlClientGenerator_Namespace\" required"));

                    return;
                }

                @namespace = namespaceIdentifier.Identifier.ValueText;

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DescriptorInfo,
                        Location.None,
                        $"\"GraphQlClientGenerator_Namespace\" not specified; using \"{@namespace}\""));
            }

            var configuration = new GraphQlGeneratorConfiguration { TargetNamespace = @namespace };

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("ClassPrefix"), out var classPrefix);
            configuration.ClassPrefix = classPrefix;

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("ClassSuffix"), out var classSuffix);
            configuration.ClassSuffix = classSuffix;

            if (compilation.LanguageVersion >= LanguageVersion.CSharp6)
                configuration.CSharpVersion =
                    compilation.Options.NullableContextOptions == NullableContextOptions.Disable
                        ? CSharpVersion.Newest
                        : CSharpVersion.NewestWithNullableReferences;

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("IncludeDeprecatedFields"), out var includeDeprecatedFieldsRaw);
            configuration.IncludeDeprecatedFields = !String.IsNullOrWhiteSpace(includeDeprecatedFieldsRaw) && Convert.ToBoolean(includeDeprecatedFieldsRaw);

            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("HttpMethod"), out var httpMethod))
                httpMethod = "POST";

            SetConfigurationEnumValue(context, "CodeDocumentationType", CodeDocumentationType.XmlSummary, v => configuration.CodeDocumentationType = v);
            SetConfigurationEnumValue(context, "FloatTypeMapping", FloatTypeMapping.Decimal, v => configuration.FloatTypeMapping = v);
            SetConfigurationEnumValue(context, "BooleanTypeMapping", BooleanTypeMapping.Boolean, v => configuration.BooleanTypeMapping = v);
            SetConfigurationEnumValue(context, "IdTypeMapping", IdTypeMapping.Guid, v => configuration.IdTypeMapping = v);
            SetConfigurationEnumValue(context, "JsonPropertyGeneration", JsonPropertyGenerationOption.CaseInsensitive, v => configuration.JsonPropertyGeneration = v);
            SetConfigurationEnumValue(context, "EnumValueNaming", EnumValueNamingOption.CSharp, v => configuration.EnumValueNaming = v);
            SetConfigurationEnumValue(context, "DataClassMemberNullability", DataClassMemberNullability.AlwaysNullable, v => configuration.DataClassMemberNullability = v);
            SetConfigurationEnumValue(context, "GenerationOrder", GenerationOrder.DefinedBySchema, v => configuration.GenerationOrder = v);

            var outputType = OutputType.SingleFile;
            SetConfigurationEnumValue(context, "OutputType", OutputType.SingleFile, v => outputType = v);

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("CustomClassMapping"), out var customClassMappingRaw);
            if (!KeyValueParameterParser.TryGetCustomClassMapping(
                    customClassMappingRaw?.Split(['|', ';', ' '], StringSplitOptions.RemoveEmptyEntries),
                    out var customMapping,
                    out var customMappingParsingErrorMessage))
            {
                context.ReportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, customMappingParsingErrorMessage));
                return;
            }

            foreach (var kvp in customMapping)
                configuration.CustomClassNameMapping.Add(kvp);

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("Headers"), out var headersRaw);
            if (!KeyValueParameterParser.TryGetCustomHeaders(
                    headersRaw?.Split(['|'], StringSplitOptions.RemoveEmptyEntries),
                    out var headers,
                    out var headerParsingErrorMessage))
            {
                context.ReportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, headerParsingErrorMessage));
                return;
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("ScalarFieldTypeMappingProvider"), out var scalarFieldTypeMappingProviderName))
            {
                if (regexScalarFieldTypeMappingProviderRules is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, "\"GraphQlClientGenerator_ScalarFieldTypeMappingProvider\" and RegexScalarFieldTypeMappingProviderConfiguration are mutually exclusive"));
                    return;
                }

                if (String.IsNullOrWhiteSpace(scalarFieldTypeMappingProviderName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, "\"GraphQlClientGenerator_ScalarFieldTypeMappingProvider\" value missing"));
                    return;
                }

                var scalarFieldTypeMappingProviderType = Type.GetType(scalarFieldTypeMappingProviderName);
                if (scalarFieldTypeMappingProviderType is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, $"ScalarFieldTypeMappingProvider \"{scalarFieldTypeMappingProviderName}\" not found"));
                    return;
                }

                var scalarFieldTypeMappingProvider = (IScalarFieldTypeMappingProvider)Activator.CreateInstance(scalarFieldTypeMappingProviderType);
                configuration.ScalarFieldTypeMappingProvider = scalarFieldTypeMappingProvider;
            }
            else if (regexScalarFieldTypeMappingProviderRules?.Count > 0)
                configuration.ScalarFieldTypeMappingProvider = new RegexScalarFieldTypeMappingProvider(regexScalarFieldTypeMappingProviderRules);

            var graphQlSchemas = new List<(string TargetFileName, GraphQlSchema Schema)>();
            if (isSchemaFileSpecified)
            {
                foreach (var schemaFile in graphQlSchemaFiles)
                {
                    var targetFileName = $"{Path.GetFileNameWithoutExtension(schemaFile.Path)}.cs";
                    graphQlSchemas.Add((targetFileName, GraphQlGenerator.DeserializeGraphQlSchema(schemaFile.GetText().ToString())));
                }
            }
            else
            {
                using var httpClientHandler = GraphQlGenerator.CreateDefaultHttpClientHandler();
                var ignoreServiceUrlCertificateErrors =
                    context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("IgnoreServiceUrlCertificateErrors"), out var ignoreServiceUrlCertificateErrorsRaw) &&
                    !String.IsNullOrWhiteSpace(ignoreServiceUrlCertificateErrorsRaw) && Convert.ToBoolean(ignoreServiceUrlCertificateErrorsRaw);

                if (ignoreServiceUrlCertificateErrors)
                    httpClientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

                var graphQlSchema = GraphQlGenerator.RetrieveSchema(new HttpMethod(httpMethod), serviceUrl, headers, httpClientHandler).GetAwaiter().GetResult();
                graphQlSchemas.Add((FileNameGraphQlClientSource, graphQlSchema));
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DescriptorInfo,
                        Location.None,
                        $"GraphQl schema fetched successfully from {serviceUrl}"));
            }

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey("FileScopedNamespaces"), out var fileScopedNamespacesRaw);
            configuration.FileScopedNamespaces = !String.IsNullOrWhiteSpace(fileScopedNamespacesRaw) && Convert.ToBoolean(fileScopedNamespacesRaw);

            var generator = new GraphQlGenerator(configuration);

            foreach (var (targetFileName, schema) in graphQlSchemas)
            {
                if (outputType == OutputType.SingleFile)
                {
                    var builder = new StringBuilder();
                    using (var writer = new StringWriter(builder))
                        generator.WriteFullClientCSharpFile(schema, writer);

                    context.AddSource(targetFileName, SourceText.From(builder.ToString(), Encoding.UTF8));
                }
                else
                {
                    var multipleFileGenerationContext = new MultipleFileGenerationContext(schema, new SourceGeneratorFileEmitter(context));
                    generator.Generate(multipleFileGenerationContext);
                }
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DescriptorInfo,
                    Location.None,
                    "GraphQlClientGenerator task completed successfully. "));
        }
        catch (Exception exception)
        {
            context.ReportDiagnostic(Diagnostic.Create(DescriptorGenerationError, Location.None, exception.Message));
        }
    }

    private static void SetConfigurationEnumValue<TEnum>(
        GeneratorExecutionContext context,
        string parameterName,
        TEnum defaultValue,
        Action<TEnum> valueSetter) where TEnum : Enum
    {
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKey(parameterName), out var enumStringValue);
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
}

public class SourceGeneratorFileEmitter(GeneratorExecutionContext sourceGeneratorContext) : ICodeFileEmitter
{
    public CodeFile CreateFile(string fileName) => new(fileName, new MemoryStream());

    public CodeFileInfo CollectFileInfo(CodeFile codeFile)
    {
        if (codeFile.Stream is not MemoryStream memoryStream)
            throw new ArgumentException($"File was not created by {nameof(SourceGeneratorFileEmitter)}.", nameof(codeFile));

        codeFile.Writer.Flush();
        memoryStream.Position = 0;
        sourceGeneratorContext.AddSource(codeFile.FileName, SourceText.From(codeFile.Stream, Encoding.UTF8));
        var fileSize = (int)codeFile.Stream.Length;
        codeFile.Dispose();
        return new CodeFileInfo { FileName = codeFile.FileName, Length = fileSize };
    }
}