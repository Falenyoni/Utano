using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Features.DoctorSchedules.GetAvailableSlots;

public record AvailableSlotDto(string StartTime, string EndTime, bool IsAvailable);

public record GetAvailableSlotsQuery(Guid DoctorId, DateOnly Date) : IRequest<IReadOnlyList<AvailableSlotDto>>;

[ApiController]
[Route("api/doctors/{doctorId:guid}")]
[Authorize]
public class GetAvailableSlotsEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("available-slots")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableSlotDto>), 200)]
    [Tags("Doctor Schedules")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromRoute] Guid doctorId,
        [FromQuery] DateOnly date,
        CancellationToken ct)
        => Ok(await sender.Send(new GetAvailableSlotsQuery(doctorId, date), ct));
}

public class GetAvailableSlotsHandler(
    IDoctorScheduleRepository scheduleRepo,
    IAppointmentReadRepository appointmentRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<GetAvailableSlotsQuery, IReadOnlyList<AvailableSlotDto>>
{
    public async Task<IReadOnlyList<AvailableSlotDto>> Handle(GetAvailableSlotsQuery query, CancellationToken ct)
    {
        var practiceId = currentUser.PracticeId;

        var schedule = await scheduleRepo.GetScheduleForDayAsync(practiceId, query.DoctorId, query.Date.DayOfWeek, ct);
        if (schedule is null || !schedule.IsActive)
            return [];

        var exception = await scheduleRepo.GetExceptionForDateAsync(practiceId, query.DoctorId, query.Date, ct);
        if (exception?.Type == ScheduleExceptionType.Unavailable)
            return [];

        var slotStart = exception?.Type == ScheduleExceptionType.ModifiedHours && exception.StartTime.HasValue
            ? exception.StartTime.Value
            : schedule.StartTime;
        var slotEnd = exception?.Type == ScheduleExceptionType.ModifiedHours && exception.EndTime.HasValue
            ? exception.EndTime.Value
            : schedule.EndTime;

        var bookedAppointments = await appointmentRepo.GetByDoctorDateAsync(practiceId, query.DoctorId, query.Date, ct);

        var slots = new List<AvailableSlotDto>();
        var current = slotStart;
        while (current.AddMinutes(schedule.SlotDurationMinutes) <= slotEnd)
        {
            var next = current.AddMinutes(schedule.SlotDurationMinutes);
            var isBooked = bookedAppointments.Any(a => a.StartTime < next && a.EndTime > current);
            slots.Add(new AvailableSlotDto(
                current.ToString("HH:mm"),
                next.ToString("HH:mm"),
                !isBooked));
            current = next;
        }

        return slots;
    }
}
