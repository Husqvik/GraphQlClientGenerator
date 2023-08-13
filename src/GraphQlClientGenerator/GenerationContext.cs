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

    public void Initialize(GraphQlGeneratorConfiguration configuration)
    {
        Configuration = configuration;
        _nameCollisionMapping.Clear();
        _referencedObjectTypes.Clear();
        _complexTypes = Schema.GetComplexTypes().ToDictionary(t => t.Name);
        ResolveNameCollisions();
    }

    public abstract void BeforeGeneration();

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
        if (type.Kind != GraphQlTypeKind.Union && type.Kind != GraphQlTypeKind.Interface)
            return Enumerable.Empty<GraphQlField>();

        var fragments = new Dictionary<string, GraphQlField>();
        foreach (var possibleType in type.PossibleTypes)
            if (_complexTypes.TryGetValue(possibleType.Name, out var consistOfType) && consistOfType.Fields is not null)
                fragments[possibleType.Name] =
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
                    };

        return fragments.Values;
    }

    protected internal ScalarFieldTypeDescription GetDataPropertyType(GraphQlType baseType, IGraphQlMember member)
    {
        var fieldType = member.Type.UnwrapIfNonNull();

        switch (fieldType.Kind)
        {
            case GraphQlTypeKind.Object:
            case GraphQlTypeKind.Interface:
            case GraphQlTypeKind.Union:
            case GraphQlTypeKind.InputObject:
                var fieldTypeName = GetCSharpClassName(fieldType.Name);
                var propertyType = $"{Configuration.ClassPrefix}{fieldTypeName}{Configuration.ClassSuffix}";
                if (fieldType.Kind == GraphQlTypeKind.Interface)
                    propertyType = $"I{propertyType}";

                return ScalarFieldTypeDescription.FromNetTypeName(AddQuestionMarkIfNullableReferencesEnabled(propertyType));

            case GraphQlTypeKind.Enum:
                return Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, member.Type, member.Name);

            case GraphQlTypeKind.List:
                var itemType = GraphQlGenerator.UnwrapListItemType(fieldType, Configuration.CSharpVersion == CSharpVersion.NewestWithNullableReferences, out var netCollectionOpenType);
                var unwrappedItemType = itemType?.UnwrapIfNonNull() ?? throw GraphQlGenerator.ListItemTypeResolutionFailedException(baseType.Name, fieldType.Name);
                var itemTypeName = GetCSharpClassName(unwrappedItemType.Name);
                var netItemType =
                    IsUnknownObjectScalar(baseType, member.Name, itemType)
                        ? "object"
                        : $"{(unwrappedItemType.Kind == GraphQlTypeKind.Interface ? "I" : null)}{Configuration.ClassPrefix}{itemTypeName}{Configuration.ClassSuffix}";

                var suggestedScalarNetType = ResolveScalarNetType(baseType, member.Name, itemType).NetTypeName.TrimEnd('?');
                if (!String.Equals(suggestedScalarNetType, "object") && !suggestedScalarNetType.TrimEnd().EndsWith("System.Object"))
                    netItemType = suggestedScalarNetType;

                if (itemType.Kind != GraphQlTypeKind.NonNull)
                    netItemType = AddQuestionMarkIfNullableReferencesEnabled(netItemType);

                var netCollectionType = String.Format(netCollectionOpenType, netItemType);
                return ScalarFieldTypeDescription.FromNetTypeName(AddQuestionMarkIfNullableReferencesEnabled(netCollectionType));

            case GraphQlTypeKind.Scalar:
                return ResolveScalarNetType(baseType, member.Name, member.Type);

            default:
                return ScalarFieldTypeDescription.FromNetTypeName(AddQuestionMarkIfNullableReferencesEnabled("string"));
        }
    }

    private string AddQuestionMarkIfNullableReferencesEnabled(string dataTypeIdentifier) =>
        GraphQlGenerator.AddQuestionMarkIfNullableReferencesEnabled(Configuration, dataTypeIdentifier);

    internal bool IsUnknownObjectScalar(GraphQlType baseType, string valueName, GraphQlFieldType fieldType)
    {
        if (fieldType.UnwrapIfNonNull().Kind != GraphQlTypeKind.Scalar)
            return false;

        var netType = ResolveScalarNetType(baseType, valueName, fieldType).NetTypeName;
        return netType == "object" || netType.EndsWith("System.Object") || netType == "object?" || netType.EndsWith("System.Object?");
    }

    internal string GetCSharpClassName(string graphQlName, bool applyNameCollisionMapping = true)
    {
        var csharpClassName = graphQlName;
        if (UseCustomClassNameIfDefined(ref csharpClassName))
            return csharpClassName;

        return applyNameCollisionMapping && _nameCollisionMapping.TryGetValue(graphQlName, out csharpClassName) ? csharpClassName : NamingHelper.ToPascalCase(graphQlName);
    }

    private bool UseCustomClassNameIfDefined(ref string typeName)
    {
        if (!Configuration.CustomClassNameMapping.TryGetValue(typeName, out var customTypeName))
            return false;

        CSharpHelper.ValidateClassName(customTypeName);
        typeName = customTypeName;
        return true;
    }

    internal ScalarFieldTypeDescription ResolveScalarNetType(GraphQlType baseType, string valueName, GraphQlFieldType valueType) =>
        valueType.UnwrapIfNonNull().Name switch
        {
            GraphQlTypeBase.GraphQlTypeScalarInteger => GetIntegerNetType(baseType, valueType, valueName),
            GraphQlTypeBase.GraphQlTypeScalarString => GetCustomScalarNetType(baseType, valueType, valueName),
            GraphQlTypeBase.GraphQlTypeScalarFloat => GetFloatNetType(baseType, valueType, valueName),
            GraphQlTypeBase.GraphQlTypeScalarBoolean => ScalarFieldTypeDescription.FromNetTypeName(GetBooleanNetType(baseType, valueType, valueName)),
            GraphQlTypeBase.GraphQlTypeScalarId => GetIdNetType(baseType, valueType, valueName),
            _ => GetCustomScalarNetType(baseType, valueType, valueName)
        };

    private string GetBooleanNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName) =>
        Configuration.BooleanTypeMapping switch
        {
            BooleanTypeMapping.Boolean => "bool?",
            BooleanTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName).NetTypeName,
            _ => throw new InvalidOperationException($"\"{Configuration.BooleanTypeMapping}\" not supported")
        };

    private ScalarFieldTypeDescription GetFloatNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName) =>
        Configuration.FloatTypeMapping switch
        {
            FloatTypeMapping.Decimal => ScalarFieldTypeDescription.FromNetTypeName("decimal?"),
            FloatTypeMapping.Float => ScalarFieldTypeDescription.FromNetTypeName("float?"),
            FloatTypeMapping.Double => ScalarFieldTypeDescription.FromNetTypeName("double?"),
            FloatTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName),
            _ => throw new InvalidOperationException($"\"{Configuration.FloatTypeMapping}\" not supported")
        };

    private ScalarFieldTypeDescription GetIntegerNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName) =>
        Configuration.IntegerTypeMapping switch
        {
            IntegerTypeMapping.Int32 => ScalarFieldTypeDescription.FromNetTypeName("int?"),
            IntegerTypeMapping.Int16 => ScalarFieldTypeDescription.FromNetTypeName("short?"),
            IntegerTypeMapping.Int64 => ScalarFieldTypeDescription.FromNetTypeName("long?"),
            IntegerTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName),
            _ => throw new InvalidOperationException($"\"{Configuration.IntegerTypeMapping}\" not supported")
        };

    private ScalarFieldTypeDescription GetIdNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName) =>
        Configuration.IdTypeMapping switch
        {
            IdTypeMapping.String => ScalarFieldTypeDescription.FromNetTypeName(AddQuestionMarkIfNullableReferencesEnabled("string")),
            IdTypeMapping.Guid => ScalarFieldTypeDescription.FromNetTypeName("Guid?"),
            IdTypeMapping.Object => ScalarFieldTypeDescription.FromNetTypeName(AddQuestionMarkIfNullableReferencesEnabled("object")),
            IdTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName),
            _ => throw new InvalidOperationException($"\"{Configuration.IdTypeMapping}\" not supported")
        };

    private ScalarFieldTypeDescription GetCustomScalarNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName)
    {
        if (Configuration.ScalarFieldTypeMappingProvider is null)
            throw new InvalidOperationException($"\"{nameof(Configuration.ScalarFieldTypeMappingProvider)}\" missing");

        var typeDescription = Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName);
        if (String.IsNullOrWhiteSpace(typeDescription.NetTypeName))
            throw new InvalidOperationException($".NET type for \"{baseType.Name}.{valueName}\" ({valueType.Name}) cannot be resolved. Please check {nameof(GraphQlGeneratorConfiguration)}.{nameof(Configuration.ScalarFieldTypeMappingProvider)} implementation. ");

        if (typeDescription.FormatMask is not null && String.IsNullOrWhiteSpace(typeDescription.FormatMask))
            throw new InvalidOperationException("invalid format mask");

        return typeDescription with { NetTypeName = typeDescription.NetTypeName.Replace(" ", String.Empty).Replace("\t", String.Empty) };
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

            var candidateClassName = NamingHelper.ToPascalCase(graphQlType.Name);
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