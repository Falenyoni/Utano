using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Patients.DatabaseMappings;

namespace Utano.Module.Patients.Infrastructure.Services;

public class PatientDemographicsLookup(PatientsDbContext context) : IPatientDemographicsLookup
{
    public async Task<IReadOnlyDictionary<Guid, PatientDemographics>> GetDemographicsAsync(
        IEnumerable<Guid> patientIds, CancellationToken cancellationToken = default)
    {
        var ids = patientIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, PatientDemographics>();

        return await context.Patients
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(
                p => p.Id,
                p => new PatientDemographics(p.Gender.ToString(), p.DateOfBirth),
                cancellationToken);
    }
}
