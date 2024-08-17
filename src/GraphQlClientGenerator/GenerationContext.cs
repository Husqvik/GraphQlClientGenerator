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
    private readonly HashSet<string> _referencedObjectTypes = [];
    private readonly Dictionary<string, GraphQlDirective> _directives = [];
    private readonly Dictionary<string, string> _nameCollisionMapping = [];
    private readonly HashSet<(string GraphQlTypeName, string FieldName)> _typeFieldCovarianceRequired = [];
    private GraphQlGeneratorConfiguration _configuration;
    private IReadOnlyDictionary<string, GraphQlType> _complexTypes;
    private ILookup<string, string> _typeUnionMembership;

    protected GraphQlGeneratorConfiguration Configuration =>
        _configuration ?? throw NotInitializedException(nameof(Configuration));

    protected internal abstract TextWriter Writer { get; }

    internal IReadOnlyCollection<string> ReferencedObjectTypes => _referencedObjectTypes;

    internal IReadOnlyCollection<GraphQlDirective> Directives => _directives.Values;

    internal ILookup<string, string> TypeUnionMembership =>
        _typeUnionMembership ?? throw NotInitializedException(nameof(TypeUnionMembership));

    public virtual byte IndentationSize => 0;

    public GraphQlSchema Schema { get; }

    public GeneratedObjectType ObjectTypes { get; }

    public Action<string> LogMessage { private get; set; }

    protected GenerationContext(GraphQlSchema schema, GeneratedObjectType objectTypes)
    {
        var optionsInteger = (int)objectTypes;
        if (optionsInteger is < 1 or > 7)
            throw new ArgumentOutOfRangeException(nameof(objectTypes), objectTypes, "invalid value");

        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        ObjectTypes = objectTypes;
    }

    protected internal void Initialize(GraphQlGeneratorConfiguration configuration)
    {
        _complexTypes = null;
        _directives.Clear();
        _nameCollisionMapping.Clear();
        _referencedObjectTypes.Clear();
        _typeFieldCovarianceRequired.Clear();
        _complexTypes = Schema.GetComplexTypes().ToDictionary(t => t.Name);
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        ResolveDirectives();
        ResolveTypeUnionLookup();
        ResolveReferencedObjectTypes();
        ResolveNameCollisions();
        ResolveCovarianceRequiredFields();
        Initialize();
    }

    protected virtual void Initialize()
    {
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

    public abstract void BeforeDataPropertyGeneration(PropertyGenerationContext context);

    public abstract void AfterDataPropertyGeneration(PropertyGenerationContext context);

    public abstract void AfterGeneration();

    protected internal List<GraphQlField> GetFieldsToGenerate(GraphQlType type) =>
        type.Fields?.Where(FilterIfDeprecated).Where(FilterIfAllFieldsDeprecated).ToList();

    public bool FilterIfDeprecated(GraphQlEnumValue field) => !field.IsDeprecated || Configuration.IncludeDeprecatedFields;

    private bool FilterIfAllFieldsDeprecated(GraphQlField field)
    {
        var fieldType = GraphQlGenerator.UnwrapIfNotNullOrList(field.Type);
        if (!fieldType.Kind.IsComplex())
            return true;

        var graphQlType = _complexTypes[fieldType.Name];
        var nestedTypeFields =
            graphQlType.Kind is GraphQlTypeKind.Union
                ? graphQlType.PossibleTypes.Select(t => _complexTypes[t.Name]).SelectMany(t => t.Fields)
                : graphQlType.Fields;

        return nestedTypeFields.Any(FilterIfDeprecated);
    }


    protected internal IEnumerable<GraphQlField> GetFragments(GraphQlType type)
    {
        if (type.Kind != GraphQlTypeKind.Union && type.Kind != GraphQlTypeKind.Interface)
            return [];

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
                var propertyType = GetFullyQualifiedNetTypeName(fieldTypeName, fieldType.Kind);
                return ScalarFieldTypeDescription.FromNetTypeName(AddQuestionMarkIfNullableReferencesEnabled(propertyType));

            case GraphQlTypeKind.Enum:
                return Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, member.Type, member.Name);

            case GraphQlTypeKind.List:
                var isCovarianceRequired = _typeFieldCovarianceRequired.Contains((baseType.Name, member.Name));
                var itemType = GraphQlGenerator.UnwrapListItemType(fieldType, Configuration.CSharpVersion == CSharpVersion.NewestWithNullableReferences, isCovarianceRequired, out var netCollectionOpenType);
                var unwrappedItemType = itemType?.UnwrapIfNonNull() ?? throw GraphQlGenerator.ListItemTypeResolutionFailedException(baseType.Name, fieldType.Name);
                var itemTypeName = GetCSharpClassName(unwrappedItemType.Name);
                var netItemType =
                    IsUnknownObjectScalar(baseType, member.Name, itemType)
                        ? "object"
                        : GetFullyQualifiedNetTypeName(itemTypeName, unwrappedItemType.Kind);

                var suggestedScalarNetType = ResolveScalarNetType(baseType, member.Name, itemType, true).NetTypeName.TrimEnd('?');
                if (!String.Equals(suggestedScalarNetType, "object") && !suggestedScalarNetType.TrimEnd().EndsWith("System.Object"))
                    netItemType = suggestedScalarNetType;

                if (itemType.Kind != GraphQlTypeKind.NonNull)
                    netItemType = AddQuestionMarkIfNullableReferencesEnabled(netItemType);

                var netCollectionType = String.Format(netCollectionOpenType, netItemType);
                return ScalarFieldTypeDescription.FromNetTypeName(AddQuestionMarkIfNullableReferencesEnabled(netCollectionType));

            case GraphQlTypeKind.Scalar:
                return ResolveScalarNetType(baseType, member.Name, member.Type, Configuration.DataClassMemberNullability != DataClassMemberNullability.DefinedBySchema);

            default:
                return ScalarFieldTypeDescription.FromNetTypeName(AddQuestionMarkIfNullableReferencesEnabled("string"));
        }
    }

    private string AddQuestionMarkIfNullableReferencesEnabled(string dataTypeIdentifier) =>
        GraphQlGenerator.AddQuestionMarkIfNullableReferencesEnabled(Configuration.CSharpVersion, dataTypeIdentifier);

    internal bool IsUnknownObjectScalar(GraphQlType baseType, string valueName, GraphQlFieldType fieldType)
    {
        if (fieldType.UnwrapIfNonNull().Kind != GraphQlTypeKind.Scalar)
            return false;

        var netType = ResolveScalarNetType(baseType, valueName, fieldType, false).NetTypeName;
        return netType is "object" or "object?" || netType.EndsWith("System.Object") || netType.EndsWith("System.Object?");
    }

    internal string GetCSharpClassName(string graphQlName, bool applyNameCollisionMapping = true)
    {
        var csharpClassName = graphQlName;
        if (UseCustomClassNameIfDefined(ref csharpClassName))
            return csharpClassName;

        csharpClassName =
            applyNameCollisionMapping && _nameCollisionMapping.TryGetValue(graphQlName, out csharpClassName)
                ? csharpClassName
                : NamingHelper.ToPascalCase(graphQlName);

        return csharpClassName;
    }

    private bool UseCustomClassNameIfDefined(ref string typeName)
    {
        if (!Configuration.CustomClassNameMapping.TryGetValue(typeName, out var customTypeName))
            return false;

        CSharpHelper.ValidateClassName(customTypeName);
        typeName = customTypeName;
        return true;
    }

    internal ScalarFieldTypeDescription ResolveScalarNetType(GraphQlType baseType, string valueName, GraphQlFieldType valueType, bool alwaysNullable) =>
        valueType.UnwrapIfNonNull().Name switch
        {
            GraphQlTypeBase.GraphQlTypeScalarInteger => GetIntegerNetType(baseType, valueType, valueName, alwaysNullable),
            GraphQlTypeBase.GraphQlTypeScalarString => GetCustomScalarNetType(baseType, valueType, valueName),
            GraphQlTypeBase.GraphQlTypeScalarFloat => GetFloatNetType(baseType, valueType, valueName, alwaysNullable),
            GraphQlTypeBase.GraphQlTypeScalarBoolean => GetBooleanNetType(baseType, valueType, valueName, alwaysNullable),
            GraphQlTypeBase.GraphQlTypeScalarId => GetIdNetType(baseType, valueType, valueName, alwaysNullable),
            _ => GetCustomScalarNetType(baseType, valueType, valueName)
        };

    internal string GetFullyQualifiedNetTypeName(string baseTypeName, GraphQlTypeKind kind) =>
        $"{(kind is GraphQlTypeKind.Interface or GraphQlTypeKind.Union ? "I" : null)}{Configuration.ClassPrefix}{baseTypeName}{Configuration.ClassSuffix}";

    private ScalarFieldTypeDescription GetBooleanNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName, bool alwaysNullable) =>
        Configuration.BooleanTypeMapping switch
        {
            BooleanTypeMapping.Boolean => NullableScalarNetTypeName(valueType, "bool", alwaysNullable),
            BooleanTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName),
            _ => throw new InvalidOperationException($"boolean mapping \"{Configuration.BooleanTypeMapping}\" not supported")
        };

    private ScalarFieldTypeDescription GetFloatNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName, bool alwaysNullable) =>
        Configuration.FloatTypeMapping switch
        {
            FloatTypeMapping.Decimal => NullableScalarNetTypeName(valueType, "decimal", alwaysNullable),
            FloatTypeMapping.Float => NullableScalarNetTypeName(valueType, "float", alwaysNullable),
            FloatTypeMapping.Double => NullableScalarNetTypeName(valueType, "double", alwaysNullable),
            FloatTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName),
            _ => throw new InvalidOperationException($"float mapping \"{Configuration.FloatTypeMapping}\" not supported")
        };

    private ScalarFieldTypeDescription GetIntegerNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName, bool alwaysNullable) =>
        Configuration.IntegerTypeMapping switch
        {
            IntegerTypeMapping.Int32 => NullableScalarNetTypeName(valueType, "int", alwaysNullable),
            IntegerTypeMapping.Int16 => NullableScalarNetTypeName(valueType, "short", alwaysNullable),
            IntegerTypeMapping.Int64 => NullableScalarNetTypeName(valueType, "long", alwaysNullable),
            IntegerTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName),
            _ => throw new InvalidOperationException($"integer mapping \"{Configuration.IntegerTypeMapping}\" not supported")
        };

    private ScalarFieldTypeDescription GetIdNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName, bool alwaysNullable) =>
        Configuration.IdTypeMapping switch
        {
            IdTypeMapping.String => NullableScalarNetTypeName(valueType, "string", alwaysNullable),
            IdTypeMapping.Guid => NullableScalarNetTypeName(valueType, "Guid", alwaysNullable),
            IdTypeMapping.Object => NullableScalarNetTypeName(valueType, "object", alwaysNullable),
            IdTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName),
            _ => throw new InvalidOperationException($"id mapping \"{Configuration.IdTypeMapping}\" not supported")
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

    private ScalarFieldTypeDescription NullableScalarNetTypeName(GraphQlTypeBase valueType, string netType, bool alwaysNullable)
    {
        var isNotNull = !alwaysNullable && valueType.Kind is GraphQlTypeKind.NonNull;
        var isReferenceType = netType is "string" or "object";
        var areNullableReferencesDisabled = Configuration.CSharpVersion != CSharpVersion.NewestWithNullableReferences && isReferenceType;
        var netTypeName = isNotNull || areNullableReferencesDisabled ? netType : $"{netType}?";
        return ScalarFieldTypeDescription.FromNetTypeName(netTypeName);
    }

    private void ResolveReferencedObjectTypes()
    {
        foreach (var graphQlType in Schema.Types.Where(t => t.Kind is GraphQlTypeKind.Object or GraphQlTypeKind.Interface or GraphQlTypeKind.List && !t.IsBuiltIn()))
            FindAllReferencedObjectTypes(graphQlType);
    }

    private void ResolveNameCollisions()
    {
        var complexTypeCSharpNames = new HashSet<string>(_complexTypes.Keys.Select(NamingHelper.ToPascalCase));
        var inputObjectTypes = new HashSet<string>(Schema.GetInputObjectTypes().Select(t => NamingHelper.ToPascalCase(t.Name)));

        foreach (var graphQlType in Schema.Types.Where(t => !t.IsBuiltIn()))
        {
            var isInputObject = graphQlType.Kind is GraphQlTypeKind.InputObject;
            var propertyNamesToGenerate = new List<string>();
            if (isInputObject)
                propertyNamesToGenerate.AddRange(graphQlType.InputFields.Select(f => NamingHelper.ToPascalCase(f.Name)));
            else if (graphQlType.Kind is GraphQlTypeKind.Object or GraphQlTypeKind.Interface)
                propertyNamesToGenerate.AddRange(GetFieldsToGenerate(graphQlType).Select(f => NamingHelper.ToPascalCase(f.Name)));
            else
                continue;

            var candidateClassName = NamingHelper.ToPascalCase(graphQlType.Name);
            var finalClassName = candidateClassName;
            var collisionIteration = 1;

            do
            {
                var finalClassNameIncludingPrefixAndSuffix = GetFullyQualifiedNetTypeName(finalClassName, graphQlType.Kind);
                var hasNameCollision = propertyNamesToGenerate.Any(n => n == finalClassNameIncludingPrefixAndSuffix);
                if (candidateClassName != finalClassName)
                    hasNameCollision |= complexTypeCSharpNames.Contains(finalClassName) || inputObjectTypes.Contains(finalClassName);

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

    private void FindAllReferencedObjectTypes(GraphQlType type)
    {
        if (type.Kind is GraphQlTypeKind.Union)
            return;

        if (type.Kind is GraphQlTypeKind.Object or GraphQlTypeKind.Interface && !_referencedObjectTypes.Add(type.Name))
            return;

        var members = (IEnumerable<IGraphQlMember>)type.InputFields ?? type.Fields?.Where(FilterIfDeprecated);
        foreach (var member in members ?? throw new InvalidOperationException($"no members defined for GraphQL type \"{type.Name}\" ({type.Kind})"))
        {
            var unwrappedType = member.Type.UnwrapIfNonNull();

            switch (unwrappedType.Kind)
            {
                case GraphQlTypeKind.Object:
                case GraphQlTypeKind.Interface:
                    var memberType = _complexTypes[unwrappedType.Name];
                    FindAllReferencedObjectTypes(memberType);
                    break;

                case GraphQlTypeKind.List:
                    var itemType = unwrappedType.OfType.UnwrapIfNonNull();
                    if (itemType.Kind.IsComplex())
                        FindAllReferencedObjectTypes(_complexTypes[itemType.Name]);

                    break;
            }
        }
    }

    private void ResolveCovarianceRequiredFields()
    {
        var interfaceImplementations =
            _complexTypes.Values
                .Where(t => t.Kind is GraphQlTypeKind.Object && t.Interfaces?.Count > 0)
                .SelectMany(t => t.Interfaces.Select(i => (InterfaceName: i.Name, ImplementationType: t)))
                .ToLookup(x => x.InterfaceName, x => x.ImplementationType);

        foreach (var interfaceType in _complexTypes.Values.Where(t => t.Kind is GraphQlTypeKind.Interface))
        foreach (var interfaceField in interfaceType.Fields.Where(f => f.Type.UnwrapIfNonNull().Kind is GraphQlTypeKind.List))
        foreach (var implementationType in interfaceImplementations[interfaceType.Name])
        {
            var implementationField = implementationType.Fields.SingleOrDefault(f => f.Name == interfaceField.Name);
            var isSchemaInvalid = implementationField is null;
            if (isSchemaInvalid)
                continue;

            var isCovarianceRequired = !implementationField.Type.Equals(interfaceField.Type);
            if (!isCovarianceRequired)
                continue;

            _typeFieldCovarianceRequired.Add((interfaceType.Name, interfaceField.Name));
            break;
        }
    }

    private void ResolveTypeUnionLookup()
    {
        var duplicateCheck = new HashSet<string>();
        _typeUnionMembership =
            _complexTypes.Values
                .Where(t => t.Kind is GraphQlTypeKind.Union)
                .SelectMany(
                    u =>
                        u.PossibleTypes
                            .Where(
                                t =>
                                {
                                    if (duplicateCheck.Add(t.Name))
                                        return true;

                                    Warn($"duplicate union \"{u.Name}\" possible type \"{t.Name}\"");
                                    return false;
                                })
                            .Select(t => (UnionName: u.Name, PossibleTypeName: t.Name)))
                .ToLookup(x => x.PossibleTypeName, x => x.UnionName);
    }

    private void ResolveDirectives()
    {
        foreach (var directive in Schema.Directives)
            if (_directives.ContainsKey(directive.Name))
                Warn($"duplicate \"{directive.Name}\" directive definition");
            else
                _directives[directive.Name] = directive;
    }

    protected void Warn(string message) => LogMessage?.Invoke($"WARNING: {message}");

    protected void Log(string message) => LogMessage?.Invoke(message);

    private static InvalidOperationException NotInitializedException(string propertyName) =>
        new($"\"{propertyName}\" not initialized; call \"{nameof(Initialize)}\" method first. ");
}

public record struct ObjectGenerationContext
{
    public GraphQlType GraphQlType { get; set; }
    public string CSharpTypeName { get; set; }
}

public record PropertyGenerationContext
{
    public ObjectGenerationContext ObjectContext { get; }
    public string PropertyName { get; }
    public string PropertyBackingFieldName { get; }
    public string PropertyCSharpTypeName { get; }

    public PropertyAccessibility SetterAccessibility { get; set; }

    internal PropertyGenerationContext(ObjectGenerationContext objectContext, string csharpTypeName, string propertyName, string propertyBackingFieldName)
    {
        ObjectContext = objectContext;
        PropertyCSharpTypeName = csharpTypeName;
        PropertyName = propertyName;
        PropertyBackingFieldName = propertyBackingFieldName;
    }
}

public enum PropertyAccessibility
{
    Public,
    Internal,
    Protected,
    Private
}