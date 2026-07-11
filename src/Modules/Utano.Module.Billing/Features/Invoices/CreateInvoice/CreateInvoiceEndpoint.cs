using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Billing.DatabaseMappings;
using Utano.Module.Billing.Domain.Entities;
using Utano.Module.Core.Services;

namespace Utano.Module.Billing.Features.Invoices.CreateInvoice;

[ApiController]
[Route("api/billing/invoices")]
[Authorize]
public class CreateInvoiceEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateInvoiceResponse), (int)HttpStatusCode.Created)]
    [EndpointSummary("Create a new invoice")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Post([FromBody] CreateInvoiceCommand cmd, CancellationToken ct)
    {
        var result = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(Post), new { id = result.Id }, result);
    }
}

public record CreateInvoiceCommand(
    Guid PatientId,
    string PatientName,
    Guid? DoctorId,
    string? DoctorName,
    Guid? VisitId,
    string? Currency,
    Guid? MedicalAidId,
    string? MedicalAidName,
    string? Notes) : IRequest<CreateInvoiceResponse>;

public record CreateInvoiceResponse(Guid Id, string InvoiceNumber);

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PatientName).NotEmpty().MaximumLength(200);
    }
}

public class CreateInvoiceHandler(BillingDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResponse>
{
    public async Task<CreateInvoiceResponse> Handle(CreateInvoiceCommand cmd, CancellationToken ct)
    {
        var invoiceNumber = await GenerateInvoiceNumberAsync(ct);
        var invoice = Invoice.Create(currentUser.PracticeId, invoiceNumber, cmd.PatientId,
            cmd.PatientName, cmd.DoctorId, cmd.DoctorName, cmd.VisitId,
            cmd.Currency ?? "USD", cmd.MedicalAidId, cmd.MedicalAidName, cmd.Notes);

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return new CreateInvoiceResponse(invoice.Id, invoice.InvoiceNumber);
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct)
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMM}-";
        var lastNumber = await db.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.PracticeId == currentUser.PracticeId && i.InvoiceNumber.StartsWith(prefix))
            .CountAsync(ct);
        return $"{prefix}{(lastNumber + 1):D4}";
    }
}