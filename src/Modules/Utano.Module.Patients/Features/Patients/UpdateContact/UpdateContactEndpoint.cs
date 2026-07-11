using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Patients.DatabaseMappings;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Features.Patients.UpdateContact;

[ApiController]
[Authorize]
[Route("api/patients/{patientId:guid}/contacts")]
public class UpdateContactEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{contactId:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Update a patient contact")]
    [Tags("Patients Module")]
    public async Task<IActionResult> Update(Guid patientId, Guid contactId,
        [FromBody] UpdateContactBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new UpdateContactCommand(patientId, contactId,
            body.Type, body.PhoneNumber, body.Email), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record UpdateContactBody(string Type, string PhoneNumber, string? Email);

public record UpdateContactCommand(Guid PatientId, Guid ContactId,
    string Type, string PhoneNumber, string? Email) : IRequest<bool>;

public class UpdateContactHandler(PatientsDbContext db) : IRequestHandler<UpdateContactCommand, bool>
{
    public async Task<bool> Handle(UpdateContactCommand cmd, CancellationToken ct)
    {
        // Load contact directly to avoid EF Core navigation tracking issues with private readonly collections
        var contact = await db.PatientContacts
            .FirstOrDefaultAsync(c => c.Id == cmd.ContactId && c.PatientId == cmd.PatientId, ct);
        if (contact is null) return false;

        var type = Enum.Parse<ContactType>(cmd.Type, ignoreCase: true);
        contact.Update(type, cmd.PhoneNumber, cmd.Email);
        await db.SaveChangesAsync(ct);
        return true;
    }
}