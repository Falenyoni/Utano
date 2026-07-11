namespace Utano.Module.Core.Services;

public record FiscalReceiptRequest(
    string InvoiceNumber,
    decimal Amount,
    string Currency,
    string PatientName,
    DateOnly Date,
    IReadOnlyList<(string Description, decimal Amount)> LineItems);

public record FiscalReceiptResult(
    bool Success,
    string? FiscalReceiptNumber,
    string? FiscalQrCode,
    string? Error);

/// <summary>
/// Abstraction over ZIMRA FDMS (Fiscal Device Management System).
/// The null implementation logs receipts without calling ZIMRA — replace with a real implementation once FDMS credentials are available.
/// </summary>
public interface IFiscalDevice
{
    Task<FiscalReceiptResult> IssueReceiptAsync(FiscalReceiptRequest request, CancellationToken ct = default);
}
