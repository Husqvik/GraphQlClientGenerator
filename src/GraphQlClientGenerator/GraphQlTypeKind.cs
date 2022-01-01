using System.Runtime.Serialization;

namespace GraphQlClientGenerator;

public enum GraphQlTypeKind
{
    [EnumMember(Value = "SCALAR")] Scalar,
    [EnumMember(Value = "ENUM")] Enum,
    [EnumMember(Value = "OBJECT")] Object,
    [EnumMember(Value = "INPUT_OBJECT")] InputObject,
    [EnumMember(Value = "UNION")] Union,
    [EnumMember(Value = "INTERFACE")] Interface,
    [EnumMember(Value = "LIST")] List,
    [EnumMember(Value = "NON_NULL")] NonNull
}