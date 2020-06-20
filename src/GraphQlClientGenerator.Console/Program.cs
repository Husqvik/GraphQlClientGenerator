using System;
using System.Threading.Tasks;
using CommandLine;

namespace GraphQlClientGenerator.Console
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            if (TryParseArguments(args, out var options))
                await GenerateGraphQlClientSourceCode(options);
            else
                Environment.Exit(1);
        }

        private static bool TryParseArguments(string[] args, out ProgramOptions options)
        {
            options = null;

            var parserResult = Parser.Default.ParseArguments<ProgramOptions>(args);
            if (parserResult.Tag == ParserResultType.NotParsed)
                return false;

            options = parserResult.MapResult(o => o, null);
            return true;
        }

        private static async Task GenerateGraphQlClientSourceCode(ProgramOptions options)
        {
            try
            {
                var files = await GraphQlCSharpFileHelper.GenerateClientSourceCode(options);
                foreach (var file in files)
                    System.Console.WriteLine($"File {file.FullName} generated successfully ({file.Length:N0} B). ");
            }
            catch (Exception exception)
            {
                System.Console.WriteLine($"An error occured:{Environment.NewLine}{exception}");
                Environment.Exit(2);
            }
        }
    }
}
