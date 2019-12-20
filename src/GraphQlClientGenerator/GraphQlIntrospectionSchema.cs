using System.Collections.Generic;
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
        public ICollection<string> Locations { get; set; }
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
}