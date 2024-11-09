public struct GraphQlFieldMetadata
{
    public string Name { get; set; }
    public string DefaultAlias { get; set; }
    public bool IsComplex { get; set; }
    public bool RequiresParameters { get; set; }
    public global::System.Type QueryBuilderType { get; set; }
}

public enum Formatting
{
    None,
    Indented
}

public class GraphQlObjectTypeAttribute : global::System.Attribute
{
    public string TypeName { get; }

    public GraphQlObjectTypeAttribute(string typeName) => TypeName = typeName;
}

#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
public class QueryBuilderParameterConverter<T> : global::Newtonsoft.Json.JsonConverter
{
    public override object ReadJson(JsonReader reader, global::System.Type objectType, object existingValue, JsonSerializer serializer)
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

    public override bool CanConvert(global::System.Type objectType) => objectType.IsSubclassOf(typeof(QueryBuilderParameter));
}

public class GraphQlInterfaceJsonConverter : global::Newtonsoft.Json.JsonConverter
{
    private const string FieldNameType = "__typename";

    private static readonly Dictionary<string, global::System.Type> InterfaceTypeMapping =
        typeof(GraphQlInterfaceJsonConverter).Assembly.GetTypes()
            .Select(t => new { Type = t, Attribute = t.GetCustomAttribute<GraphQlObjectTypeAttribute>() })
            .Where(x => x.Attribute != null && x.Type.Namespace == typeof(GraphQlInterfaceJsonConverter).Namespace)
            .ToDictionary(x => x.Attribute.TypeName, x => x.Type);

    public override bool CanConvert(global::System.Type objectType) => objectType.IsInterface || objectType.IsArray;

    public override object ReadJson(JsonReader reader, global::System.Type objectType, object existingValue, JsonSerializer serializer)
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

    private static global::System.Type GetElementType(global::System.Type arrayOrGenericContainer) =>
        arrayOrGenericContainer.IsArray ? arrayOrGenericContainer.GetElementType() : arrayOrGenericContainer.GenericTypeArguments.FirstOrDefault();

    private IList ReadArray(JsonReader reader, global::System.Type targetType, global::System.Type elementType, JsonSerializer serializer)
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

    private static IList CreateCompatibleList(global::System.Type targetContainerType, global::System.Type elementType) =>
        (IList)Activator.CreateInstance(targetContainerType.IsArray || targetContainerType.IsAbstract ? typeof(List<>).MakeGenericType(elementType) : targetContainerType);
}
#endif

internal static class GraphQlQueryHelper
{
    private static readonly Regex RegexGraphQlIdentifier = new Regex(@"^[_A-Za-z][_0-9A-Za-z]*$", RegexOptions.Compiled);
    private static readonly Regex RegexEscapeGraphQlString = new Regex(@"[\\\""/\b\f\n\r\t]", RegexOptions.Compiled);

    public static string GetIndentation(int level, byte indentationSize)
    {
        return new String(' ', level * indentationSize);
    }

    public static string EscapeGraphQlStringValue(string value)
    {
        return RegexEscapeGraphQlString.Replace(value, m => @$"\{GetEscapeSequence(m.Value)}");
    }

    private static string GetEscapeSequence(string input)
    {
        switch (input)
        {
            case "\\":
                return "\\";
            case "\"":
                return "\"";
            case "/":
                return "/";
            case "\b":
                return "b";
            case "\f":
                return "f";
            case "\n":
                return "n";
            case "\r":
                return "r";
            case "\t":
                return "t";
            default:
                throw new InvalidOperationException($"invalid character: {input}");
        }
    }

