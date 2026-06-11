using Triggerly.Shared.Models;

namespace Triggerly.Domain.Entities;

public class ClientService
{
    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid ServiceTypeId { get; private set; }
    public Guid WorkflowId { get; private set; }
    public FilingPeriod FilingPeriod { get; private set; }
    public DateTime? LastFiledAt { get; private set; }
    public DateTime? NextDueAt { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    private ClientService() { }

    public static ClientService Create(Guid clientId, Guid serviceTypeId, Guid workflowId,
        FilingPeriod filingPeriod, string? notes = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ServiceTypeId = serviceTypeId,
            WorkflowId = workflowId,
            FilingPeriod = filingPeriod,
            IsActive = true,
            Notes = notes?.Trim(),
        };

    public void Update(Guid workflowId, FilingPeriod filingPeriod, bool isActive, string? notes)
    {
        WorkflowId = workflowId;
        FilingPeriod = filingPeriod;
        IsActive = isActive;
        Notes = notes?.Trim();
    }

    public void RecordFiling(DateTime filedAt)
    {
        LastFiledAt = filedAt;
        NextDueAt = ComputeNextDue(filedAt, FilingPeriod);
    }

    public static DateTime? ComputeNextDue(DateTime from, FilingPeriod period) => period switch
    {
        FilingPeriod.Monthly     => from.AddMonths(1),
        FilingPeriod.TwoMonthly  => from.AddMonths(2),
        FilingPeriod.SixMonthly  => from.AddMonths(6),
        FilingPeriod.Annual      => from.AddYears(1),
        FilingPeriod.OneOff      => null,
        _                        => null,
    };
}
