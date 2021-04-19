using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GraphQlClientGenerator.Console
{
    internal static class GraphQlCSharpFileHelper
    {
        public static async Task<IReadOnlyCollection<FileInfo>> GenerateClientSourceCode(ProgramOptions options)
        {
            var isServiceUrlMissing = String.IsNullOrWhiteSpace(options.ServiceUrl);
            if (isServiceUrlMissing && String.IsNullOrWhiteSpace(options.SchemaFileName))
            {
                System.Console.WriteLine("ERROR: Either \"serviceUrl\" or \"schemaFileName\" parameter must be specified. ");
                Environment.Exit(4);
            }

            if (!isServiceUrlMissing && !String.IsNullOrWhiteSpace(options.SchemaFileName))
            {
                System.Console.WriteLine("ERROR: \"serviceUrl\" and \"schemaFileName\" parameters are mutually exclusive. ");
                Environment.Exit(5);
            }

            GraphQlSchema schema;
            if (isServiceUrlMissing)
                schema = GraphQlGenerator.DeserializeGraphQlSchema(await File.ReadAllTextAsync(options.SchemaFileName));
            else
            {
                if (!KeyValueParameterParser.TryGetCustomHeaders(options.Headers, out var headers, out var headerParsingErrorMessage))
                {
                    System.Console.WriteLine("ERROR: " + headerParsingErrorMessage);
                    Environment.Exit(3);
                }

                schema = await GraphQlGenerator.RetrieveSchema(options.ServiceUrl, options.HttpGet, headers);
            }
            
            var generatorConfiguration =
                new GraphQlGeneratorConfiguration
                {
                    CSharpVersion = options.CSharpVersion,
                    ClassPrefix = options.ClassPrefix,
                    ClassSuffix = options.ClassSuffix,
                    GeneratePartialClasses = options.PartialClasses,
                    MemberAccessibility = options.MemberAccessibility,
                    IdTypeMapping = options.IdTypeMapping,
                    FloatTypeMapping = options.FloatTypeMapping,
                    JsonPropertyGeneration = options.JsonPropertyAttribute
                };

            if (!KeyValueParameterParser.TryGetCustomClassMapping(options.ClassMapping, out var customMapping, out var customMappingParsingErrorMessage))
            {
                System.Console.WriteLine("ERROR: " + customMappingParsingErrorMessage);
                Environment.Exit(3);
            }

            foreach (var kvp in customMapping)
                generatorConfiguration.CustomClassNameMapping.Add(kvp);
            
            var generator = new GraphQlGenerator(generatorConfiguration);

            if (options.OutputType == OutputType.SingleFile)
            {
                await File.WriteAllTextAsync(options.OutputPath, generator.GenerateFullClientCSharpFile(schema, options.Namespace));
                return new[] { new FileInfo(options.OutputPath) };
            }

            var multipleFileGenerationContext = new MultipleFileGenerationContext(schema, options.OutputPath, options.Namespace);
            generator.Generate(multipleFileGenerationContext);
            return multipleFileGenerationContext.Files;
        }
    }
}