    public static string BuildArgumentValue(object value, string formatMask, GraphQlBuilderOptions options, int level)
    {
        var serializer = options.ArgumentBuilder ?? DefaultGraphQlArgumentBuilder.Instance;
        if (serializer.TryBuild(new GraphQlArgumentBuilderContext { Value = value, FormatMask = formatMask, Options = options, Level = level }, out var serializedValue))
            return serializedValue;

        if (value is null)
            return "null";

        var enumerable = value as IEnumerable;
        if (!String.IsNullOrEmpty(formatMask) && enumerable == null)
            return
                value is IFormattable formattable
                    ? $"\"{EscapeGraphQlStringValue(formattable.ToString(formatMask, CultureInfo.InvariantCulture))}\""
                    : throw new ArgumentException($"Value must implement {nameof(IFormattable)} interface to use a format mask. ", nameof(value));

        if (value is Enum @enum)
            return ConvertEnumToString(@enum);

        if (value is bool @bool)
            return @bool ? "true" : "false";

        if (value is DateTime dateTime)
            return $"\"{dateTime.ToString("O")}\"";

        if (value is DateTimeOffset dateTimeOffset)
            return $"\"{dateTimeOffset.ToString("O")}\"";

        if (value is IGraphQlInputObject inputObject)
            return BuildInputObject(inputObject, options, level + 2);

        if (value is Guid)
            return $"\"{value}\"";

        if (value is String @string)
            return $"\"{EscapeGraphQlStringValue(@string)}\"";

        if (enumerable != null)
            return BuildEnumerableArgument(enumerable, formatMask, options, level, '[', ']');

        if (value is short || value is ushort || value is byte || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
            return Convert.ToString(value, CultureInfo.InvariantCulture);

        var argumentValue = EscapeGraphQlStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
        return $"\"{argumentValue}\"";
    }

    public static string BuildEnumerableArgument(IEnumerable enumerable, string formatMask, GraphQlBuilderOptions options, int level, char openingSymbol, char closingSymbol)
    {
        var builder = new StringBuilder();
        builder.Append(openingSymbol);
        var delimiter = String.Empty;
        foreach (var item in enumerable)
        {
            builder.Append(delimiter);

            if (options.Formatting == Formatting.Indented)
            {
                builder.AppendLine();
                builder.Append(GetIndentation(level + 1, options.IndentationSize));
            }

            builder.Append(BuildArgumentValue(item, formatMask, options, level));
            delimiter = ",";
        }

        builder.Append(closingSymbol);
        return builder.ToString();
    }

    public static string BuildInputObject(IGraphQlInputObject inputObject, GraphQlBuilderOptions options, int level)
    {
        var builder = new StringBuilder();
        builder.Append("{");

        var isIndentedFormatting = options.Formatting == Formatting.Indented;
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
                    ? $"${queryBuilderParameter.Name}"
                    : BuildArgumentValue(queryBuilderParameter == null ? propertyValue.Value : queryBuilderParameter.Value, propertyValue.FormatMask, options, level);

            builder.Append(isIndentedFormatting ? GetIndentation(level, options.IndentationSize) : separator);
            builder.Append(propertyValue.Name);
            builder.Append(valueSeparator);
            builder.Append(value);

            separator = ",";

            if (isIndentedFormatting)
                builder.AppendLine();
        }

        if (isIndentedFormatting)
            builder.Append(GetIndentation(level - 1, options.IndentationSize));

        builder.Append("}");

        return builder.ToString();
    }

