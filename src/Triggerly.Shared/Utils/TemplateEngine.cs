using System.Text.RegularExpressions;

namespace Triggerly.Shared.Utils;

public static partial class TemplateEngine
{
    [GeneratedRegex(@"\{\{input\.([^}]+)\}\}")]
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
        if (value is string s)
            return TokenRegex().Replace(s, m =>
                inputData.TryGetValue(m.Groups[1].Value, out var v)
                    ? v?.ToString() ?? string.Empty
                    : m.Value);

        if (value is Dictionary<string, object> dict)
            return Resolve(dict, inputData);

        return value;
    }
}
