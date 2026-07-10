using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.Patients.Features.MedicalAids.ToggleMedicalAid;

[ApiController]
[Route("api/medical-aids")]
[Authorize]
public class ToggleMedicalAidEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}/activate")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [EndpointSummary("Activate a medical aid scheme")]
    [Tags("Medical Aids")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new ActivateMedicalAidCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [EndpointSummary("Deactivate a medical aid scheme")]
    [Tags("Medical Aids")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateMedicalAidCommand(id), cancellationToken);
        return NoContent();
    }
}
