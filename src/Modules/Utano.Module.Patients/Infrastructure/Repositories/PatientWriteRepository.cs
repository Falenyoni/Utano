using Microsoft.EntityFrameworkCore;
using Utano.Module.Patients.DatabaseMappings;
using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Infrastructure.Repositories;

internal class PatientWriteRepository(PatientsDbContext context) : IPatientWriteRepository
{
    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await context.Patients.AddAsync(patient, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        context.Patients.Update(patient);
        await context.SaveChangesAsync(cancellationToken);
    }
}