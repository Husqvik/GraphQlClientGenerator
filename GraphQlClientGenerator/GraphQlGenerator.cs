using System;
using System.Collections.Generic;
using System.IO;
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
    public static class GraphQlGenerator
    {
        private const string GraphQlTypeKindObject = "OBJECT";
        private const string GraphQlTypeKindEnum = "ENUM";
        private const string GraphQlTypeKindScalar = "SCALAR";
        private const string GraphQlTypeKindList = "LIST";

        internal static readonly JsonSerializerSettings SerializerSettings =
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
            using (var reader = new StreamReader(typeof(GraphQlGenerator).Assembly.GetManifestResourceStream("GraphQlClientGenerator.BaseClasses")))
                builder.AppendLine(reader.ReadToEnd());

            builder.AppendLine("#region builder classes");

            GenerarateEnums(schema, builder);

            var complexTypes = schema.Types.Where(t => t.Kind == GraphQlTypeKindObject && !t.Name.StartsWith("__")).ToArray();
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

            var objectTypes = schema.Types.Where(t => t.Kind == GraphQlTypeKindObject && !t.Name.StartsWith("__")).ToArray();
            for (var i = 0; i < objectTypes.Length; i++)
            {
                var type = objectTypes[i];
                GenerateDataClass(type, builder);

                builder.AppendLine();

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
                    case GraphQlTypeKindObject:
                        propertyType = field.Type.Name;
                        break;
                    case GraphQlTypeKindEnum:
                        propertyType = $"{field.Type.Name}?";
                        break;
                    case GraphQlTypeKindList:
                        var itemType = field.Type.OfType.Name;
                        if (itemType == "DynamicType")
                            itemType = "object";

                        propertyType = $"ICollection<{itemType}>";
                        break;
                    case GraphQlTypeKindScalar:
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
                var isList = field.Type.Kind == GraphQlTypeKindList;
                var isComplex = isList || field.Type.Name == "DynamicType" || field.Type.Kind == GraphQlTypeKindObject;
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
                var fieldTypeName = field.Type.Kind == GraphQlTypeKindList ? field.Type.OfType.Name : field.Type.Name;

                var args = field.Args?.Where(a => a.Type.Kind == GraphQlTypeKindScalar || a.Type.Kind == GraphQlTypeKindEnum).ToArray() ?? new GraphQlArgument[0];
                var methodParameters = String.Join(", ", args.Select(a => $"{(a.Type.Kind == GraphQlTypeKindEnum ? $"{a.Type.Name}?" : ScalarToNetType(a.Type.Name))} {a.Name} = null"));

                if (field.Type.Kind == GraphQlTypeKindScalar || field.Type.Kind == GraphQlTypeKindEnum || fieldTypeName == "DynamicType")
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
            foreach (var type in schema.Types.Where(t => t.Kind == GraphQlTypeKindEnum && !t.Name.StartsWith("__")))
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

        private static bool IsObjectScalar(string graphQlTypeName)
        {
            return String.Equals(ScalarToNetType(graphQlTypeName), "object");
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