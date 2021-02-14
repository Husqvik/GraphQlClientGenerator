public struct FieldMetadata
{
    public string Name { get; set; }
    public string DefaultAlias { get; set; }
    public bool IsComplex { get; set; }
    public Type QueryBuilderType { get; set; }
}

public enum Formatting
{
    None,
    Indented
}

public class GraphQlObjectTypeAttribute : Attribute
{
    public string TypeName { get; }

    public GraphQlObjectTypeAttribute(string typeName) => TypeName = typeName;
}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
public class QueryBuilderParameterConverter<T> : JsonConverter
{
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.Null:
                return null;

            default:
                return (QueryBuilderParameter<T>)(T)serializer.Deserialize(reader, typeof(T));
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
            writer.WriteNull();
        else
            serializer.Serialize(writer, ((QueryBuilderParameter<T>)value).Value, typeof(T));
    }

    public override bool CanConvert(Type objectType) => objectType.IsSubclassOf(typeof(QueryBuilderParameter));
}

public class GraphQlInterfaceJsonConverter : JsonConverter
{
    private const string FieldNameType = "__typename";

    private static readonly Dictionary<string, Type> InterfaceTypeMapping =
        typeof(GraphQlInterfaceJsonConverter).Assembly.GetTypes()
            .Select(t => new { Type = t, Attribute = t.GetCustomAttribute<GraphQlObjectTypeAttribute>() })
            .Where(x => x.Attribute != null && x.Type.Namespace == typeof(GraphQlInterfaceJsonConverter).Namespace)
            .ToDictionary(x => x.Attribute.TypeName, x => x.Type);

    public override bool CanConvert(Type objectType) => objectType.IsInterface || objectType.IsArray;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        while (reader.TokenType == JsonToken.Comment)
            reader.Read();

        switch (reader.TokenType)
        {
            case JsonToken.Null:
                return null;

            case JsonToken.StartObject:
                var jObject = JObject.Load(reader);
                if (!jObject.TryGetValue(FieldNameType, out var token) || token.Type != JTokenType.String)
                    throw CreateJsonReaderException(reader, $"\"{GetType().FullName}\" requires JSON object to contain \"{FieldNameType}\" field with type name");

                var typeName = token.Value<string>();
                if (!InterfaceTypeMapping.TryGetValue(typeName, out var type))
                    throw CreateJsonReaderException(reader, $"type \"{typeName}\" not found");

                using (reader = CloneReader(jObject, reader))
                    return serializer.Deserialize(reader, type);

            case JsonToken.StartArray:
                var elementType = GetElementType(objectType);
                if (elementType == null)
                    throw CreateJsonReaderException(reader, $"array element type could not be resolved for type \"{objectType.FullName}\"");

                return ReadArray(reader, objectType, elementType, serializer);

            default:
                throw CreateJsonReaderException(reader, $"unrecognized token: {reader.TokenType}");
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => serializer.Serialize(writer, value);

    private static JsonReader CloneReader(JToken jToken, JsonReader reader)
    {
        var jObjectReader = jToken.CreateReader();
        jObjectReader.Culture = reader.Culture;
        jObjectReader.CloseInput = reader.CloseInput;
        jObjectReader.SupportMultipleContent = reader.SupportMultipleContent;
        jObjectReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
        jObjectReader.FloatParseHandling = reader.FloatParseHandling;
        jObjectReader.DateFormatString = reader.DateFormatString;
        jObjectReader.DateParseHandling = reader.DateParseHandling;
        return jObjectReader;
    }

    private static JsonReaderException CreateJsonReaderException(JsonReader reader, string message)
    {
        if (reader is IJsonLineInfo lineInfo && lineInfo.HasLineInfo())
            return new JsonReaderException(message, reader.Path, lineInfo.LineNumber, lineInfo.LinePosition, null);

        return new JsonReaderException(message);
    }

    private static Type GetElementType(Type arrayOrGenericContainer) =>
        arrayOrGenericContainer.IsArray ? arrayOrGenericContainer.GetElementType() : arrayOrGenericContainer.GenericTypeArguments.FirstOrDefault();

    private IList ReadArray(JsonReader reader, Type targetType, Type elementType, JsonSerializer serializer)
    {
        var list = CreateCompatibleList(targetType, elementType);
        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            list.Add(ReadJson(reader, elementType, null, serializer));

        if (!targetType.IsArray)
            return list;

        var array = Array.CreateInstance(elementType, list.Count);
        list.CopyTo(array, 0);
        return array;
    }

    private static IList CreateCompatibleList(Type targetContainerType, Type elementType) =>
        (IList)Activator.CreateInstance(targetContainerType.IsArray || targetContainerType.IsAbstract ? typeof(List<>).MakeGenericType(elementType) : targetContainerType);
}
#endif

internal static class GraphQlQueryHelper
{
    private static readonly Regex RegexWhiteSpace = new Regex(@"\s", RegexOptions.Compiled);
    private static readonly Regex RegexGraphQlIdentifier = new Regex(@"^[_A-Za-z][_0-9A-Za-z]*$", RegexOptions.Compiled);

