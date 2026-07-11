using Utano.Module.Core.Domain.Aggregate;

namespace Utano.Module.ClinicalNotes.Domain.Entities;

public class VisitProcedure : AggregateRoot
{
    private VisitProcedure() { }

    public Guid VisitId { get; private set; }
    public Guid ServiceItemId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? NhrplCode { get; private set; }
    public string? Notes { get; private set; }
    public bool PostedToInvoice { get; private set; }

    public static VisitProcedure Create(
        Guid practiceId,
        Guid visitId,
        Guid serviceItemId,
        string name,
        string? nhrplCode,
        string? notes)
    {
        return new VisitProcedure
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            VisitId = visitId,
            ServiceItemId = serviceItemId,
            Name = name.Trim(),
            NhrplCode = nhrplCode?.Trim(),
            Notes = notes?.Trim(),
            PostedToInvoice = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkPostedToInvoice()
    {
        PostedToInvoice = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
