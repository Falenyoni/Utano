using Utano.Module.Core.Domain.Aggregate;

namespace Utano.Module.ClinicalNotes.Domain.Entities;

public class VisitDiagnosis : AggregateRoot
{
    private VisitDiagnosis() { }

    public Guid VisitId { get; private set; }
    public string IcdCode { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public bool IsPrimary { get; private set; }

    public static VisitDiagnosis Create(
        Guid practiceId,
        Guid visitId,
        string icdCode,
        string description,
        bool isPrimary)
    {
        return new VisitDiagnosis
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            VisitId = visitId,
            IcdCode = icdCode.Trim().ToUpper(),
            Description = description.Trim(),
            IsPrimary = isPrimary,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
