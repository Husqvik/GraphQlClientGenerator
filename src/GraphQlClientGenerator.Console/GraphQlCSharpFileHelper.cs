using System.IO;
using System.Threading.Tasks;

namespace GraphQlClientGenerator.Console
{
    internal static class GraphQlCSharpFileHelper
    {
        public static async Task<FileInfo> GenerateClientCSharpFile(ProgramOptions options)
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
            await File.WriteAllTextAsync(options.OutputFileName, generator.GenerateFullClientCSharpFile(schema, options.Namespace));
            return new FileInfo(options.OutputFileName);
        }
    }
}