    public static string GetIndentation(int level, byte indentationSize)
    {
        return new String(' ', level * indentationSize);
    }

    public static string BuildArgumentValue(object value, string formatMask, Formatting formatting, int level, byte indentationSize)
    {
        if (value is null || value is QueryBuilderParameter queryBuilderParameter && queryBuilderParameter.Value == null)
            return "null";

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        if (value is JValue jValue)
        {
            switch (jValue.Type)
            {
                case JTokenType.Null: return "null";
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Boolean:
                    return BuildArgumentValue(jValue.Value, null, formatting, level, indentationSize);
                default:
                    return "\"" + jValue.Value + "\"";
            }
        }

        if (value is JProperty jProperty)
        {
            if (RegexWhiteSpace.IsMatch(jProperty.Name))
                throw new ArgumentException($"JSON object keys used as GraphQL arguments must not contain whitespace; key: {jProperty.Name}");

            return $"{jProperty.Name}:{(formatting == Formatting.Indented ? " " : null)}{BuildArgumentValue(jProperty.Value, null, formatting, level, indentationSize)}";
        }

        if (value is JObject jObject)
            return BuildEnumerableArgument(jObject, null, formatting, level + 1, indentationSize, '{', '}');
#endif

        var enumerable = value as IEnumerable;
        if (!String.IsNullOrEmpty(formatMask) && enumerable == null)
            return
                value is IFormattable formattable
                    ? "\"" + formattable.ToString(formatMask, CultureInfo.InvariantCulture) + "\""
                    : throw new ArgumentException($"Value must implement {nameof(IFormattable)} interface to use a format mask. ", nameof(value));

        if (value is Enum @enum)
            return ConvertEnumToString(@enum);

        if (value is bool @bool)
            return @bool ? "true" : "false";

        if (value is DateTime dateTime)
            return "\"" + dateTime.ToString("O") + "\"";

        if (value is DateTimeOffset dateTimeOffset)
            return "\"" + dateTimeOffset.ToString("O") + "\"";

        if (value is IGraphQlInputObject inputObject)
            return BuildInputObject(inputObject, formatting, level + 2, indentationSize);

        if (value is String || value is Guid)
            return "\"" + value + "\"";

        if (enumerable != null)
            return BuildEnumerableArgument(enumerable, formatMask, formatting, level, indentationSize, '[', ']');

        if (value is short || value is ushort || value is byte || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
            return Convert.ToString(value, CultureInfo.InvariantCulture);

        var argumentValue = Convert.ToString(value, CultureInfo.InvariantCulture);
        return "\"" + argumentValue + "\"";
    }

    private static string BuildEnumerableArgument(IEnumerable enumerable, string formatMask, Formatting formatting, int level, byte indentationSize, char openingSymbol, char closingSymbol)
    {
        var builder = new StringBuilder();
        builder.Append(openingSymbol);
        var delimiter = String.Empty;
        foreach (var item in enumerable)
        {
            builder.Append(delimiter);

            if (formatting == Formatting.Indented)
            {
                builder.AppendLine();
                builder.Append(GetIndentation(level + 1, indentationSize));
            }

            builder.Append(BuildArgumentValue(item, formatMask, formatting, level, indentationSize));
            delimiter = ",";
        }

        builder.Append(closingSymbol);
        return builder.ToString();
    }

    public static string BuildInputObject(IGraphQlInputObject inputObject, Formatting formatting, int level, byte indentationSize)
    {
        var builder = new StringBuilder();
        builder.Append("{");

        var isIndentedFormatting = formatting == Formatting.Indented;
        string valueSeparator;
        if (isIndentedFormatting)
        {
            builder.AppendLine();
            valueSeparator = ": ";
        }
        else
            valueSeparator = ":";

        var separator = String.Empty;
        foreach (var propertyValue in inputObject.GetPropertyValues())
        {
            var queryBuilderParameter = propertyValue.Value as QueryBuilderParameter;
            var value =
                queryBuilderParameter?.Name != null
                    ? "$" + queryBuilderParameter.Name
                    : BuildArgumentValue(queryBuilderParameter?.Value ?? propertyValue.Value, propertyValue.FormatMask, formatting, level, indentationSize);

            builder.Append(isIndentedFormatting ? GetIndentation(level, indentationSize) : separator);
            builder.Append(propertyValue.Name);
            builder.Append(valueSeparator);
            builder.Append(value);

            separator = ",";

            if (isIndentedFormatting)
                builder.AppendLine();
        }

        if (isIndentedFormatting)
            builder.Append(GetIndentation(level - 1, indentationSize));

        builder.Append("}");

        return builder.ToString();
    }

    public static string BuildDirective(GraphQlDirective directive, Formatting formatting, int level, byte indentationSize)
    {
        if (directive == null)
            return String.Empty;

        var isIndentedFormatting = formatting == Formatting.Indented;
        var indentationSpace = isIndentedFormatting ? " " : String.Empty;
        var builder = new StringBuilder();
        builder.Append(indentationSpace);
        builder.Append("@");
        builder.Append(directive.Name);
        builder.Append("(");

        string separator = null;
        foreach (var kvp in directive.Arguments)
        {
            var argumentName = kvp.Key;
            var argument = kvp.Value;

            builder.Append(separator);
            builder.Append(argumentName);
            builder.Append(":");
            builder.Append(indentationSpace);

            if (argument.Name == null)
                builder.Append(BuildArgumentValue(argument.Value, null, formatting, level, indentationSize));
            else
            {
                builder.Append("$");
                builder.Append(argument.Name);
            }

            separator = isIndentedFormatting ? ", " : ",";
        }

        builder.Append(")");
        return builder.ToString();
    }

    public static void ValidateGraphQlIdentifier(string name, string identifier)
    {
        if (identifier != null && !RegexGraphQlIdentifier.IsMatch(identifier))
            throw new ArgumentException("value must match [_A-Za-z][_0-9A-Za-z]*", name);
    }

    private static string ConvertEnumToString(Enum @enum)
    {
        var enumMember = @enum.GetType().GetField(@enum.ToString());
            if (enumMember == null)
                throw new InvalidOperationException("enum member resolution failed");

        var enumMemberAttribute = (EnumMemberAttribute)enumMember.GetCustomAttribute(typeof(EnumMemberAttribute));

        return enumMemberAttribute == null
            ? @enum.ToString()
            : enumMemberAttribute.Value;
    }
}

internal struct InputPropertyInfo
{
    public string Name { get; set; }
    public object Value { get; set; }
    public string FormatMask { get; set; }
}

internal interface IGraphQlInputObject
{
    IEnumerable<InputPropertyInfo> GetPropertyValues();
}

public interface IGraphQlQueryBuilder
{
    void Clear();
    void IncludeAllFields();
    string Build(Formatting formatting = Formatting.None, byte indentationSize = 2);
}

public struct QueryBuilderArgumentInfo
{
    public string ArgumentName { get; set; }
    public QueryBuilderParameter ArgumentValue { get; set; }
    public string FormatMask { get; set; }
}

public abstract class QueryBuilderParameter
{
    private string _name;

