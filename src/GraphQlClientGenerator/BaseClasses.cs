#region base classes
public class FieldMetadata
{
    public string Name { get; set; }
    public bool IsComplex { get; set; }
    public Type QueryBuilderType { get; set; }
}

public enum Formatting
{
    None,
    Indented
}

internal static class GraphQlQueryHelper
{
    private static readonly Regex RegexWhiteSpace = new Regex(@"\s", RegexOptions.Compiled);

    public static string GetIndentation(int level, byte indentationSize)
    {
        return new String(' ', level * indentationSize);
    }

    public static string BuildArgumentValue(object value, Formatting formatting, int level, byte indentationSize)
    {
        if (value is JValue jValue)
        {
            switch (jValue.Type)
            {
                case JTokenType.Null: return "null";
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Boolean:
                    return BuildArgumentValue(jValue.Value, formatting, level, indentationSize);
                default:
                    return $"\"{jValue.Value}\"";
            }
        }

        if (value is Enum @enum)
            return ConvertEnumToString(@enum);

        if (value is bool @bool)
            return @bool ? "true" : "false";

        if (value is DateTime dateTime)
            return $"\"{dateTime:O}\"";

        if (value is DateTimeOffset dateTimeOffset)
            return $"\"{dateTimeOffset:O}\"";

        if (value is IGraphQlInputObject inputObject)
            return BuildInputObject(inputObject, formatting, level + 2, indentationSize);

        if (value is String || value is Guid)
            return $"\"{value}\"";

        if (value is JProperty jProperty)
        {
            if (RegexWhiteSpace.IsMatch(jProperty.Name))
			    throw new ArgumentException($"JSON object keys used as GraphQL arguments must not contain whitespace; key: {jProperty.Name}");

            return $"{jProperty.Name}:{(formatting == Formatting.Indented ? " " : null)}{BuildArgumentValue(jProperty.Value, formatting, level, indentationSize)}";
        }

        if (value is JObject jObject)
            return BuildEnumerableArgument(jObject, formatting, level + 1, indentationSize, '{', '}');

        if (value is IEnumerable enumerable)
            return BuildEnumerableArgument(enumerable, formatting, level, indentationSize, '[', ']');

        if (value is short || value is ushort || value is byte || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
            return Convert.ToString(value, CultureInfo.InvariantCulture);

        var argumentValue = Convert.ToString(value, CultureInfo.InvariantCulture);
        return $"\"{argumentValue}\"";
    }

    private static string BuildEnumerableArgument(IEnumerable enumerable, Formatting formatting, int level, byte indentationSize, char openingSymbol, char closingSymbol)
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

            builder.Append(BuildArgumentValue(item, formatting, level, indentationSize));
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
        foreach (var propertyValue in inputObject.GetPropertyValues().Where(p => p.Value != null))
        {
            var value = BuildArgumentValue(propertyValue.Value, formatting, level, indentationSize);
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

    private static string ConvertEnumToString(Enum @enum)
    {
        var enumMember = @enum.GetType().GetTypeInfo().GetField(@enum.ToString());
            if (enumMember == null)
                throw new InvalidOperationException("enum member resolution failed");

        var enumMemberAttribute = (EnumMemberAttribute)enumMember.GetCustomAttribute(typeof(EnumMemberAttribute));

        return enumMemberAttribute == null
            ? @enum.ToString()
            : enumMemberAttribute.Value;
    }
}

public struct InputPropertyInfo
{
    public string Name { get; set; }
    public object Value { get; set; }
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
            ValidateParameterValue(nameof(Name), value);
            _name = value;
        }
    }

    protected QueryBuilderParameter(string name, string graphQlTypeName, object value)
    {
        Name = name;

        ValidateParameterValue(nameof(graphQlTypeName), graphQlTypeName);

        GraphQlTypeName = graphQlTypeName;
        Value = value;
    }

    protected QueryBuilderParameter(object value) => Value = value;

    private void ValidateParameterValue(string parameterName, string value)
    {
        if (String.IsNullOrWhiteSpace(value) || !value.All(Char.IsLetterOrDigit))
            throw new ArgumentException("invalid value", parameterName);
    }
}

public class QueryBuilderParameter<T> : QueryBuilderParameter
{
    public QueryBuilderParameter(string name, string graphQlTypeName, T value) : base(name, graphQlTypeName, value)
    {
    }

