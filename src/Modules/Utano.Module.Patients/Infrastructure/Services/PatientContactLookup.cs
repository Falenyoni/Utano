using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Patients.DatabaseMappings;

namespace Utano.Module.Patients.Infrastructure.Services;

public class PatientContactLookup(PatientsDbContext context) : IPatientContactLookup
{
    public async Task<string?> GetEmailAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var contacts = await context.PatientContacts
            .AsNoTracking()
            .Where(c => c.PatientId == patientId && c.Email != null)
            .OrderByDescending(c => c.IsPrimary)
            .ToListAsync(cancellationToken);

        return contacts.FirstOrDefault()?.Email;
    }
}
