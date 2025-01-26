﻿namespace GraphQlClientGenerator;

public class SingleFileGenerationContext(GraphQlSchema schema, TextWriter writer, GeneratedObjectType objectTypes = GeneratedObjectType.All)
    : GenerationContext(schema, objectTypes)
{
    private bool _isNullableReferenceScopeEnabled;
    private int _enums;
    private int _directives;
    private int _queryBuilders;
    private int _dataClasses;

    public override byte IndentationSize => (byte)(Configuration.FileScopedNamespaces ? 0 : 4);

    protected internal override TextWriter Writer { get; } = writer ?? throw new ArgumentNullException(nameof(writer));

    public override void BeforeGeneration()
    {
        _enums = _directives = _queryBuilders = _dataClasses = 0;

        Writer.WriteLine(GraphQlGenerator.AutoGeneratedLabel);
        Writer.WriteLine();
        Writer.WriteLine(GraphQlGenerator.RequiredNamespaces);
        Writer.Write("namespace ");
        Writer.Write(Configuration.TargetNamespace);

        if (Configuration.FileScopedNamespaces)
        {
            Writer.WriteLine(";");
            Writer.WriteLine();
        }
        else
        {
            Writer.WriteLine();
            Writer.WriteLine("{");
        }
    }

    public override void BeforeBaseClassGeneration() => WriteLine("#region base classes");

    public override void AfterBaseClassGeneration()
    {
        WriteLine("#endregion");
        Writer.WriteLine();
    }

    public override void BeforeGraphQlTypeNameGeneration() => WriteLine("#region GraphQL type helpers");

    public override void AfterGraphQlTypeNameGeneration()
    {
        WriteLine("#endregion");
        Writer.WriteLine();
    }

    public override void BeforeEnumsGeneration() => WriteLine("#region enums");

    public override void BeforeEnumGeneration(ObjectGenerationContext context)
    {
        if (_enums > 0)
            Writer.WriteLine();
    }

    public override void AfterEnumGeneration(ObjectGenerationContext context) => _enums++;

    public override void AfterEnumsGeneration()
    {
        WriteLine("#endregion");
        Writer.WriteLine();
    }

    public override void BeforeDirectivesGeneration()
    {
        EnterNullableReferenceScope();
        WriteLine("#region directives");
    }

    public override void BeforeDirectiveGeneration(string className)
    {
        if (_directives > 0)
            Writer.WriteLine();
    }

    public override void AfterDirectiveGeneration(string className) => _directives++;

    public override void AfterDirectivesGeneration()
    {
        WriteLine("#endregion");
        Writer.WriteLine();
    }

    public override void BeforeQueryBuildersGeneration()
    {
        EnterNullableReferenceScope();
        WriteLine("#region builder classes");
    }

    public override void BeforeQueryBuilderGeneration(ObjectGenerationContext context)
    {
        if (_queryBuilders > 0)
            Writer.WriteLine();
    }

    public override void AfterQueryBuilderGeneration(ObjectGenerationContext context) => _queryBuilders++;

    public override void AfterQueryBuildersGeneration()
    {
        WriteLine("#endregion");
        Writer.WriteLine();
    }

    public override void BeforeInputClassesGeneration()
    {
        EnterNullableReferenceScope();
        WriteLine("#region input classes");
    }

    public override void AfterInputClassesGeneration()
    {
        WriteLine("#endregion");
        Writer.WriteLine();
    }

    public override void BeforeDataClassesGeneration()
    {
        _dataClasses = 0;
        EnterNullableReferenceScope();
        WriteLine("#region data classes");
    }

    public override void BeforeDataClassGeneration(ObjectGenerationContext context)
    {
        if (_dataClasses > 0)
            Writer.WriteLine();
    }

    public override void OnDataClassConstructorGeneration(ObjectGenerationContext context)
    {
    }

    public override void AfterDataClassGeneration(ObjectGenerationContext context) => _dataClasses++;

    public override void AfterDataClassesGeneration() => WriteLine("#endregion");

    public override void BeforeDataPropertyGeneration(PropertyGenerationContext context)
    {
    }

    public override void AfterDataPropertyGeneration(PropertyGenerationContext context)
    {
    }

    public override void AfterGeneration()
    {
        ExitNullableReferenceScope();

        if (!Configuration.FileScopedNamespaces)
            Writer.WriteLine("}");
    }

    private void EnterNullableReferenceScope()
    {
        if (_isNullableReferenceScopeEnabled || !Configuration.EnableNullableReferences)
            return;

        WriteLine("#nullable enable");
        _isNullableReferenceScopeEnabled = true;
    }

    private void ExitNullableReferenceScope()
    {
        if (!_isNullableReferenceScopeEnabled)
            return;

        WriteLine("#nullable restore");
        _isNullableReferenceScopeEnabled = false;
    }

    private void WriteLine(string text)
    {
        Writer.Write(GraphQlGenerator.GetIndentation(IndentationSize));
        Writer.WriteLine(text);
    }
}