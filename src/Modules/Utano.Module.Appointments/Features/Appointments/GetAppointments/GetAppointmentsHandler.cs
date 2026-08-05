using MediatR;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Models;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Features.Appointments.GetAppointments;

public class GetAppointmentsHandler(
    IAppointmentReadRepository readRepository,
    IVisitLookup visitLookup)
    : IRequestHandler<GetAppointmentsQuery, PagedResult<AppointmentSummaryResponse>>
{
    public async Task<PagedResult<AppointmentSummaryResponse>> Handle(
        GetAppointmentsQuery query, CancellationToken cancellationToken)
    {
        var paged = await readRepository.GetPagedAsync(
            query.Date,
            query.PatientId,
            query.DoctorId,
            query.Status,
            query.OnlyOverdue,
            query.Page,
            query.PageSize,
            cancellationToken);

        var visitIds = await visitLookup.GetVisitIdsForAppointmentsAsync(
            paged.Data.Select(a => a.Id), cancellationToken);

        return new PagedResult<AppointmentSummaryResponse>
        {
            Data = paged.Data.Select(a => new AppointmentSummaryResponse(
                a.Id,
                a.PatientId,
                a.PatientName,
                a.DoctorId,
                a.DoctorName,
                a.AppointmentDate,
                a.StartTime,
                a.EndTime,
                a.Type.ToString(),
                a.Status.ToString(),
                a.Notes,
                a.CreatedAt,
                visitIds.TryGetValue(a.Id, out var visitId) ? visitId : null,
                a.IsOverdue)),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }
}
