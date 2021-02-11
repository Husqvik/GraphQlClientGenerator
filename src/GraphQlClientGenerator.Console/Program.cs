using System;
using System.Threading.Tasks;
using CommandLine;
using GraphQlClientGenerator.Console;

if (TryParseArguments(args, out var options))
    await GenerateGraphQlClientSourceCode(options);
else
    Environment.Exit(1);

static bool TryParseArguments(string[] args, out ProgramOptions options)
{
    options = null;

    var parserResult = Parser.Default.ParseArguments<ProgramOptions>(args);
    if (parserResult.Tag == ParserResultType.NotParsed)
        return false;

    options = parserResult.MapResult(o => o, null);
    return true;
}

static async Task GenerateGraphQlClientSourceCode(ProgramOptions options)
{
    try
    {
        var files = await GraphQlCSharpFileHelper.GenerateClientSourceCode(options);
        foreach (var file in files)
            Console.WriteLine($"File {file.FullName} generated successfully ({file.Length:N0} B). ");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"An error occurred: {exception}");
        Environment.Exit(2);
    }
}