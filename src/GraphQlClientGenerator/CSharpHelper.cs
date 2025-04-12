using System.Globalization;

namespace GraphQlClientGenerator;

public static class CSharpHelper
{
    private static readonly HashSet<string> CSharpKeywords =
    [
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "void",
        "volatile",
        "while"
    ];

    public static string EnsureCSharpQuoting(string name) => CSharpKeywords.Contains(name) ? $"@{name}" : name;

    public static bool IsValidIdentifier(string value)
    {
        var nextMustBeStartChar = true;

        if (value.Length == 0)
            return false;

        foreach (var ch in value)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
            switch (unicodeCategory)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.OtherLetter:
                    nextMustBeStartChar = false;
                    break;

                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.DecimalDigitNumber:
                    if (nextMustBeStartChar && ch != '_')
                        return false;

                    nextMustBeStartChar = false;
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    public static bool IsValidNamespace(string @namespace)
    {
        if (String.IsNullOrWhiteSpace(@namespace))
            return false;

        var namespaceElements = @namespace.Split('.');
        return namespaceElements.All(e => IsValidIdentifier(e.Trim()));
    }

    public static void ValidateClassName(string className)
    {
        if (!IsValidIdentifier(className))
            throw new InvalidOperationException($"Resulting class name \"{className}\" is not valid. ");
    }

    internal static bool IsTargetTypedNewSupported(this CSharpVersion cSharpVersion) =>
        cSharpVersion >= CSharpVersion.CSharp12;

    internal static bool IsCollectionExpressionSupported(this CSharpVersion cSharpVersion) =>
        cSharpVersion >= CSharpVersion.CSharp12;

    internal static bool IsSystemTextJsonSupported(this CSharpVersion cSharpVersion) =>
        cSharpVersion >= CSharpVersion.CSharp12;

    internal static bool IsFieldKeywordSupported(this CSharpVersion cSharpVersion) =>
        cSharpVersion >= CSharpVersion.CSharp12;
}