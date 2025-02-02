using System.Globalization;

namespace GraphQlClientGenerator;

public static class CSharpHelper
{
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
        if (!CSharpHelper.IsValidIdentifier(className))
            throw new InvalidOperationException($"Resulting class name \"{className}\" is not valid. ");
    }

    public static bool UseTargetTypedNew(this CSharpVersion cSharpVersion) =>
        cSharpVersion >= CSharpVersion.CSharp12;

    public static bool UseCollectionExpression(this CSharpVersion cSharpVersion) =>
        cSharpVersion >= CSharpVersion.CSharp12;
}