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
        public ICollection<GraphQlType> Types { get; set; }
        public ICollection<GraphQlDirective> Directives { get; set; }
        public GraphQlRequestType MutationType { get; set; }
        public GraphQlRequestType SubscriptionType { get; set; }
    }

    public class GraphQlDirective
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<string> Locations { get; set; }
        public ICollection<GraphQlArgument> Args { get; set; }
    }

    public class GraphQlRequestType
    {
        public string Name { get; set; }
    }

    public class GraphQlType : GraphQlTypeBase
    {
        public string Description { get; set; }
        public ICollection<GraphQlField> Fields { get; set; }
        public ICollection<GraphQlArgument> InputFields { get; set; }
        public ICollection<GraphQlFieldType> Interfaces { get; set; }
        public ICollection<GraphQlEnumValue> EnumValues { get; set; }
        public ICollection<GraphQlFieldType> PossibleTypes { get; set; }
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
        public ICollection<GraphQlArgument> Args { get; set; }
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
        public const string GraphQlTypeScalarJson = "Json";

        public string Kind { get; set; }
        public string Name { get; set; }
    }

    public interface IGraphQlMember
    {
        string Name { get; }
        string Description { get; }
        GraphQlFieldType Type { get; }
    }
}