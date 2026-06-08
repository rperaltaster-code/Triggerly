using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Temporalio.Extensions.Hosting;
using Triggerly.Infrastructure.Extensions;
using Triggerly.Shared.Contracts;
using Triggerly.Worker.Activities;
using Triggerly.Worker.Workflows;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHttpClient("webhook");

builder.Services
    .AddTemporalClient(opts =>
    {
        opts.TargetHost = builder.Configuration["Temporal:Address"] ?? "localhost:7233";
    })
    .AddHostedTemporalWorker(TemporalConstants.TaskQueue)
    .AddScopedActivities<WorkflowActivities>()
    .AddScopedActivities<NotificationActivities>()
    .AddScopedActivities<DataActivities>()
    .AddWorkflow<AutomationWorkflow>();

await builder.Build().RunAsync();
