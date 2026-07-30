using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.ClinicalNotes.Features.Visits.CompleteVisit;

[ApiController]
[Route("api/visits")]
[Authorize]
public class CompleteVisitEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}/complete")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Mark visit as completed")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> CompleteVisit(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteVisitCommand(id), cancellationToken);
        return result ? NoContent() : NotFound();
    }
}

public record CompleteVisitCommand(Guid Id) : IRequest<bool>;

public class CompleteVisitHandler(
    Utano.Module.ClinicalNotes.Domain.Interfaces.IVisitReadRepository readRepository,
    Utano.Module.ClinicalNotes.Domain.Interfaces.IVisitWriteRepository writeRepository,
    Utano.Module.Core.Services.IAppointmentLinker appointmentLinker,
    Utano.Module.Core.Services.IBillingService billingService,
    Utano.Module.Core.Services.ICurrentUserService currentUser)
    : IRequestHandler<CompleteVisitCommand, bool>
{
    public async Task<bool> Handle(CompleteVisitCommand command, CancellationToken cancellationToken)
    {
        var visit = await readRepository.GetByIdAsync(command.Id, cancellationToken);
        if (visit is null) return false;
        visit.Complete();
        await writeRepository.UpdateAsync(visit, cancellationToken);
        if (visit.AppointmentId.HasValue)
            await appointmentLinker.MarkCompletedAsync(visit.AppointmentId.Value, cancellationToken);
        await billingService.CreateDraftInvoiceForVisitAsync(currentUser.PracticeId, visit.Id,
            visit.PatientId, visit.PatientName, visit.DoctorId, visit.DoctorName, cancellationToken);
        return true;
    }
}