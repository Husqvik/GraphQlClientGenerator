using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace GraphQlClientGenerator
{
    public class GraphQlGenerator
    {
        private static readonly JsonSerializerSettings SerializerSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = { new StringEnumConverter() }
            };

        public static async Task<GraphQlSchema> RetrieveSchema(string url, string token)
        {
            using (var client = new HttpClient { BaseAddress = new Uri(url) })
            {
                var response =
                    await client.PostAsync(
                        $"{url}/gql?token={token}",
                        new StringContent(JsonConvert.SerializeObject(new { query = IntrospectionQuery.Text }), Encoding.UTF8, "application/json")
                    );

                var jsonResult = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new InvalidOperationException(jsonResult);

                return JsonConvert.DeserializeObject<GraphQlResult>(jsonResult, SerializerSettings).Data.Schema;
            }
        }

        public static void GenerateQueryBuilder(GraphQlSchema schema, StringBuilder builder)
        {
            builder.AppendLine(
                @"#region base classes
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

public abstract class GraphQlQueryBuilder
{
    private const int IndentationSize = 2;

    private static readonly IList<FieldMetadata> EmptyFieldCollection = new List<FieldMetadata>();

    private readonly Dictionary<string, GraphQlFieldCriteria> _fieldCriteria = new Dictionary<string, GraphQlFieldCriteria>();

    protected virtual IList<FieldMetadata> AllFields { get; } = EmptyFieldCollection;

    public void Clear()
    {
        _fieldCriteria.Clear();
    }

    public void IncludeAllFields()
    {
        IncludeFields(AllFields);
    }

    public string Build(Formatting formatting = Formatting.Indented)
    {
        return Build(formatting, 1);
    }

    protected string Build(Formatting formatting, int level)
    {
        var builder = new StringBuilder();
        builder.Append(""{"");
        
        if (formatting == Formatting.Indented)
            builder.AppendLine();

        var index = 0;
        foreach (var criteria in _fieldCriteria.Values)
        {
            builder.Append(criteria.Build(formatting, level));
            if (formatting == Formatting.Indented)
                builder.AppendLine();
            else if (++index < _fieldCriteria.Count)
                builder.Append("","");
        }

        if (formatting == Formatting.Indented)
            builder.Append(GetIndentation(level - 1));
        
        builder.Append(""}"");
        return builder.ToString();
    }

    protected void IncludeScalarField(string fieldName, IDictionary<string, object> args)
    {
        _fieldCriteria[fieldName] = new GraphQlScalarFieldCriteria(fieldName, args);
    }

    protected void IncludeObjectField(string fieldName, GraphQlQueryBuilder objectFieldQueryBuilder, IDictionary<string, object> args)
    {
        _fieldCriteria[fieldName] = new GraphQlObjectFieldCriteria(fieldName, objectFieldQueryBuilder, args);
    }

    protected void IncludeFields(IEnumerable<FieldMetadata> fields)
    {
        foreach (var field in fields)
        {
            if (field.QueryBuilderType == null)
                IncludeScalarField(field.Name, null);
            else
            {
                var queryBuilder = (GraphQlQueryBuilder)Activator.CreateInstance(field.QueryBuilderType);
                queryBuilder.IncludeAllFields();
                IncludeObjectField(field.Name, queryBuilder, null);
            }
        }
    }

    private static string GetIndentation(int level)
    {
        return new String(' ', level * IndentationSize);
    }

    private abstract class GraphQlFieldCriteria
    {
        public readonly string FieldName;
        public readonly IDictionary<string, object> Args;

        protected GraphQlFieldCriteria(string fieldName, IDictionary<string, object> args)
        {
            FieldName = fieldName;
            Args = args;
        }

        public abstract string Build(Formatting formatting, int level);

        protected string BuildArgumentClause(Formatting formatting)
        {
            var separator = formatting == Formatting.Indented ? "" "" : null;
            return
                Args?.Count > 0
                    ? $""({String.Join($"",{separator}"", Args.Select(kvp => $""{kvp.Key}:{separator}{BuildArgumentValue(kvp.Value)}""))}){separator}""
                    : String.Empty;
        }

        private string BuildArgumentValue(object value)
        {
            if (value is Enum)
                return ConvertEnumToString((Enum)value);

            var argumentValue = Convert.ToString(value, CultureInfo.InvariantCulture);
            return value is String ? $""\""{argumentValue}\"""" : argumentValue;
        }

        private static string ConvertEnumToString(Enum @enum)
        {
            var enumMember =
                @enum.GetType()
                    .GetTypeInfo()
                    .GetMembers()
                    .Single(m => String.Equals(m.Name, @enum.ToString()));

            var enumMemberAttribute = (EnumMemberAttribute)enumMember.GetCustomAttribute(typeof(EnumMemberAttribute));

            return enumMemberAttribute == null
                ? @enum.ToString()
                : enumMemberAttribute.Value;
        }
    }

    private class GraphQlScalarFieldCriteria : GraphQlFieldCriteria
    {
        public GraphQlScalarFieldCriteria(string fieldName, IDictionary<string, object> args) : base(fieldName, args)
        {
        }

        public override string Build(Formatting formatting, int level)
        {
            var builder = new StringBuilder();
            if (formatting == Formatting.Indented)
                builder.Append(GetIndentation(level));

            builder.Append(FieldName);
            builder.Append(BuildArgumentClause(formatting));
            return builder.ToString();
        }
    }

    private class GraphQlObjectFieldCriteria : GraphQlFieldCriteria
    {
        private readonly GraphQlQueryBuilder _objectQueryBuilder;

        public GraphQlObjectFieldCriteria(string fieldName, GraphQlQueryBuilder objectQueryBuilder, IDictionary<string, object> args) : base(fieldName, args)
        {
            _objectQueryBuilder = objectQueryBuilder;
        }

        public override string Build(Formatting formatting, int level)
        {
            var builder = new StringBuilder();
            var fieldName = FieldName;
            if (formatting == Formatting.Indented)
                fieldName = $""{GetIndentation(level)}{FieldName} "";

            builder.Append(fieldName);
            builder.Append(BuildArgumentClause(formatting));
            builder.Append(_objectQueryBuilder.Build(formatting, level + 1));
            return builder.ToString();
        }
    }
}

public abstract class GraphQlQueryBuilder<TQueryBuilder> : GraphQlQueryBuilder where TQueryBuilder : GraphQlQueryBuilder<TQueryBuilder>
{
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

    protected TQueryBuilder WithScalarField(string fieldName, IDictionary<string, object> args = null)
    {
        IncludeScalarField(fieldName, args);
        return (TQueryBuilder)this;
    }

    protected TQueryBuilder WithObjectField(string fieldName, GraphQlQueryBuilder queryBuilder, IDictionary<string, object> args = null)
    {
        IncludeObjectField(fieldName, queryBuilder, args);
        return (TQueryBuilder)this;
    }
}
#endregion
");

            builder.AppendLine("#region builder classes");

            GenerarateEnums(schema, builder);

            var complexTypes = schema.Types.Where(t => t.Kind == "OBJECT" && !t.Name.StartsWith("__")).ToArray();
            for (var i = 0; i < complexTypes.Length; i++)
            {
                var type = complexTypes[i];
                GenerateTypeQueryBuilder(type, builder);

                if (i < complexTypes.Length - 1)
                    builder.AppendLine();
            }

            builder.Append("#endregion");
        }

        public static void GenerateDataClasses(GraphQlSchema schema, StringBuilder builder)
        {
            builder.AppendLine("#region data classes");

            var objectTypes = schema.Types.Where(t => t.Kind == "OBJECT" && !t.Name.StartsWith("__")).ToArray();
            for (var i = 0; i < objectTypes.Length; i++)
            {
                var type = objectTypes[i];
                GenerateDataClass(type, builder);

                if (i < objectTypes.Length - 1)
                    builder.AppendLine();
            }

            builder.Append("#endregion");
        }

        private static void GenerateDataClass(GraphQlType type, StringBuilder builder)
        {
            builder.Append("public class ");
            builder.AppendLine(type.Name);
            builder.AppendLine("{");

            foreach (var field in type.Fields)
            {
                var propertyName = NamingHelper.CapitalizeFirst(field.Name);

                string propertyType;
                switch (field.Type.Kind)
                {
                    case "OBJECT":
                        propertyType = field.Type.Name;
                        break;
                    case "ENUM":
                        propertyType = $"{field.Type.Name}?";
                        break;
                    case "LIST":
                        var itemType = field.Type.OfType.Name;
                        if (itemType == "DynamicType")
                            itemType = "object";

                        propertyType = $"ICollection<{itemType}>";
                        break;
                    case "SCALAR":
                        switch (field.Type.Name)
                        {
                            case "DynamicType":
                                propertyType = "object";
                                break;
                            case "Int":
                                propertyType = "int?";
                                break;
                            case "String":
                                propertyType = "string";
                                if (propertyName == "From" || propertyName == "ValidFrom" || propertyName == "CreatedAt" ||
                                    propertyName == "To" || propertyName == "ValidTo" || propertyName == "ModifiedAt" || propertyName.EndsWith("Timestamp"))
                                    propertyType = "DateTimeOffset?";

                                break;
                            case "Float":
                                propertyType = "decimal?";
                                break;
                            case "Boolean":
                                propertyType = "bool?";
                                break;
                            case "ID":
                                propertyType = "Guid?";
                                break;
                            default:
                                propertyType = field.Type.Name;
                                break;
                        }

                        break;
                    default:
                        propertyType = "string";
                        break;
                }

                builder.AppendLine($"    public {propertyType} {propertyName} {{ get; set; }}");
            }

            builder.Append("}");
        }

        private static void GenerateTypeQueryBuilder(GraphQlType type, StringBuilder builder)
        {
            builder.AppendLine($"public class {type.Name}QueryBuilder : GraphQlQueryBuilder<{type.Name}QueryBuilder>");
            builder.AppendLine("{");

            var fields = type.Fields.ToArray();
            builder.AppendLine("    protected override IList<FieldMetadata> AllFields { get; } =");
            builder.AppendLine("        new []");
            builder.AppendLine("        {");

            for (var i = 0; i < fields.Length; i++)
            {
                var comma = i == fields.Length - 1 ? null : ",";
                var field = fields[i];
                var isList = field.Type.Kind == "LIST";
                var isComplex = isList || field.Type.Name == "DynamicType" || field.Type.Kind == "OBJECT";
                var fieldTypeName = isList ? field.Type.OfType.Name : field.Type.Name;

                builder.Append($"            new FieldMetadata {{ Name = \"{field.Name}\"");

                if (isComplex)
                {
                    builder.Append(", IsComplex = true");

                    if (fieldTypeName != "DynamicType")
                    {
                        builder.Append($", QueryBuilderType = typeof({fieldTypeName}QueryBuilder)");
                    }
                }

                builder.AppendLine($" }}{comma}");
            }

            builder.AppendLine("        };");
            builder.AppendLine();

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var fieldTypeName = field.Type.Kind == "LIST" ? field.Type.OfType.Name : field.Type.Name;

                var args = field.Args?.Where(a => a.Type.Kind == "SCALAR" || a.Type.Kind == "ENUM").ToArray() ?? new GraphQlArgument[0];
                var methodParameters = String.Join(", ", args.Select(a => $"{(a.Type.Kind == "ENUM" ? $"{a.Type.Name}?" : ScalarToNetType(a.Type.Name))} {a.Name} = null"));

                if (field.Type.Kind == "SCALAR" || field.Type.Kind == "ENUM" || fieldTypeName == "DynamicType")
                {
                    builder.AppendLine($"    public {type.Name}QueryBuilder With{NamingHelper.CapitalizeFirst(field.Name)}({methodParameters})");
                    builder.AppendLine("    {");

                    AppendArgumentDictionary(builder, args);

                    builder.Append($"        return WithScalarField(\"{field.Name}\"");

                    if (args.Length > 0)
                        builder.Append(", args");

                    builder.AppendLine(");");
                    builder.AppendLine("    }");
                }
                else
                {
                    var builderParameterName = NamingHelper.LowerFirst(fieldTypeName);
                    builder.Append($"    public {type.Name}QueryBuilder With{NamingHelper.CapitalizeFirst(field.Name)}({fieldTypeName}QueryBuilder {builderParameterName}QueryBuilder");

                    if (args.Length > 0)
                    {
                        builder.Append(", ");
                        builder.Append(methodParameters);
                    }

                    builder.AppendLine(")");
                    builder.AppendLine("    {");

                    AppendArgumentDictionary(builder, args);

                    builder.Append($"        return WithObjectField(\"{field.Name}\", {builderParameterName}QueryBuilder");

                    if (args.Length > 0)
                        builder.Append(", args");

                    builder.AppendLine(");");
                    builder.AppendLine("    }");
                }

                if (i < fields.Length - 1)
                    builder.AppendLine();
            }

            builder.AppendLine("}");
        }

        private static void AppendArgumentDictionary(StringBuilder builder, ICollection<GraphQlArgument> args)
        {
            if (args.Count == 0)
                return;

            builder.AppendLine("        var args = new Dictionary<string, object>();");

            foreach (var arg in args)
            {
                builder.AppendLine($"        if ({arg.Name} != null)");
                builder.AppendLine($"            args.Add(\"{arg.Name}\", {arg.Name});");
                builder.AppendLine();
            }
        }

        private static void GenerarateEnums(GraphQlSchema schema, StringBuilder builder)
        {
            foreach (var type in schema.Types.Where(t => t.Kind == "ENUM" && !t.Name.StartsWith("__")))
            {
                GenerateEnum(type, builder);
                builder.AppendLine();
            }
        }

        private static void GenerateEnum(GraphQlType type, StringBuilder builder)
        {
            builder.Append("public enum ");
            builder.AppendLine(type.Name);
            builder.AppendLine("{");

            var enumValues = type.EnumValues.ToList();
            for (var i = 0; i < enumValues.Count; i++)
            {
                var enumValue = enumValues[i];
                builder.Append($"    [EnumMember(Value=\"{enumValue.Name}\")] {NamingHelper.CapitalizeFirst(enumValue.Name)}");

                if (i < enumValues.Count - 1)
                    builder.Append(",");

                builder.AppendLine();
            }

            builder.AppendLine("}");
        }

        private static string ScalarToNetType(string graphQlTypeName)
        {
            switch (graphQlTypeName)
            {
                case "Int":
                    return "int?";
                case "String":
                    return "string";
                case "Float":
                    return "decimal?";
                case "Boolean":
                    return "bool?";
                case "ID":
                    return "Guid?";
                default:
                    return "object";
            }
        }
    }
}