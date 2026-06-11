using Triggerly.Shared.Models;

namespace Triggerly.Domain.Entities;

public class Client
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? BalanceDate { get; private set; }
    public string? IrdNumber { get; private set; }
    public string? Notes { get; private set; }
    public string? ExternalId { get; private set; }
    public ClientSource Source { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<ClientService> _services = [];
    public IReadOnlyList<ClientService> Services => _services.AsReadOnly();

    private Client() { }

    public static Client Create(string tenantId, string name, string email,
        string? phone = null, string? balanceDate = null, string? irdNumber = null,
        string? notes = null, ClientSource source = ClientSource.Internal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Client
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone?.Trim(),
            BalanceDate = balanceDate?.Trim(),
            IrdNumber = irdNumber?.Trim(),
            Notes = notes?.Trim(),
            Source = source,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string name, string email, string? phone, string? balanceDate,
        string? irdNumber, string? notes)
    {
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        BalanceDate = balanceDate?.Trim();
        IrdNumber = irdNumber?.Trim();
        Notes = notes?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
