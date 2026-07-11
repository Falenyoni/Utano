using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Patients.DatabaseMappings;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Features.Patients.UpdateAddress;

[ApiController]
[Authorize]
[Route("api/patients/{patientId:guid}/addresses")]
public class UpdateAddressEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{addressId:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Update a patient address")]
    [Tags("Patients Module")]
    public async Task<IActionResult> Update(Guid patientId, Guid addressId,
        [FromBody] UpdateAddressBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new UpdateAddressCommand(patientId, addressId,
            body.Type, body.Street, body.City, body.Country, body.Suburb), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record UpdateAddressBody(string Type, string Street, string City,
    string Country, string? Suburb);

public record UpdateAddressCommand(Guid PatientId, Guid AddressId,
    string Type, string Street, string City, string Country, string? Suburb) : IRequest<bool>;

public class UpdateAddressHandler(PatientsDbContext db) : IRequestHandler<UpdateAddressCommand, bool>
{
    public async Task<bool> Handle(UpdateAddressCommand cmd, CancellationToken ct)
    {
        var address = await db.PatientAddresses
            .FirstOrDefaultAsync(a => a.Id == cmd.AddressId && a.PatientId == cmd.PatientId, ct);
        if (address is null) return false;

        var type = Enum.Parse<AddressType>(cmd.Type, ignoreCase: true);
        address.Update(type, cmd.Street, cmd.City, cmd.Country, cmd.Suburb);
        await db.SaveChangesAsync(ct);
        return true;
    }
}