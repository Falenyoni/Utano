using Microsoft.EntityFrameworkCore;
using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.DatabaseMappings;

public class ClinicalNotesDbContext(
    DbContextOptions<ClinicalNotesDbContext> options,
    ICurrentUserService currentUserService) : DbContext(options)
{
    public DbSet<Visit> Visits { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("clinical");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicalNotesDbContext).Assembly);

        modelBuilder.Entity<Visit>()
            .HasQueryFilter(v => v.PracticeId == currentUserService.PracticeId);

        modelBuilder.Entity<Prescription>()
            .HasQueryFilter(p => p.PracticeId == currentUserService.PracticeId);
    }
}
