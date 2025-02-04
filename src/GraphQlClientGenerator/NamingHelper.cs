using System.Text;
using System.Text.RegularExpressions;

namespace GraphQlClientGenerator;

internal static class NamingHelper
{
    internal const string MetadataFieldTypeName = "__typename";

    private static readonly char[] UnderscoreSeparator = ['_'];

    public static string LowerFirst(string value) => $"{Char.ToLowerInvariant(value[0])}{value.Substring(1)}";

    private static readonly Regex RegexInvalidCharacters = new("[^_a-zA-Z0-9]");
    private static readonly Regex RegexNextWhiteSpace = new(@"(?<=\s)");
    private static readonly Regex RegexWhiteSpace = new(@"\s");
    private static readonly Regex RegexUpperCaseFirstLetter = new("^[a-z]");
    private static readonly Regex RegexFirstCharFollowedByUpperCasesOnly = new("(?<=[A-Z])[A-Z0-9]+$");
    private static readonly Regex RegexLowerCaseNextToNumber = new("(?<=[0-9])[a-z]");
    private static readonly Regex RegexUpperCaseInside = new("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

    /// <remarks>https://stackoverflow.com/questions/18627112/how-can-i-convert-text-to-pascal-case</remarks>>
    public static string ToPascalCase(string text)
    {
        if (text is MetadataFieldTypeName)
            return "TypeName";

        var textWithoutWhiteSpace = RegexInvalidCharacters.Replace(RegexWhiteSpace.Replace(text, String.Empty), String.Empty);
        if (textWithoutWhiteSpace.All(c => c is '_'))
            return textWithoutWhiteSpace;

        var pascalCase =
            RegexInvalidCharacters
                // Replaces white spaces with underscore, then replace all invalid chars with an empty string.
                .Replace(RegexNextWhiteSpace.Replace(text, "_"), String.Empty)
                .Split(UnderscoreSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => RegexUpperCaseFirstLetter.Replace(w, m => m.Value.ToUpper()))
                // Replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc).
                .Select(w => RegexFirstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
                // Set upper case the first lower case following a number (Ab9cd -> Ab9Cd).
                .Select(w => RegexLowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
                // Lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef).
                .Select(w => RegexUpperCaseInside.Replace(w, m => m.Value.ToLower()));

        return String.Concat(pascalCase);
    }

    public static string ToCSharpEnumName(string name)
    {
        var builder = new StringBuilder();
        var startNewWord = true;
        var hasLowerLetters = false;
        var hasUpperLetters = false;
        var length = name?.Length ?? throw new ArgumentNullException(nameof(name));

        for (var i = 0; i < length; i++)
        {
            var @char = name[i];
            if (@char is '_')
            {
                startNewWord = true;

                if (i == 0 && length > 1 && Char.IsDigit(name[i + 1]))
                    builder.Append('_');

                continue;
            }

            hasLowerLetters |= Char.IsLower(@char);
            hasUpperLetters |= Char.IsUpper(@char);

            builder.Append(startNewWord ? Char.ToUpper(@char) : Char.ToLower(@char));

            startNewWord = Char.IsDigit(@char);
        }

        return hasLowerLetters && hasUpperLetters ? name : builder.ToString();
    }
}