    internal string GraphQlTypeName { get; }
    internal object Value { get; set; }

    public string Name
    {
        get => _name;
        set
        {
            GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(Name), value);
            _name = value;
        }
    }

    protected QueryBuilderParameter(string name, string graphQlTypeName, object value)
    {
        Name = name?.Trim();
        GraphQlTypeName = graphQlTypeName?.Replace(" ", null).Replace("\t", null).Replace("\n", null).Replace("\r", null);
        Value = value;
    }

    protected QueryBuilderParameter(object value) => Value = value;
}

public class QueryBuilderParameter<T> : QueryBuilderParameter
{
    public new T Value
    {
        get => (T)base.Value;
        set => base.Value = value;
    }

    protected QueryBuilderParameter(string name, string graphQlTypeName, T value) : base(name, graphQlTypeName, value)
    {
        if (String.IsNullOrWhiteSpace(graphQlTypeName))
            throw new ArgumentException("value required", nameof(graphQlTypeName));
    }

    private QueryBuilderParameter(T value) : base(value)
    {
    }

    public static implicit operator QueryBuilderParameter<T>(T value) => new QueryBuilderParameter<T>(value);

    public static implicit operator T(QueryBuilderParameter<T> parameter) => parameter.Value;
}

public class GraphQlQueryParameter<T> : QueryBuilderParameter<T>
{
    private string _formatMask;

