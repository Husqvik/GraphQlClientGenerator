using System;
using System.Threading.Tasks;

namespace GraphQlClientGenerator.Console
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                PrintHelp();
                return;
            }

            var url = args[0];
            var targetFileName = args[1];
            var @namespace = args[2];
            string token = "";

            if (args.Length > 3)
                token = args[3];

            try
            {
                await GraphQlCSharpFileHelper.GenerateGraphQlClient(url, targetFileName, @namespace, token);
                System.Console.WriteLine($"File {targetFileName} generated successfully. ");
            }
            catch (Exception exception)
            {
                System.Console.WriteLine($"An error occured: {exception.Message}");
            }
        }

        private static void PrintHelp()
        {
            System.Console.WriteLine("GraphQL C# client generator");
            System.Console.WriteLine();
            System.Console.WriteLine("Usage: ");
            System.Console.WriteLine("GraphQlClientGenerator <GraphQlServiceUrl> <TargetFileName> <TargetNamespace> <OptionalToken>");
        }
    }
}
