﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
        private const string GraphQlTypeKindNonNull = "NON_NULL";
        private const string GraphQlTypeKindInputObject = "INPUT_OBJECT";
        private const string GraphQlTypeKindUnion = "UNION";
        private const string GraphQlTypeKindInterface = "INTERFACE";

        internal static readonly JsonSerializerSettings SerializerSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = { new StringEnumConverter() }
            };

        public static async Task<GraphQlSchema> RetrieveSchema(string url)
        {
            using (var client = new HttpClient())
            {
                return await RetrieveSchema(client, url);
            }
        }
        public static async Task<GraphQlSchema> RetrieveSchema(HttpClient client, string url)
        {
            string content;

            using (var response =
                await client.PostAsync(
                    url,
                    new StringContent(JsonConvert.SerializeObject(new { query = IntrospectionQuery.Text }), Encoding.UTF8, "application/json")))
            {
                content =
                    response.Content == null
                        ? "(no content)"
                        : await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Status code: {(int)response.StatusCode} ({response.StatusCode}); content: {content}");
            }

            return JsonConvert.DeserializeObject<GraphQlResult>(content, SerializerSettings).Data.Schema;
        }

        private static void GenerateSharedTypes(GraphQlSchema schema, StringBuilder builder)
        {
            builder.AppendLine("#region shared types");
            GenerateEnums(schema, builder);
            builder.AppendLine("#endregion");
            builder.AppendLine();
        }

        public static void GenerateQueryBuilder(GraphQlSchema schema, StringBuilder builder)
        {
            using (var reader = new StreamReader(typeof(GraphQlGenerator).GetTypeInfo().Assembly.GetManifestResourceStream("GraphQlClientGenerator.BaseClasses")))
                builder.AppendLine(reader.ReadToEnd());

            GenerateSharedTypes(schema, builder);

            builder.AppendLine("#region builder classes");

            var complexTypes = schema.Types.Where(t => t.Kind == GraphQlTypeKindObject && !t.Name.StartsWith("__")).ToArray();
            for (var i = 0; i < complexTypes.Length; i++)
            {
                var type = complexTypes[i];

                string queryPrefix;
                if (String.Equals(type.Name, schema.MutationType?.Name))
                    queryPrefix = "mutation";
                else if (String.Equals(type.Name, schema.SubscriptionType?.Name))
                    queryPrefix = "subscription";
                else
                    queryPrefix = null;

                GenerateTypeQueryBuilder(type, queryPrefix, builder);

                if (i < complexTypes.Length - 1)
                    builder.AppendLine();
            }

            builder.AppendLine("#endregion");
        }

        public static void GenerateDataClasses(GraphQlSchema schema, StringBuilder builder)
        {
            var objectTypes = schema.Types.Where(t => t.Kind == GraphQlTypeKindObject && !t.Name.StartsWith("__")).ToArray();
            var hasObjectType = objectTypes.Any();
            if (hasObjectType)
            {
                builder.AppendLine("#region data classes");

                for (var i = 0; i < objectTypes.Length; i++)
                {
                    var type = objectTypes[i];
                    GenerateDataClass(type.Name, null, builder, () => GenerateDataClassBody(type, builder));

                    builder.AppendLine();

                    if (i < objectTypes.Length - 1)
                        builder.AppendLine();
                }

                builder.AppendLine("#endregion");
            }

            var inputTypes = schema.Types.Where(t => t.Kind == GraphQlTypeKindInputObject && !t.Name.StartsWith("__")).ToArray();
            if (inputTypes.Any())
            {
                if (hasObjectType)
                    builder.AppendLine();

                builder.AppendLine("#region input classes");

                for (var i = 0; i < inputTypes.Length; i++)
                {
                    var type = inputTypes[i];
                    GenerateDataClass(type.Name, "GraphQlMutationInput", builder, () => GenerateInputDataClassBody(type, builder));

                    builder.AppendLine();

                    if (i < inputTypes.Length - 1)
                        builder.AppendLine();
                }

                builder.AppendLine("#endregion");
            }
        }

        private static void GenerateDataClassBody(GraphQlType type, StringBuilder builder)
        {
            foreach (var field in type.Fields.Where(f => !f.IsDeprecated || GraphQlGeneratorConfiguration.IncludeDeprecatedFields))
                GenerateDataProperty(type, field, field.IsDeprecated, field.DeprecationReason, builder);
        }

        private static void GenerateInputDataClassBody(GraphQlType type, StringBuilder builder)
        {
            foreach (var field in type.InputFields)
                GenerateDataProperty(type, field, false, null, builder);

            builder.AppendLine();
            builder.AppendLine("    protected override IEnumerable<InputPropertyInfo> GetPropertyValues()");
            builder.AppendLine("    {");

            foreach (var field in type.InputFields)
            {
                var propertyName = NamingHelper.ToPascalCase(field.Name);
                builder.Append("        yield return new InputPropertyInfo { Name = \"");
                builder.Append(field.Name);
                builder.Append("\", Value = ");
                builder.Append(propertyName);
                builder.AppendLine(" };");
            }

            builder.AppendLine("    }");
        }

        private static void GenerateDataClass(string typeName, string baseTypeName, StringBuilder builder, Action generateClassBody)
        {
            var className = $"{typeName}{GraphQlGeneratorConfiguration.ClassPostfix}";
            ValidateClassName(className);

            builder.Append("public class ");
            builder.Append(className);

            if (!String.IsNullOrEmpty(baseTypeName))
            {
                builder.Append(" : ");
                builder.Append(baseTypeName);
            }

            builder.AppendLine();
            builder.AppendLine("{");

            generateClassBody();

            builder.Append("}");
        }

        private static void GenerateDataProperty(GraphQlType baseType, IGraphQlMember member, bool isDeprecated, string deprecationReason, StringBuilder builder)
        {
            var propertyName = NamingHelper.ToPascalCase(member.Name);

            string propertyType;
            var fieldType = UnwrapNonNull(member.Type);
            switch (fieldType.Kind)
            {
                case GraphQlTypeKindObject:
                    propertyType = $"{fieldType.Name}{GraphQlGeneratorConfiguration.ClassPostfix}";
                    break;
                case GraphQlTypeKindEnum:
                    propertyType = $"{fieldType.Name}?";
                    break;
                case GraphQlTypeKindList:
                    var itemType = IsObjectScalar(fieldType.OfType) ? "object" : $"{UnwrapNonNull(fieldType.OfType).Name}{GraphQlGeneratorConfiguration.ClassPostfix}";
                    var suggestedNetType = ScalarToNetType(baseType, member.Name, UnwrapNonNull(fieldType.OfType)).TrimEnd('?');
                    if (!String.Equals(suggestedNetType, "object"))
                        itemType = suggestedNetType;

                    propertyType = $"ICollection<{itemType}>";
                    break;
                case GraphQlTypeKindScalar:
                    switch (fieldType.Name)
                    {
                        case "Int":
                            propertyType = "int?";
                            break;
                        case "String":
                            propertyType = GraphQlGeneratorConfiguration.CustomScalarFieldTypeMapping(baseType, member.Name);
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
                            propertyType = "object";
                            break;
                    }

                    break;
                default:
                    propertyType = "string";
                    break;
            }

            if (GraphQlGeneratorConfiguration.GenerateComments && !String.IsNullOrWhiteSpace(member.Description))
            {
                builder.AppendLine("    /// <summary>");
                builder.AppendLine($"    /// {member.Description}");
                builder.AppendLine("    /// </summary>");
            }

            if (isDeprecated)
            {
                deprecationReason = String.IsNullOrWhiteSpace(deprecationReason) ? null : $"(\"{deprecationReason.Replace("\\", "\\\\").Replace("\"", "\\\"")}\")";
                builder.AppendLine($"    [Obsolete{deprecationReason}]");
            }

            builder.AppendLine($"    public {propertyType} {propertyName} {{ get; set; }}");
        }

        private static void GenerateTypeQueryBuilder(GraphQlType type, string queryPrefix, StringBuilder builder)
        {
            var className = $"{type.Name}QueryBuilder{GraphQlGeneratorConfiguration.ClassPostfix}";
            ValidateClassName(className);

            builder.AppendLine($"public class {className} : GraphQlQueryBuilder<{className}>");
            builder.AppendLine("{");

            builder.AppendLine("    private static readonly FieldMetadata[] AllFieldMetadata =");
            builder.AppendLine("        new []");
            builder.AppendLine("        {");

            var fields = type.Fields.ToArray();
            for (var i = 0; i < fields.Length; i++)
            {
                var comma = i == fields.Length - 1 ? null : ",";
                var field = fields[i];
                var fieldType = UnwrapNonNull(field.Type);
                var isList = fieldType.Kind == GraphQlTypeKindList;
                var isComplex = isList || IsObjectScalar(fieldType) || fieldType.Kind == GraphQlTypeKindObject;

                builder.Append($"            new FieldMetadata {{ Name = \"{field.Name}\"");

                if (isComplex)
                {
                    builder.Append(", IsComplex = true");

                    fieldType = isList ? UnwrapNonNull(fieldType.OfType) : fieldType;
                    if (fieldType.Kind != GraphQlTypeKindScalar)
                    {
                        builder.Append($", QueryBuilderType = typeof({fieldType.Name}QueryBuilder{GraphQlGeneratorConfiguration.ClassPostfix})");
                    }
                }

                builder.AppendLine($" }}{comma}");
            }

            builder.AppendLine("        };");
            builder.AppendLine();

            if (!String.IsNullOrEmpty(queryPrefix))
                WriteOverrideProperty("string", "Prefix", $"\"{queryPrefix}\"", builder);

            WriteOverrideProperty("IList<FieldMetadata>", "AllFields", "AllFieldMetadata", builder);

            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var fieldType = UnwrapNonNull(field.Type);
                if (fieldType.Kind == GraphQlTypeKindList)
                    fieldType = fieldType.OfType;
                fieldType = UnwrapNonNull(fieldType);

                bool IsCompatibleArgument(GraphQlFieldType argumentType)
                {
                    argumentType = UnwrapNonNull(argumentType);
                    switch (argumentType.Kind)
                    {
                        case GraphQlTypeKindScalar:
                        case GraphQlTypeKindEnum:
                        case GraphQlTypeKindInputObject:
                            return true;
                        case GraphQlTypeKindList:
                            return IsCompatibleArgument(argumentType.OfType);
                        default:
                            return false;
                    }
                }

                var args = field.Args?.Where(a => IsCompatibleArgument(a.Type)).ToArray() ?? new GraphQlArgument[0];
                var methodParameters =
                    String.Join(
                        ", ",
                        args
                            .OrderByDescending(a => a.Type.Kind == GraphQlTypeKindNonNull)
                            .Select(a => BuildMethodParameterDefinition(type, a)));

                var requiresFullBody = GraphQlGeneratorConfiguration.CSharpVersion == CSharpVersion.Compatible || args.Any();
                var returnPrefix = requiresFullBody ? "        return " :  String.Empty;

                if (fieldType.Kind == GraphQlTypeKindScalar || fieldType.Kind == GraphQlTypeKindEnum || fieldType.Kind == GraphQlTypeKindScalar)
                {
                    builder.Append($"    public {className} With{NamingHelper.ToPascalCase(field.Name)}({methodParameters})");

                    if (requiresFullBody)
                    {
                        builder.AppendLine();
                        builder.AppendLine("    {");
                        AppendArgumentDictionary(builder, args);
                    }
                    else
                        builder.Append(" => ");

                    builder.Append($"{returnPrefix}WithScalarField(\"{field.Name}\"");

                    if (args.Length > 0)
                        builder.Append(", args");

                    builder.AppendLine(");");

                    if (requiresFullBody)
                        builder.AppendLine("    }");
                }
                else
                {
                    if (String.IsNullOrEmpty(fieldType.Name))
                        throw new InvalidOperationException($"Field '{field.Name}' type name not resolved. ");

                    var builderParameterName = NamingHelper.LowerFirst(fieldType.Name);
                    builder.Append($"    public {className} With{NamingHelper.ToPascalCase(field.Name)}({fieldType.Name}QueryBuilder{GraphQlGeneratorConfiguration.ClassPostfix} {builderParameterName}QueryBuilder");

                    if (args.Length > 0)
                    {
                        builder.Append(", ");
                        builder.Append(methodParameters);
                    }

                    builder.Append(")");

                    if (requiresFullBody)
                    {
                        builder.AppendLine();
                        builder.AppendLine("    {");
                    }
                    else
                        builder.Append(" => ");

                    AppendArgumentDictionary(builder, args);

                    builder.Append($"{returnPrefix}WithObjectField(\"{field.Name}\", {builderParameterName}QueryBuilder");

                    if (args.Length > 0)
                        builder.Append(", args");

                    builder.AppendLine(");");

                    if (requiresFullBody)
                        builder.AppendLine("    }");
                }

                if (i < fields.Length - 1)
                    builder.AppendLine();
            }

            builder.AppendLine("}");
        }

        private static void WriteOverrideProperty(string propertyType, string propertyName, string propertyValue, StringBuilder builder)
        {
            builder.Append("    protected override ");
            builder.Append(propertyType);
            builder.Append(" ");
            builder.Append(propertyName);
            builder.Append(" { get");

            if (GraphQlGeneratorConfiguration.CSharpVersion == CSharpVersion.Compatible)
            {
                builder.Append(" { return ");
                builder.Append(propertyValue);
                builder.AppendLine("; } } ");
            }
            else
            {
                builder.Append("; } = ");
                builder.Append(propertyValue);
                builder.AppendLine(";");
            }

            builder.AppendLine();
        }

        private static string BuildMethodParameterDefinition(GraphQlType baseType, GraphQlArgument argument)
        {
            var isArgumentNotNull = argument.Type.Kind == GraphQlTypeKindNonNull;
            var isTypeNotNull = isArgumentNotNull;
            var unwrappedType = UnwrapNonNull(argument.Type);
            var isCollection = unwrappedType.Kind == GraphQlTypeKindList;
            if (isCollection)
            {
                isTypeNotNull = unwrappedType.OfType.Kind == GraphQlTypeKindNonNull;
                unwrappedType = UnwrapNonNull(unwrappedType.OfType);
            }

            var argumentNetType = unwrappedType.Kind == GraphQlTypeKindEnum ? $"{unwrappedType.Name}?" : ScalarToNetType(baseType, argument.Name, unwrappedType);
            if (isTypeNotNull)
                argumentNetType = argumentNetType.TrimEnd('?');

            if (unwrappedType.Kind == GraphQlTypeKindInputObject)
                argumentNetType = $"{unwrappedType.Name}{GraphQlGeneratorConfiguration.ClassPostfix}";

            if (isCollection)
                argumentNetType = $"IEnumerable<{argumentNetType}>";

            var argumentDefinition = $"{argumentNetType} {argument.Name}";
            if (!isArgumentNotNull)
                argumentDefinition = $"{argumentDefinition} = null";

            return argumentDefinition;
        }

        private static void ValidateClassName(string className)
        {
            if (!CSharpHelper.IsValidIdentifier(className))
                throw new InvalidOperationException($"Resulting class name '{className}' is not valid. ");
        }

        private static void AppendArgumentDictionary(StringBuilder builder, ICollection<GraphQlArgument> args)
        {
            if (args.Count == 0)
                return;

            builder.AppendLine("        var args = new Dictionary<string, object>();");

            foreach (var arg in args)
            {
                if (arg.Type.Kind == GraphQlTypeKindNonNull)
                    builder.AppendLine($"        args.Add(\"{arg.Name}\", {arg.Name});");
                else
                {
                    builder.AppendLine($"        if ({arg.Name} != null)");
                    builder.AppendLine($"            args.Add(\"{arg.Name}\", {arg.Name});");
                    builder.AppendLine();
                }
            }
        }

        private static void GenerateEnums(GraphQlSchema schema, StringBuilder builder)
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
                builder.Append($"    [EnumMember(Value=\"{enumValue.Name}\")] {NamingHelper.ToNetEnumName(enumValue.Name)}");

                if (i < enumValues.Count - 1)
                    builder.Append(",");

                builder.AppendLine();
            }

            builder.AppendLine("}");
        }

        private static bool IsObjectScalar(GraphQlFieldType graphQlType)
        {
            graphQlType = UnwrapNonNull(graphQlType);
            return graphQlType.Kind == GraphQlTypeKindScalar && !graphQlType.IsScalar;
        }

        private static GraphQlFieldType UnwrapNonNull(GraphQlFieldType graphQlType) =>
            graphQlType.Kind == GraphQlTypeKindNonNull ? graphQlType.OfType : graphQlType;

        private static string ScalarToNetType(GraphQlType baseType, string valueName, GraphQlTypeBase valueType)
        {
            switch (valueType.Name)
            {
                case GraphQlTypeBase.GraphQlTypeScalarInteger:
                    return "int?";
                case GraphQlTypeBase.GraphQlTypeScalarString:
                    return GraphQlGeneratorConfiguration.CustomScalarFieldTypeMapping(baseType, valueName);
                case GraphQlTypeBase.GraphQlTypeScalarFloat:
                    return "decimal?";
                case GraphQlTypeBase.GraphQlTypeScalarBoolean:
                    return "bool?";
                case GraphQlTypeBase.GraphQlTypeScalarId:
                    return "Guid?";
                default:
                    return "object";
            }
        }
    }
}