using System.Runtime.Serialization;

namespace GraphQlClientGenerator
{
    public enum GraphQlTypeKind
    {
        [EnumMember(Value = "OBJECT")] Object,
        [EnumMember(Value = "ENUM")] Enum,
        [EnumMember(Value = "SCALAR")] Scalar,
        [EnumMember(Value = "LIST")] List,
        [EnumMember(Value = "NON_NULL")] NonNull,
        [EnumMember(Value = "INPUT_OBJECT")] InputObject,
        [EnumMember(Value = "UNION")] Union,
        [EnumMember(Value = "INTERFACE")] Interface
    }
}