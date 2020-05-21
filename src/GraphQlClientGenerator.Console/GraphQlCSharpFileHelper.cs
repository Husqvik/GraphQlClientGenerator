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
                    MemberAccessibility = options.MemberAccessibility
                };
            
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