    private QueryBuilderParameter(T value) : base(value)
    {
    }

    public static implicit operator QueryBuilderParameter<T>(T value) => new QueryBuilderParameter<T>(value);
}

public class GraphQlQueryParameter<T> : QueryBuilderParameter<T>
{
    public new T Value
    {
        get => (T)base.Value;
        set => base.Value = value;
    }

    public GraphQlQueryParameter(string name, string graphQlTypeName, T value) : base(name, graphQlTypeName, value)
    {
    }
}

public abstract class GraphQlQueryBuilder : IGraphQlQueryBuilder
{
    private static readonly Type[] MethodParameterTypeString = { typeof(String) };
    private static readonly string[] MethodParameterString = { null };

    private readonly Dictionary<string, GraphQlFieldCriteria> _fieldCriteria = new Dictionary<string, GraphQlFieldCriteria>();
    
    private Dictionary<string, QueryBuilderParameter> _queryParameters;

    protected virtual string Prefix { get { return null; } }

    protected abstract IList<FieldMetadata> AllFields { get; }

    public string Alias { get; }

    protected GraphQlQueryBuilder(string alias = null)
    {
        ValidateAlias(alias);
        Alias = alias;
    }

    public virtual void Clear()
    {
        _fieldCriteria.Clear();
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
        var builder = new StringBuilder();

        if (!String.IsNullOrEmpty(Prefix))
        {
            builder.Append(Prefix);

            var indentationSpace = isIndentedFormatting ? " " : String.Empty;

            if (!String.IsNullOrEmpty(Alias))
            {
                builder.Append(" ");
                builder.Append(Alias);
            }

            builder.Append(indentationSpace);

            if (_queryParameters != null)
            {
                builder.Append("(");

                foreach (var queryParameter in _queryParameters.Values)
                {
                    builder.Append(separator);
                    builder.Append("$");
                    builder.Append(queryParameter.Name);
                    builder.Append(":");
                    builder.Append(indentationSpace);

                    builder.Append(queryParameter.GraphQlTypeName);
                    builder.Append(isIndentedFormatting ? " = " : "=");
                    builder.Append(GraphQlQueryHelper.BuildArgumentValue(queryParameter.Value, formatting, 0, indentationSize));

                    separator = " ";
                }

                builder.Append(")");
                builder.Append(indentationSpace);
            }
        }

        builder.Append("{");

        if (isIndentedFormatting)
            builder.AppendLine();

        separator = String.Empty;
        
        foreach (var criteria in _fieldCriteria.Values)
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

    protected void IncludeScalarField(string fieldName, string alias, IDictionary<string, QueryBuilderParameter> args)
    {
        ValidateAlias(alias);
        _fieldCriteria[alias ?? fieldName] = new GraphQlScalarFieldCriteria(fieldName, alias, args);
    }

    protected void IncludeObjectField(string fieldName, GraphQlQueryBuilder objectFieldQueryBuilder, IDictionary<string, QueryBuilderParameter> args)
    {
        _fieldCriteria[objectFieldQueryBuilder.Alias ?? fieldName] = new GraphQlObjectFieldCriteria(fieldName, objectFieldQueryBuilder, args);
    }

    protected void ExcludeField(string fieldName)
    {
        if (fieldName == null)
            throw new ArgumentNullException(nameof(fieldName));

        _fieldCriteria.Remove(fieldName);
    }

    protected void IncludeFields(IEnumerable<FieldMetadata> fields)
    {
        foreach (var field in fields)
        {
            if (field.QueryBuilderType == null)
                IncludeScalarField(field.Name, null, null);
            else
            {
                var builderType = GetType();
                if (builderType.IsAssignableFrom(field.QueryBuilderType))
                    throw new InvalidOperationException($"Field '{builderType.Name}.{field.Name}' cannot be added because its type is the same as the parent type (or inherited) and leads to infinite recursion. ");

                var constructor = field.QueryBuilderType.GetConstructor(MethodParameterTypeString);
                if (constructor == null)
                    throw new InvalidOperationException($"{field.QueryBuilderType.FullName} constructor not found");

                var queryBuilder = (GraphQlQueryBuilder)constructor.Invoke(MethodParameterString);
                queryBuilder.IncludeAllFields();
                IncludeObjectField(field.Name, queryBuilder, null);
            }
        }
    }

    protected void AddParameter<T>(GraphQlQueryParameter<T> parameter)
    {
        if (_queryParameters == null)
            _queryParameters = new Dictionary<string, QueryBuilderParameter>();
        
        _queryParameters.Add(parameter.Name, parameter);
    }

    private static void ValidateAlias(string alias)
    {
        if (alias != null && String.IsNullOrWhiteSpace(alias))
            throw new ArgumentException("Value must not be white space. ", nameof(alias));
    }

    private abstract class GraphQlFieldCriteria
    {
        private readonly IDictionary<string, QueryBuilderParameter> _args;

        protected readonly string FieldName;

        protected GraphQlFieldCriteria(string fieldName, IDictionary<string, QueryBuilderParameter> args)
        {
            FieldName = fieldName;
            _args = args;
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
                    kvp => $"{kvp.Key}:{separator}{(kvp.Value.Name == null ? GraphQlQueryHelper.BuildArgumentValue(kvp.Value.Value, formatting, level, indentationSize) : "$" + kvp.Value.Name)}");

            return $"({String.Join($",{separator}", arguments)}){separator}";
        }

        protected static string BuildAliasPrefix(string alias, Formatting formatting)
        {
            var separator = formatting == Formatting.Indented ? " " : String.Empty;
            return String.IsNullOrWhiteSpace(alias) ? null : alias + ':' + separator;
        }
    }

