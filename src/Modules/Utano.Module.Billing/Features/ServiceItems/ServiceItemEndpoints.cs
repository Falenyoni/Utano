using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Billing.DatabaseMappings;
using Utano.Module.Billing.Domain.Entities;
using Utano.Module.Billing.Domain.Enums;
using Utano.Module.Core.Services;

namespace Utano.Module.Billing.Features.ServiceItems;

[ApiController]
[Authorize]
[Route("api/settings/service-items")]
public class ServiceItemEndpoints(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceItemRow>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List all service items visible to this practice (global + practice-specific)")]
    [Tags("Billing Module")]
    public async Task<IActionResult> GetAll([FromQuery] string? category, CancellationToken ct)
        => Ok(await sender.Send(new GetServiceItemsQuery(category), ct));

    [HttpPost]
    [ProducesResponseType(typeof(ServiceItemRow), (int)HttpStatusCode.Created)]
    [EndpointSummary("Add a practice-specific service item to the price list")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Create([FromBody] UpsertServiceItemBody body, CancellationToken ct)
    {
        var result = await sender.Send(new CreateServiceItemCommand(body), ct);
        return CreatedAtAction(nameof(GetAll), new { }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ServiceItemRow), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Update a practice-specific service item")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertServiceItemBody body, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateServiceItemCommand(id, body), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Deactivate a service item")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var ok = await sender.Send(new ToggleServiceItemCommand(id, false), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Activate a service item")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var ok = await sender.Send(new ToggleServiceItemCommand(id, true), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record UpsertServiceItemBody(
    string Name,
    string Category,
    decimal DefaultPrice,
    string? NhrplCode = null,
    string? DefaultIcdCode = null,
    string? AppointmentTypeKey = null);

public record ServiceItemRow(
    Guid Id,
    Guid? PracticeId,
    string Name,
    string Category,
    decimal DefaultPrice,
    string? NhrplCode,
    string? DefaultIcdCode,
    string? AppointmentTypeKey,
    bool IsActive,
    bool IsGlobal);

// ─── Get ────────────────────────────────────────────────────────────────────

public record GetServiceItemsQuery(string? Category) : IRequest<List<ServiceItemRow>>;

public class GetServiceItemsHandler(BillingDbContext db)
    : IRequestHandler<GetServiceItemsQuery, List<ServiceItemRow>>
{
    public async Task<List<ServiceItemRow>> Handle(GetServiceItemsQuery q, CancellationToken ct)
    {
        var query = db.ServiceItems.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Category) &&
            Enum.TryParse<ServiceItemCategory>(q.Category, out var cat))
            query = query.Where(s => s.Category == cat);

        return await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .Select(s => new ServiceItemRow(
                s.Id, s.PracticeId, s.Name, s.Category.ToString(),
                s.DefaultPrice, s.NhrplCode, s.DefaultIcdCode,
                s.AppointmentTypeKey, s.IsActive, s.PracticeId == null))
            .ToListAsync(ct);
    }
}

// ─── Create ─────────────────────────────────────────────────────────────────

public record CreateServiceItemCommand(UpsertServiceItemBody Body) : IRequest<ServiceItemRow>;

public class CreateServiceItemHandler(BillingDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateServiceItemCommand, ServiceItemRow>
{
    public async Task<ServiceItemRow> Handle(CreateServiceItemCommand cmd, CancellationToken ct)
    {
        var category = Enum.TryParse<ServiceItemCategory>(cmd.Body.Category, out var cat)
            ? cat : ServiceItemCategory.Other;

        var item = ServiceItem.Create(
            currentUser.PracticeId,
            cmd.Body.Name,
            category,
            cmd.Body.NhrplCode,
            cmd.Body.DefaultIcdCode,
            cmd.Body.DefaultPrice,
            cmd.Body.AppointmentTypeKey);

        db.ServiceItems.Add(item);
        await db.SaveChangesAsync(ct);

        return ServiceItemMapping.ToRow(item);
    }
}

// ─── Update ─────────────────────────────────────────────────────────────────

public record UpdateServiceItemCommand(Guid Id, UpsertServiceItemBody Body) : IRequest<ServiceItemRow?>;

public class UpdateServiceItemHandler(BillingDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateServiceItemCommand, ServiceItemRow?>
{
    public async Task<ServiceItemRow?> Handle(UpdateServiceItemCommand cmd, CancellationToken ct)
    {
        var item = await db.ServiceItems
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, ct);
        if (item is null) return null;

        // Practice-specific items: only the owning practice can edit
        if (item.PracticeId.HasValue && item.PracticeId != currentUser.PracticeId) return null;

        item.Update(cmd.Body.Name, cmd.Body.NhrplCode, cmd.Body.DefaultIcdCode,
            cmd.Body.DefaultPrice, cmd.Body.AppointmentTypeKey);

        await db.SaveChangesAsync(ct);
        return ServiceItemMapping.ToRow(item);
    }
}

// ─── Toggle Active ───────────────────────────────────────────────────────────

public record ToggleServiceItemCommand(Guid Id, bool Activate) : IRequest<bool>;

public class ToggleServiceItemHandler(BillingDbContext db)
    : IRequestHandler<ToggleServiceItemCommand, bool>
{
    public async Task<bool> Handle(ToggleServiceItemCommand cmd, CancellationToken ct)
    {
        var item = await db.ServiceItems
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, ct);
        if (item is null) return false;

        if (cmd.Activate) item.Activate(); else item.Deactivate();
        await db.SaveChangesAsync(ct);
        return true;
    }
}

// ─── Shared ──────────────────────────────────────────────────────────────────

internal static class ServiceItemMapping
{
    internal static ServiceItemRow ToRow(ServiceItem s) => new(
        s.Id, s.PracticeId, s.Name, s.Category.ToString(),
        s.DefaultPrice, s.NhrplCode, s.DefaultIcdCode,
        s.AppointmentTypeKey, s.IsActive, s.PracticeId is null);
}
