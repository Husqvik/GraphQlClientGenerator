using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GraphQlClientGenerator
{
    [Generator]
    public class GraphQlClientSourceGenerator : ISourceGenerator
    {
        private const string ApplicationCode = "GRAPHQLGEN";
        private const string GraphQlClientDefaultFileName = "GraphQlClient.cs";
        private const string BuildPropertyKeyPrefix = "build_property.GraphQlClientGenerator_";

        private static readonly DiagnosticDescriptor DescriptorParameterError = CreateDiagnosticDescriptor(DiagnosticSeverity.Error, 1000);
        private static readonly DiagnosticDescriptor DescriptorGenerationError = CreateDiagnosticDescriptor(DiagnosticSeverity.Error, 1001);
        private static readonly DiagnosticDescriptor DescriptorInfo = CreateDiagnosticDescriptor(DiagnosticSeverity.Info, 3000);

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation as CSharpCompilation;
            if (compilation == null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DescriptorParameterError,
                        Location.None,
                        "incompatible language: " + context.Compilation.Language));

                return;
            }

            try
            {
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + "ServiceUrl", out var serviceUrl);
                var isServiceUrlMissing = String.IsNullOrWhiteSpace(serviceUrl);
                var graphQlSchemaFiles = context.AdditionalFiles.Where(f => String.Equals(Path.GetExtension(f.Path), ".json", StringComparison.OrdinalIgnoreCase)).ToArray();
                var isSchemaFileSpecified = graphQlSchemaFiles.Any();
                if (isServiceUrlMissing && !isSchemaFileSpecified)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DescriptorParameterError,
                            Location.None,
                            "Either \"GraphQlClientGenerator_ServiceUrl\" parameter or GraphQL JSON schema additional file must be specified. "));

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

                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + "Namespace", out var @namespace);
                if (String.IsNullOrWhiteSpace(@namespace))
                {
                    var root = (CompilationUnitSyntax)compilation.SyntaxTrees.FirstOrDefault()?.GetRoot();
                    var namespaceIdentifier = (IdentifierNameSyntax)root?.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name;
                    if (namespaceIdentifier == null)
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

                var configuration = new GraphQlGeneratorConfiguration { TreatUnknownObjectAsScalar = true };

                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + "ClassPrefix", out var classPrefix);
                configuration.ClassPrefix = classPrefix;

                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + "ClassSuffix", out var classSuffix);
                configuration.ClassSuffix = classSuffix;

                if (compilation.LanguageVersion >= LanguageVersion.CSharp6)
                    configuration.CSharpVersion =
                        compilation.Options.NullableContextOptions == NullableContextOptions.Disable
                            ? CSharpVersion.Newest
                            : CSharpVersion.NewestWithNullableReferences;

                var currentParameterName = "IncludeDeprecatedFields";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + currentParameterName, out var includeDeprecatedFieldsRaw);
                configuration.IncludeDeprecatedFields = !String.IsNullOrWhiteSpace(includeDeprecatedFieldsRaw) && Convert.ToBoolean(includeDeprecatedFieldsRaw);

                currentParameterName = "CommentGeneration";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + currentParameterName, out var commentGenerationRaw);
                configuration.CommentGeneration =
                    String.IsNullOrWhiteSpace(commentGenerationRaw)
                        ? CommentGenerationOption.CodeSummary
                        : (CommentGenerationOption)Enum.Parse(typeof(CommentGenerationOption), commentGenerationRaw, true);

                currentParameterName = "FloatTypeMapping";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + currentParameterName, out var floatTypeMappingRaw);
                configuration.FloatTypeMapping =
                    String.IsNullOrWhiteSpace(floatTypeMappingRaw)
                        ? FloatTypeMapping.Decimal
                        : (FloatTypeMapping)Enum.Parse(typeof(FloatTypeMapping), floatTypeMappingRaw, true);

                currentParameterName = "BooleanTypeMapping";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + currentParameterName, out var booleanTypeMappingRaw);
                configuration.BooleanTypeMapping =
                    String.IsNullOrWhiteSpace(booleanTypeMappingRaw)
                        ? BooleanTypeMapping.Boolean
                        : (BooleanTypeMapping)Enum.Parse(typeof(BooleanTypeMapping), booleanTypeMappingRaw, true);

                currentParameterName = "IdTypeMapping";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + currentParameterName, out var idTypeMappingRaw);
                configuration.IdTypeMapping =
                    String.IsNullOrWhiteSpace(idTypeMappingRaw)
                        ? IdTypeMapping.Guid
                        : (IdTypeMapping)Enum.Parse(typeof(IdTypeMapping), idTypeMappingRaw, true);

                currentParameterName = "JsonPropertyGeneration";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + currentParameterName, out var jsonPropertyGenerationRaw);
                configuration.JsonPropertyGeneration =
                    String.IsNullOrWhiteSpace(jsonPropertyGenerationRaw)
                        ? JsonPropertyGenerationOption.CaseInsensitive
                        : (JsonPropertyGenerationOption)Enum.Parse(typeof(JsonPropertyGenerationOption), jsonPropertyGenerationRaw, true);

                currentParameterName = "CustomClassMapping";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + currentParameterName, out var customClassMappingRaw);
                if (!KeyValueParameterParser.TryGetCustomClassMapping(
                    customClassMappingRaw?.Split(new[] { '|', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries),
                    out var customMapping,
                    out var customMappingParsingErrorMessage))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, DiagnosticSeverity.Error, customMappingParsingErrorMessage));
                    return;
                }

                foreach (var kvp in customMapping)
                    configuration.CustomClassNameMapping.Add(kvp);

                currentParameterName = "Headers";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(BuildPropertyKeyPrefix + currentParameterName, out var headersRaw);
                if (!KeyValueParameterParser.TryGetCustomHeaders(
                    headersRaw?.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries),
                    out var headers,
                    out var headerParsingErrorMessage))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, DiagnosticSeverity.Error, headerParsingErrorMessage));
                    return;
                }

                var graphQlSchemas = new List<(string TargetFileName, GraphQlSchema Schema)>();
                if (isSchemaFileSpecified)
                {
                    foreach (var schemaFile in graphQlSchemaFiles)
                    {
                        var targetFileName = Path.GetFileNameWithoutExtension(schemaFile.Path) + ".cs";
                        graphQlSchemas.Add((targetFileName, GraphQlGenerator.DeserializeGraphQlSchema(schemaFile.GetText().ToString())));
                    }
                }
                else
                {
                    graphQlSchemas.Add((GraphQlClientDefaultFileName, GraphQlGenerator.RetrieveSchema(serviceUrl, headers).GetAwaiter().GetResult()));
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DescriptorInfo,
                            Location.None,
                            "GraphQl schema fetched successfully from " + serviceUrl));
                }

                var generator = new GraphQlGenerator(configuration);

                foreach (var (targetFileName, schema) in graphQlSchemas)
                {
                    var builder = new StringBuilder();
                    using (var writer = new StringWriter(builder))
                        generator.WriteFullClientCSharpFile(schema, @namespace, writer);

                    context.AddSource(targetFileName, SourceText.From(builder.ToString(), Encoding.UTF8));
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

        private static DiagnosticDescriptor CreateDiagnosticDescriptor(DiagnosticSeverity severity, int code) =>
            new DiagnosticDescriptor(
                ApplicationCode + code,
                severity + " " + ApplicationCode + code,
                "{0}",
                "GraphQlClientGenerator",
                severity,
                true);
    }
}