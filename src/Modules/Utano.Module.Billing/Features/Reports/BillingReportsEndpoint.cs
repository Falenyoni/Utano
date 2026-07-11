using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Billing.DatabaseMappings;
using Utano.Module.Billing.Domain.Enums;

namespace Utano.Module.Billing.Features.Reports;

[ApiController]
[Route("api/billing/reports")]
[Authorize]
public class BillingReportsEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("revenue")]
    [ProducesResponseType(typeof(RevenueSummaryResponse), (int)HttpStatusCode.OK)]
    [EndpointSummary("Revenue summary for a date range")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Revenue(
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        CancellationToken ct)
    {
        var result = await sender.Send(new RevenueSummaryQuery(dateFrom, dateTo), ct);
        return Ok(result);
    }
}

public record RevenueSummaryQuery(string? DateFrom, string? DateTo) : IRequest<RevenueSummaryResponse>;

public record MonthlyRevenueRow(string Month, decimal Invoiced, decimal Collected);

public record RevenueSummaryResponse(
    decimal TotalInvoiced,
    decimal TotalCollected,
    decimal TotalOutstanding,
    int InvoiceCount,
    int PaidCount,
    int OutstandingCount,
    IReadOnlyList<MonthlyRevenueRow> ByMonth);

public class RevenueSummaryHandler(BillingDbContext db) : IRequestHandler<RevenueSummaryQuery, RevenueSummaryResponse>
{
    public async Task<RevenueSummaryResponse> Handle(RevenueSummaryQuery q, CancellationToken ct)
    {
        var query = db.Invoices.AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Void && i.Status != InvoiceStatus.Draft);

        if (DateOnly.TryParse(q.DateFrom, out var from))
            query = query.Where(i => i.InvoiceDate >= from);
        if (DateOnly.TryParse(q.DateTo, out var to))
            query = query.Where(i => i.InvoiceDate <= to);

        var invoices = await query
            .Select(i => new { i.InvoiceDate, i.TotalAmount, i.AmountPaid, i.Status })
            .ToListAsync(ct);

        var totalInvoiced = invoices.Sum(i => i.TotalAmount);
        var totalCollected = invoices.Sum(i => i.AmountPaid);
        var outstanding = invoices.Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Void)
            .Sum(i => i.TotalAmount - i.AmountPaid);

        var byMonth = invoices
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyRevenueRow(
                $"{g.Key.Year}-{g.Key.Month:D2}",
                g.Sum(i => i.TotalAmount),
                g.Sum(i => i.AmountPaid)))
            .ToList();

        return new RevenueSummaryResponse(
            totalInvoiced, totalCollected, outstanding,
            invoices.Count,
            invoices.Count(i => i.Status == InvoiceStatus.Paid),
            invoices.Count(i => i.Status != InvoiceStatus.Paid),
            byMonth);
    }
}