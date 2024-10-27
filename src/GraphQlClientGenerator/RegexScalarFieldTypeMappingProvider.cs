using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace GraphQlClientGenerator;

public class RegexScalarFieldTypeMappingProvider(IReadOnlyCollection<RegexScalarFieldTypeMappingRule> rules) : IScalarFieldTypeMappingProvider
{
    private readonly IReadOnlyCollection<RegexScalarFieldTypeMappingRule> _rules = rules ?? throw new ArgumentNullException(nameof(rules));

    public static IReadOnlyCollection<RegexScalarFieldTypeMappingRule> ParseRulesFromJson(string json)
    {
        var rules = JsonConvert.DeserializeObject<IReadOnlyCollection<RegexScalarFieldTypeMappingRule>>(json);
        return rules ?? [];
    }

    public ScalarFieldTypeDescription GetCustomScalarFieldType(ScalarFieldTypeProviderContext context)
    {
        var valueType = context.FieldType.UnwrapIfNonNull();

        foreach (var rule in _rules)
            if (Regex.IsMatch(context.FieldName, rule.PatternValueName) &&
                Regex.IsMatch(context.OwnerType.Name, rule.PatternBaseType) &&
                Regex.IsMatch(valueType.Name ?? String.Empty, rule.PatternValueType))
                return new ScalarFieldTypeDescription { NetTypeName = rule.NetTypeName, FormatMask = rule.FormatMask };

        return DefaultScalarFieldTypeMappingProvider.GetFallbackFieldType(context);
    }
}

public class RegexScalarFieldTypeMappingRule
{
    public string PatternBaseType { get; set; }
    public string PatternValueType { get; set; }
    public string PatternValueName { get; set; }
    public string NetTypeName { get; set; }
    public string FormatMask { get; set; }
}