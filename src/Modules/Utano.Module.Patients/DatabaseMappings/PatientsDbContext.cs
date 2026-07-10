using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Patients.Domain.Entities;

namespace Utano.Module.Patients.DatabaseMappings;

public class PatientsDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public PatientsDbContext(
        DbContextOptions<PatientsDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<PatientContact> PatientContacts { get; set; }
    public DbSet<PatientAddress> PatientAddresses { get; set; }
    public DbSet<MedicalAid> MedicalAids { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>()
            .HasQueryFilter(p => p.PracticeId == _currentUserService.PracticeId);

        modelBuilder.Entity<MedicalAid>()
            .HasQueryFilter(m => m.PracticeId == _currentUserService.PracticeId);
    }
}