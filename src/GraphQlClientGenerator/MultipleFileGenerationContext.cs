namespace GraphQlClientGenerator;

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
        $"""
        using System;
        using System.Collections.Generic;
        using System.ComponentModel;
        using System.Globalization;
        using System.Runtime.Serialization;
        #if !{GraphQlGenerator.PreprocessorDirectiveDisableNewtonsoftJson}
        using Newtonsoft.Json;
        #endif

        """;

    private readonly ICodeFileEmitter _codeFileEmitter;
    private readonly string _projectFileName;

    private CodeFile _currentFile;

    protected internal override TextWriter Writer =>
        (_currentFile ?? throw new InvalidOperationException($"\"{nameof(Writer)}\" not initialized")).Writer;

    public override byte IndentationSize => (byte)(Configuration.FileScopedNamespaces ? 0 : 4);

    public MultipleFileGenerationContext(
        GraphQlSchema schema,
        ICodeFileEmitter codeFileEmitter,
        string projectFileName = null,
        GeneratedObjectType objectTypes = GeneratedObjectType.All)
        : base(schema, objectTypes)
    {
        _codeFileEmitter = codeFileEmitter ?? throw new ArgumentNullException(nameof(codeFileEmitter));

        if (projectFileName is not null && !projectFileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Project file name must end with .csproj.", nameof(projectFileName));

        _projectFileName = projectFileName;
    }

    public override void BeforeGeneration()
    {
    }

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

    public override void BeforeDirectiveGeneration(ObjectGenerationContext context) => InitializeNewSourceCodeFile(context.CSharpTypeName);

    public override void AfterDirectiveGeneration(ObjectGenerationContext context) => WriteNamespaceEnd();

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

    public override void OnDataClassConstructorGeneration(ObjectGenerationContext context)
    {
    }

    public override void AfterDataClassGeneration(ObjectGenerationContext context) => WriteNamespaceEnd();

    public override void AfterDataClassesGeneration()
    {
    }

    public override void BeforeDataPropertyGeneration(PropertyGenerationContext context)
    {
    }

    public override void AfterDataPropertyGeneration(PropertyGenerationContext context)
    {
    }

    public override void AfterGeneration()
    {
        CollectCurrentFile();

        if (String.IsNullOrEmpty(_projectFileName))
            return;

        var projectFile = _codeFileEmitter.CreateFile(_projectFileName);
        projectFile.Writer.Write(ProjectTemplate);
        LogFileCreation(_codeFileEmitter.CollectFileInfo(projectFile));
    }

    private void InitializeNewSourceCodeFile(string memberName, string requiredNamespaces = RequiredNamespaces)
    {
        CollectCurrentFile();

        _currentFile = _codeFileEmitter.CreateFile($"{memberName}.cs");

        var writer = _currentFile.Writer;
        writer.WriteLine(GraphQlGenerator.AutoGeneratedLabel);
        writer.WriteLine();
        writer.WriteLine(requiredNamespaces);
        writer.Write("namespace ");
        writer.Write(Configuration.TargetNamespace);

        if (Configuration.FileScopedNamespaces)
        {
            writer.WriteLine(';');
            writer.WriteLine();
        }
        else
        {
            writer.WriteLine();
            writer.WriteLine('{');
        }
    }

    private void WriteNamespaceEnd() => _currentFile.Writer.WriteLine(Configuration.FileScopedNamespaces ? String.Empty : "}");

    private void CollectCurrentFile()
    {
        if (_currentFile is not null)
            LogFileCreation(_codeFileEmitter.CollectFileInfo(_currentFile));

        _currentFile = null;
    }

    private void LogFileCreation(CodeFileInfo fileInfo) =>
        Log($"File {fileInfo.FileName} generated successfully ({fileInfo.Length:N0} B). ");
}

public class FileSystemEmitter : ICodeFileEmitter
{
    private readonly string _outputDirectory;

    public FileSystemEmitter(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
            throw new ArgumentException($"Directory \"{outputDirectory}\" does not exist.", nameof(outputDirectory));

        _outputDirectory = outputDirectory;
    }

    public CodeFile CreateFile(string fileName)
    {
        fileName = Path.Combine(_outputDirectory, fileName);
        return new CodeFile(fileName, File.Create(fileName));
    }

    public CodeFileInfo CollectFileInfo(CodeFile codeFile)
    {
        codeFile.Writer.Flush();
        codeFile.Dispose();

        return
            new CodeFileInfo
            {
                FileName = codeFile.FileName,
                Length = (int)new FileInfo(codeFile.FileName).Length
            };
    }
}
