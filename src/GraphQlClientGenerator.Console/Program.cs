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
                var fileInfo = await GraphQlCSharpFileHelper.GenerateClientCSharpFile(options);
                System.Console.WriteLine($"File {options.OutputFileName} generated successfully ({fileInfo.Length:N0} B). ");
            }
            catch (Exception exception)
            {
                System.Console.WriteLine($"An error occured: {exception.Message}");
                Environment.Exit(2);
            }
        }
    }
}
