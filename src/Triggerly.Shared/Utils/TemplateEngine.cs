using System.Text.Json;
using System.Text.RegularExpressions;

namespace Triggerly.Shared.Utils;

public static partial class TemplateEngine
{
    [GeneratedRegex(@"\{\{(input|client|service)\.([^}]+)\}\}")]
    private static partial Regex TokenRegex();

    public static Dictionary<string, object> Resolve(
        Dictionary<string, object> config,
        Dictionary<string, object> inputData)
    {
        var result = new Dictionary<string, object>(config.Count);
        foreach (var (key, value) in config)
            result[key] = ResolveValue(value, inputData);
        return result;
    }

    private static object ResolveValue(object value, Dictionary<string, object> inputData)
    {
        // Temporal deserializes Dictionary<string,object> values as JsonElement
        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => ReplaceTokens(je.GetString()!, inputData),
                JsonValueKind.Object => Resolve(
                    je.Deserialize<Dictionary<string, object>>() ?? [], inputData),
                _ => value,
            };
        }

        if (value is string s)
            return ReplaceTokens(s, inputData);

        if (value is Dictionary<string, object> dict)
            return Resolve(dict, inputData);

        return value;
    }

    private static string ReplaceTokens(string s, Dictionary<string, object> inputData) =>
        TokenRegex().Replace(s, m =>
        {
            var ns = m.Groups[1].Value;
            var key = m.Groups[2].Value;
            // input.fieldId → look up "fieldId"; client.x / service.x → look up "client.x" / "service.x"
            var lookupKey = ns == "input" ? key : $"{ns}.{key}";
            if (!inputData.TryGetValue(lookupKey, out var v)) return m.Value;
            return v switch
            {
                JsonElement je => je.ValueKind == JsonValueKind.String
                    ? je.GetString()!
                    : je.ToString(),
                _ => v?.ToString() ?? string.Empty,
            };
        });
}
