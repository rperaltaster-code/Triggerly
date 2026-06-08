using System.Net.Http.Json;
using Temporalio.Activities;

namespace Triggerly.Worker.Activities;

public class DataActivities
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DataActivities(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [Activity]
    public Task<Dictionary<string, object>> TransformDataAsync(
        Dictionary<string, object> input, Dictionary<string, object> config)
    {
        var result = new Dictionary<string, object>(input);

        if (config.TryGetValue("mappings", out var mappingsObj) &&
            mappingsObj is Dictionary<string, object> mappings)
        {
            foreach (var (target, source) in mappings)
            {
                if (source is string sourceKey && input.TryGetValue(sourceKey, out var value))
                    result[target] = value;
            }
        }

        return Task.FromResult(result);
    }

    [Activity]
    public async Task<string> CallWebhookAsync(Dictionary<string, object> config, Dictionary<string, object> context)
    {
        if (!config.TryGetValue("url", out var urlObj) || urlObj is not string url)
            throw new InvalidOperationException("Webhook URL not configured.");

        var method = config.TryGetValue("method", out var m) ? m?.ToString() ?? "POST" : "POST";
        var client = _httpClientFactory.CreateClient("webhook");

        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        request.Content = JsonContent.Create(context);

        using var response = await client.SendAsync(request, ActivityExecutionContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
