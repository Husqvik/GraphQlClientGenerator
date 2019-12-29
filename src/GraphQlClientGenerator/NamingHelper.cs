using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GraphQlClientGenerator
{
    internal static class NamingHelper
    {
        private static readonly HashSet<string> CSharpKeywords =
            new HashSet<string>
            {
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
                "using",
                "static",
                "void",
                "volatile",
                "while",
            };

        public static string LowerFirst(string value)
        {
            var firstLetter = value[0];
            return value.Remove(0, 1).Insert(0, firstLetter.ToString().ToLowerInvariant());
        }

        public static string ToValidVariableName(string name)
        {
            if (CSharpKeywords.Contains(name))
                return "@" + name;

            return name;
        }

        /// <remarks>https://stackoverflow.com/questions/18627112/how-can-i-convert-text-to-pascal-case</remarks>>
        public static string ToPascalCase(string value)
        {
            var invalidCharsRegex = new Regex("[^_a-zA-Z0-9]");
            var whiteSpace = new Regex(@"(?<=\s)");
            var upperCaseFirstLetter = new Regex("^[a-z]");
            var firstCharFollowedByUpperCasesOnly = new Regex("(?<=[A-Z])[A-Z0-9]+$");
            var lowerCaseNextToNumber = new Regex("(?<=[0-9])[a-z]");
            var upperCaseInside = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

            var pascalCase =
                invalidCharsRegex
                    // Replaces white spaces with underscore, then replace all invalid chars with an empty string.
                    .Replace(whiteSpace.Replace(value, "_"), String.Empty)
                    .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => upperCaseFirstLetter.Replace(w, m => m.Value.ToUpper()))
                    // Replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc).
                    .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
                    // Set upper case the first lower case following a number (Ab9cd -> Ab9Cd).
                    .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
                    // Lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef).
                    .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

            return String.Concat(pascalCase);
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