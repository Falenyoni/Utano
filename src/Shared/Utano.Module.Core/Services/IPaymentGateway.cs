namespace Utano.Module.Core.Services;

public record InitiatePaymentRequest(
    string Reference,
    decimal Amount,
    string Currency,
    string Description,
    string ReturnUrl,
    string ResultUrl);

public record InitiatePaymentResult(
    bool Success,
    string? RedirectUrl,
    string? GatewayReference,
    string? Error);

public record PaymentStatusResult(
    string Status,
    decimal? AmountPaid,
    string? GatewayReference,
    string? Error);

public interface IPaymentGateway
{
    string ProviderName { get; }
    Task<InitiatePaymentResult> InitiateAsync(InitiatePaymentRequest request, CancellationToken ct = default);
    Task<PaymentStatusResult> GetStatusAsync(string gatewayReference, CancellationToken ct = default);
}
