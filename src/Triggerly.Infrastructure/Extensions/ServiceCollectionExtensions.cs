using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Temporalio.Client;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Auth; // PasswordHasher only — TokenService lives in Api (needs JwtBearer)
using Triggerly.Infrastructure.Email;
using Triggerly.Infrastructure.Persistence;
using Triggerly.Infrastructure.Repositories;
using Triggerly.Infrastructure.Temporal;

namespace Triggerly.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("TriggerlyDb"));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IAutomationRuleRepository, AutomationRuleRepository>();
        services.AddScoped<IWorkflowExecutionRepository, WorkflowExecutionRepository>();

        services.AddSingleton<ITemporalClient>(_ =>
        {
            var address = configuration["Temporal:Address"] ?? "localhost:7233";
            return TemporalClient.ConnectAsync(new TemporalClientConnectOptions(address))
                .GetAwaiter().GetResult();
        });

        services.AddScoped<ITemporalService, TemporalService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
