namespace GraphQlClientGenerator;

public static class KeyValueParameterParser
{
    public static bool TryGetCustomClassMapping(IEnumerable<string> sourceParameters, out ICollection<KeyValuePair<string, string>> customMapping, out string errorMessage)
    {
        errorMessage =
            TryGetColonSplitKeyValuePairs(sourceParameters, out customMapping)
                ? null
                : "\"classMapping\" value must have format {GraphQlTypeName}:{C#ClassName}. ";

        if (errorMessage is null)
        {
            var firstInvalid = customMapping.FirstOrDefault(m => !CSharpHelper.IsValidIdentifier(m.Value));
            if (firstInvalid.Value is not null)
                errorMessage = $"\"{firstInvalid.Value}\" is not valid C# class name. ";
        }

        return errorMessage is null;
    }

    public static bool TryGetCustomHeaders(IEnumerable<string> sourceParameters, out ICollection<KeyValuePair<string, string>> headers, out string errorMessage)
    {
        errorMessage =
            TryGetColonSplitKeyValuePairs(sourceParameters, out headers)
                ? null
                : "\"header\" value must have format {Header}:{Value}. ";

        return errorMessage is null;
    }

    private static bool TryGetColonSplitKeyValuePairs(IEnumerable<string> sourceParameters, out ICollection<KeyValuePair<string, string>> keyValuePairs)
    {
        keyValuePairs = new List<KeyValuePair<string, string>>();

        foreach (var parameter in sourceParameters ?? [])
        {
            var parts = parameter.Split([':'], 2);
            if (parts.Length != 2)
                return false;

            keyValuePairs.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
        }

        return true;
    }
}