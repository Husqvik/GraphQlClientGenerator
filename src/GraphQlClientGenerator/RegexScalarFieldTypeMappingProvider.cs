using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace GraphQlClientGenerator;

public class RegexScalarFieldTypeMappingProvider : IScalarFieldTypeMappingProvider
{
    private readonly IReadOnlyCollection<RegexScalarFieldTypeMappingRule> _rules;

    public static IReadOnlyCollection<RegexScalarFieldTypeMappingRule> ParseRulesFromJson(string json)
    {
        var rules = JsonConvert.DeserializeObject<IReadOnlyCollection<RegexScalarFieldTypeMappingRule>>(json);
        return rules ?? [];
    }

    public RegexScalarFieldTypeMappingProvider(IReadOnlyCollection<RegexScalarFieldTypeMappingRule> rules) =>
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));

    public ScalarFieldTypeDescription GetCustomScalarFieldType(GraphQlGeneratorConfiguration configuration, GraphQlType baseType, GraphQlTypeBase valueType, string valueName)
    {
        var unwrappedValueType = (valueType as GraphQlFieldType)?.UnwrapIfNonNull() ?? valueType;

        foreach (var rule in _rules)
        {
            var expectNonNullType = unwrappedValueType != valueType && rule.PatternValueType.EndsWith("!");

            if (Regex.IsMatch(valueName, rule.PatternValueName) &&
                Regex.IsMatch(baseType.Name, rule.PatternBaseType) &&
                Regex.IsMatch((expectNonNullType ? $"{unwrappedValueType.Name}!" : valueType.Name) ?? String.Empty, rule.PatternValueType))
                return new ScalarFieldTypeDescription { NetTypeName = rule.NetTypeName, FormatMask = rule.FormatMask };
        }

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