    public string FormatMask
    {
        get => _formatMask;
        set => _formatMask =
            typeof(IFormattable).IsAssignableFrom(typeof(T))
                ? value
                : throw new InvalidOperationException($"Value must be of {nameof(IFormattable)} type. ");
    }

    public GraphQlQueryParameter(string name, string graphQlTypeName, T value) : base(name, graphQlTypeName, value)
    {
    }

    public GraphQlQueryParameter(string name, T value, bool isNullable = true) : base(name, GetGraphQlTypeName(value, isNullable), value)
    {
    }

    private static string GetGraphQlTypeName(T value, bool isNullable)
    {
        var graphQlTypeName = GetGraphQlTypeName(typeof(T));
        if (!isNullable)
            graphQlTypeName += "!";

        return graphQlTypeName;
    }

    private static string GetGraphQlTypeName(Type valueType)
    {
        valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (valueType.IsArray)
        {
            var arrayItemType = GetGraphQlTypeName(valueType.GetElementType());
            return arrayItemType == null ? null : "[" + arrayItemType + "]";
        }

        if (typeof(IEnumerable).IsAssignableFrom(valueType))
        {
            var genericArguments = valueType.GetGenericArguments();
            if (genericArguments.Length == 1)
            {
                var listItemType = GetGraphQlTypeName(valueType.GetGenericArguments()[0]);
                return listItemType == null ? null : "[" + listItemType + "]";
            }
        }

        if (GraphQlTypes.ReverseMapping.TryGetValue(valueType, out var graphQlTypeName))
            return graphQlTypeName;

        if (valueType == typeof(bool))
            return "Boolean";

        if (valueType == typeof(float) || valueType == typeof(double) || valueType == typeof(decimal))
            return "Float";

        if (valueType == typeof(Guid))
            return "ID";

        if (valueType == typeof(sbyte) || valueType == typeof(byte) || valueType == typeof(short) || valueType == typeof(ushort) || valueType == typeof(int) || valueType == typeof(uint) ||
            valueType == typeof(long) || valueType == typeof(ulong))
            return "Int";

        if (valueType == typeof(string))
            return "String";

        return null;
    }
}

public abstract class GraphQlDirective
{
    private readonly Dictionary<string, QueryBuilderParameter> _arguments = new Dictionary<string, QueryBuilderParameter>();

    internal IEnumerable<KeyValuePair<string, QueryBuilderParameter>> Arguments => _arguments;

    public string Name { get; }

    protected GraphQlDirective(string name)
    {
        GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(name), name);
        Name = name;
    }

    protected void AddArgument(string name, QueryBuilderParameter value)
    {
        if (value != null)
            _arguments[name] = value;
    }
}

public abstract class GraphQlQueryBuilder : IGraphQlQueryBuilder
{
    private readonly Dictionary<string, GraphQlFieldCriteria> _fieldCriteria = new Dictionary<string, GraphQlFieldCriteria>();

    private readonly string _operationType;
    private readonly string _operationName;
    private Dictionary<string, GraphQlFragmentCriteria> _fragments;
    private List<QueryBuilderArgumentInfo> _queryParameters;

    protected abstract string TypeName { get; }

    public abstract IReadOnlyList<FieldMetadata> AllFields { get; }

