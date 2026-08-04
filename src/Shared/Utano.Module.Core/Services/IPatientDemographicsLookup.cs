namespace Utano.Module.Core.Services;

public record PatientDemographics(string? Gender, DateOnly? DateOfBirth);

public interface IPatientDemographicsLookup
{
    /// <summary>Batch-looks up Gender/DateOfBirth for the given patient IDs. Patients missing
    /// from the result simply have no matching record (e.g. deleted or invalid ID).</summary>
    Task<IReadOnlyDictionary<Guid, PatientDemographics>> GetDemographicsAsync(
        IEnumerable<Guid> patientIds, CancellationToken cancellationToken = default);
}
