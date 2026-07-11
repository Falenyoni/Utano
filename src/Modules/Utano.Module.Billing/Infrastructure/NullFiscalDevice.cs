using Microsoft.Extensions.Logging;
using Utano.Module.Core.Services;

namespace Utano.Module.Billing.Infrastructure;

/// <summary>
/// No-op ZIMRA FDMS implementation. Replace with real FDMS client once credentials are available.
/// Logs receipt details so nothing is silently dropped.
/// </summary>
public class NullFiscalDevice(ILogger<NullFiscalDevice> logger) : IFiscalDevice
{
    public Task<FiscalReceiptResult> IssueReceiptAsync(FiscalReceiptRequest request, CancellationToken ct = default)
    {
        logger.LogInformation(
            "FiscalDevice(stub): invoice={Invoice} amount={Amount} {Currency} patient={Patient}",
            request.InvoiceNumber, request.Amount, request.Currency, request.PatientName);

        return Task.FromResult(new FiscalReceiptResult(true, null, null, null));
    }
}
