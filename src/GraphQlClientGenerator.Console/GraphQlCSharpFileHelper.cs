using System.IO;
using System.Threading.Tasks;

namespace GraphQlClientGenerator.Console
{
    internal static class GraphQlCSharpFileHelper
    {
        public static async Task<FileInfo> GenerateClientCSharpFile(string url, string targetFileName, string @namespace)
        {
            var schema = await GraphQlGenerator.RetrieveSchema(url);
            await File.WriteAllTextAsync(targetFileName, new GraphQlGenerator().GenerateFullClientCSharpFile(schema, @namespace));
            return new FileInfo(targetFileName);
        }
    }
}