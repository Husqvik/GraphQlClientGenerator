using System;
using System.IO;

namespace GraphQlClientGenerator
{
    public class MultipleFileGenerationContext : GenerationContext
    {
        private const string ProjectTemplate =
            @"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup Condition=""!$(DefineConstants.Contains(" + GraphQlGenerator.PreprocessorDirectiveDisableNewtonsoftJson + @"))"">
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.*"" />
  </ItemGroup>

</Project>
";

        internal const string RequiredNamespaces =
            @"using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
#if!" + GraphQlGenerator.PreprocessorDirectiveDisableNewtonsoftJson + @"
using Newtonsoft.Json;
#endif
";

        private readonly string _outputDirectory;
        private readonly string _namespace;
        private readonly string _projectFileName;

        private TextWriter _currentWriter;
        
        public override byte Indentation { get; } = 4;

        public MultipleFileGenerationContext(
            GraphQlSchema schema,
            string outputDirectory,
            string @namespace,
            string projectFileName = null,
            GeneratedObjectType objectTypes = GeneratedObjectType.DataClasses | GeneratedObjectType.QueryBuilders)
            : base(schema, objectTypes, 4)
        {
            if (!Directory.Exists(outputDirectory))
                throw new ArgumentException($"Directory '{outputDirectory}' does not exist. ", nameof(outputDirectory));

            if (String.IsNullOrWhiteSpace(@namespace))
                throw new ArgumentException("namespace required", nameof(@namespace));

            _outputDirectory = outputDirectory;
            _namespace = @namespace;
            _projectFileName = projectFileName;
        }

        public override TextWriter Writer => _currentWriter;

        public override void BeforeBaseClassGeneration() => InitializeNewSourceCodeFile("BaseClasses", GraphQlGenerator.RequiredNamespaces);

        public override void AfterBaseClassGeneration() => WriteNamespaceEnd();

        public override void BeforeEnumsGeneration()
        {
        }

        public override void BeforeEnumGeneration(string enumName) => InitializeNewSourceCodeFile(enumName);

        public override void AfterEnumGeneration(string enumName) => WriteNamespaceEnd();

        public override void AfterEnumsGeneration()
        {
        }

        public override void BeforeDirectivesGeneration()
        {
        }

        public override void BeforeDirectiveGeneration(string className) => InitializeNewSourceCodeFile(className);

        public override void AfterDirectiveGeneration(string className) => WriteNamespaceEnd();

        public override void AfterDirectivesGeneration()
        {
        }

        public override void BeforeQueryBuildersGeneration()
        {
        }

        public override void BeforeQueryBuilderGeneration(string className) => InitializeNewSourceCodeFile(className);

        public override void AfterQueryBuilderGeneration(string className) => WriteNamespaceEnd();

        public override void AfterQueryBuildersGeneration()
        {
        }

        public override void BeforeInputClassesGeneration()
        {
        }

        public override void AfterInputClassesGeneration()
        {
        }

        public override void BeforeDataClassesGeneration()
        {
        }

        public override void BeforeDataClassGeneration(string className) => InitializeNewSourceCodeFile(className);

        public override void AfterDataClassGeneration(string className) => WriteNamespaceEnd();

        public override void AfterDataClassesGeneration()
        {
        }

        public override void AfterGeneration()
        {
            _currentWriter?.Dispose();
            _currentWriter = null;

            if (!String.IsNullOrEmpty(_projectFileName))
                File.WriteAllText(Path.Combine(_outputDirectory, _projectFileName), ProjectTemplate);
        }

        private void InitializeNewSourceCodeFile(string memberName, string requiredNamespaces = RequiredNamespaces)
        {
            _currentWriter?.Dispose();
            _currentWriter = File.CreateText(Path.Combine(_outputDirectory, memberName + ".cs"));
            _currentWriter.WriteLine(requiredNamespaces);
            _currentWriter.Write("namespace ");
            _currentWriter.WriteLine(_namespace);
            _currentWriter.WriteLine("{");
        }

        private void WriteNamespaceEnd() => _currentWriter.WriteLine("}");
    }
}