    public static string BuildDirective(GraphQlDirective directive, GraphQlBuilderOptions options, int level)
    {
        if (directive == null)
            return String.Empty;

        var isIndentedFormatting = options.Formatting == Formatting.Indented;
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
                builder.Append(BuildArgumentValue(argument.Value, null, options, level));
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
            throw new ArgumentException("value must match " + RegexGraphQlIdentifier, name);
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

public interface IGraphQlArgumentBuilder
{
    bool TryBuild(GraphQlArgumentBuilderContext context, out string graphQlString);
}

public class GraphQlArgumentBuilderContext
{
    public object Value { get; set; }
    public string FormatMask { get; set; }
    public GraphQlBuilderOptions Options { get; set; }
    public int Level { get; set; }
}

public class DefaultGraphQlArgumentBuilder : IGraphQlArgumentBuilder
{
    private static readonly Regex RegexWhiteSpace = new Regex(@"\s", RegexOptions.Compiled);

    public static readonly DefaultGraphQlArgumentBuilder Instance = new();

    public bool TryBuild(GraphQlArgumentBuilderContext context, out string graphQlString)
    {
#if !GRAPHQL_GENERATOR_DISABLE_NEWTONSOFT_JSON
        if (context.Value is JValue jValue)
        {
            switch (jValue.Type)
            {
                case JTokenType.Null:
                    graphQlString = "null";
                    return true;

                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Boolean:
                    graphQlString = GraphQlQueryHelper.BuildArgumentValue(jValue.Value, null, context.Options, context.Level);
                    return true;

                case JTokenType.String:
                    graphQlString = $"\"{GraphQlQueryHelper.EscapeGraphQlStringValue((string)jValue.Value)}\"";
                    return true;

                default:
                    graphQlString = $"\"{jValue.Value}\"";
                    return true;
            }
        }

        if (context.Value is JProperty jProperty)
        {
            if (RegexWhiteSpace.IsMatch(jProperty.Name))
                throw new ArgumentException($"JSON object keys used as GraphQL arguments must not contain whitespace; key: {jProperty.Name}");

            graphQlString = $"{jProperty.Name}:{(context.Options.Formatting == Formatting.Indented ? " " : null)}{GraphQlQueryHelper.BuildArgumentValue(jProperty.Value, null, context.Options, context.Level)}";
            return true;
        }

        if (context.Value is JObject jObject)
        {
            graphQlString = GraphQlQueryHelper.BuildEnumerableArgument(jObject, null, context.Options, context.Level + 1, '{', '}');
            return true;
        }
#endif

        graphQlString = null;
        return false;
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
        get => base.Value == null ? default : (T)base.Value;
        set => base.Value = value;
    }

    protected QueryBuilderParameter(string name, string graphQlTypeName, T value) : base(name, graphQlTypeName, value)
    {
        EnsureGraphQlTypeName(graphQlTypeName);
    }

    protected QueryBuilderParameter(string name, string graphQlTypeName) : base(name, graphQlTypeName, null)
    {
        EnsureGraphQlTypeName(graphQlTypeName);
    }

    private QueryBuilderParameter(T value) : base(value)
    {
    }

    public void ResetValue() => base.Value = null;

    public static implicit operator QueryBuilderParameter<T>(T value) => new QueryBuilderParameter<T>(value);

    public static implicit operator T(QueryBuilderParameter<T> parameter) => parameter.Value;

    private static void EnsureGraphQlTypeName(string graphQlTypeName)
    {
        if (String.IsNullOrWhiteSpace(graphQlTypeName))
            throw new ArgumentException("value required", nameof(graphQlTypeName));
    }
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

    public GraphQlQueryParameter(string name, string graphQlTypeName = null)
        : base(name, graphQlTypeName ?? GetGraphQlTypeName(typeof(T)))
    {
    }

    public GraphQlQueryParameter(string name, string graphQlTypeName, T defaultValue)
        : base(name, graphQlTypeName, defaultValue)
    {
    }

    public GraphQlQueryParameter(string name, T defaultValue, bool isNullable = true)
        : base(name, GetGraphQlTypeName(typeof(T), isNullable), defaultValue)
    {
    }

    private static string GetGraphQlTypeName(global::System.Type valueType, bool isNullable)
    {
        var graphQlTypeName = GetGraphQlTypeName(valueType);
        if (!isNullable)
            graphQlTypeName += "!";

        return graphQlTypeName;
    }

    private static string GetGraphQlTypeName(global::System.Type valueType)
    {
        var nullableUnderlyingType = Nullable.GetUnderlyingType(valueType);
        valueType = nullableUnderlyingType ?? valueType;

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

        if (valueType == typeof(string))
            return "String";

        var nullableSuffix = nullableUnderlyingType == null ? null : "?";
        graphQlTypeName = GetValueTypeGraphQlTypeName(valueType);
        return graphQlTypeName == null ? null : graphQlTypeName + nullableSuffix;
    }

    private static string GetValueTypeGraphQlTypeName(global::System.Type valueType)
    {
        if (valueType == typeof(bool))
            return "Boolean";

        if (valueType == typeof(float) || valueType == typeof(double) || valueType == typeof(decimal))
            return "Float";

        if (valueType == typeof(Guid))
            return "ID";

        if (valueType == typeof(sbyte) || valueType == typeof(byte) || valueType == typeof(short) || valueType == typeof(ushort) || valueType == typeof(int) || valueType == typeof(uint) ||
            valueType == typeof(long) || valueType == typeof(ulong))
            return "Int";

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

public class GraphQlBuilderOptions
{
    public Formatting Formatting { get; set; }
    public byte IndentationSize { get; set; } = 2;
    public IGraphQlArgumentBuilder ArgumentBuilder { get; set; }
}

public abstract partial class GraphQlQueryBuilder : IGraphQlQueryBuilder
{
    private readonly Dictionary<string, GraphQlFieldCriteria> _fieldCriteria = new Dictionary<string, GraphQlFieldCriteria>();

    private readonly string _operationType;
    private readonly string _operationName;
    private Dictionary<string, GraphQlFragmentCriteria> _fragments;
    private List<QueryBuilderArgumentInfo> _queryParameters;

    protected abstract string TypeName { get; }

    public abstract IReadOnlyList<GraphQlFieldMetadata> AllFields { get; }

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
        return Build(new GraphQlBuilderOptions { Formatting = formatting, IndentationSize = indentationSize });
    }

    public string Build(GraphQlBuilderOptions options)
    {
        return Build(options, 1);
    }

    protected void IncludeAllFields()
    {
        IncludeFields(AllFields.Where(f => !f.RequiresParameters));
    }

    protected virtual string Build(GraphQlBuilderOptions options, int level)
    {
        var isIndentedFormatting = options.Formatting == Formatting.Indented;
        var separator = String.Empty;
        var indentationSpace = isIndentedFormatting ? " " : String.Empty;
        var builder = new StringBuilder();

        BuildOperationSignature(builder, options, indentationSpace, level);

        if (builder.Length > 0 || level > 1)
            builder.Append(indentationSpace);

        builder.Append("{");

        if (isIndentedFormatting)
            builder.AppendLine();

        separator = String.Empty;

        foreach (var criteria in _fieldCriteria.Values.Concat(_fragments?.Values ?? Enumerable.Empty<GraphQlFragmentCriteria>()))
        {
            var fieldCriteria = criteria.Build(options, level);
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
            builder.Append(GraphQlQueryHelper.GetIndentation(level - 1, options.IndentationSize));

        builder.Append("}");

        return builder.ToString();
    }

    private void BuildOperationSignature(StringBuilder builder, GraphQlBuilderOptions options, string indentationSpace, int level)
    {
        if (String.IsNullOrEmpty(_operationType))
            return;

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

            var separator = String.Empty;
            var isIndentedFormatting = options.Formatting == Formatting.Indented;

            foreach (var queryParameterInfo in _queryParameters)
            {
                if (isIndentedFormatting)
                {
                    builder.AppendLine(separator);
                    builder.Append(GraphQlQueryHelper.GetIndentation(level, options.IndentationSize));
                }
                else
                    builder.Append(separator);

                builder.Append("$");
                builder.Append(queryParameterInfo.ArgumentValue.Name);
                builder.Append(":");
                builder.Append(indentationSpace);

                builder.Append(queryParameterInfo.ArgumentValue.GraphQlTypeName);

                if (!queryParameterInfo.ArgumentValue.GraphQlTypeName.EndsWith("!") && queryParameterInfo.ArgumentValue.Value is not null)
                {
                    builder.Append(indentationSpace);
                    builder.Append("=");
                    builder.Append(indentationSpace);
                    builder.Append(GraphQlQueryHelper.BuildArgumentValue(queryParameterInfo.ArgumentValue.Value, queryParameterInfo.FormatMask, options, 0));
                }

                if (!isIndentedFormatting)
                    separator = ",";
            }

            builder.Append(")");
        }
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

    protected void IncludeFields(IEnumerable<GraphQlFieldMetadata> fields)
    {
        IncludeFields(fields, 0, new Dictionary<global::System.Type, int>());
    }

    private void IncludeFields(IEnumerable<GraphQlFieldMetadata> fields, int level, Dictionary<global::System.Type, int> parentTypeLevel)
    {
        global::System.Type builderType = null;

        foreach (var field in fields)
        {
            if (field.QueryBuilderType == null)
                IncludeScalarField(field.Name, field.DefaultAlias, null, null);
            else
            {
                int parentLevel;
                if (_operationType != null && GetType() == field.QueryBuilderType ||
                    parentTypeLevel.TryGetValue(field.QueryBuilderType, out parentLevel) && parentLevel < level)
                    continue;

                if (builderType == null)
                {
                    builderType = GetType();
                    parentLevel = parentTypeLevel.TryGetValue(builderType, out parentLevel) ? parentLevel : level;
                    parentTypeLevel[builderType] = Math.Min(level, parentLevel);
                }

                var queryBuilder = InitializeChildQueryBuilder(field.QueryBuilderType, level, parentTypeLevel);

                foreach (var includeFragmentMethod in field.QueryBuilderType.GetMethods().Where(IsIncludeFragmentMethod))
                {
                    var includeFragmentParameterInfo = includeFragmentMethod.GetParameters();
                    var includeFragmentQueryBuilderType = includeFragmentParameterInfo[0].ParameterType;
                    if (parentTypeLevel.TryGetValue(includeFragmentQueryBuilderType, out parentLevel))
                        continue;

                    var includeFragmentParameters = new object[includeFragmentParameterInfo.Length];
                    includeFragmentParameters[0] = InitializeChildQueryBuilder(includeFragmentQueryBuilderType, level, parentTypeLevel);
                    includeFragmentMethod.Invoke(queryBuilder, includeFragmentParameters);
                }

                if (queryBuilder._fieldCriteria.Count > 0 || queryBuilder._fragments != null)
                    IncludeObjectField(field.Name, field.DefaultAlias, queryBuilder, null, null);
            }
        }
    }

    private static GraphQlQueryBuilder InitializeChildQueryBuilder(global::System.Type queryBuilderType, int level, Dictionary<global::System.Type, int> parentTypeLevel)
    {
        var queryBuilder = (GraphQlQueryBuilder)Activator.CreateInstance(queryBuilderType);
        queryBuilder.IncludeFields(
            queryBuilder.AllFields.Where(f => !f.RequiresParameters),
            level + 1,
            parentTypeLevel);

        return queryBuilder;
    }

    private static bool IsIncludeFragmentMethod(MethodInfo methodInfo)
    {
        if (!methodInfo.Name.StartsWith("With") || !methodInfo.Name.EndsWith("Fragment"))
            return false;

        var parameters = methodInfo.GetParameters();
        return parameters.Count(p => !p.IsOptional) == 1 && parameters[0].ParameterType.IsSubclassOf(typeof(GraphQlQueryBuilder));
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

        public abstract string Build(GraphQlBuilderOptions options, int level);

        protected string BuildArgumentClause(GraphQlBuilderOptions options, int level)
        {
            var separator = options.Formatting == Formatting.Indented ? " " : null;
            var argumentCount = _args?.Count ?? 0;
            if (argumentCount == 0)
                return String.Empty;

            var arguments =
                _args.Select(
                    a => $"{a.ArgumentName}:{separator}{(a.ArgumentValue.Name == null ? GraphQlQueryHelper.BuildArgumentValue(a.ArgumentValue.Value, a.FormatMask, options, level) : $"${a.ArgumentValue.Name}")}");

            return $"({String.Join($",{separator}", arguments)})";
        }

        protected string BuildDirectiveClause(GraphQlBuilderOptions options, int level) =>
            _directives == null ? null : String.Concat(_directives.Select(d => d == null ? null : GraphQlQueryHelper.BuildDirective(d, options, level)));

        protected static string BuildAliasPrefix(string alias, Formatting formatting)
        {
            var separator = formatting == Formatting.Indented ? " " : String.Empty;
            return String.IsNullOrWhiteSpace(alias) ? null : $"{alias}:{separator}";
        }
    }

    private class GraphQlScalarFieldCriteria : GraphQlFieldCriteria
    {
        public GraphQlScalarFieldCriteria(string fieldName, string alias, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
            : base(fieldName, alias, args, directives)
        {
        }

        public override string Build(GraphQlBuilderOptions options, int level) =>
            GetIndentation(options.Formatting, level, options.IndentationSize) +
            BuildAliasPrefix(Alias, options.Formatting) +
            FieldName +
            BuildArgumentClause(options, level) +
            BuildDirectiveClause(options, level);
    }

    private class GraphQlObjectFieldCriteria : GraphQlFieldCriteria
    {
        private readonly GraphQlQueryBuilder _objectQueryBuilder;

        public GraphQlObjectFieldCriteria(string fieldName, string alias, GraphQlQueryBuilder objectQueryBuilder, IList<QueryBuilderArgumentInfo> args, GraphQlDirective[] directives)
            : base(fieldName, alias, args, directives)
        {
            _objectQueryBuilder = objectQueryBuilder;
        }

        public override string Build(GraphQlBuilderOptions options, int level) =>
            _objectQueryBuilder._fieldCriteria.Count > 0 || _objectQueryBuilder._fragments?.Count > 0
                ? GetIndentation(options.Formatting, level, options.IndentationSize) + BuildAliasPrefix(Alias, options.Formatting) + FieldName +
                  BuildArgumentClause(options, level) + BuildDirectiveClause(options, level) + _objectQueryBuilder.Build(options, level + 1)
                : null;
    }

    private class GraphQlFragmentCriteria : GraphQlFieldCriteria
    {
        private readonly GraphQlQueryBuilder _objectQueryBuilder;

        public GraphQlFragmentCriteria(GraphQlQueryBuilder objectQueryBuilder, GraphQlDirective[] directives) : base(objectQueryBuilder.TypeName, null, null, directives)
        {
            _objectQueryBuilder = objectQueryBuilder;
        }

        public override string Build(GraphQlBuilderOptions options, int level) =>
            _objectQueryBuilder._fieldCriteria.Count == 0
                ? null
                : GetIndentation(options.Formatting, level, options.IndentationSize) + "..." + (options.Formatting == Formatting.Indented ? " " : null) + "on " +
                  FieldName + BuildArgumentClause(options, level) + BuildDirectiveClause(options, level) + _objectQueryBuilder.Build(options, level + 1);
    }
}

public abstract partial class GraphQlQueryBuilder<TQueryBuilder> : GraphQlQueryBuilder where TQueryBuilder : GraphQlQueryBuilder<TQueryBuilder>
{
    protected GraphQlQueryBuilder(string operationType = null, string operationName = null) : base(operationType, operationName)
    {
    }

    /// <summary>
    /// Includes all fields that don't require parameters into the query.
    /// </summary>
    public TQueryBuilder WithAllFields()
    {
        IncludeAllFields();
        return (TQueryBuilder)this;
    }

    /// <summary>
    /// Includes all scalar fields that don't require parameters into the query.
    /// </summary>
    public TQueryBuilder WithAllScalarFields()
    {
        IncludeFields(AllFields.Where(f => !f.IsComplex && !f.RequiresParameters));
        return (TQueryBuilder)this;
    }

    public TQueryBuilder ExceptField(string fieldName)
    {
        ExcludeField(fieldName);
        return (TQueryBuilder)this;
    }

    /// <summary>
    /// Includes "__typename" field; included automatically for interface and union types.
    /// </summary>
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
    public ICollection<GraphQlQueryError> Errors { get; set; }
}

public class GraphQlQueryError
{
    public string Message { get; set; }
    public ICollection<GraphQlErrorLocation> Locations { get; set; }
}

public class GraphQlErrorLocation
{
    public int Line { get; set; }
    public int Column { get; set; }
}