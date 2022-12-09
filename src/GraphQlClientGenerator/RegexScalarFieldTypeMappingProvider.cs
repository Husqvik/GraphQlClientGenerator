using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace GraphQlClientGenerator;

public class RegexScalarFieldTypeMappingProvider : IScalarFieldTypeMappingProvider
{
    private readonly ICollection<RegexScalarFieldTypeMappingRule> _rules;

    public static ICollection<RegexScalarFieldTypeMappingRule> ParseRulesFromJson(string json)
    {
        var rules = JsonConvert.DeserializeObject<ICollection<RegexScalarFieldTypeMappingRule>>(json);
        return rules ?? Array.Empty<RegexScalarFieldTypeMappingRule>();
    }

    public RegexScalarFieldTypeMappingProvider(ICollection<RegexScalarFieldTypeMappingRule> rules) =>
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));

    public ScalarFieldTypeDescription GetCustomScalarFieldType(GraphQlGeneratorConfiguration configuration, GraphQlType baseType, GraphQlTypeBase valueType, string valueName)
    {
        valueType = (valueType as GraphQlFieldType)?.UnwrapIfNonNull() ?? valueType;

        foreach (var rule in _rules)
            if (Regex.IsMatch(valueName, rule.PatternValueName) &&
                Regex.IsMatch(baseType.Name, rule.PatternBaseType) &&
                Regex.IsMatch(valueType.Name ?? String.Empty, rule.PatternValueType))
                return new ScalarFieldTypeDescription { NetTypeName = rule.NetTypeName, FormatMask = rule.FormatMask };

        return DefaultScalarFieldTypeMappingProvider.GetFallbackFieldType(configuration, valueType);
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