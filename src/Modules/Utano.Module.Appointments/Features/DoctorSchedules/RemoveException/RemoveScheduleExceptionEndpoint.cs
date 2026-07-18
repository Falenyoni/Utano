using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Features.DoctorSchedules.RemoveException;

public record RemoveScheduleExceptionCommand(Guid DoctorId, Guid ExceptionId) : IRequest<Unit>;

[ApiController]
[Route("api/doctors/{doctorId:guid}/schedule")]
[Authorize]
public class RemoveScheduleExceptionEndpoint(ISender sender) : ControllerBase
{
    [HttpDelete("exceptions/{exceptionId:guid}")]
    [ProducesResponseType(204)]
    [Tags("Doctor Schedules")]
    public async Task<IActionResult> RemoveException(
        [FromRoute] Guid doctorId,
        [FromRoute] Guid exceptionId,
        CancellationToken ct)
    {
        await sender.Send(new RemoveScheduleExceptionCommand(doctorId, exceptionId), ct);
        return NoContent();
    }
}

public class RemoveScheduleExceptionHandler(
    IDoctorScheduleRepository scheduleRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<RemoveScheduleExceptionCommand, Unit>
{
    public async Task<Unit> Handle(RemoveScheduleExceptionCommand command, CancellationToken ct)
    {
        await scheduleRepo.RemoveExceptionAsync(command.ExceptionId, currentUser.PracticeId, ct);
        return Unit.Value;
    }
}
