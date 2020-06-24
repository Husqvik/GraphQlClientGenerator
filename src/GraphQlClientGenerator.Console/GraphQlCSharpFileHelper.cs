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
            var schema = await GraphQlGenerator.RetrieveSchema(options.ServiceUrl, options.Authorization);
            var generatorConfiguration =
                new GraphQlGeneratorConfiguration
                {
                    CSharpVersion = options.CSharpVersion,
                    ClassPostfix = options.ClassPostfix,
                    GeneratePartialClasses = options.PartialClasses,
                    MemberAccessibility = options.MemberAccessibility,
                    IdTypeMapping = options.IdTypeMapping,
                    FloatTypeMapping = options.FloatTypeMapping
                };

            foreach (var kvp in GetCustomClassMapping(options.ClassMapping))
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

        private static IEnumerable<KeyValuePair<string, string>> GetCustomClassMapping(IEnumerable<string> sourceParameters)
        {
            foreach (var parameter in sourceParameters)
            {
                var parts = parameter.Split(':');
                if (parts.Length != 2)
                {
                    System.Console.WriteLine("ERROR: 'classMapping' value must have format {GraphQlTypeName}:{C#ClassName}. ");
                    Environment.Exit(3);
                }

                var cSharpClassName = parts[1];
                if (!CSharpHelper.IsValidIdentifier(cSharpClassName))
                {
                    System.Console.WriteLine($"ERROR: '{cSharpClassName}' is not valid C# class name. ");
                    Environment.Exit(3);
                }

                yield return new KeyValuePair<string, string>(parts[0], cSharpClassName);
            }
        }
    }
}