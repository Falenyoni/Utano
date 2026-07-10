using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.Patients.Features.MedicalAids.AddMedicalAid;

namespace Utano.Module.Patients.Features.MedicalAids.GetMedicalAids;

[ApiController]
[Route("api/medical-aids")]
[Authorize]
public class GetMedicalAidsEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MedicalAidResponse>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List all medical aid schemes for this practice")]
    [Tags("Medical Aids")]
    public async Task<IActionResult> GetMedicalAids(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMedicalAidsQuery(), cancellationToken);
        return Ok(result);
    }
}
