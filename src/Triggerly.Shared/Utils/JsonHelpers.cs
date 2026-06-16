using System.Globalization;
using System.Text.Json;

namespace Triggerly.Shared.Utils;

public static class JsonHelpers
{
    public static string? GetString(object? v) => v switch
    {
        string s => s,
        JsonElement je => je.ValueKind == JsonValueKind.String ? je.GetString() : je.ToString(),
        _ => v?.ToString()
    };

    public static int GetInt(object? v, int defaultValue = 0)
    {
        var s = GetString(v);
        return s is not null && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? (int)Math.Round(d)
            : defaultValue;
    }
}
