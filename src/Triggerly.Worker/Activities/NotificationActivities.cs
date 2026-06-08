using Temporalio.Activities;

namespace Triggerly.Worker.Activities;

public class NotificationActivities
{
    [Activity]
    public async Task SendNotificationAsync(string tenantId, Dictionary<string, object> config, Dictionary<string, object> context)
    {
        var channel = config.TryGetValue("channel", out var ch) ? ch?.ToString() : "email";
        var recipient = config.TryGetValue("recipient", out var r) ? r?.ToString() : null;
        var message = config.TryGetValue("message", out var m) ? m?.ToString() : "Workflow notification";

        ActivityExecutionContext.Current.Logger.LogInformation(
            "Sending notification via {Channel} to {Recipient}: {Message}",
            channel, recipient, message);

        await Task.Delay(50);
    }
}
