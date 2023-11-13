﻿namespace GraphQlClientGenerator;

public class MultipleFileGenerationContext : GenerationContext
{
    private const string ProjectTemplate =
        $"""
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <TargetFramework>netstandard2.0</TargetFramework>
            <LangVersion>latest</LangVersion>
          </PropertyGroup>

          <ItemGroup Condition="!$(DefineConstants.Contains({GraphQlGenerator.PreprocessorDirectiveDisableNewtonsoftJson}))">
            <PackageReference Include="Newtonsoft.Json" Version="13.*" />
          </ItemGroup>

        </Project>

        """;

    private const string RequiredNamespaces =
        $@"using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
#if!{GraphQlGenerator.PreprocessorDirectiveDisableNewtonsoftJson}
using Newtonsoft.Json;
#endif
";

    private readonly List<FileInfo> _files = new();

    private readonly string _outputDirectory;
    private readonly string _namespace;
    private readonly string _projectFileName;

    private string _currentFileName;
    private TextWriter _currentWriter;

    protected internal override TextWriter Writer => _currentWriter;

    public IReadOnlyCollection<FileInfo> Files => _files;

    public MultipleFileGenerationContext(
        GraphQlSchema schema,
        string outputDirectory,
        string @namespace,
        string projectFileName = null,
        GeneratedObjectType objectTypes = GeneratedObjectType.All)
        : base(schema, objectTypes)
    {
        if (!Directory.Exists(outputDirectory))
            throw new ArgumentException($"Directory \"{outputDirectory}\" does not exist. ", nameof(outputDirectory));

        if (String.IsNullOrWhiteSpace(@namespace))
            throw new ArgumentException("namespace required", nameof(@namespace));

        _outputDirectory = outputDirectory;
        _namespace = @namespace;
        _projectFileName = projectFileName;
    }

    public override void BeforeGeneration() => _files.Clear();

    public override void BeforeBaseClassGeneration() => InitializeNewSourceCodeFile("BaseClasses", GraphQlGenerator.RequiredNamespaces);

    public override void AfterBaseClassGeneration() => WriteNamespaceEnd();

    public override void BeforeGraphQlTypeNameGeneration() => InitializeNewSourceCodeFile("GraphQlTypes");

    public override void AfterGraphQlTypeNameGeneration() => WriteNamespaceEnd();

    public override void BeforeEnumsGeneration()
    {
    }

    public override void BeforeEnumGeneration(ObjectGenerationContext context) => InitializeNewSourceCodeFile(context.CSharpTypeName);

    public override void AfterEnumGeneration(ObjectGenerationContext context) => WriteNamespaceEnd();

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

    public override void BeforeQueryBuilderGeneration(ObjectGenerationContext context) => InitializeNewSourceCodeFile(context.CSharpTypeName);

    public override void AfterQueryBuilderGeneration(ObjectGenerationContext context) => WriteNamespaceEnd();

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

    public override void BeforeDataClassGeneration(ObjectGenerationContext context) => InitializeNewSourceCodeFile(context.CSharpTypeName);

    public override void AfterDataClassGeneration(ObjectGenerationContext context) => WriteNamespaceEnd();

    public override void AfterDataClassesGeneration()
    {
    }

    public override void AfterGeneration()
    {
        _currentWriter?.Dispose();
        _currentWriter = null;

        CollectFileInfo();

        _currentFileName = null;

        if (String.IsNullOrEmpty(_projectFileName))
            return;

        var projectFileName = Path.Combine(_outputDirectory, _projectFileName);
        File.WriteAllText(projectFileName, ProjectTemplate);
        _files.Add(new FileInfo(projectFileName));
    }

    private void InitializeNewSourceCodeFile(string memberName, string requiredNamespaces = RequiredNamespaces)
    {
        if (_currentWriter != null)
        {
            _currentWriter.Dispose();
            CollectFileInfo();
        }

        _currentFileName = Path.Combine(_outputDirectory, $"{memberName}.cs");
        _currentWriter = File.CreateText(_currentFileName);
        _currentWriter.WriteLine(GraphQlGenerator.AutoGeneratedLabel);
        _currentWriter.WriteLine();
        _currentWriter.WriteLine(requiredNamespaces);
        WriteNamespaceStart(_namespace);
    }

    private void CollectFileInfo() => _files.Add(new FileInfo(_currentFileName));
}