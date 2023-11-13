﻿namespace GraphQlClientGenerator;

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

    public byte Indentation { get; protected set; }

    protected internal abstract TextWriter Writer { get; }


    protected GenerationContext(GraphQlSchema schema, GeneratedObjectType objectTypes)
    {
        var optionsInteger = (int)objectTypes;
        if (optionsInteger is < 1 or > 7)
            throw new ArgumentException("invalid value", nameof(objectTypes));

        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        ObjectTypes = objectTypes;
    }

    public bool FilterDeprecatedFields(GraphQlEnumValue field) =>
        !field.IsDeprecated || Configuration.IncludeDeprecatedFields;

    public void Initialize(GraphQlGeneratorConfiguration configuration)
    {
        Configuration = configuration;
        _nameCollisionMapping.Clear();
        _referencedObjectTypes.Clear();
        _complexTypes = Schema.GetComplexTypes().ToDictionary(t => t.Name);
        ResolveNameCollisions();

        Indentation = (byte)(configuration.FileScopedNamespaces ? 0 : 4);
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
        GraphQlGenerator.AddQuestionMarkIfNullableReferencesEnabled(Configuration, dataTypeIdentifier);

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

    private ScalarFieldTypeDescription GetBooleanNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName, bool alwaysNullable) =>
        Configuration.BooleanTypeMapping switch
        {
            BooleanTypeMapping.Boolean => NullableScalarNetTypeName(valueType, "bool", alwaysNullable),
            BooleanTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName),
            _ => throw new InvalidOperationException($"\"{Configuration.BooleanTypeMapping}\" not supported")
        };

    private ScalarFieldTypeDescription GetFloatNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName, bool alwaysNullable) =>
        Configuration.FloatTypeMapping switch
        {
            FloatTypeMapping.Decimal => NullableScalarNetTypeName(valueType, "decimal", alwaysNullable),
            FloatTypeMapping.Float => NullableScalarNetTypeName(valueType, "float", alwaysNullable),
            FloatTypeMapping.Double => NullableScalarNetTypeName(valueType, "double", alwaysNullable),
            FloatTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName),
            _ => throw new InvalidOperationException($"\"{Configuration.FloatTypeMapping}\" not supported")
        };

    private ScalarFieldTypeDescription GetIntegerNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName, bool alwaysNullable) =>
        Configuration.IntegerTypeMapping switch
        {
            IntegerTypeMapping.Int32 => NullableScalarNetTypeName(valueType, "int", alwaysNullable),
            IntegerTypeMapping.Int16 => NullableScalarNetTypeName(valueType, "short", alwaysNullable),
            IntegerTypeMapping.Int64 => NullableScalarNetTypeName(valueType, "long", alwaysNullable),
            IntegerTypeMapping.Custom => Configuration.ScalarFieldTypeMappingProvider.GetCustomScalarFieldType(Configuration, baseType, valueType, valueName),
            _ => throw new InvalidOperationException($"\"{Configuration.IntegerTypeMapping}\" not supported")
        };

    private ScalarFieldTypeDescription GetIdNetType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName, bool alwaysNullable) =>
        Configuration.IdTypeMapping switch
        {
            IdTypeMapping.String => NullableScalarNetTypeName(valueType, "string", alwaysNullable),
            IdTypeMapping.Guid => NullableScalarNetTypeName(valueType, "Guid", alwaysNullable),
            IdTypeMapping.Object => NullableScalarNetTypeName(valueType, "object", alwaysNullable),
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

    private ScalarFieldTypeDescription NullableScalarNetTypeName(GraphQlTypeBase valueType, string netType, bool alwaysNullable)
    {
        var isNotNull = !alwaysNullable && valueType.Kind is GraphQlTypeKind.NonNull;
        var isReferenceType = netType is "string" or "object";
        var areNullableReferencesDisabled = Configuration.CSharpVersion != CSharpVersion.NewestWithNullableReferences && isReferenceType;
        var netTypeName = isNotNull || areNullableReferencesDisabled ? netType : $"{netType}?";
        return ScalarFieldTypeDescription.FromNetTypeName(netTypeName);
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
                FindAllReferencedObjectTypes(graphQlType);
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

    private void FindAllReferencedObjectTypes(GraphQlType type)
    {
        foreach (var member in (IEnumerable<IGraphQlMember>)type.InputFields ?? type.Fields)
        {
            var unwrappedType = member.Type.UnwrapIfNonNull();
            GraphQlType memberType;
            switch (unwrappedType.Kind)
            {
                case GraphQlTypeKind.Object:
                    _referencedObjectTypes.Add(unwrappedType.Name);
                    memberType = _complexTypes[unwrappedType.Name];
                    FindAllReferencedObjectTypes(memberType);
                    break;

                case GraphQlTypeKind.List:
                    var itemType = unwrappedType.OfType.UnwrapIfNonNull();
                    if (itemType.Kind.IsComplex())
                    {
                        memberType = _complexTypes[itemType.Name];
                        FindAllReferencedObjectTypes(memberType);
                    }

                    break;
            }
        }
    }

    public void WriteNamespaceStart(string @namespace)
    {
        Writer.Write($"namespace {@namespace}");
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

    protected void WriteNamespaceEnd()
    {
        if(!Configuration.FileScopedNamespaces) Writer.WriteLine("}");
    }
}

public struct ObjectGenerationContext
{
    public GraphQlType GraphQlType { get; set; }
    public string CSharpTypeName { get; set; }
}