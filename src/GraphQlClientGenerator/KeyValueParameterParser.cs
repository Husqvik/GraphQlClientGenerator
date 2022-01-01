namespace GraphQlClientGenerator;

public static class KeyValueParameterParser
{
    public static bool TryGetCustomClassMapping(IEnumerable<string> sourceParameters, out ICollection<KeyValuePair<string, string>> customMapping, out string errorMessage)
    {
        customMapping = new List<KeyValuePair<string, string>>();

        foreach (var parameter in sourceParameters ?? Enumerable.Empty<string>())
        {
            var parts = parameter.Split(':');
            if (parts.Length != 2)
            {
                errorMessage = "\"classMapping\" value must have format {GraphQlTypeName}:{C#ClassName}. ";
                return false;
            }

            var cSharpClassName = parts[1];
            if (!CSharpHelper.IsValidIdentifier(cSharpClassName))
            {
                errorMessage = $"\"{cSharpClassName}\" is not valid C# class name. ";
                return false;
            }

            customMapping.Add(new KeyValuePair<string, string>(parts[0], cSharpClassName));
        }

        errorMessage = null;
        return true;
    }

    public static bool TryGetCustomHeaders(IEnumerable<string> sourceParameters, out ICollection<KeyValuePair<string, string>> headers, out string errorMessage)
    {
        headers = new List<KeyValuePair<string, string>>();

        foreach (var parameter in sourceParameters ?? Enumerable.Empty<string>())
        {
            var parts = parameter.Split(new[] { ':' }, 2);
            if (parts.Length != 2)
            {
                errorMessage = "\"header\" value must have format {Header}:{Value}. ";
                return false;
            }

            headers.Add(new KeyValuePair<string, string>(parts[0], parts[1]));
        }

        errorMessage = null;
        return true;
    }
}