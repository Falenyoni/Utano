using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Appointments.Domain.Entities;

namespace Utano.Module.Appointments.DatabaseMappings;

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.DoctorId).IsRequired();
        builder.Property(s => s.DayOfWeek).IsRequired();
        builder.Property(s => s.StartTime).IsRequired();
        builder.Property(s => s.EndTime).IsRequired();
        builder.Property(s => s.SlotDurationMinutes).IsRequired();
        builder.Property(s => s.IsActive).IsRequired();

        builder.HasIndex(s => new { s.PracticeId, s.DoctorId, s.DayOfWeek }).IsUnique();
    }
}
