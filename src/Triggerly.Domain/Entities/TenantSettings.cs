using Triggerly.Shared.Models;

namespace Triggerly.Domain.Entities;

public class TenantSettings
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public ClientSource ClientDataSource { get; private set; } = ClientSource.Internal;
    public DateTime UpdatedAt { get; private set; }

    private TenantSettings() { }

    public static TenantSettings Create(string tenantId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClientDataSource = ClientSource.Internal,
            UpdatedAt = DateTime.UtcNow,
        };

    public void SetClientDataSource(ClientSource source)
    {
        ClientDataSource = source;
        UpdatedAt = DateTime.UtcNow;
    }
}
