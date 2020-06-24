using System.Globalization;

namespace GraphQlClientGenerator
{
    public class CSharpHelper
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
    }
}