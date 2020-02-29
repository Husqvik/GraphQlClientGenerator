using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GraphQlClientGenerator
{
    public class GraphQlResult
    {
        public GraphQlData Data { get; set; }
    }

    public class GraphQlData
    {
        [JsonProperty("__schema")]
        public GraphQlSchema Schema { get; set; }
    }

    public class GraphQlSchema
    {
        public GraphQlRequestType QueryType { get; set; }
        public IList<GraphQlType> Types { get; set; }
        public IList<GraphQlDirective> Directives { get; set; }
        public GraphQlRequestType MutationType { get; set; }
        public GraphQlRequestType SubscriptionType { get; set; }
    }

    public class GraphQlDirective
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<GraphQlDirectiveLocation> Locations { get; set; }
        public IList<GraphQlArgument> Args { get; set; }
    }

    public class GraphQlRequestType
    {
        public string Name { get; set; }
    }

    public class GraphQlType : GraphQlTypeBase
    {
        public string Description { get; set; }
        public IList<GraphQlField> Fields { get; set; }
        public IList<GraphQlArgument> InputFields { get; set; }
        public IList<GraphQlFieldType> Interfaces { get; set; }
        public IList<GraphQlEnumValue> EnumValues { get; set; }
        public IList<GraphQlFieldType> PossibleTypes { get; set; }
    }

    public abstract class GraphQlValueBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class GraphQlEnumValue : GraphQlValueBase
    {
        public bool IsDeprecated { get; set; }
        public string DeprecationReason { get; set; }
    }

    public class GraphQlField : GraphQlEnumValue, IGraphQlMember
    {
        public IList<GraphQlArgument> Args { get; set; }
        public GraphQlFieldType Type { get; set; }
    }

    public class GraphQlArgument : GraphQlValueBase, IGraphQlMember
    {
        public GraphQlFieldType Type { get; set; }
        public object DefaultValue { get; set; }
    }

    public class GraphQlFieldType : GraphQlTypeBase
    {
        public GraphQlFieldType OfType { get; set; }
    }

    public abstract class GraphQlTypeBase
    {
        public const string GraphQlTypeScalarBoolean = "Boolean";
        public const string GraphQlTypeScalarFloat = "Float";
        public const string GraphQlTypeScalarId = "ID";
        public const string GraphQlTypeScalarInteger = "Int";
        public const string GraphQlTypeScalarString = "String";

        public GraphQlTypeKind Kind { get; set; }
        public string Name { get; set; }
    }

    public interface IGraphQlMember
    {
        string Name { get; }
        string Description { get; }
        GraphQlFieldType Type { get; }
    }

    public enum GraphQlDirectiveLocation
    {
        /// <summary>
        /// Location adjacent to a query operation.
        /// </summary>
        [EnumMember(Value = "QUERY")] Query,
        /// <summary>
        /// Location adjacent to a mutation operation.
        /// </summary>
        [EnumMember(Value = "MUTATION")] Mutation,
        /// <summary>
        /// Location adjacent to a subscription operation.
        /// </summary>
        [EnumMember(Value = "SUBSCRIPTION")] Subscription,
        /// <summary>
        /// Location adjacent to a field.
        /// </summary>
        [EnumMember(Value = "FIELD")] Field,
        /// <summary>
        /// Location adjacent to a fragment definition.
        /// </summary>
        [EnumMember(Value = "FRAGMENT_DEFINITION")] FragmentDefinition,
        /// <summary>
        /// Location adjacent to a fragment spread.
        /// </summary>
        [EnumMember(Value = "FRAGMENT_SPREAD")] FragmentSpread,
        /// <summary>
        /// Location adjacent to an inline fragment.
        /// </summary>
        [EnumMember(Value = "INLINE_FRAGMENT")] InlineFragment,
        /// <summary>
        /// Location adjacent to a variable definition.
        /// </summary>
        [EnumMember(Value = "VARIABLE_DEFINITION")] VariableDefinition,
        /// <summary>
        /// Location adjacent to a schema definition.
        /// </summary>
        [EnumMember(Value = "SCHEMA")] Schema,
        /// <summary>
        /// Location adjacent to a scalar definition.
        /// </summary>
        [EnumMember(Value = "SCALAR")] Scalar,
        /// <summary>
        /// Location adjacent to an object type definition.
        /// </summary>
        [EnumMember(Value = "OBJECT")] Object,
        /// <summary>
        /// Location adjacent to a field definition.
        /// </summary>
        [EnumMember(Value = "FIELD_DEFINITION")] FieldDefinition,
        /// <summary>
        /// Location adjacent to an argument definition.
        /// </summary>
        [EnumMember(Value = "ARGUMENT_DEFINITION")] ArgumentDefinition,
        /// <summary>
        /// Location adjacent to an interface definition.
        /// </summary>
        [EnumMember(Value = "INTERFACE")] Interface,
        /// <summary>
        /// Location adjacent to a union definition.
        /// </summary>
        [EnumMember(Value = "UNION")] Union,
        /// <summary>
        /// Location adjacent to an enum definition.
        /// </summary>
        [EnumMember(Value = "ENUM")] Enum,
        /// <summary>
        /// Location adjacent to an enum value definition.
        /// </summary>
        [EnumMember(Value = "ENUM_VALUE")] EnumValue,
        /// <summary>
        /// Location adjacent to an input object type definition.
        /// </summary>
        [EnumMember(Value = "INPUT_OBJECT")] InputObject,
        /// <summary>
        /// Location adjacent to an input field definition.
        /// </summary>
        [EnumMember(Value = "INPUT_FIELD_DEFINITION")] InputFieldDefinition
    }
}