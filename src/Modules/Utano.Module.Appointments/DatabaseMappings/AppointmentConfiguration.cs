using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Utano.Module.Appointments.Domain.Entities;

namespace Utano.Module.Appointments.DatabaseMappings;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", "appointments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PatientName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.DoctorName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Notes).HasMaxLength(1000);
        builder.Property(a => a.CancellationReason).HasMaxLength(500);

        builder.Property(a => a.Type).HasConversion<string>();
        builder.Property(a => a.Status).HasConversion<string>();

        builder.HasIndex(a => new { a.PracticeId, a.AppointmentDate });
        builder.HasIndex(a => new { a.PracticeId, a.PatientId });
        builder.HasIndex(a => new { a.PracticeId, a.DoctorId, a.AppointmentDate });
    }
}
