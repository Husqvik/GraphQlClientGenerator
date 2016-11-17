namespace GraphQlClientGenerator
{
    public static class NamingHelper
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
    }
}