using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GraphQlClientGenerator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("GraphQL C# client generator");
                Console.WriteLine();
                Console.WriteLine("Usage: ");
                Console.WriteLine("GraphQlClientGenerator <GraphQlServiceUrl> <AccessToken> <TargetNamespace> <TargetFileName> <Namespace>");
                return;
            }

            var url = args[0];
            var token = args[1];
            var targetFileName = args[2];
            var @namespace = args[3];

            try
            {
                GenerateGraphQlClient(url, token, targetFileName, @namespace);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error occured: {exception.Message}");
                return;
            }

            Console.WriteLine($"File {targetFileName} generated successfully. ");
        }

        private static void GenerateGraphQlClient(string url, string token, string targetFileName, string @namespace)
        {
            var schema = GraphQlGenerator.RetrieveSchema(url, token).Result;

            var builder = new StringBuilder();

            GraphQlGenerator.GenerateQueryBuilder(schema, builder);

            builder.AppendLine();
            builder.AppendLine();

            GraphQlGenerator.GenerateDataClasses(schema, builder);

            using (var writer = File.CreateText(targetFileName))
            {
                writer.WriteLine("using System;");
                writer.WriteLine("using System.Collections.Generic;");
                writer.WriteLine("using System.Globalization;");
                writer.WriteLine("using System.Linq;");
                writer.WriteLine("using System.Reflection;");
                writer.WriteLine("using System.Runtime.Serialization;");
                writer.WriteLine("using System.Text;");
                writer.WriteLine();

                writer.WriteLine($"namespace {@namespace}");
                writer.WriteLine("{");

                var indentedLines =
                    builder
                        .ToString()
                        .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                        .Select(l => $"    {l}");

                foreach (var line in indentedLines)
                {
                    writer.WriteLine(line);
                }

                writer.WriteLine("}");
            }
        }
    }
}
