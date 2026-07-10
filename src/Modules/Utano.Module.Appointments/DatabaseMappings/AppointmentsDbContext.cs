using Microsoft.EntityFrameworkCore;
using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.DatabaseMappings;

public class AppointmentsDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public AppointmentsDbContext(
        DbContextOptions<AppointmentsDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Appointment> Appointments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppointmentsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Appointment>()
            .HasQueryFilter(a => a.PracticeId == _currentUserService.PracticeId);
    }
}
