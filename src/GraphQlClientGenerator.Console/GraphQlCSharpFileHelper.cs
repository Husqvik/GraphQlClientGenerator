using System.IO;
using System.Threading.Tasks;

namespace GraphQlClientGenerator.Console
{
    internal static class GraphQlCSharpFileHelper
    {
        public static async Task<FileInfo> GenerateClientCSharpFile(ProgramOptions options)
        {
            var schema = await GraphQlGenerator.RetrieveSchema(options.ServiceUrl, options.Authorization);
            await File.WriteAllTextAsync(options.OutputFileName, new GraphQlGenerator().GenerateFullClientCSharpFile(schema, options.Namespace));
            return new FileInfo(options.OutputFileName);
        }
    }
}