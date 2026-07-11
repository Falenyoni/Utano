using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Billing.DatabaseMappings;
using Utano.Module.Billing.Domain.Enums;
using Utano.Module.Core.Models;

namespace Utano.Module.Billing.Features.Invoices.GetInvoices;

[ApiController]
[Route("api/billing/invoices")]
[Authorize]
public class GetInvoicesEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InvoiceSummary>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List invoices")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Get(
        [FromQuery] string? patientName,
        [FromQuery] string? status,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetInvoicesQuery(patientName, status, dateFrom, dateTo, page, pageSize), ct);
        return Ok(result);
    }
}

public record GetInvoicesQuery(
    string? PatientName, string? Status,
    string? DateFrom, string? DateTo,
    int Page, int PageSize) : IRequest<PagedResult<InvoiceSummary>>;

public record InvoiceSummary(
    Guid Id, string InvoiceNumber, string PatientName, string? DoctorName,
    string Status, DateOnly InvoiceDate, DateOnly DueDate, string Currency,
    decimal TotalAmount, decimal AmountPaid, decimal BalanceDue,
    Guid? VisitId, DateTimeOffset CreatedAt);

public class GetInvoicesHandler(BillingDbContext db) : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceSummary>>
{
    public async Task<PagedResult<InvoiceSummary>> Handle(GetInvoicesQuery q, CancellationToken ct)
    {
        var query = db.Invoices.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.PatientName))
            query = query.Where(i => i.PatientName.ToLower().Contains(q.PatientName.ToLower()));

        if (Enum.TryParse<InvoiceStatus>(q.Status, true, out var status))
            query = query.Where(i => i.Status == status);

        if (DateOnly.TryParse(q.DateFrom, out var from))
            query = query.Where(i => i.InvoiceDate >= from);

        if (DateOnly.TryParse(q.DateTo, out var to))
            query = query.Where(i => i.InvoiceDate <= to);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(i => new InvoiceSummary(i.Id, i.InvoiceNumber, i.PatientName, i.DoctorName,
                i.Status.ToString(), i.InvoiceDate, i.DueDate, i.Currency,
                i.TotalAmount, i.AmountPaid, i.TotalAmount - i.AmountPaid, i.VisitId, i.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<InvoiceSummary> { Data = items, TotalCount = total, Page = q.Page, PageSize = q.PageSize };
    }
}