    private class GraphQlScalarFieldCriteria : GraphQlFieldCriteria
    {
        private readonly string _alias;

        public GraphQlScalarFieldCriteria(string fieldName, string alias, IDictionary<string, QueryBuilderParameter> args) : base(fieldName, args)
        {
            _alias = alias;
        }

        public override string Build(Formatting formatting, int level, byte indentationSize)
        {
            var builder = new StringBuilder();
            if (formatting == Formatting.Indented)
                builder.Append(GraphQlQueryHelper.GetIndentation(level, indentationSize));

            builder.Append(BuildAliasPrefix(_alias, formatting));
            builder.Append(FieldName);
            builder.Append(BuildArgumentClause(formatting, level, indentationSize));
            return builder.ToString();
        }
    }

    private class GraphQlObjectFieldCriteria : GraphQlFieldCriteria
    {
        private readonly GraphQlQueryBuilder _objectQueryBuilder;

        public GraphQlObjectFieldCriteria(string fieldName, GraphQlQueryBuilder objectQueryBuilder, IDictionary<string, QueryBuilderParameter> args) : base(fieldName, args)
        {
            _objectQueryBuilder = objectQueryBuilder;
        }

        public override string Build(Formatting formatting, int level, byte indentationSize)
        {
            if (_objectQueryBuilder._fieldCriteria.Count == 0)
                return String.Empty;

            var builder = new StringBuilder();

            if (formatting == Formatting.Indented)
                builder.Append(GraphQlQueryHelper.GetIndentation(level, indentationSize));

            builder.Append(BuildAliasPrefix(_objectQueryBuilder.Alias, formatting));
            builder.Append(FieldName);
            builder.Append(BuildArgumentClause(formatting, level, indentationSize));
            builder.Append(_objectQueryBuilder.Build(formatting, level + 1, indentationSize));
            return builder.ToString();
        }
    }
}

public abstract class GraphQlQueryBuilder<TQueryBuilder> : GraphQlQueryBuilder where TQueryBuilder : GraphQlQueryBuilder<TQueryBuilder>
{
    protected GraphQlQueryBuilder(string alias = null) : base(alias)
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

    protected TQueryBuilder WithScalarField(string fieldName, string alias = null, IDictionary<string, QueryBuilderParameter> args = null)
    {
        IncludeScalarField(fieldName, alias, args);
        return (TQueryBuilder)this;
    }

    protected TQueryBuilder WithObjectField(string fieldName, GraphQlQueryBuilder queryBuilder, IDictionary<string, QueryBuilderParameter> args = null)
    {
        IncludeObjectField(fieldName, queryBuilder, args);
        return (TQueryBuilder)this;
    }

    public TQueryBuilder ExceptField(string fieldName)
    {
        ExcludeField(fieldName);
        return (TQueryBuilder)this;
    }

    protected TQueryBuilder WithParameterInternal<T>(GraphQlQueryParameter<T> parameter)
    {
        AddParameter(parameter);
        return (TQueryBuilder)this;
    }
}
#endregion
