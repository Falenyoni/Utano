namespace Utano.Module.Core.Services;

public interface IPatientContactLookup
{
    /// <summary>The patient's primary contact's email if it has one, else the first contact with
    /// an email on file, else null (no email on record for this patient).</summary>
    Task<string?> GetEmailAsync(Guid patientId, CancellationToken cancellationToken = default);
}
