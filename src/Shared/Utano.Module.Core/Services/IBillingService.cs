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

    /// <summary>
    /// Looks up the service item mapped to the given appointment type and adds it as a
    /// Consultation line item on the visit invoice. No-ops if no matching item is found.
    /// </summary>
    Task TryAddAppointmentConsultationFeeAsync(
        Guid practiceId,
        Guid visitId,
        Guid patientId,
        string patientName,
        Guid? doctorId,
        string? doctorName,
        string appointmentTypeKey,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a service (consultation/procedure) line item to the visit invoice.
    /// lineItemType must be a valid LineItemType enum name (e.g. "Consultation", "Procedure").
    /// </summary>
    Task AddServiceLineItemAsync(
        Guid practiceId,
        Guid visitId,
        Guid patientId,
        string patientName,
        Guid? doctorId,
        string? doctorName,
        string description,
        decimal unitPrice,
        string lineItemType,
        Guid serviceItemId,
        CancellationToken ct = default);
}
