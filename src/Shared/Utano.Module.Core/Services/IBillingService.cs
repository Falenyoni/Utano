namespace Utano.Module.Core.Services;

public interface IBillingService
{
    Task CreateDraftInvoiceForVisitAsync(
        Guid practiceId,
        Guid visitId,
        Guid patientId,
        string patientName,
        Guid? doctorId,
        string? doctorName,
        CancellationToken cancellationToken = default);

    Task AddPrescriptionLineItemAsync(
        Guid practiceId,
        Guid visitId,
        Guid patientId,
        string patientName,
        string description,
        decimal quantity,
        decimal unitPrice,
        Guid? stockItemId,
        CancellationToken ct = default);
}
