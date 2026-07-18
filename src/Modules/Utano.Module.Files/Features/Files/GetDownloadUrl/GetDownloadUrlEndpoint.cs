using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Utano.Module.Core.Exceptions;
using Utano.Module.Files.Domain.Interfaces;

namespace Utano.Module.Files.Features.Files.GetDownloadUrl;

public record GetDownloadUrlQuery(Guid Id) : IRequest<GetDownloadUrlResponse>;

public record GetDownloadUrlResponse(string DownloadUrl, string FileName, string ContentType);

[ApiController]
[Route("api/files")]
[Authorize]
public class GetDownloadUrlEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}/url")]
    [ProducesResponseType(typeof(GetDownloadUrlResponse), 200)]
    [Tags("Files Module")]
    public async Task<IActionResult> GetDownloadUrl(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetDownloadUrlQuery(id), ct));
}

public class GetDownloadUrlHandler(
    IFileAttachmentRepository repository,
    IFileStorageService storage)
    : IRequestHandler<GetDownloadUrlQuery, GetDownloadUrlResponse>
{
    public async Task<GetDownloadUrlResponse> Handle(GetDownloadUrlQuery query, CancellationToken ct)
    {
        var file = await repository.GetByIdAsync(query.Id, ct)
            ?? throw new UtanoDomainException("File not found.");

        var downloadUrl = await storage.GenerateDownloadUrlAsync(file.ObjectKey, ct);

        return new GetDownloadUrlResponse(downloadUrl, file.FileName, file.ContentType);
    }
}
