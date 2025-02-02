using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GraphQlClientGenerator.Test;

internal static class CompilationHelper
{
    public static CSharpCompilation CreateCompilation(string sourceCode, string assemblyName, NullableContextOptions nullableContextOptions = NullableContextOptions.Disable)
    {
        var syntaxTree =
            SyntaxFactory.ParseSyntaxTree(
                $$"""
                {{GraphQlGenerator.RequiredNamespaces}}

                namespace {{assemblyName}}
                {
                {{sourceCode}}
                }
                """,
                CSharpParseOptions.Default.WithLanguageVersion(Enum.GetValues(typeof(LanguageVersion)).Cast<LanguageVersion>().Max()));

        var compilationOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullableContextOptions)
                .WithPlatform(Platform.AnyCpu)
                .WithOverflowChecks(true)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithSpecificDiagnosticOptions(
                    new Dictionary<string, ReportDiagnostic>
                    {
                        { "CS1701", ReportDiagnostic.Suppress }
                    });

        var systemReference = MetadataReference.CreateFromFile(typeof(DateTimeOffset).Assembly.Location);
        var systemObjectModelReference = MetadataReference.CreateFromFile(Assembly.Load("System.ObjectModel").Location);
        var systemTextRegularExpressionsReference = MetadataReference.CreateFromFile(Assembly.Load("System.Text.RegularExpressions").Location);
        var systemRuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);
        var systemCollectionsReference = MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location);
        var systemGlobalizationReference = MetadataReference.CreateFromFile(Assembly.Load("System.Globalization").Location);
        var systemRuntimeExtensionsReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.Extensions").Location);
        var netStandardReference = MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location);
        var linqReference = MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location);
        var linqExpressionsReference = MetadataReference.CreateFromFile(Assembly.Load("System.Linq.Expressions").Location);
        var systemDynamicRuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Dynamic.Runtime").Location);
        var systemIoReference = MetadataReference.CreateFromFile(Assembly.Load("System.IO").Location);
        var jsonNetReference = MetadataReference.CreateFromFile(Assembly.Load("Newtonsoft.Json").Location);
        var runtimeSerializationReference = MetadataReference.CreateFromFile(typeof(EnumMemberAttribute).Assembly.Location);
        var componentModelReference = MetadataReference.CreateFromFile(typeof(DescriptionAttribute).Assembly.Location);
        var componentModelTypeConverterReference = MetadataReference.CreateFromFile(Assembly.Load("System.ComponentModel.TypeConverter").Location);

        return
            CSharpCompilation.Create(
                assemblyName,
                [syntaxTree],
                [
                    systemReference,
                    systemIoReference,
                    systemDynamicRuntimeReference,
                    runtimeSerializationReference,
                    systemObjectModelReference,
                    systemTextRegularExpressionsReference,
                    componentModelReference,
                    componentModelTypeConverterReference,
                    systemRuntimeReference,
                    systemRuntimeExtensionsReference,
                    systemCollectionsReference,
                    systemGlobalizationReference,
                    jsonNetReference,
                    linqReference,
                    linqExpressionsReference,
                    netStandardReference
                ],
                compilationOptions);

    }
}