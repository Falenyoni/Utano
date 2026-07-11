using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.ClinicalNotes.Domain.Enums;

namespace Utano.Module.ClinicalNotes.Features.Dispensary;

[ApiController]
[Authorize]
[Route("api/clinical")]
public class GetDispensaryEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("dispensary")]
    [ProducesResponseType(typeof(List<DispensaryRow>), (int)HttpStatusCode.OK)]
    [EndpointSummary("Get pending prescriptions queue for the dispensary")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetDispensaryQuery(), ct);
        return Ok(result);
    }
}

public record DispensaryRow(
    Guid PrescriptionId,
    Guid VisitId,
    string PatientName,
    string VisitDate,
    string DoctorName,
    string Description,
    decimal Quantity,
    string? DosageInstructions,
    string DispensingType,
    Guid? StockItemId,
    string? StockItemName,
    DateTimeOffset CreatedAt);

public record GetDispensaryQuery : IRequest<List<DispensaryRow>>;

public class GetDispensaryHandler(ClinicalNotesDbContext db)
    : IRequestHandler<GetDispensaryQuery, List<DispensaryRow>>
{
    public async Task<List<DispensaryRow>> Handle(GetDispensaryQuery q, CancellationToken ct)
    {
        var rows = await db.Prescriptions
            .Where(p => p.Status == PrescriptionStatus.Pending)
            .Join(db.Visits.IgnoreQueryFilters(),
                p => p.VisitId,
                v => v.Id,
                (p, v) => new { p, v })
            .OrderBy(x => x.p.CreatedAt)
            .Select(x => new DispensaryRow(
                x.p.Id,
                x.p.VisitId,
                x.p.PatientName,
                x.v.VisitDate.ToString(),
                x.v.DoctorName,
                x.p.Description,
                x.p.Quantity,
                x.p.DosageInstructions,
                x.p.DispensingType.ToString(),
                x.p.StockItemId,
                x.p.StockItemName,
                x.p.CreatedAt))
            .ToListAsync(ct);

        return rows;
    }
}