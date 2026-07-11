using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Patients.DatabaseMappings;
using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Features.Patients.AddContact;

[ApiController]
[Authorize]
[Route("api/patients/{patientId:guid}/contacts")]
public class AddContactEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Add a contact to a patient")]
    [Tags("Patients Module")]
    public async Task<IActionResult> Add(Guid patientId,
        [FromBody] AddContactBody body, CancellationToken ct)
    {
        var result = await sender.Send(
            new AddContactCommand(patientId, body.Type, body.PhoneNumber, body.Email, body.IsPrimary), ct);
        if (result is null) return NotFound();
        return Created(string.Empty, new { id = result });
    }
}

public record AddContactBody(string Type, string PhoneNumber, string? Email, bool IsPrimary);

public record AddContactCommand(Guid PatientId, string Type, string PhoneNumber, string? Email, bool IsPrimary)
    : IRequest<Guid?>;

public class AddContactHandler(PatientsDbContext db) : IRequestHandler<AddContactCommand, Guid?>
{
    public async Task<Guid?> Handle(AddContactCommand cmd, CancellationToken ct)
    {
        var exists = await db.Patients.AnyAsync(p => p.Id == cmd.PatientId, ct);
        if (!exists) return null;

        if (cmd.IsPrimary)
        {
            await db.PatientContacts
                .Where(c => c.PatientId == cmd.PatientId && c.IsPrimary)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsPrimary, false), ct);
        }

        var type = Enum.Parse<ContactType>(cmd.Type, ignoreCase: true);
        var contact = PatientContact.Create(cmd.PatientId, type, cmd.PhoneNumber, cmd.Email, cmd.IsPrimary);
        db.PatientContacts.Add(contact);
        await db.SaveChangesAsync(ct);
        return contact.Id;
    }
}