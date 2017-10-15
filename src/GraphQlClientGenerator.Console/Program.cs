using System;

namespace GraphQlClientGenerator.Console
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                PrintHelp();
                return;
            }

            var url = args[0];
            var token = args[1];
            var targetFileName = args[2];
            var @namespace = args[3];

            try
            {
                GraphQlCSharpFileHelper.GenerateGraphQlClient(url, token, targetFileName, @namespace);
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
            System.Console.WriteLine("GraphQlClientGenerator <GraphQlServiceUrl> <AccessToken> <TargetNamespace> <TargetFileName> <Namespace>");
        }
    }
}
