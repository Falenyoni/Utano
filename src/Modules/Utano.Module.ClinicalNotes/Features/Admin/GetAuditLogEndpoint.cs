using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.ClinicalNotes.DatabaseMappings;

namespace Utano.Module.ClinicalNotes.Features.Admin;

[ApiController]
[Authorize]
[Route("api/admin")]
public class GetAuditLogEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("audit-log")]
    [ProducesResponseType(typeof(PagedAuditLog), (int)HttpStatusCode.OK)]
    [EndpointSummary("Get paginated audit log")]
    [Tags("Admin")]
    public async Task<IActionResult> Get(
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] string? action,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new GetAuditLogQuery(entityType, entityId, action, dateFrom, dateTo, page, pageSize), ct);
        return Ok(result);
    }
}

public record AuditLogRow(
    Guid Id,
    string UserName,
    string EntityType,
    string EntityId,
    string Action,
    string? Description,
    DateTimeOffset Timestamp);

public record PagedAuditLog(List<AuditLogRow> Data, int TotalCount, int Page, int PageSize, int TotalPages);

public record GetAuditLogQuery(
    string? EntityType, string? EntityId, string? Action,
    DateTimeOffset? DateFrom, DateTimeOffset? DateTo,
    int Page, int PageSize) : IRequest<PagedAuditLog>;

public class GetAuditLogHandler(ClinicalNotesDbContext db)
    : IRequestHandler<GetAuditLogQuery, PagedAuditLog>
{
    public async Task<PagedAuditLog> Handle(GetAuditLogQuery q, CancellationToken ct)
    {
        var query = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.EntityType))
            query = query.Where(a => a.EntityType == q.EntityType);
        if (!string.IsNullOrWhiteSpace(q.EntityId))
            query = query.Where(a => a.EntityId == q.EntityId);
        if (!string.IsNullOrWhiteSpace(q.Action))
            query = query.Where(a => a.Action == q.Action);
        if (q.DateFrom.HasValue)
            query = query.Where(a => a.Timestamp >= q.DateFrom.Value);
        if (q.DateTo.HasValue)
            query = query.Where(a => a.Timestamp <= q.DateTo.Value);

        var total = await query.CountAsync(ct);
        var data = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(a => new AuditLogRow(a.Id, a.UserName, a.EntityType, a.EntityId, a.Action, a.Description, a.Timestamp))
            .ToListAsync(ct);

        return new PagedAuditLog(data, total, q.Page, q.PageSize, (int)Math.Ceiling((double)total / q.PageSize));
    }
}