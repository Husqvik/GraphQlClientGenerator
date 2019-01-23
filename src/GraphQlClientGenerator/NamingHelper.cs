using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

        public static string ToPascalCase(string value)
        {
            /*
             * Source: https://stackoverflow.com/questions/18627112/how-can-i-convert-text-to-pascal-case
             * Credits: chviLadislav
             * 
             * Example Output:
             * "WARD_VS_VITAL_SIGNS"          "WardVsVitalSigns"
             * "Who am I?"                    "WhoAmI"
             * "I ate before you got here"    "IAteBeforeYouGotHere"
             * "Hello|Who|Am|I?"              "HelloWhoAmI"
             * "Live long and prosper"        "LiveLongAndProsper"
             * "Lorem ipsum dolor..."         "LoremIpsumDolor"
             * "CoolSP"                       "CoolSp"
             * "AB9CD"                        "Ab9Cd"
             * "CCCTrigger"                   "CccTrigger"
             * "CIRC"                         "Circ"
             * "ID_SOME"                      "IdSome"
             * "ID_SomeOther"                 "IdSomeOther"
             * "ID_SOMEOther"                 "IdSomeOther"
             * "CCC_SOME_2Phases"             "CccSome2Phases"
             * "AlreadyGoodPascalCase"        "AlreadyGoodPascalCase"
             * "999 999 99 9 "                "999999999"
             * "1 2 3 "                       "123"
             * "1 AB cd EFDDD 8"              "1AbCdEfddd8"
             * "INVALID VALUE AND _2THINGS"   "InvalidValueAnd2Things"
             */
            
            var invalidCharsRgx = new Regex("[^_a-zA-Z0-9]");
            var whiteSpace = new Regex(@"(?<=\s)");
            var startsWithLowerCaseChar = new Regex("^[a-z]");
            var firstCharFollowedByUpperCasesOnly = new Regex("(?<=[A-Z])[A-Z0-9]+$");
            var lowerCaseNextToNumber = new Regex("(?<=[0-9])[a-z]");
            var upperCaseInside = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");
            
            var pascalCase = invalidCharsRgx
                                     
                             // Replaces white spaces with underscore, then replace all invalid chars with an empty string.
                            .Replace(whiteSpace.Replace(value, "_"), string.Empty)
                             
                             // Split by underscores.
                            .Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries)
                             
                             // Set first letter to uppercase.
                            .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
                             
                             // Replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc).
                            .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
                             
                             // Set upper case the first lower case following a number (Ab9cd -> Ab9Cd).
                            .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
                             
                             // Lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef).
                            .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

            return string.Concat(pascalCase);
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