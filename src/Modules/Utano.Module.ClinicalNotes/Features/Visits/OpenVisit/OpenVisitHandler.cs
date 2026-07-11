using MediatR;
using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.ClinicalNotes.Domain.Interfaces;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Features.Visits.OpenVisit;

public class OpenVisitHandler(
    IVisitWriteRepository writeRepository,
    ICurrentUserService currentUserService,
    IAppointmentLinker appointmentLinker,
    IBillingService billingService)
    : IRequestHandler<OpenVisitCommand, OpenVisitResponse>
{
    public async Task<OpenVisitResponse> Handle(OpenVisitCommand command, CancellationToken cancellationToken)
    {
        var visit = Visit.Open(
            currentUserService.PracticeId,
            command.PatientId,
            command.PatientName,
            command.DoctorId,
            command.DoctorName,
            command.VisitDate,
            command.AppointmentId,
            command.Department,
            command.PatientGender,
            command.PatientDateOfBirth);

        await writeRepository.AddAsync(visit, cancellationToken);

        if (command.AppointmentId.HasValue)
        {
            await appointmentLinker.MarkInProgressAsync(command.AppointmentId.Value, cancellationToken);

            var typeKey = await appointmentLinker.GetAppointmentTypeKeyAsync(command.AppointmentId.Value, cancellationToken);
            if (typeKey is not null)
            {
                await billingService.TryAddAppointmentConsultationFeeAsync(
                    currentUserService.PracticeId,
                    visit.Id,
                    visit.PatientId,
                    visit.PatientName,
                    visit.DoctorId,
                    visit.DoctorName,
                    typeKey,
                    cancellationToken);
            }
        }

        return new OpenVisitResponse(visit.Id, visit.PatientId, visit.PatientName, visit.DoctorId, visit.DoctorName, visit.VisitDate, visit.Department, visit.Status.ToString(), visit.CreatedAt);
    }
}
