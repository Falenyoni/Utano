using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.Core.Exceptions;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Features.MedicalAids.UpdateMedicalAid;

[ApiController]
[Route("api/medical-aids")]
[Authorize]
public class UpdateMedicalAidEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Update a medical aid scheme's name and code")]
    [Tags("Medical Aids")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicalAidBody body, CancellationToken ct)
    {
        await sender.Send(new UpdateMedicalAidCommand(id, body.Name, body.Code), ct);
        return NoContent();
    }
}

public record UpdateMedicalAidBody(string Name, string Code);

public record UpdateMedicalAidCommand(Guid Id, string Name, string Code) : IRequest;

public class UpdateMedicalAidHandler(IMedicalAidRepository repository)
    : IRequestHandler<UpdateMedicalAidCommand>
{
    public async Task Handle(UpdateMedicalAidCommand command, CancellationToken cancellationToken)
    {
        var aid = await repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new UtanoDomainException("Medical aid scheme not found.");
        aid.Update(command.Name, command.Code);
        await repository.SaveAsync(cancellationToken);
    }
}
