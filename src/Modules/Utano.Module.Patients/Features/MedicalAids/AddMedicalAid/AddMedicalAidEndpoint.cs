using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.Patients.Features.MedicalAids.AddMedicalAid;

[ApiController]
[Route("api/medical-aids")]
[Authorize]
public class AddMedicalAidEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(MedicalAidResponse), (int)HttpStatusCode.Created)]
    [EndpointSummary("Add a medical aid scheme")]
    [Tags("Medical Aids")]
    public async Task<IActionResult> AddMedicalAid(
        [FromBody] AddMedicalAidCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(AddMedicalAid), new { id = result.Id }, result);
    }
}
