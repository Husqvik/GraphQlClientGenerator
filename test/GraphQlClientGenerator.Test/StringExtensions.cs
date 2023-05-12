using System.Text.RegularExpressions;

namespace GraphQlClientGenerator.Test;

public static class StringExtensions
{
    public static string NormalizeLineEndings(this string source, string lineEnding = "\r\n")
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return Regex.Replace(source, @"\r\n?|\n", lineEnding);
    }
}