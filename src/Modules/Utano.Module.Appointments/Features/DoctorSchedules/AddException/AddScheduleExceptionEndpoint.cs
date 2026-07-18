using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Features.DoctorSchedules.AddException;

public record AddScheduleExceptionCommand(
    Guid DoctorId,
    DateOnly Date,
    ScheduleExceptionType Type,
    string? StartTime,
    string? EndTime,
    string? Reason)
    : IRequest<Guid>;

public record AddScheduleExceptionBody(
    DateOnly Date,
    ScheduleExceptionType Type,
    string? StartTime = null,
    string? EndTime = null,
    string? Reason = null);

[ApiController]
[Route("api/doctors/{doctorId:guid}/schedule")]
[Authorize]
public class AddScheduleExceptionEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("exceptions")]
    [ProducesResponseType(typeof(Guid), 201)]
    [Tags("Doctor Schedules")]
    public async Task<IActionResult> AddException(
        [FromRoute] Guid doctorId,
        [FromBody] AddScheduleExceptionBody body,
        CancellationToken ct)
    {
        var id = await sender.Send(new AddScheduleExceptionCommand(
            doctorId, body.Date, body.Type, body.StartTime, body.EndTime, body.Reason), ct);
        return Created($"/api/doctors/{doctorId}/schedule/exceptions/{id}", id);
    }
}

public class AddScheduleExceptionHandler(
    IDoctorScheduleRepository scheduleRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<AddScheduleExceptionCommand, Guid>
{
    public async Task<Guid> Handle(AddScheduleExceptionCommand command, CancellationToken ct)
    {
        var existing = await scheduleRepo.GetExceptionForDateAsync(
            currentUser.PracticeId, command.DoctorId, command.Date, ct);
        if (existing is not null)
            throw new UtanoDomainException($"An exception already exists for {command.Date:yyyy-MM-dd}. Remove it first.");

        TimeOnly? startTime = null;
        TimeOnly? endTime = null;

        if (command.Type == ScheduleExceptionType.ModifiedHours)
        {
            if (!TimeOnly.TryParse(command.StartTime, out var start))
                throw new UtanoDomainException("Start time is required for ModifiedHours exceptions.");
            if (!TimeOnly.TryParse(command.EndTime, out var end))
                throw new UtanoDomainException("End time is required for ModifiedHours exceptions.");
            if (end <= start)
                throw new UtanoDomainException("End time must be after start time.");
            startTime = start;
            endTime = end;
        }

        var exception = DoctorScheduleException.Create(
            currentUser.PracticeId,
            command.DoctorId,
            command.Date,
            command.Type,
            startTime,
            endTime,
            command.Reason);

        await scheduleRepo.AddExceptionAsync(exception, ct);
        return exception.Id;
    }
}
