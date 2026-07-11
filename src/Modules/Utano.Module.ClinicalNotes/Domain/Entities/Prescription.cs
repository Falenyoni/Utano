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
    public Guid StockItemId { get; private set; }
    public string StockItemName { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal? QuantityDispensed { get; private set; }
    public string? DosageInstructions { get; private set; }
    public PrescriptionStatus Status { get; private set; }

    public static Prescription Create(
        Guid practiceId,
        Guid visitId,
        Guid patientId,
        string patientName,
        Guid stockItemId,
        string stockItemName,
        decimal quantity,
        string? dosageInstructions = null)
    {
        if (quantity <= 0)
            throw new UtanoDomainException("Quantity must be greater than zero.");

        return new Prescription
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            VisitId = visitId,
            PatientId = patientId,
            PatientName = patientName,
            StockItemId = stockItemId,
            StockItemName = stockItemName.Trim(),
            Description = stockItemName.Trim(),
            Quantity = quantity,
            Status = PrescriptionStatus.Pending,
            DosageInstructions = dosageInstructions?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Dispense(decimal quantityDispensed)
    {
        if (Status != PrescriptionStatus.Pending)
            throw new UtanoDomainException("Prescription cannot be dispensed in its current state.");
        if (quantityDispensed <= 0)
            throw new UtanoDomainException("Quantity dispensed must be greater than zero.");

        QuantityDispensed = quantityDispensed;
        Status = quantityDispensed >= Quantity
            ? PrescriptionStatus.Dispensed
            : PrescriptionStatus.PartiallyDispensed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkExternal()
    {
        if (Status != PrescriptionStatus.Pending)
            throw new UtanoDomainException("Prescription cannot be marked external in its current state.");
        Status = PrescriptionStatus.External;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
