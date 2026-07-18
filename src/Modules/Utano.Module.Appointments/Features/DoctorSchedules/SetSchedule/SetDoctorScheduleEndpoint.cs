using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Features.DoctorSchedules.SetSchedule;

public record DayScheduleInput(
    int DayOfWeek,
    string StartTime,
    string EndTime,
    int SlotDurationMinutes = 30,
    bool IsActive = true);

public record SetDoctorScheduleCommand(Guid DoctorId, IReadOnlyList<DayScheduleInput> Days)
    : IRequest<Unit>;

[ApiController]
[Route("api/doctors/{doctorId:guid}")]
[Authorize]
public class SetDoctorScheduleEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("schedule")]
    [ProducesResponseType(204)]
    [Tags("Doctor Schedules")]
    public async Task<IActionResult> SetSchedule(
        [FromRoute] Guid doctorId,
        [FromBody] SetDoctorScheduleBody body,
        CancellationToken ct)
    {
        await sender.Send(new SetDoctorScheduleCommand(doctorId, body.Days), ct);
        return NoContent();
    }
}

public record SetDoctorScheduleBody(IReadOnlyList<DayScheduleInput> Days);

public class SetDoctorScheduleHandler(
    IDoctorScheduleRepository scheduleRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<SetDoctorScheduleCommand, Unit>
{
    public async Task<Unit> Handle(SetDoctorScheduleCommand command, CancellationToken ct)
    {
        var schedules = command.Days.Select(d =>
        {
            if (!TimeOnly.TryParse(d.StartTime, out var start))
                throw new UtanoDomainException($"Invalid start time '{d.StartTime}'.");
            if (!TimeOnly.TryParse(d.EndTime, out var end))
                throw new UtanoDomainException($"Invalid end time '{d.EndTime}'.");
            if (end <= start)
                throw new UtanoDomainException("End time must be after start time.");
            if (d.SlotDurationMinutes < 5 || d.SlotDurationMinutes > 120)
                throw new UtanoDomainException("Slot duration must be between 5 and 120 minutes.");

            return DoctorSchedule.Create(
                currentUser.PracticeId,
                command.DoctorId,
                (DayOfWeek)d.DayOfWeek,
                start,
                end,
                d.SlotDurationMinutes,
                d.IsActive);
        }).ToList();

        await scheduleRepo.ReplaceScheduleAsync(currentUser.PracticeId, command.DoctorId, schedules, ct);
        return Unit.Value;
    }
}
