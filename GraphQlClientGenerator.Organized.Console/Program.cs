using System;
using System.Collections;
using System.Collections.Generic;
using CommandLine;

namespace GraphQlClientGenerator.Organized
{
    public class Options
    {
        [Option('u', "url", Required = true, HelpText = "Url of GraphQl Schema Introspection")]
        public string Url { get; set; }
        [Option('n', "namespace", Required = true, HelpText = "Top Level Namespace for generated types")]
        public string TopNamespace { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output path")]
        public string OutputPath { get; set; }

        [Option('m',"multipart", Required = false, HelpText = "Generate Multiple Files (default: false)")]
        public bool GenerateMultipleFiles { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunWithOptions)
                .WithNotParsed(HandleParseErrors);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void RunWithOptions(Options options)
        {
            Console.WriteLine($"Creating GraphQl Queries and Objects for: \n\tUrl:{options.Url}\n\tNamespace: {options.TopNamespace}\n\tOutput: {options.OutputPath}");
            Console.WriteLine($"\tGenerate Multipart Client: {options.GenerateMultipleFiles}");
            GraphQlSchemaProcessor.Start(options);
        }

        static void HandleParseErrors(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                Console.Error.WriteLine($"Error: {error}");
            }
        }
    }
}
