using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Patients.DatabaseMappings;
using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Features.Patients.AddAddress;

[ApiController]
[Authorize]
[Route("api/patients/{patientId:guid}/addresses")]
public class AddAddressEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Add an address to a patient")]
    [Tags("Patients Module")]
    public async Task<IActionResult> Add(Guid patientId,
        [FromBody] AddAddressBody body, CancellationToken ct)
    {
        var result = await sender.Send(
            new AddAddressCommand(patientId, body.Type, body.Street, body.City, body.Country, body.Suburb, body.IsPrimary), ct);
        if (result is null) return NotFound();
        return Created(string.Empty, new { id = result });
    }
}

public record AddAddressBody(string Type, string Street, string City, string Country, string? Suburb, bool IsPrimary);

public record AddAddressCommand(Guid PatientId, string Type, string Street, string City, string Country,
    string? Suburb, bool IsPrimary) : IRequest<Guid?>;

public class AddAddressHandler(PatientsDbContext db) : IRequestHandler<AddAddressCommand, Guid?>
{
    public async Task<Guid?> Handle(AddAddressCommand cmd, CancellationToken ct)
    {
        var exists = await db.Patients.AnyAsync(p => p.Id == cmd.PatientId, ct);
        if (!exists) return null;

        if (cmd.IsPrimary)
        {
            await db.PatientAddresses
                .Where(a => a.PatientId == cmd.PatientId && a.IsPrimary)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsPrimary, false), ct);
        }

        var type = Enum.Parse<AddressType>(cmd.Type, ignoreCase: true);
        var address = PatientAddress.Create(cmd.PatientId, type, cmd.Street, cmd.City, cmd.Country, cmd.Suburb, cmd.IsPrimary);
        db.PatientAddresses.Add(address);
        await db.SaveChangesAsync(ct);
        return address.Id;
    }
}