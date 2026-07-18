using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Features.DoctorSchedules.GetSchedule;

public record DayScheduleDto(
    Guid Id,
    int DayOfWeek,
    string DayName,
    string StartTime,
    string EndTime,
    int SlotDurationMinutes,
    bool IsActive);

public record ScheduleExceptionDto(
    Guid Id,
    string Date,
    string Type,
    string? StartTime,
    string? EndTime,
    string? Reason);

public record DoctorScheduleResponse(
    IReadOnlyList<DayScheduleDto> Schedule,
    IReadOnlyList<ScheduleExceptionDto> Exceptions);

public record GetDoctorScheduleQuery(Guid DoctorId) : IRequest<DoctorScheduleResponse>;

[ApiController]
[Route("api/doctors/{doctorId:guid}")]
[Authorize]
public class GetDoctorScheduleEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("schedule")]
    [ProducesResponseType(typeof(DoctorScheduleResponse), 200)]
    [Tags("Doctor Schedules")]
    public async Task<IActionResult> GetSchedule([FromRoute] Guid doctorId, CancellationToken ct)
        => Ok(await sender.Send(new GetDoctorScheduleQuery(doctorId), ct));
}

public class GetDoctorScheduleHandler(
    IDoctorScheduleRepository scheduleRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<GetDoctorScheduleQuery, DoctorScheduleResponse>
{
    public async Task<DoctorScheduleResponse> Handle(GetDoctorScheduleQuery query, CancellationToken ct)
    {
        var schedules = await scheduleRepo.GetScheduleAsync(currentUser.PracticeId, query.DoctorId, ct);
        var exceptions = await scheduleRepo.GetExceptionsAsync(currentUser.PracticeId, query.DoctorId, ct);

        var scheduleDtos = schedules.Select(s => new DayScheduleDto(
            s.Id,
            (int)s.DayOfWeek,
            s.DayOfWeek.ToString(),
            s.StartTime.ToString("HH:mm"),
            s.EndTime.ToString("HH:mm"),
            s.SlotDurationMinutes,
            s.IsActive)).ToList();

        var exceptionDtos = exceptions.Select(e => new ScheduleExceptionDto(
            e.Id,
            e.Date.ToString("yyyy-MM-dd"),
            e.Type.ToString(),
            e.StartTime?.ToString("HH:mm"),
            e.EndTime?.ToString("HH:mm"),
            e.Reason)).ToList();

        return new DoctorScheduleResponse(scheduleDtos, exceptionDtos);
    }
}
