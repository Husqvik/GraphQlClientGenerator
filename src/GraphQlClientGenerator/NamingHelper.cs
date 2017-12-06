using System;
using System.Text;

namespace GraphQlClientGenerator
{
    internal static class NamingHelper
    {
        public static string CapitalizeFirst(string value)
        {
            var firstLetter = value[0];
            return value.Remove(0, 1).Insert(0, firstLetter.ToString().ToUpperInvariant());
        }

        public static string LowerFirst(string value)
        {
            var firstLetter = value[0];
            return value.Remove(0, 1).Insert(0, firstLetter.ToString().ToLowerInvariant());
        }

        public static string ToNetEnumName(string name)
        {
            var builder = new StringBuilder();
            var startNewWord = true;
            var hasLowerLetters = false;
            var hasUpperLetters = false;
            foreach (var @char in name)
            {
                if (@char == '_')
                {
                    startNewWord = true;
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
}