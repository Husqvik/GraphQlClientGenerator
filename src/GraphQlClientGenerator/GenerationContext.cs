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
    private readonly HashSet<string> _referencedObjectTypes = new();
    private readonly Dictionary<string, string> _nameCollisionMapping = new();
    private IReadOnlyDictionary<string, GraphQlType> _complexTypes;

    protected GraphQlGeneratorConfiguration Configuration { get; private set; }

    internal IReadOnlyCollection<string> ReferencedObjectTypes => _referencedObjectTypes;

    internal IReadOnlyDictionary<string, string> NameCollisionMapping => _nameCollisionMapping;

    public GraphQlSchema Schema { get; }

    public GeneratedObjectType ObjectTypes { get; }

    public virtual byte Indentation { get; }

    protected internal abstract TextWriter Writer { get; }


    protected GenerationContext(GraphQlSchema schema, GeneratedObjectType objectTypes, byte indentationSize)
    {
        var optionsInteger = (int)objectTypes;
        if (optionsInteger is < 1 or > 7)
            throw new ArgumentException("invalid value", nameof(objectTypes));

        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        ObjectTypes = objectTypes;
        Indentation = indentationSize;
    }

    public bool FilterDeprecatedFields(GraphQlField field) =>
        !field.IsDeprecated || Configuration.IncludeDeprecatedFields;

    public virtual void BeforeGeneration(GraphQlGeneratorConfiguration configuration)
    {
        Configuration = configuration;
        _nameCollisionMapping.Clear();
        _referencedObjectTypes.Clear();
        _complexTypes = Schema.GetComplexTypes().ToDictionary(t => t.Name);
        ResolveNameCollisions();
    }

    public abstract void BeforeBaseClassGeneration();

    public abstract void AfterBaseClassGeneration();

    public abstract void BeforeGraphQlTypeNameGeneration();

    public abstract void AfterGraphQlTypeNameGeneration();

    public abstract void BeforeEnumsGeneration();

    public abstract void BeforeEnumGeneration(ObjectGenerationContext context);

    public abstract void AfterEnumGeneration(ObjectGenerationContext context);

    public abstract void AfterEnumsGeneration();

    public abstract void BeforeDirectivesGeneration();

    public abstract void BeforeDirectiveGeneration(string className);

    public abstract void AfterDirectiveGeneration(string className);

    public abstract void AfterDirectivesGeneration();

    public abstract void BeforeQueryBuildersGeneration();

    public abstract void BeforeQueryBuilderGeneration(ObjectGenerationContext context);

    public abstract void AfterQueryBuilderGeneration(ObjectGenerationContext context);

    public abstract void AfterQueryBuildersGeneration();

    public abstract void BeforeInputClassesGeneration();

    public abstract void AfterInputClassesGeneration();

    public abstract void BeforeDataClassesGeneration();

    public abstract void BeforeDataClassGeneration(ObjectGenerationContext context);

    public abstract void AfterDataClassGeneration(ObjectGenerationContext context);

    public abstract void AfterDataClassesGeneration();

    public abstract void AfterGeneration();

    protected internal List<GraphQlField> GetFieldsToGenerate(GraphQlType type)
    {
        var typeFields = type.Fields;
        if (type.Kind == GraphQlTypeKind.Union)
        {
            var unionFields = new List<GraphQlField>();
            var unionFieldNames = new HashSet<string>();
            foreach (var possibleType in type.PossibleTypes)
                if (_complexTypes.TryGetValue(possibleType.Name, out var consistOfType) && consistOfType.Fields is not null)
                    unionFields.AddRange(consistOfType.Fields.Where(f => unionFieldNames.Add(f.Name)));

            typeFields = unionFields;
        }

        return typeFields?.Where(FilterDeprecatedFields).ToList();
    }

    protected internal IEnumerable<GraphQlField> GetFragments(GraphQlType type)
    {
        var fragments = new List<GraphQlField>();
        if (type.Kind != GraphQlTypeKind.Union && type.Kind != GraphQlTypeKind.Interface)
            return fragments;

        foreach (var possibleType in type.PossibleTypes)
            if (_complexTypes.TryGetValue(possibleType.Name, out var consistOfType) && consistOfType.Fields is not null)
                fragments.Add(
                    new GraphQlField
                    {
                        Name = consistOfType.Name,
                        Description = consistOfType.Description,
                        Type =
                            new GraphQlFieldType
                            {
                                Name = consistOfType.Name,
                                Kind = consistOfType.Kind
                            }
                    });

        return fragments;
    }

    private void ResolveNameCollisions()
    {
        var complexTypeCsharpNames = new HashSet<string>(_complexTypes.Keys.Select(NamingHelper.ToPascalCase));
        var inputObjectTypes = new HashSet<string>(Schema.GetInputObjectTypes().Select(t => NamingHelper.ToPascalCase(t.Name)));

        foreach (var graphQlType in Schema.Types.Where(t => !t.IsBuiltIn()))
        {
            var isInputObject = graphQlType.Kind == GraphQlTypeKind.InputObject;
            var propertyNamesToGenerate = new List<string>();
            if (isInputObject)
            {
                FindAllReferencedObjectTypes(Schema, graphQlType, _referencedObjectTypes);
                propertyNamesToGenerate.AddRange(graphQlType.InputFields.Select(f => NamingHelper.ToPascalCase(f.Name)));
            }
            else if (graphQlType.Kind.IsComplex())
                propertyNamesToGenerate.AddRange(GetFieldsToGenerate(graphQlType).Select(f => NamingHelper.ToPascalCase(f.Name)));
            else
                continue;

            var candidateClassName = NamingHelper.ToPascalCase(graphQlType.Name); ;
            var finalClassName = candidateClassName;
            var collisionIteration = 1;

            do
            {
                var finalClassNameIncludingPrefixAndSuffix = $"{Configuration.ClassPrefix}{finalClassName}{Configuration.ClassSuffix}";
                var hasNameCollision = propertyNamesToGenerate.Any(n => n == finalClassNameIncludingPrefixAndSuffix);
                if (candidateClassName != finalClassName)
                    hasNameCollision |= complexTypeCsharpNames.Contains(finalClassName) || inputObjectTypes.Contains(finalClassName);

                if (!hasNameCollision)
                    break;

                if (collisionIteration == 1)
                {
                    if (isInputObject && !candidateClassName.EndsWith("Input"))
                        finalClassName = $"{candidateClassName}Input";
                    if (isInputObject && !candidateClassName.EndsWith("InputObject"))
                        finalClassName = $"{candidateClassName}InputObject";
                    else if (!candidateClassName.EndsWith("Data"))
                        finalClassName = $"{candidateClassName}Data";
                    else if (!candidateClassName.EndsWith("Record"))
                        finalClassName = $"{candidateClassName}Record";
                    else if (!candidateClassName.EndsWith("Data") && !candidateClassName.EndsWith("DataRecord"))
                        finalClassName = $"{candidateClassName}DataRecord";

                    collisionIteration++;
                }
                else
                    finalClassName = $"{candidateClassName}{collisionIteration++}";
            } while (true);

            if (finalClassName != candidateClassName)
                _nameCollisionMapping.Add(graphQlType.Name, finalClassName);
        }
    }

    private static void FindAllReferencedObjectTypes(GraphQlSchema schema, GraphQlType type, ISet<string> objectTypes)
    {
        foreach (var member in (IEnumerable<IGraphQlMember>)type.InputFields ?? type.Fields)
        {
            var unwrappedType = member.Type.UnwrapIfNonNull();
            GraphQlType memberType;
            switch (unwrappedType.Kind)
            {
                case GraphQlTypeKind.Object:
                    objectTypes.Add(unwrappedType.Name);
                    memberType = schema.Types.Single(t => t.Name == unwrappedType.Name);
                    FindAllReferencedObjectTypes(schema, memberType, objectTypes);
                    break;

                case GraphQlTypeKind.List:
                    var itemType = unwrappedType.OfType.UnwrapIfNonNull();
                    if (itemType.Kind.IsComplex())
                    {
                        memberType = schema.Types.Single(t => t.Name == itemType.Name);
                        FindAllReferencedObjectTypes(schema, memberType, objectTypes);
                    }

                    break;
            }
        }
    }
}

public struct ObjectGenerationContext
{
    public GraphQlType GraphQlType { get; set; }
    public string CSharpTypeName { get; set; }
}