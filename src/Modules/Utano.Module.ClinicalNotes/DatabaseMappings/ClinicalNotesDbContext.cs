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
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<VisitProcedure> VisitProcedures { get; set; }
    public DbSet<VisitDiagnosis> VisitDiagnoses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("clinical");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicalNotesDbContext).Assembly);

        modelBuilder.Entity<Visit>()
            .HasQueryFilter(v => v.PracticeId == currentUserService.PracticeId);

        modelBuilder.Entity<Prescription>()
            .HasQueryFilter(p => p.PracticeId == currentUserService.PracticeId);

        modelBuilder.Entity<VisitProcedure>()
            .HasQueryFilter(p => p.PracticeId == currentUserService.PracticeId);

        modelBuilder.Entity<VisitDiagnosis>()
            .HasQueryFilter(d => d.PracticeId == currentUserService.PracticeId);

        // AuditLog already had a PracticeId column and practice-scoped indexes, but was missing
        // from this list - GetAuditLogHandler queried db.AuditLogs directly with no filter of its
        // own, so every practice could see every other practice's audit trail. Writes were always
        // correct (AuditService.LogAsync sets PracticeId properly); this was purely a read-side gap.
        modelBuilder.Entity<AuditLog>()
            .HasQueryFilter(a => a.PracticeId == currentUserService.PracticeId);
    }
}
