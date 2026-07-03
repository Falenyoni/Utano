using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Patients.Features.Patients.GetPatientById;

[ApiController]
[Route("api/patients")]
public class GetPatientByIdEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPatientByIdResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Get a patient by ID")]
    [EndpointDescription("Returns full patient details including all contacts and addresses.")]
    [Tags("Patients Module")]
    public async Task<IActionResult> GetPatientById(Guid id, CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetPatientByIdQuery(id), cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}