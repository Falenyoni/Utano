using FluentValidation;
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

namespace Utano.Module.Billing.Features.PaymentPlans.CreatePaymentPlan;

[ApiController]
[Route("api/billing/invoices/{invoiceId:guid}/payment-plan")]
[Authorize]
public class CreatePaymentPlanEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentPlanResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [EndpointSummary("Create a payment plan for an invoice")]
    [Tags("Billing Module")]
    public async Task<IActionResult> Post(Guid invoiceId, [FromBody] CreatePaymentPlanBody body, CancellationToken ct)
    {
        var result = await sender.Send(new CreatePaymentPlanCommand(invoiceId, body.InstallmentCount,
            body.Frequency, body.StartDate, body.Notes), ct);
        if (result is null) return NotFound();
        return CreatedAtAction(nameof(Post), result);
    }
}

public record CreatePaymentPlanBody(int InstallmentCount, string Frequency, DateOnly StartDate, string? Notes);

public record CreatePaymentPlanCommand(
    Guid InvoiceId, int InstallmentCount, string Frequency,
    DateOnly StartDate, string? Notes) : IRequest<CreatePaymentPlanResponse?>;

public record CreatePaymentPlanResponse(Guid PlanId, int InstallmentCount, decimal TotalAmount, decimal PerInstallment);

public class CreatePaymentPlanValidator : AbstractValidator<CreatePaymentPlanCommand>
{
    public CreatePaymentPlanValidator()
    {
        RuleFor(x => x.InstallmentCount).InclusiveBetween(2, 24);
        RuleFor(x => x.Frequency)
            .Must(f => Enum.TryParse<PaymentPlanFrequency>(f, true, out _))
            .WithMessage($"Frequency must be one of: {string.Join(", ", Enum.GetNames<PaymentPlanFrequency>())}");
    }
}

public class CreatePaymentPlanHandler(BillingDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreatePaymentPlanCommand, CreatePaymentPlanResponse?>
{
    public async Task<CreatePaymentPlanResponse?> Handle(CreatePaymentPlanCommand cmd, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == cmd.InvoiceId, ct);
        if (invoice is null) return null;

        var existing = await db.PaymentPlans.AnyAsync(pp => pp.InvoiceId == cmd.InvoiceId, ct);
        if (existing) throw new InvalidOperationException("A payment plan already exists for this invoice.");

        var frequency = Enum.Parse<PaymentPlanFrequency>(cmd.Frequency, ignoreCase: true);
        var plan = PaymentPlan.Create(currentUser.PracticeId, cmd.InvoiceId, invoice.BalanceDue,
            cmd.InstallmentCount, frequency, cmd.StartDate, cmd.Notes);

        db.PaymentPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        var perInstallment = plan.Installments.FirstOrDefault()?.Amount ?? 0;
        return new CreatePaymentPlanResponse(plan.Id, plan.InstallmentCount, plan.TotalAmount, perInstallment);
    }
}