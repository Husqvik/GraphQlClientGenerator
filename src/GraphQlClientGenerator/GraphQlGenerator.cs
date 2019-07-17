using System;
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
        internal const string GraphQlTypeKindObject = "OBJECT";
        internal const string GraphQlTypeKindEnum = "ENUM";
        internal const string GraphQlTypeKindScalar = "SCALAR";
        internal const string GraphQlTypeKindList = "LIST";
        internal const string GraphQlTypeKindNonNull = "NON_NULL";
        internal const string GraphQlTypeKindInputObject = "INPUT_OBJECT";
        internal const string GraphQlTypeKindUnion = "UNION";
        internal const string GraphQlTypeKindInterface = "INTERFACE";
        private const string IntrospectionOperation = "IntrospectionQuery";

        public const string RequiredNamespaces =
            @"using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
";

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
                string content;

                using (var response =
                    await client.PostAsync(
                        url,
                        new StringContent(JsonConvert.SerializeObject(new { OperationNAme = IntrospectionOperation, query = IntrospectionQuery.Text }), Encoding.UTF8, "application/json")))
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
        }

        private static bool IsComplexType(string graphQlTypeKind) =>
            graphQlTypeKind == GraphQlTypeKindObject || graphQlTypeKind == GraphQlTypeKindInterface || graphQlTypeKind == GraphQlTypeKindUnion;

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

            var complexTypes = schema.Types.Where(t => IsComplexType(t.Kind) && !t.Name.StartsWith("__")).ToArray();
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

        private static void FindAllReferencedObjectTypes(GraphQlSchema schema, GraphQlType type, ISet<string> objectTypes)
        {
            foreach (var member in (IEnumerable<IGraphQlMember>)type.InputFields ?? type.Fields)
            {
                var unwrappedType = member.Type.UnwrapIfNonNull();
                GraphQlType memberType;
                switch (unwrappedType.Kind)
                {
                    case GraphQlTypeKindObject:
                        objectTypes.Add(unwrappedType.Name);
                        memberType = schema.Types.Single(t => t.Name == unwrappedType.Name);
                        FindAllReferencedObjectTypes(schema, memberType, objectTypes);
                        break;

                    case GraphQlTypeKindList:
                        var itemType = unwrappedType.OfType.UnwrapIfNonNull();
                        if (IsComplexType(itemType.Kind))
                        {
                            memberType = schema.Types.Single(t => t.Name == itemType.Name);
                            FindAllReferencedObjectTypes(schema, memberType, objectTypes);
                        }

                        break;
                }
            }
        }

        public static void GenerateDataClasses(GraphQlSchema schema, StringBuilder builder)
        {
            var inputTypes = schema.Types.Where(t => t.Kind == GraphQlTypeKindInputObject && !t.Name.StartsWith("__")).ToArray();
            var hasInputType = inputTypes.Any();
            var referencedObjectTypes = new HashSet<string>();

            if (hasInputType)
            {
                builder.AppendLine();
                builder.AppendLine("#region input classes");

                for (var i = 0; i < inputTypes.Length; i++)
                {
                    var type = inputTypes[i];
                    FindAllReferencedObjectTypes(schema, type, referencedObjectTypes);
                    GenerateDataClass(type.Name, "IGraphQlInputObject", builder, () => GenerateInputDataClassBody(type, type.InputFields.Cast<IGraphQlMember>().ToArray(), builder));

                    builder.AppendLine();

                    if (i < inputTypes.Length - 1)
                        builder.AppendLine();
                }

                builder.AppendLine("#endregion");
            }

            var complexTypes = schema.Types.Where(t => IsComplexType(t.Kind) && !t.Name.StartsWith("__")).ToArray();
            if (!complexTypes.Any())
                return;

            if (hasInputType)
                builder.AppendLine();

            builder.AppendLine("#region data classes");

            for (var i = 0; i < complexTypes.Length; i++)
            {
                var type = complexTypes[i];
                var hasInputReference = referencedObjectTypes.Contains(type.Name);
                var isInterface = type.Kind == GraphQlTypeKindInterface;

                void GenerateBody(bool isInterfaceMember)
                {
                    var fieldsToGenerate = type.Fields?.Where(f => !f.IsDeprecated || GraphQlGeneratorConfiguration.IncludeDeprecatedFields).ToArray();
                    if (hasInputReference)
                        GenerateInputDataClassBody(type, fieldsToGenerate, builder);
                    else if (fieldsToGenerate != null)
                        foreach (var field in fieldsToGenerate)
                            GenerateDataProperty(type, field, isInterfaceMember, field.IsDeprecated, field.DeprecationReason, builder);
                }

                var interfacesToImplement = new List<string>();
                if (isInterface)
                {
                    interfacesToImplement.Add(GenerateInterface($"I{type.Name}", builder, () => GenerateBody(true)));
                    builder.AppendLine();
                    builder.AppendLine();
                }
                else if (type.Interfaces?.Count > 0)
                    interfacesToImplement.AddRange(type.Interfaces.Select(x => $"I{x.Name}{GraphQlGeneratorConfiguration.ClassPostfix}"));

                if (hasInputReference)
                    interfacesToImplement.Add("IGraphQlInputObject");

                GenerateDataClass(type.Name, String.Join(", ", interfacesToImplement), builder, () => GenerateBody(false));

                builder.AppendLine();

                if (i < complexTypes.Length - 1)
                    builder.AppendLine();
            }

            builder.AppendLine("#endregion");
        }

        private static void GenerateInputDataClassBody(GraphQlType type, ICollection<IGraphQlMember> members, StringBuilder builder)
        {
            foreach (var member in members)
                GenerateDataProperty(type, member, false, false, null, builder);

            builder.AppendLine();
            builder.AppendLine("    IEnumerable<InputPropertyInfo> IGraphQlInputObject.GetPropertyValues()");
            builder.AppendLine("    {");

            foreach (var member in members)
            {
                var propertyName = NamingHelper.ToPascalCase(member.Name);
                builder.Append("        yield return new InputPropertyInfo { Name = \"");
                builder.Append(member.Name);
                builder.Append("\", Value = ");
                builder.Append(propertyName);
                builder.AppendLine(" };");
            }

            builder.AppendLine("    }");
        }

        private static string GenerateInterface(string interfaceName, StringBuilder builder, Action generateInterfaceBody) =>
            GenerateFileMember("interface", interfaceName, null, builder, generateInterfaceBody);

        private static string GenerateDataClass(string typeName, string baseTypeName, StringBuilder builder, Action generateClassBody) =>
            GenerateFileMember("class", typeName, baseTypeName, builder, generateClassBody);

        private static string GenerateFileMember(string memberType, string typeName, string baseTypeName, StringBuilder builder, Action generateFileMemberBody)
        {
            var memberName = $"{typeName}{GraphQlGeneratorConfiguration.ClassPostfix}";
            ValidateClassName(memberName);

            builder.Append("public ");
            builder.Append(memberType);
            builder.Append(" ");
            builder.Append(memberName);

            if (!String.IsNullOrEmpty(baseTypeName))
            {
                builder.Append(" : ");
                builder.Append(baseTypeName);
            }

            builder.AppendLine();
            builder.AppendLine("{");

            generateFileMemberBody();

            builder.Append("}");

            return memberName;
        }

        private static void GenerateDataProperty(GraphQlType baseType, IGraphQlMember member, bool isInterfaceMember, bool isDeprecated, string deprecationReason, StringBuilder builder)
        {
            var propertyName = NamingHelper.ToPascalCase(member.Name);

            string propertyType;
            var fieldType = member.Type.UnwrapIfNonNull();
            switch (fieldType.Kind)
            {
                case GraphQlTypeKindObject:
                case GraphQlTypeKindInterface:
                case GraphQlTypeKindUnion:
                case GraphQlTypeKindInputObject:
                    propertyType = $"{fieldType.Name}{GraphQlGeneratorConfiguration.ClassPostfix}";
                    break;
                case GraphQlTypeKindEnum:
                    propertyType = $"{fieldType.Name}?";
                    break;
                case GraphQlTypeKindList:
                    var itemType = IsUnknownObjectScalar(baseType, member.Name, fieldType.OfType) ? "object" : $"{fieldType.OfType.UnwrapIfNonNull().Name}{GraphQlGeneratorConfiguration.ClassPostfix}";
                    var suggestedNetType = ScalarToNetType(baseType, member.Name, fieldType.OfType.UnwrapIfNonNull()).TrimEnd('?');
                    if (!String.Equals(suggestedNetType, "object") && !suggestedNetType.TrimEnd().EndsWith("System.Object"))
                        itemType = suggestedNetType;

                    propertyType = $"ICollection<{itemType}>";
                    break;
                case GraphQlTypeKindScalar:
                    switch (fieldType.Name)
                    {
                        case GraphQlTypeBase.GraphQlTypeScalarInteger:
                            propertyType = "long?";
                            break;
                        case GraphQlTypeBase.GraphQlTypeScalarString:
                            propertyType = GetCustomScalarType(baseType, fieldType, member.Name);
                            break;
                        case GraphQlTypeBase.GraphQlTypeScalarFloat:
                            propertyType = GetFloatNetType();
                            break;
                        case GraphQlTypeBase.GraphQlTypeScalarBoolean:
                            propertyType = "bool?";
                            break;
                        case GraphQlTypeBase.GraphQlTypeScalarId:
                            propertyType = "dynamic";
                            break;
                        case GraphQlTypeBase.GraphQlTypeScalarJson:
                            propertyType = "dynamic";
                            break;
                        default:
                            propertyType = GetCustomScalarType(baseType, fieldType, member.Name);
                            break;
                    }

                    break;
                default:
                    propertyType = "string";
                    break;
            }

            GenerateCodeComments(builder, member.Description);

            if (isDeprecated)
            {
                deprecationReason = string.IsNullOrWhiteSpace(deprecationReason) ? null : $"(@\"{deprecationReason.Replace("\"", "\"\"")}\")";
                builder.AppendLine($"    [Obsolete{deprecationReason}]");
            }

            builder.AppendLine($"    {(isInterfaceMember ? null : "public ")}{propertyType} {propertyName} {{ get; set; }}");
        }

        private static string GetFloatNetType()
        {
            switch (GraphQlGeneratorConfiguration.FloatType)
            {
                case FloatType.Decimal: return "decimal?";
                case FloatType.Float: return "float?";
                case FloatType.Double: return "double?";
                default: throw new InvalidOperationException($"'{GraphQlGeneratorConfiguration.FloatType}' not supported");
            }
        }

        private static void GenerateTypeQueryBuilder(GraphQlType type, string queryPrefix, StringBuilder builder)
        {
            var className = $"{type.Name}QueryBuilder{GraphQlGeneratorConfiguration.ClassPostfix}";
            ValidateClassName(className);

            builder.AppendLine($"public class {className} : GraphQlQueryBuilder<{className}>");
            builder.AppendLine("{");

            builder.AppendLine("    private static readonly FieldMetadata[] AllFieldMetadata =");
            builder.AppendLine("        new FieldMetadata[]");
            builder.AppendLine("        {");

            var fields = type.Fields?.ToArray();
            for (var i = 0; i < fields?.Length; i++)
            {
                var comma = i == fields.Length - 1 ? null : ",";
                var field = fields[i];
                var fieldType = field.Type.UnwrapIfNonNull();
                var isList = fieldType.Kind == GraphQlTypeKindList;
                var treatUnknownObjectAsComplex = IsUnknownObjectScalar(type, field.Name, fieldType) && !GraphQlGeneratorConfiguration.TreatUnknownObjectAsScalar;
                var isComplex = isList || treatUnknownObjectAsComplex || IsComplexType(fieldType.Kind);

                builder.Append($"            new FieldMetadata {{ Name = \"{field.Name}\"");

                if (isComplex)
                {
                    builder.Append(", IsComplex = true");

                    fieldType = isList ? fieldType.OfType.UnwrapIfNonNull() : fieldType;
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

            for (var i = 0; i < fields?.Length; i++)
            {
                var field = fields[i];
                var fieldType = field.Type.UnwrapIfNonNull();
                if (fieldType.Kind == GraphQlTypeKindList)
                    fieldType = fieldType.OfType;
                fieldType = fieldType.UnwrapIfNonNull();

                bool IsCompatibleArgument(GraphQlFieldType argumentType)
                {
                    argumentType = argumentType.UnwrapIfNonNull();
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
                var returnPrefix = requiresFullBody ? "        return " : String.Empty;

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
            var unwrappedType = argument.Type.UnwrapIfNonNull();
            var isCollection = unwrappedType.Kind == GraphQlTypeKindList;
            if (isCollection)
            {
                isTypeNotNull = unwrappedType.OfType.Kind == GraphQlTypeKindNonNull;
                unwrappedType = unwrappedType.OfType.UnwrapIfNonNull();
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
                GenerateCodeComments(builder, enumValue.Description);
                builder.Append($"    [EnumMember(Value=\"{enumValue.Name}\")] {enumValue.Name}");

                if (i < enumValues.Count - 1)
                    builder.Append(",");

                builder.AppendLine();
            }

            builder.AppendLine("}");
        }

        private static void GenerateCodeComments(StringBuilder builder, string description)
        {
            if (String.IsNullOrWhiteSpace(description))
                return;

            if (GraphQlGeneratorConfiguration.CommentGeneration.HasFlag(CommentGenerationOption.CodeSummary))
            {
                builder.AppendLine("    /// <summary>");
                builder.AppendLine($"    /// {description}");
                builder.AppendLine("    /// </summary>");
            }

            if (GraphQlGeneratorConfiguration.CommentGeneration.HasFlag(CommentGenerationOption.DescriptionAttribute))
                builder.AppendLine($"    [Description(@\"{description.Replace("\"", "\"\"")}\")]");
        }

        private static bool IsUnknownObjectScalar(GraphQlType baseType, string valueName, GraphQlFieldType fieldType)
        {
            fieldType = fieldType.UnwrapIfNonNull();

            if (fieldType.Kind != GraphQlTypeKindScalar)
                return false;

            var netType = ScalarToNetType(baseType, valueName, fieldType);
            return netType == "object" || netType.TrimEnd().EndsWith("System.Object");
        }

        private static string ScalarToNetType(GraphQlType baseType, string valueName, GraphQlTypeBase valueType)
        {
            switch (valueType.Name)
            {
                case GraphQlTypeBase.GraphQlTypeScalarInteger:
                    return "long?";
                case GraphQlTypeBase.GraphQlTypeScalarString:
                    return GetCustomScalarType(baseType, valueType, valueName);
                case GraphQlTypeBase.GraphQlTypeScalarFloat:
                    return GetFloatNetType();
                case GraphQlTypeBase.GraphQlTypeScalarBoolean:
                    return "bool?";
                case GraphQlTypeBase.GraphQlTypeScalarId:
                    return "dynamic";
                case GraphQlTypeBase.GraphQlTypeScalarJson:
                    return "dynamic";
                default:
                    return GetCustomScalarType(baseType, valueType, valueName);
            }
        }

        private static string GetCustomScalarType(GraphQlType baseType, GraphQlTypeBase valueType, string valueName)
        {
            if (GraphQlGeneratorConfiguration.CustomScalarFieldTypeMapping == null)
                throw new InvalidOperationException($"'{nameof(GraphQlGeneratorConfiguration.CustomScalarFieldTypeMapping)}' missing");

            var netType = GraphQlGeneratorConfiguration.CustomScalarFieldTypeMapping(baseType, valueType, valueName);
            if (String.IsNullOrWhiteSpace(netType))
                throw new InvalidOperationException($".NET type for '{baseType.Name}.{valueName}' ({valueType.Name}) cannot be resolved. Please check {nameof(GraphQlGeneratorConfiguration)}.{nameof(GraphQlGeneratorConfiguration.CustomScalarFieldTypeMapping)} implementation. ");

            return netType;
        }
    }
}