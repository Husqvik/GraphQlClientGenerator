using System.Runtime.CompilerServices;

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

    protected GraphQlGeneratorConfiguration Configuration => _configuration ?? throw NotInitializedException();

    protected internal abstract TextWriter Writer { get; }

    internal IReadOnlyCollection<string> ReferencedObjectTypes => _referencedObjectTypes;

    internal IReadOnlyCollection<GraphQlDirective> Directives => _directives.Values;

    internal ILookup<string, string> TypeUnionMembership => _typeUnionMembership ?? throw NotInitializedException();

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

    public abstract void OnDataClassConstructorGeneration(ObjectGenerationContext context);

    public abstract void AfterDataClassGeneration(ObjectGenerationContext context);

    public abstract void AfterDataClassesGeneration();

    public abstract void BeforeDataPropertyGeneration(PropertyGenerationContext context);

    public virtual void OnDataPropertyGeneration(PropertyGenerationContext context)
    {
        var generateBackingFields =
            Configuration.PropertyGeneration is PropertyGenerationOption.BackingField &&
            context.ObjectContext.GraphQlType.Kind is GraphQlTypeKind.Object;

        if (generateBackingFields)
        {
            var useCompatibleVersion = Configuration.CSharpVersion is CSharpVersion.Compatible;
            var isFieldKeywordSupported = Configuration.CSharpVersion.IsFieldKeywordSupported();
            Writer.Write(" { get");
            Writer.Write(useCompatibleVersion ? " { return " : " => ");
            Writer.Write(isFieldKeywordSupported ? "field" : context.PropertyBackingFieldName);
            Writer.Write(";");

            if (useCompatibleVersion)
                Writer.Write(" }");

            Writer.Write(context.SetterAccessibility.ToSetterAccessibilityPrefix());
            Writer.Write(" set");
            Writer.Write(useCompatibleVersion ? " { " : " => ");
            Writer.Write(isFieldKeywordSupported ? "field" : context.PropertyBackingFieldName);
            Writer.Write(" = value;");

            if (useCompatibleVersion)
                Writer.Write(" }");

            Writer.Write(" }");
        }
        else
        {
            Writer.Write(" { get; ");
            Writer.Write(context.SetterAccessibility.ToSetterAccessibilityPrefix());
            Writer.Write("set; }");
        }
    }

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

    protected internal ScalarFieldTypeDescription GetDataPropertyType(GraphQlType ownerType, IGraphQlMember member)
    {
        var fieldType = member.Type.UnwrapIfNonNull();
        var memberTypeContext =
            new ScalarFieldTypeProviderContext
            {
                Configuration = Configuration,
                ComponentType = ClientComponentType.DataClassProperty,
                OwnerType = ownerType,
                FieldType = member.Type,
                FieldName = member.Name
            };

        switch (fieldType.Kind)
        {
            case GraphQlTypeKind.Object:
            case GraphQlTypeKind.Interface:
            case GraphQlTypeKind.Union:
            case GraphQlTypeKind.InputObject:
                var fieldTypeName = GetCSharpClassName(fieldType.Name);
                var propertyType = GetFullyQualifiedNetTypeName(fieldTypeName, fieldType.Kind);
                return NullableNetTypeDescription(memberTypeContext, propertyType, true);

            case GraphQlTypeKind.Enum:
                return GetEnumNetType(memberTypeContext);

            case GraphQlTypeKind.List:
                var isCovarianceRequired = _typeFieldCovarianceRequired.Contains((ownerType.Name, member.Name));
                var itemType = GraphQlGenerator.UnwrapListItemType(fieldType, Configuration.EnableNullableReferences, isCovarianceRequired, out var netCollectionOpenType);
                var unwrappedItemType = itemType?.UnwrapIfNonNull() ?? throw GraphQlGenerator.ListItemTypeResolutionFailedException(ownerType.Name, fieldType.Name);
                var itemTypeName = GetCSharpClassName(unwrappedItemType.Name);
                var listItemTypeContext =
                    new ScalarFieldTypeProviderContext
                    {
                        Configuration = Configuration,
                        ComponentType = ClientComponentType.DataClassArrayItem,
                        OwnerType = ownerType,
                        FieldType = itemType,
                        FieldName = member.Name
                    };

                var itemDescription =
                    unwrappedItemType.Kind is GraphQlTypeKind.Enum
                        ? GetEnumNetType(listItemTypeContext)
                        : NullableNetTypeDescription(
                            listItemTypeContext,
                            IsUnknownObjectScalar(listItemTypeContext) ? "object" : GetFullyQualifiedNetTypeName(itemTypeName, unwrappedItemType.Kind),
                            true);

                var netItemType = itemDescription.NetTypeName;
                var suggestedScalarNetType = ResolveScalarNetType(listItemTypeContext).NetTypeName;
                if (!ScalarFieldTypeDescription.IsNetObject(suggestedScalarNetType))
                    netItemType = suggestedScalarNetType;

                var netCollectionType = String.Format(netCollectionOpenType, netItemType);
                return NullableNetTypeDescription(memberTypeContext, netCollectionType, true);

            case GraphQlTypeKind.Scalar:
                return ResolveScalarNetType(memberTypeContext);

            default:
                throw new InvalidOperationException($"unexpected GraphQL type kind: {fieldType.Kind}");
        }
    }

    private bool IsUnknownObjectScalar(ScalarFieldTypeProviderContext context)
    {
        var fieldType = context.FieldType.UnwrapIfNonNull();
        if (fieldType.Kind != GraphQlTypeKind.Scalar)
            return false;

        var netType = ResolveScalarNetType(context).NetTypeName;
        return ScalarFieldTypeDescription.IsNetObject(netType);
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

    internal ScalarFieldTypeDescription ResolveScalarNetType(ScalarFieldTypeProviderContext context) =>
        context.FieldType.UnwrapIfNonNull().Name
            switch
            {
                GraphQlTypeBase.GraphQlTypeScalarInteger => GetIntegerNetType(context),
                GraphQlTypeBase.GraphQlTypeScalarString => GetStringNetType(context),
                GraphQlTypeBase.GraphQlTypeScalarFloat => GetFloatNetType(context),
                GraphQlTypeBase.GraphQlTypeScalarBoolean => GetBooleanNetType(context),
                GraphQlTypeBase.GraphQlTypeScalarId => GetIdNetType(context),
                _ => GetCustomScalarNetType(context)
            };

    internal string GetFullyQualifiedNetTypeName(string baseTypeName, GraphQlTypeKind kind) =>
        GetFullyQualifiedNetTypeName(Configuration, baseTypeName, kind);

    private static string GetFullyQualifiedNetTypeName(GraphQlGeneratorConfiguration configuration, string baseTypeName, GraphQlTypeKind kind) =>
        $"{(kind is GraphQlTypeKind.Interface or GraphQlTypeKind.Union ? "I" : null)}{configuration.ClassPrefix}{baseTypeName}{configuration.ClassSuffix}";

    private ScalarFieldTypeDescription GetEnumNetType(ScalarFieldTypeProviderContext context) =>
        Configuration.ScalarFieldTypeMappingProvider is null
            ? GetDefaultEnumNetType(context)
            : ScalarFieldTypeProvider.GetCustomScalarFieldType(context);

    internal static ScalarFieldTypeDescription GetDefaultEnumNetType(ScalarFieldTypeProviderContext context) =>
        NullableNetTypeDescription(
            context,
            GetFullyQualifiedNetTypeName(context.Configuration, NamingHelper.ToPascalCase(context.FieldType.UnwrapIfNonNull().Name), GraphQlTypeKind.Enum));

    private ScalarFieldTypeDescription GetStringNetType(ScalarFieldTypeProviderContext context) =>
        Configuration.ScalarFieldTypeMappingProvider is null
            ? NullableNetTypeDescription(context, "string", true)
            : GetCustomScalarNetType(context);

    private ScalarFieldTypeDescription GetBooleanNetType(ScalarFieldTypeProviderContext context) =>
        Configuration.BooleanTypeMapping switch
        {
            BooleanTypeMapping.Boolean => NullableNetTypeDescription(context, "bool"),
            BooleanTypeMapping.Custom => ScalarFieldTypeProvider.GetCustomScalarFieldType(context),
            _ => throw new InvalidOperationException($"unexpected {nameof(BooleanTypeMapping)}: \"{Configuration.BooleanTypeMapping}\"")
        };

    private ScalarFieldTypeDescription GetFloatNetType(ScalarFieldTypeProviderContext context) =>
        Configuration.FloatTypeMapping switch
        {
            FloatTypeMapping.Decimal => NullableNetTypeDescription(context, "decimal"),
            FloatTypeMapping.Float => NullableNetTypeDescription(context, "float"),
            FloatTypeMapping.Double => NullableNetTypeDescription(context, "double"),
            FloatTypeMapping.Custom => ScalarFieldTypeProvider.GetCustomScalarFieldType(context),
            _ => throw new InvalidOperationException($"unexpected {nameof(FloatTypeMapping)}: \"{Configuration.FloatTypeMapping}\"")
        };

    private ScalarFieldTypeDescription GetIntegerNetType(ScalarFieldTypeProviderContext context) =>
        Configuration.IntegerTypeMapping switch
        {
            IntegerTypeMapping.Int32 => NullableNetTypeDescription(context, "int"),
            IntegerTypeMapping.Int16 => NullableNetTypeDescription(context, "short"),
            IntegerTypeMapping.Int64 => NullableNetTypeDescription(context, "long"),
            IntegerTypeMapping.Custom => ScalarFieldTypeProvider.GetCustomScalarFieldType(context),
            _ => throw new InvalidOperationException($"unexpected {nameof(IntegerTypeMapping)}: \"{Configuration.IntegerTypeMapping}\"")
        };

    private ScalarFieldTypeDescription GetIdNetType(ScalarFieldTypeProviderContext context) =>
        Configuration.IdTypeMapping switch
        {
            IdTypeMapping.String => NullableNetTypeDescription(context, "string", true),
            IdTypeMapping.Guid => NullableNetTypeDescription(context, "Guid"),
            IdTypeMapping.Object => NullableNetTypeDescription(context, "object", true),
            IdTypeMapping.Custom => ScalarFieldTypeProvider.GetCustomScalarFieldType(context),
            _ => throw new InvalidOperationException($"unexpected {nameof(IdTypeMapping)}: \"{Configuration.IdTypeMapping}\"")
        };

    private IScalarFieldTypeMappingProvider ScalarFieldTypeProvider =>
        Configuration.ScalarFieldTypeMappingProvider
        ?? throw new InvalidOperationException($"{nameof(GraphQlGeneratorConfiguration)}.{nameof(GraphQlGeneratorConfiguration.ScalarFieldTypeMappingProvider)} not set");

    private ScalarFieldTypeDescription GetCustomScalarNetType(ScalarFieldTypeProviderContext context)
    {
        var typeDescription = ScalarFieldTypeProvider.GetCustomScalarFieldType(context);

        if (String.IsNullOrWhiteSpace(typeDescription.NetTypeName))
            throw new InvalidOperationException($".NET type for \"{context.OwnerType.Name}.{context.FieldName}\" ({context.FieldType.UnwrapIfNonNull().Name}) cannot be resolved. Please check \"{ScalarFieldTypeProvider.GetType().FullName}\" implementation. ");

        return typeDescription with { NetTypeName = typeDescription.NetTypeName.Replace(" ", null).Replace("\t", null) };
    }

    private static ScalarFieldTypeDescription NullableNetTypeDescription(ScalarFieldTypeProviderContext context, string netType, bool isReferenceType = false) =>
        ScalarFieldTypeDescription.FromNetTypeName(GetNullableNetTypeName(context, netType, isReferenceType));

    public static string GetNullableNetTypeName(ScalarFieldTypeProviderContext context, string netType, bool isReferenceType)
    {
        var alwaysNullable =
            context.Configuration.DataClassMemberNullability is DataClassMemberNullability.AlwaysNullable &&
            context.ComponentType is ClientComponentType.DataClassProperty;

        var isNotNull = !alwaysNullable && context.FieldType.Kind is GraphQlTypeKind.NonNull;
        var areNullableReferencesDisabled = !context.Configuration.EnableNullableReferences && isReferenceType;
        return isNotNull || areNullableReferencesDisabled ? netType : $"{netType}?";
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
                    {
                        duplicateCheck.Clear();
                        return u.PossibleTypes
                            .Where(
                                pt =>
                                {
                                    if (duplicateCheck.Add(pt.Name))
                                        return true;

                                    Warn($"duplicate union \"{u.Name}\" possible type \"{pt.Name}\"");
                                    return false;
                                })
                            .Select(t => (UnionName: u.Name, PossibleTypeName: t.Name));
                    })
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

    private static InvalidOperationException NotInitializedException([CallerMemberName] string propertyName = null) =>
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
    ProtectedInternal,
    Private
}

public struct ScalarFieldTypeProviderContext
{
    public GraphQlGeneratorConfiguration Configuration { get; set; }
    public ClientComponentType ComponentType { get; set; }
    public GraphQlTypeBase OwnerType { get; set; }
    public GraphQlFieldType FieldType { get; set; }
    public string FieldName { get; set; }
}

public enum ClientComponentType
{
    QueryBuilderParameter,
    DataClassProperty,
    DataClassArrayItem
}

public struct ScalarFieldTypeDescription
{
    public string NetTypeName { get; set; }
    public string FormatMask { get; set; }

    public static ScalarFieldTypeDescription FromNetTypeName(string netTypeName) => new() { NetTypeName = netTypeName };

    internal static bool IsNetObject(string netTypeName) =>
        netTypeName is "object" or "object?" or "System.Object" or "System.Object?" or "global::System.Object" or "global::System.Object?";
}