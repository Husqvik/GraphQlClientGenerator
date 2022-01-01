namespace GraphQlClientGenerator;

[Flags]
public enum GeneratedObjectType
{
    BaseClasses = 1,
    QueryBuilders = 2,
    DataClasses = 4,
    All = BaseClasses | QueryBuilders | DataClasses
}

public abstract class GenerationContext
{
    public GraphQlSchema Schema { get; }

    public GeneratedObjectType ObjectTypes { get; }

    public virtual byte Indentation { get; }

    public abstract TextWriter Writer { get; }

    protected GraphQlGeneratorConfiguration Configuration { get; private set; }

    protected GenerationContext(GraphQlSchema schema, GeneratedObjectType objectTypes, byte indentationSize)
    {
        var optionsInteger = (int)objectTypes;
        if (optionsInteger is < 1 or > 7)
            throw new ArgumentException("invalid value", nameof(objectTypes));

        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        ObjectTypes = objectTypes;
        Indentation = indentationSize;
    }

    public virtual void BeforeGeneration(GraphQlGeneratorConfiguration configuration) => Configuration = configuration;

    public abstract void BeforeBaseClassGeneration();

    public abstract void AfterBaseClassGeneration();

    public abstract void BeforeGraphQlTypeNameGeneration();

    public abstract void AfterGraphQlTypeNameGeneration();

    public abstract void BeforeEnumsGeneration();

    public abstract void BeforeEnumGeneration(string enumName);

    public abstract void AfterEnumGeneration(string enumName);

    public abstract void AfterEnumsGeneration();

    public abstract void BeforeDirectivesGeneration();

    public abstract void BeforeDirectiveGeneration(string className);

    public abstract void AfterDirectiveGeneration(string className);

    public abstract void AfterDirectivesGeneration();

    public abstract void BeforeQueryBuildersGeneration();

    public abstract void BeforeQueryBuilderGeneration(string className);

    public abstract void AfterQueryBuilderGeneration(string className);

    public abstract void AfterQueryBuildersGeneration();

    public abstract void BeforeInputClassesGeneration();

    public abstract void AfterInputClassesGeneration();

    public abstract void BeforeDataClassesGeneration();

    public abstract void BeforeDataClassGeneration(string className);

    public abstract void AfterDataClassGeneration(string className);

    public abstract void AfterDataClassesGeneration();

    public abstract void AfterGeneration();
}