using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.ClinicalNotes.Domain.Enums;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Features.Dispensary;

[ApiController]
[Authorize]
[Route("api/clinical")]
public class GetDispensaryEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("dispensary")]
    [ProducesResponseType(typeof(List<DispensaryRow>), (int)HttpStatusCode.OK)]
    [EndpointSummary("Pending prescriptions queue — includes current stock levels so the UI can auto-determine dispense outcome")]
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
    Guid StockItemId,
    string StockItemName,
    decimal QuantityOnHand,
    string Unit,
    DateTimeOffset CreatedAt);

public record GetDispensaryQuery : IRequest<List<DispensaryRow>>;

public class GetDispensaryHandler(ClinicalNotesDbContext db, ICurrentUserService currentUser, IInventoryService inventoryService)
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
            .Select(x => new
            {
                x.p.Id,
                x.p.VisitId,
                x.p.PatientName,
                VisitDate = x.v.VisitDate.ToString(),
                x.v.DoctorName,
                x.p.Description,
                x.p.Quantity,
                x.p.DosageInstructions,
                x.p.StockItemId,
                x.p.StockItemName,
                x.p.CreatedAt
            })
            .ToListAsync(ct);

        var result = new List<DispensaryRow>(rows.Count);
        foreach (var row in rows)
        {
            var stock = await inventoryService.GetStockItemAsync(currentUser.PracticeId, row.StockItemId, ct);
            result.Add(new DispensaryRow(
                row.Id,
                row.VisitId,
                row.PatientName,
                row.VisitDate,
                row.DoctorName,
                row.Description,
                row.Quantity,
                row.DosageInstructions,
                row.StockItemId,
                row.StockItemName,
                stock?.QuantityOnHand ?? 0,
                stock?.Unit ?? "",
                row.CreatedAt));
        }

        return result;
    }
}
