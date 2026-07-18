using FluentValidation;
using Utano.Module.Appointments.Domain.Enums;

namespace Utano.Module.Appointments.Features.Appointments.BookAppointment;

public class BookAppointmentValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("Patient is required.");
        RuleFor(x => x.PatientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DoctorId).NotEmpty().WithMessage("Doctor is required.");
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AppointmentDate)
            .NotEmpty()
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Appointment date cannot be in the past.");
        RuleFor(x => x.StartTime).NotEmpty();
        RuleFor(x => x)
            .Must(x =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                if (x.AppointmentDate != today) return true;
                return x.StartTime > TimeOnly.FromDateTime(DateTime.UtcNow);
            })
            .WithMessage("Cannot book an appointment at a time that has already passed.")
            .When(x => x.AppointmentDate != default && x.StartTime != default);
        RuleFor(x => x.EndTime).NotEmpty().GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x.Type).NotEmpty()
            .Must(t => Enum.TryParse<AppointmentType>(t, true, out _))
            .WithMessage("Invalid appointment type.");
    }
}
