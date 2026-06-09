using Cronos;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Triggerly.Application.Commands.Workflows;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Infrastructure.Services;

public class SchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulerHostedService> _logger;

    public SchedulerHostedService(IServiceScopeFactory scopeFactory, ILogger<SchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Align to the next whole minute then tick every 60 seconds
        var delay = 60 - DateTime.UtcNow.Second;
        await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            await ProcessDueRulesAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessDueRulesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var ruleRepo = scope.ServiceProvider.GetRequiredService<IAutomationRuleRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var rules = await ruleRepo.GetEnabledScheduleRulesAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var rule in rules)
        {
            try
            {
                var cron = ParseCron(rule.TriggerConfig);
                if (cron is null) continue;

                var from = DateTime.SpecifyKind(rule.LastTriggeredAt ?? rule.CreatedAt, DateTimeKind.Utc);
                var next = cron.GetNextOccurrence(from, TimeZoneInfo.Utc);
                if (next is null || next > now) continue;

                _logger.LogInformation("Triggering scheduled rule {RuleId} ({Name})", rule.Id, rule.Name);

                await mediator.Send(new TriggerWorkflowCommand(
                    rule.WorkflowId,
                    rule.TenantId,
                    "system",
                    "Scheduler",
                    null), cancellationToken);

                await ruleRepo.RecordTriggerAsync(rule.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger scheduled rule {RuleId}", rule.Id);
            }
        }
    }

    private CronExpression? ParseCron(string triggerConfig)
    {
        try
        {
            var doc = JsonDocument.Parse(triggerConfig);
            if (!doc.RootElement.TryGetProperty("cron", out var cronProp)) return null;
            var expr = cronProp.GetString();
            return string.IsNullOrWhiteSpace(expr) ? null : CronExpression.Parse(expr);
        }
        catch
        {
            return null;
        }
    }
}
