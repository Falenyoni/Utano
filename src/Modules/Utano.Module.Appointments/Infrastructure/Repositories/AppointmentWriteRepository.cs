using Microsoft.EntityFrameworkCore;
using Utano.Module.Appointments.DatabaseMappings;
using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Appointments.Domain.Interfaces;

namespace Utano.Module.Appointments.Infrastructure.Repositories;

public class AppointmentWriteRepository(AppointmentsDbContext context) : IAppointmentWriteRepository
{
    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await context.Appointments.AddAsync(appointment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        if (context.Entry(appointment).State == EntityState.Detached)
            context.Appointments.Update(appointment);
        await context.SaveChangesAsync(cancellationToken);
    }
}
