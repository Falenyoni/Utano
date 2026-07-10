using FluentValidation;
using MediatR;
using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Features.Appointments.BookAppointment;

public class BookAppointmentHandler(
    IAppointmentWriteRepository writeRepository,
    ICurrentUserService currentUserService,
    IPatientStatusChecker patientStatusChecker,
    IValidator<BookAppointmentCommand> validator)
    : IRequestHandler<BookAppointmentCommand, BookAppointmentResponse>
{
    public async Task<BookAppointmentResponse> Handle(
        BookAppointmentCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var isActive = await patientStatusChecker.IsActiveAsync(command.PatientId, cancellationToken);
        if (!isActive)
            throw new UtanoDomainException("Cannot book an appointment for an inactive patient.");

        var type = Enum.Parse<AppointmentType>(command.Type, ignoreCase: true);

        var appointment = Appointment.Book(
            currentUserService.PracticeId,
            command.PatientId,
            command.PatientName,
            command.DoctorId,
            command.DoctorName,
            command.AppointmentDate,
            command.StartTime,
            command.EndTime,
            type,
            command.Notes);

        await writeRepository.AddAsync(appointment, cancellationToken);

        return new BookAppointmentResponse(
            appointment.Id,
            appointment.PatientId,
            appointment.PatientName,
            appointment.DoctorId,
            appointment.DoctorName,
            appointment.AppointmentDate,
            appointment.StartTime,
            appointment.EndTime,
            appointment.Type.ToString(),
            appointment.Status.ToString(),
            appointment.Notes,
            appointment.CreatedAt);
    }
}
