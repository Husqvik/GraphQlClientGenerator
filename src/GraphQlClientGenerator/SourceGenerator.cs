using System;
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
        private const string GraphQlClientFileName = "GraphQlClient.cs";

        private static readonly DiagnosticDescriptor DescriptorParameterError =
            new DiagnosticDescriptor(
                "GRAPHQLGEN1000",
                "error GRAPHQLGEN1000",
                "{0}",
                "GraphQlClientGenerator",
                DiagnosticSeverity.Error,
                true);

        private static readonly DiagnosticDescriptor DescriptorGenerationError =
            new DiagnosticDescriptor(
                "GRAPHQLGEN1001",
                "error GRAPHQLGEN1001",
                "{0}",
                "GraphQlClientGenerator",
                DiagnosticSeverity.Error,
                true);

        private static readonly DiagnosticDescriptor DescriptorInfo =
            new DiagnosticDescriptor(
                "GRAPHQLGEN3000",
                "info GRAPHQLGEN1001",
                "{0}",
                "GraphQlClientGenerator",
                DiagnosticSeverity.Info,
                true);

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
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_ServiceUrl", out var serviceUrl);
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_SchemaFileName", out var schemaFileName);
                var isServiceUrlMissing = String.IsNullOrWhiteSpace(serviceUrl);
                if (isServiceUrlMissing && String.IsNullOrWhiteSpace(schemaFileName))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DescriptorParameterError,
                            Location.None,
                            "Either \"GraphQlClientGenerator_ServiceUrl\" or \"GraphQlClientGenerator_SchemaFileName\" parameter must be specified. "));

                    return;
                }

                if (!isServiceUrlMissing && !String.IsNullOrWhiteSpace(schemaFileName))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DescriptorParameterError,
                            Location.None,
                            DiagnosticSeverity.Error,
                            "\"GraphQlClientGenerator_ServiceUrl\" and \"GraphQlClientGenerator_SchemaFileName\" parameters are mutually exclusive. "));

                    return;
                }

                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_Namespace", out var @namespace);
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

                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_ClassPrefix", out var classPrefix);
                configuration.ClassPrefix = classPrefix;

                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_ClassSuffix", out var classSuffix);
                configuration.ClassSuffix = classSuffix;

                if (compilation.LanguageVersion >= LanguageVersion.CSharp6)
                    configuration.CSharpVersion = compilation.Options.NullableContextOptions == NullableContextOptions.Disable ? CSharpVersion.Newest : CSharpVersion.NewestWithNullableReferences;

                var currentParameterName = "IncludeDeprecatedFields";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_" + currentParameterName, out var includeDeprecatedFieldsRaw);
                configuration.IncludeDeprecatedFields = !String.IsNullOrWhiteSpace(includeDeprecatedFieldsRaw) && Convert.ToBoolean(includeDeprecatedFieldsRaw);

                currentParameterName = "CommentGeneration";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_" + currentParameterName, out var commentGenerationRaw);
                configuration.CommentGeneration =
                    String.IsNullOrWhiteSpace(commentGenerationRaw)
                        ? CommentGenerationOption.CodeSummary
                        : (CommentGenerationOption)Enum.Parse(typeof(CommentGenerationOption), commentGenerationRaw, true);

                currentParameterName = "FloatTypeMapping";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_" + currentParameterName, out var floatTypeMappingRaw);
                configuration.FloatTypeMapping = String.IsNullOrWhiteSpace(floatTypeMappingRaw) ? FloatTypeMapping.Decimal : (FloatTypeMapping)Enum.Parse(typeof(FloatTypeMapping), floatTypeMappingRaw, true);

                currentParameterName = "BooleanTypeMapping";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_" + currentParameterName, out var booleanTypeMappingRaw);
                configuration.BooleanTypeMapping = String.IsNullOrWhiteSpace(booleanTypeMappingRaw) ? BooleanTypeMapping.Boolean : (BooleanTypeMapping)Enum.Parse(typeof(BooleanTypeMapping), booleanTypeMappingRaw, true);

                currentParameterName = "IdTypeMapping";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_" + currentParameterName, out var idTypeMappingRaw);
                configuration.IdTypeMapping = String.IsNullOrWhiteSpace(idTypeMappingRaw) ? IdTypeMapping.Guid : (IdTypeMapping)Enum.Parse(typeof(IdTypeMapping), idTypeMappingRaw, true);

                currentParameterName = "JsonPropertyGeneration";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_" + currentParameterName, out var jsonPropertyGenerationRaw);
                configuration.JsonPropertyGeneration =
                    String.IsNullOrWhiteSpace(jsonPropertyGenerationRaw)
                        ? JsonPropertyGenerationOption.CaseInsensitive
                        : (JsonPropertyGenerationOption)Enum.Parse(typeof(JsonPropertyGenerationOption), jsonPropertyGenerationRaw, true);

                currentParameterName = "CustomClassMapping";
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_" + currentParameterName, out var customClassMappingRaw);
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
                context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GraphQlClientGenerator_" + currentParameterName, out var headersRaw);
                if (!KeyValueParameterParser.TryGetCustomHeaders(
                    headersRaw?.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries),
                    out var headers,
                    out var headerParsingErrorMessage))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DescriptorParameterError, Location.None, DiagnosticSeverity.Error, headerParsingErrorMessage));
                    return;
                }

                var schema = GraphQlGenerator.RetrieveSchema(serviceUrl, headers).GetAwaiter().GetResult();
                var generator = new GraphQlGenerator(configuration);
                var builder = new StringBuilder();
                using (var writer = new StringWriter(builder))
                    generator.WriteFullClientCSharpFile(schema, @namespace, writer);

                context.AddSource(GraphQlClientFileName, SourceText.From(builder.ToString(), Encoding.UTF8));

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
    }
}