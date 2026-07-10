using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Patients.DatabaseMappings;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Infrastructure.Services;

public class PatientStatusChecker(PatientsDbContext context) : IPatientStatusChecker
{
    public async Task<bool> IsActiveAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await context.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == patientId && p.Status == PatientStatus.Active, cancellationToken);
    }
}