    protected GraphQlQueryBuilder(string operationType, string operationName)
    {
        GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(operationName), operationName);
        _operationType = operationType;
        _operationName = operationName;
    }

    public virtual void Clear()
    {
        _fieldCriteria.Clear();
        _fragments?.Clear();
        _queryParameters?.Clear();
    }

    void IGraphQlQueryBuilder.IncludeAllFields()
    {
        IncludeAllFields();
    }

    public string Build(Formatting formatting = Formatting.None, byte indentationSize = 2)
    {
        return Build(formatting, 1, indentationSize);
    }

    protected void IncludeAllFields()
    {
        IncludeFields(AllFields);
    }

    protected virtual string Build(Formatting formatting, int level, byte indentationSize)
    {
        var isIndentedFormatting = formatting == Formatting.Indented;
        var separator = String.Empty;
        var indentationSpace = isIndentedFormatting ? " " : String.Empty;
        var builder = new StringBuilder();

        if (!String.IsNullOrEmpty(_operationType))
        {
            builder.Append(_operationType);

            if (!String.IsNullOrEmpty(_operationName))
            {
                builder.Append(" ");
                builder.Append(_operationName);
            }

            if (_queryParameters?.Count > 0)
            {
                builder.Append(indentationSpace);
                builder.Append("(");

                foreach (var queryParameterInfo in _queryParameters)
                {
                    if (isIndentedFormatting)
                    {
                        builder.AppendLine(separator);
                        builder.Append(GraphQlQueryHelper.GetIndentation(level, indentationSize));
                    }
                    else
                        builder.Append(separator);
                    
                    builder.Append("$");
                    builder.Append(queryParameterInfo.ArgumentValue.Name);
                    builder.Append(":");
                    builder.Append(indentationSpace);

                    builder.Append(queryParameterInfo.ArgumentValue.GraphQlTypeName);

                    if (!queryParameterInfo.ArgumentValue.GraphQlTypeName.EndsWith("!"))
                    {
                        builder.Append(indentationSpace);
                        builder.Append("=");
                        builder.Append(indentationSpace);
                        builder.Append(GraphQlQueryHelper.BuildArgumentValue(queryParameterInfo.ArgumentValue.Value, queryParameterInfo.FormatMask, formatting, 0, indentationSize));
                    }

                    separator = ",";
                }

                builder.Append(")");
            }
        }

        builder.Append(indentationSpace);
        builder.Append("{");

        if (isIndentedFormatting)
            builder.AppendLine();

        separator = String.Empty;
        
        foreach (var criteria in _fieldCriteria.Values.Concat(_fragments?.Values ?? Enumerable.Empty<GraphQlFragmentCriteria>()))
        {
            var fieldCriteria = criteria.Build(formatting, level, indentationSize);
            if (isIndentedFormatting)
                builder.AppendLine(fieldCriteria);
            else if (!String.IsNullOrEmpty(fieldCriteria))
            {
                builder.Append(separator);
                builder.Append(fieldCriteria);
            }

            separator = ",";
        }

        if (isIndentedFormatting)
            builder.Append(GraphQlQueryHelper.GetIndentation(level - 1, indentationSize));
        
        builder.Append("}");

        return builder.ToString();
    }

    protected void IncludeScalarField(string fieldName, string alias, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
    {
        _fieldCriteria[alias ?? fieldName] = new GraphQlScalarFieldCriteria(fieldName, alias, args, directives);
    }

    protected void IncludeObjectField(string fieldName, string alias, GraphQlQueryBuilder objectFieldQueryBuilder, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
    {
        _fieldCriteria[alias ?? fieldName] = new GraphQlObjectFieldCriteria(fieldName, alias, objectFieldQueryBuilder, args, directives);
    }

    protected void IncludeFragment(GraphQlQueryBuilder objectFieldQueryBuilder, GraphQlDirective[] directives)
    {
        _fragments = _fragments ?? new Dictionary<string, GraphQlFragmentCriteria>();
        _fragments[objectFieldQueryBuilder.TypeName] = new GraphQlFragmentCriteria(objectFieldQueryBuilder, directives);
    }

    protected void ExcludeField(string fieldName)
    {
        if (fieldName == null)
            throw new ArgumentNullException(nameof(fieldName));

        _fieldCriteria.Remove(fieldName);
    }

    protected void IncludeFields(IEnumerable<FieldMetadata> fields)
    {
        IncludeFields(fields, null);
    }

    private void IncludeFields(IEnumerable<FieldMetadata> fields, List<Type> parentTypes)
    {
        foreach (var field in fields)
        {
            if (field.QueryBuilderType == null)
                IncludeScalarField(field.Name, field.DefaultAlias, null, null);
            else
            {
                var builderType = GetType();

                if (parentTypes != null && parentTypes.Any(t => t.IsAssignableFrom(field.QueryBuilderType)))
                    continue;

                parentTypes?.Add(builderType);

                var queryBuilder = InitializeChildBuilder(builderType, field.QueryBuilderType, parentTypes);

                var includeFragmentMethods = field.QueryBuilderType.GetMethods().Where(IsIncludeFragmentMethod);

                foreach (var includeFragmentMethod in includeFragmentMethods)
                    includeFragmentMethod.Invoke(queryBuilder, new object[] { InitializeChildBuilder(builderType, includeFragmentMethod.GetParameters()[0].ParameterType, parentTypes) });

                IncludeObjectField(field.Name, field.DefaultAlias, queryBuilder, null, null);
            }
        }
    }

    private static GraphQlQueryBuilder InitializeChildBuilder(Type parentQueryBuilderType, Type queryBuilderType, List<Type> parentTypes)
    {
        var queryBuilder = (GraphQlQueryBuilder)Activator.CreateInstance(queryBuilderType);
        queryBuilder.IncludeFields(queryBuilder.AllFields, parentTypes ?? new List<Type> { parentQueryBuilderType });
        return queryBuilder;
    }

    private static bool IsIncludeFragmentMethod(MethodInfo methodInfo)
    {
        if (!methodInfo.Name.StartsWith("With") || !methodInfo.Name.EndsWith("Fragment"))
            return false;

        var parameters = methodInfo.GetParameters();
        return parameters.Length == 1 && parameters[0].ParameterType.IsSubclassOf(typeof(GraphQlQueryBuilder));
    }

    protected void AddParameter<T>(GraphQlQueryParameter<T> parameter)
    {
        if (_queryParameters == null)
            _queryParameters = new List<QueryBuilderArgumentInfo>();
        
        _queryParameters.Add(new QueryBuilderArgumentInfo { ArgumentValue = parameter, FormatMask = parameter.FormatMask });
    }

    private abstract class GraphQlFieldCriteria
    {
        private readonly IList<QueryBuilderArgumentInfo> _args;
        private readonly GraphQlDirective[] _directives;

        protected readonly string FieldName;
        protected readonly string Alias;

        protected static string GetIndentation(Formatting formatting, int level, byte indentationSize) =>
            formatting == Formatting.Indented ? GraphQlQueryHelper.GetIndentation(level, indentationSize) : null;

        protected GraphQlFieldCriteria(string fieldName, string alias, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
        {
            GraphQlQueryHelper.ValidateGraphQlIdentifier(nameof(alias), alias);
            FieldName = fieldName;
            Alias = alias;
            _args = args;
            _directives = directives;
        }

        public abstract string Build(Formatting formatting, int level, byte indentationSize);

        protected string BuildArgumentClause(Formatting formatting, int level, byte indentationSize)
        {
            var separator = formatting == Formatting.Indented ? " " : null;
            var argumentCount = _args?.Count ?? 0;
            if (argumentCount == 0)
                return String.Empty;

            var arguments =
                _args.Select(
                    a => $"{a.ArgumentName}:{separator}{(a.ArgumentValue.Name == null ? GraphQlQueryHelper.BuildArgumentValue(a.ArgumentValue.Value, a.FormatMask, formatting, level, indentationSize) : "$" + a.ArgumentValue.Name)}");

            return $"({String.Join($",{separator}", arguments)})";
        }

        protected string BuildDirectiveClause(Formatting formatting, int level, byte indentationSize) =>
            _directives == null ? null : String.Concat(_directives.Select(d => d == null ? null : GraphQlQueryHelper.BuildDirective(d, formatting, level, indentationSize)));

        protected static string BuildAliasPrefix(string alias, Formatting formatting)
        {
            var separator = formatting == Formatting.Indented ? " " : String.Empty;
            return String.IsNullOrWhiteSpace(alias) ? null : alias + ':' + separator;
        }
    }

    private class GraphQlScalarFieldCriteria : GraphQlFieldCriteria
    {
        public GraphQlScalarFieldCriteria(string fieldName, string alias, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
            : base(fieldName, alias, args, directives)
        {
        }

        public override string Build(Formatting formatting, int level, byte indentationSize) =>
            GetIndentation(formatting, level, indentationSize) +
            BuildAliasPrefix(Alias, formatting) +
            FieldName +
            BuildArgumentClause(formatting, level, indentationSize) +
            BuildDirectiveClause(formatting, level, indentationSize);
    }

    private class GraphQlObjectFieldCriteria : GraphQlFieldCriteria
    {
        private readonly GraphQlQueryBuilder _objectQueryBuilder;

        public GraphQlObjectFieldCriteria(string fieldName, string alias, GraphQlQueryBuilder objectQueryBuilder, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
            : base(fieldName, alias, args, directives)
        {
            _objectQueryBuilder = objectQueryBuilder;
        }

        public override string Build(Formatting formatting, int level, byte indentationSize) =>
            _objectQueryBuilder._fieldCriteria.Count > 0 || _objectQueryBuilder._fragments?.Count > 0
                ? GetIndentation(formatting, level, indentationSize) + BuildAliasPrefix(Alias, formatting) + FieldName +
                  BuildArgumentClause(formatting, level, indentationSize) + BuildDirectiveClause(formatting, level, indentationSize) + _objectQueryBuilder.Build(formatting, level + 1, indentationSize)
                : null;
    }

    private class GraphQlFragmentCriteria : GraphQlFieldCriteria
    {
        private readonly GraphQlQueryBuilder _objectQueryBuilder;

        public GraphQlFragmentCriteria(GraphQlQueryBuilder objectQueryBuilder, GraphQlDirective[] directives) : base(objectQueryBuilder.TypeName, null, null, directives)
        {
            _objectQueryBuilder = objectQueryBuilder;
        }

        public override string Build(Formatting formatting, int level, byte indentationSize) =>
            _objectQueryBuilder._fieldCriteria.Count == 0
                ? null
                : GetIndentation(formatting, level, indentationSize) + "..." + (formatting == Formatting.Indented ? " " : null) + "on " +
                  FieldName + BuildArgumentClause(formatting, level, indentationSize) + BuildDirectiveClause(formatting, level, indentationSize) + _objectQueryBuilder.Build(formatting, level + 1, indentationSize);
    }
}

public abstract class GraphQlQueryBuilder<TQueryBuilder> : GraphQlQueryBuilder where TQueryBuilder : GraphQlQueryBuilder<TQueryBuilder>
{
    protected GraphQlQueryBuilder(string operationType = null, string operationName = null) : base(operationType, operationName)
    {
    }

    public TQueryBuilder WithAllFields()
    {
        IncludeAllFields();
        return (TQueryBuilder)this;
    }

    public TQueryBuilder WithAllScalarFields()
    {
        IncludeFields(AllFields.Where(f => !f.IsComplex));
        return (TQueryBuilder)this;
    }

    public TQueryBuilder ExceptField(string fieldName)
    {
        ExcludeField(fieldName);
        return (TQueryBuilder)this;
    }

    public TQueryBuilder WithTypeName(string alias = null, params GraphQlDirective[] directives)
    {
        IncludeScalarField("__typename", alias, null, directives);
        return (TQueryBuilder)this;
    }

    protected TQueryBuilder WithScalarField(string fieldName, string alias, GraphQlDirective[] directives, IList<QueryBuilderArgumentInfo> args = null)
    {
        IncludeScalarField(fieldName, alias, args, directives);
        return (TQueryBuilder)this;
    }

    protected TQueryBuilder WithObjectField(string fieldName, string alias, GraphQlQueryBuilder queryBuilder, GraphQlDirective[] directives, IList<QueryBuilderArgumentInfo> args = null)
    {
        IncludeObjectField(fieldName, alias, queryBuilder, args, directives);
        return (TQueryBuilder)this;
    }

    protected TQueryBuilder WithFragment(GraphQlQueryBuilder queryBuilder, GraphQlDirective[] directives)
    {
        IncludeFragment(queryBuilder, directives);
        return (TQueryBuilder)this;
    }

    protected TQueryBuilder WithParameterInternal<T>(GraphQlQueryParameter<T> parameter)
    {
        AddParameter(parameter);
        return (TQueryBuilder)this;
    }
}

public abstract class GraphQlResponse<TDataContract>
{
    public TDataContract Data { get; set; }
    public ICollection<QueryError> Errors { get; set; }
}

public class QueryError
{
    public string Message { get; set; }
    public ICollection<ErrorLocation> Locations { get; set; }
}

public class ErrorLocation
{
    public int Line { get; set; }
    public int Column { get; set; }
}