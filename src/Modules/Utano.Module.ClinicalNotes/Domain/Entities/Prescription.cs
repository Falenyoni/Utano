using Utano.Module.ClinicalNotes.Domain.Enums;
using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.ClinicalNotes.Domain.Entities;

public class Prescription : AggregateRoot
{
    private Prescription() { }

    public Guid VisitId { get; private set; }
    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public Guid? StockItemId { get; private set; }
    public string? StockItemName { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public string? DosageInstructions { get; private set; }
    public DispensingType DispensingType { get; private set; }
    public PrescriptionStatus Status { get; private set; }

    public static Prescription Create(
        Guid practiceId,
        Guid visitId,
        Guid patientId,
        string patientName,
        string description,
        decimal quantity,
        DispensingType dispensingType,
        Guid? stockItemId = null,
        string? stockItemName = null,
        string? dosageInstructions = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new UtanoDomainException("Prescription description is required.");
        if (quantity <= 0)
            throw new UtanoDomainException("Quantity must be greater than zero.");
        if (dispensingType == DispensingType.BillAndDispense && stockItemId is null)
            throw new UtanoDomainException("A stock item must be selected for Bill & Dispense prescriptions.");

        return new Prescription
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            VisitId = visitId,
            PatientId = patientId,
            PatientName = patientName,
            Description = description.Trim(),
            Quantity = quantity,
            DispensingType = dispensingType,
            Status = PrescriptionStatus.Pending,
            StockItemId = stockItemId,
            StockItemName = stockItemName?.Trim(),
            DosageInstructions = dosageInstructions?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkDispensed()
    {
        if (Status == PrescriptionStatus.Dispensed)
            throw new UtanoDomainException("Prescription is already dispensed.");
        Status = PrescriptionStatus.Dispensed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
