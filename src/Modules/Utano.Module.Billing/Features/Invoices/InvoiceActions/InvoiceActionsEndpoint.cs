using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using Utano.Module.Billing.DatabaseMappings;
using Utano.Module.Billing.Domain.Enums;
using Utano.Module.Core.Services;

namespace Utano.Module.Billing.Features.Invoices.InvoiceActions;

[ApiController]
[Route("api/billing/invoices/{id:guid}")]
[Authorize]
public class InvoiceActionsEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("issue")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Issue invoice (Draft → Issued)")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Issue(Guid id, CancellationToken ct)
    {
        var ok = await sender.Send(new IssueInvoiceCommand(id), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPut("void")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Void an invoice")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Void(Guid id, CancellationToken ct)
    {
        var ok = await sender.Send(new VoidInvoiceCommand(id), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPut("submit-claim")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Submit a medical aid claim for this invoice")]
    [Tags("Billing Module")]
    public async Task<IActionResult> SubmitClaim(Guid id, [FromBody] SubmitClaimBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new SubmitClaimCommand(id, body.ClaimAmount), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPut("claim-status")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Update claim status (Approved / Rejected)")]
    [Tags("Billing Module")]
    public async Task<IActionResult> UpdateClaimStatus(Guid id, [FromBody] UpdateClaimStatusBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new UpdateClaimStatusCommand(id, body.Status), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record IssueInvoiceCommand(Guid Id) : IRequest<bool>;

public class IssueInvoiceHandler(BillingDbContext db, IAuditService auditService, ILogger<IssueInvoiceHandler> logger)
    : IRequestHandler<IssueInvoiceCommand, bool>
{
    public async Task<bool> Handle(IssueInvoiceCommand cmd, CancellationToken ct)
    {
        var invoice = await db.Invoices.Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == cmd.Id, ct);
        if (invoice is null) return false;
        invoice.Issue();
        await db.SaveChangesAsync(ct);

        try
        {
            await auditService.LogAsync("Invoice", invoice.Id.ToString(), "Issued",
                $"Invoice {invoice.InvoiceNumber} · Patient: {invoice.PatientName} · Total: {invoice.TotalAmount:C}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log invoice issue for {InvoiceId}", invoice.Id);
        }

        return true;
    }
}

public record VoidInvoiceCommand(Guid Id) : IRequest<bool>;

public class VoidInvoiceHandler(BillingDbContext db, IAuditService auditService, ILogger<VoidInvoiceHandler> logger)
    : IRequestHandler<VoidInvoiceCommand, bool>
{
    public async Task<bool> Handle(VoidInvoiceCommand cmd, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == cmd.Id, ct);
        if (invoice is null) return false;
        invoice.Void();
        await db.SaveChangesAsync(ct);

        try
        {
            await auditService.LogAsync("Invoice", invoice.Id.ToString(), "Voided",
                $"Invoice {invoice.InvoiceNumber} · Patient: {invoice.PatientName} · Total: {invoice.TotalAmount:C}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log invoice void for {InvoiceId}", invoice.Id);
        }

        return true;
    }
}

public record SubmitClaimBody(decimal ClaimAmount);
public record SubmitClaimCommand(Guid Id, decimal ClaimAmount) : IRequest<bool>;

public class SubmitClaimHandler(BillingDbContext db, IAuditService auditService, ILogger<SubmitClaimHandler> logger)
    : IRequestHandler<SubmitClaimCommand, bool>
{
    public async Task<bool> Handle(SubmitClaimCommand cmd, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == cmd.Id, ct);
        if (invoice is null || invoice.MedicalAidId is null) return false;
        invoice.SetMedAidClaim(cmd.ClaimAmount, MedAidClaimStatus.Pending);
        await db.SaveChangesAsync(ct);

        try
        {
            await auditService.LogAsync("Invoice", invoice.Id.ToString(), "ClaimSubmitted",
                $"Invoice {invoice.InvoiceNumber} · Patient: {invoice.PatientName} · Claim: {cmd.ClaimAmount:C}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log claim submission for {InvoiceId}", invoice.Id);
        }

        return true;
    }
}

public record UpdateClaimStatusBody(string Status);
public record UpdateClaimStatusCommand(Guid Id, string Status) : IRequest<bool>;

public class UpdateClaimStatusHandler(BillingDbContext db, IAuditService auditService, ILogger<UpdateClaimStatusHandler> logger)
    : IRequestHandler<UpdateClaimStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateClaimStatusCommand cmd, CancellationToken ct)
    {
        if (!Enum.TryParse<MedAidClaimStatus>(cmd.Status, ignoreCase: true, out var status)) return false;
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == cmd.Id, ct);
        if (invoice is null) return false;
        invoice.SetMedAidClaim(invoice.MedAidClaimAmount, status);
        await db.SaveChangesAsync(ct);

        try
        {
            await auditService.LogAsync("Invoice", invoice.Id.ToString(), "ClaimStatusUpdated",
                $"Invoice {invoice.InvoiceNumber} · Patient: {invoice.PatientName} · Status: {status}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log claim status update for {InvoiceId}", invoice.Id);
        }

        return true;
    }
}