using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Utano.Module.Appointments.Domain.Entities;
using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Features.Appointments.BulkImport;

public record BulkImportRow(
    string PatientName,
    string DoctorName,
    DateOnly AppointmentDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Type,
    string? Notes);

public record BulkImportAppointmentsCommand(List<BulkImportRow> Rows) : IRequest<BulkImportResult>;

public record BulkImportResult(int Imported, int Failed, List<string> Errors);

[ApiController]
[Route("api/appointments")]
[Authorize]
public class BulkImportAppointmentsEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("import")]
    [ProducesResponseType(typeof(BulkImportResult), 200)]
    [EndpointSummary("Bulk import appointments from a CSV-parsed payload")]
    [Tags("Appointments Module")]
    public async Task<IActionResult> Import(
        [FromBody] BulkImportAppointmentsCommand command,
        CancellationToken ct)
        => Ok(await sender.Send(command, ct));
}

public class BulkImportAppointmentsHandler(
    IAppointmentWriteRepository repo,
    ICurrentUserService currentUser)
    : IRequestHandler<BulkImportAppointmentsCommand, BulkImportResult>
{
    public async Task<BulkImportResult> Handle(BulkImportAppointmentsCommand cmd, CancellationToken ct)
    {
        var errors = new List<string>();
        int imported = 0;

        for (int i = 0; i < cmd.Rows.Count; i++)
        {
            var row = cmd.Rows[i];
            try
            {
                if (!Enum.TryParse<AppointmentType>(row.Type, ignoreCase: true, out var type))
                    throw new InvalidOperationException($"Unknown type '{row.Type}'");

                var appointment = Appointment.Book(
                    currentUser.PracticeId,
                    Guid.NewGuid(), row.PatientName,
                    Guid.NewGuid(), row.DoctorName,
                    row.AppointmentDate,
                    row.StartTime,
                    row.EndTime,
                    type,
                    row.Notes);

                await repo.AddAsync(appointment, ct);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"Row {i + 1} ({row.PatientName}): {ex.Message}");
            }
        }

        return new BulkImportResult(imported, errors.Count, errors);
    }
}