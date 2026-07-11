namespace Utano.Module.Core.Services;

public record MedicalAidAuthRequest(
    string MedicalAidName,
    string MemberNumber,
    string DiagnosisCode,
    string ProcedureCode,
    decimal EstimatedAmount);

public record MedicalAidAuthResult(
    bool Approved,
    string? AuthorizationNumber,
    decimal? ApprovedAmount,
    string? Reason);

public record MedicalAidClaimRequest(
    string AuthorizationNumber,
    string InvoiceNumber,
    decimal ClaimAmount,
    string PatientName,
    string MemberNumber,
    DateOnly ServiceDate,
    IReadOnlyList<(string Code, string Description, decimal Amount)> LineItems);

public record MedicalAidClaimResult(
    bool Submitted,
    string? ClaimReference,
    string? Error);

/// <summary>
/// Abstraction for medical aid integrations (CIMAS, PSMAS, Old Mutual, etc.).
/// Start with manual claim workflow; wire a real implementation per medical aid as needed.
/// </summary>
public interface IMedicalAidGateway
{
    string ProviderName { get; }
    Task<MedicalAidAuthResult> RequestAuthorizationAsync(MedicalAidAuthRequest request, CancellationToken ct = default);
    Task<MedicalAidClaimResult> SubmitClaimAsync(MedicalAidClaimRequest request, CancellationToken ct = default);
}
