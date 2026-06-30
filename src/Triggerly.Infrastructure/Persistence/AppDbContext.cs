using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TenantRole> TenantRoles => Set<TenantRole>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WorkflowDefinition> Workflows => Set<WorkflowDefinition>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowVersion> WorkflowVersions => Set<WorkflowVersion>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<WorkflowExecution> Executions => Set<WorkflowExecution>();
    public DbSet<ExecutionStep> ExecutionSteps => Set<ExecutionStep>();
    public DbSet<ExecutionComment> ExecutionComments => Set<ExecutionComment>();
    public DbSet<TeamInvite> TeamInvites => Set<TeamInvite>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<ClientService> ClientServices => Set<ClientService>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
    public DbSet<TenantSsoConfig> TenantSsoConfigs => Set<TenantSsoConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
