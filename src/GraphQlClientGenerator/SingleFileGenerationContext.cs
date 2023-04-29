namespace GraphQlClientGenerator;

public class SingleFileGenerationContext : GenerationContext
{
    private readonly string _indentation;

    private bool _isNullableReferenceScopeEnabled;
    private int _enums;
    private int _directives;
    private int _queryBuilders;
    private int _dataClasses;

    public override TextWriter Writer { get; }

    public SingleFileGenerationContext(GraphQlSchema schema, TextWriter writer, GeneratedObjectType objectTypes = GeneratedObjectType.All, byte indentationSize = 0)
        : base(schema, objectTypes, indentationSize)
    {
        Writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _indentation = new String(' ', indentationSize);
    }

    public override void BeforeGeneration(GraphQlGeneratorConfiguration configuration)
    {
        _enums = _directives = _queryBuilders = _dataClasses = 0;
        base.BeforeGeneration(configuration);
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

    public override void AfterDataClassGeneration(ObjectGenerationContext context) => _dataClasses++;

    public override void AfterDataClassesGeneration() => WriteLine("#endregion");

    public override void AfterGeneration() => ExitNullableReferenceScope();

    private void EnterNullableReferenceScope()
    {
        if (_isNullableReferenceScopeEnabled || Configuration.CSharpVersion != CSharpVersion.NewestWithNullableReferences)
            return;

        WriteLine("#nullable enable");
        _isNullableReferenceScopeEnabled = true;
    }

    private void ExitNullableReferenceScope()
    {
        if (!_isNullableReferenceScopeEnabled)
            return;

        Writer.WriteLine("#nullable restore");
        _isNullableReferenceScopeEnabled = false;
    }

    private void WriteLine(string text)
    {
        Writer.Write(_indentation);
        Writer.WriteLine(text);
    }
}