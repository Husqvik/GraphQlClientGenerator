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
        public object SubscriptionType { get; set; }
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
        public ICollection<GraphQlField> InputFields { get; set; }
        public ICollection<GraphQlInterface> Interfaces { get; set; }
        public ICollection<GraphQlEnumValue> EnumValues { get; set; }
        public ICollection<GraphQlFieldType> PossibleTypes { get; set; }
    }

    public class GraphQlInterface
    {
    }

    public class GraphQlEnumValue
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDeprecated { get; set; }
        public string DeprecationReason { get; set; }
    }

    public class GraphQlField : GraphQlEnumValue
    {
        public ICollection<GraphQlArgument> Args { get; set; }
        public GraphQlFieldType Type { get; set; }
    }

    public class GraphQlArgument
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public GraphQlFieldType Type { get; set; }
        public object DefaultValue { get; set; }
    }

    public class GraphQlFieldType : GraphQlTypeBase
    {
        public GraphQlFieldType OfType { get; set; }
    }

    public abstract class GraphQlTypeBase
    {
        public string Kind { get; set; }
        public string Name { get; set; }
    }
}