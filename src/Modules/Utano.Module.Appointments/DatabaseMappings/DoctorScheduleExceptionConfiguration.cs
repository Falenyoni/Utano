using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Appointments.Domain.Entities;

namespace Utano.Module.Appointments.DatabaseMappings;

public class DoctorScheduleExceptionConfiguration : IEntityTypeConfiguration<DoctorScheduleException>
{
    public void Configure(EntityTypeBuilder<DoctorScheduleException> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DoctorId).IsRequired();
        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.Type).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(500);

        builder.HasIndex(e => new { e.PracticeId, e.DoctorId, e.Date }).IsUnique();
    }
}
