using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GraphQlClientGenerator.Organized
{
    public static class GraphQlSchemaProcessor
    {
        public static void Start(Options options)
        {
            if (!options.OutputPath.Exists())
                options.OutputPath.CreateDirectory();
            else
                options.OutputPath.ResetDirectory();
            var schema = GraphQlGenerator.RetrieveSchema(options.Url).Result;
            if (options.GenerateMultipleFiles)
            {
                schema.CreateQueryBuilder(options);
                schema.CreateDataClasses(options);
            }
            else
                GraphQlCSharpFileHelper.GenerateGraphQlClient(options.Url, $"{options.OutputPath}\\GraphQlClient.cs",
                    options.TopNamespace);
        }

        public static void CreateDataClasses(this GraphQlSchema schema, Options options)
        {
            var dataClassesPath = $"{options.OutputPath}\\Models";
            dataClassesPath.CreateDirectory();
            var objectTypes = schema.Types.Where(t => t.Kind == GraphQlGenerator.GraphQlTypeKindObject && !t.Name.StartsWith("__")).ToArray();
            foreach (var graphQlType in objectTypes)
            {
                StringBuilder classBuilder = new StringBuilder();
                GraphQlGenerator.GenerateDataClass(graphQlType, classBuilder);
                classBuilder.AppendLine();
                var filePath = $"{dataClassesPath}\\{graphQlType.Name}.cs";
                var file = filePath.CreateTextFile();
                CreateCsContent(file, $"{options.TopNamespace}.Models", classBuilder.IndentLines());
                file.Flush();
                file.Close();
            }
        }

        public static void CreateQueryBuilder(this GraphQlSchema schema, Options options)
        {
            var queryBuilderPath = $"{options.OutputPath}\\Builders";
            queryBuilderPath.CreateDirectory();
            var filePath = $"{queryBuilderPath}\\QueryQueryBuilder.cs";
            var file = filePath.CreateTextFile();
            var builder = new StringBuilder();

            GraphQlGenerator.GenerateQueryBuilder(schema, builder);

            builder.AppendLine();
            builder.AppendLine();
            var indentedLines =
                builder.IndentLines();
            CreateCsContent(file, $"{options.TopNamespace}.Builders", indentedLines);
            file.Flush();
            file.Close();
        }

        public static void CreateCsContent(StreamWriter writer, string namespaceName, IEnumerable<string> codeLines)
        {
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine("using System.Globalization;");
            writer.WriteLine("using System.Linq;");
            writer.WriteLine("using System.Reflection;");
            writer.WriteLine("using System.Runtime.Serialization;");
            writer.WriteLine("using System.Text;");
            writer.WriteLine();
            writer.WriteLine($"namespace {namespaceName}");
            writer.WriteLine("{");
            foreach (var codeLine in codeLines)
                writer.WriteLine(codeLine);
            writer.WriteLine("}");
        }

        #region [ Directory Extensions ]

        public static bool Exists(this string name)=> Directory.Exists(name);
        public static void CreateDirectory(this string name) => Directory.CreateDirectory(name);
        public static void DeleteDirectory(this string name) => Directory.Delete(name, true);

        public static void ResetDirectory(this string name)
        {
            name.DeleteDirectory();
            name.CreateDirectory();
        }

        #endregion

        #region [ File Extensions ]

        public static StreamWriter CreateTextFile(this string name) => File.CreateText(name);

        #endregion

        #region [ Indentation Extensions ]

        public static IEnumerable<string> IndentLines(this StringBuilder builder) =>
            builder.ToString()
                .Split(new[] {Environment.NewLine}, StringSplitOptions.None)
                .Select(l => $"    {l}");

        #endregion

    }
}
