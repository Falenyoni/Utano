using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Utano.Module.Files.Domain.Enums;
using Utano.Module.Files.Domain.Interfaces;

namespace Utano.Module.Files.Features.Files.ListFiles;

public record ListFilesQuery(Guid PatientId, FileAttachmentType? Type) : IRequest<IReadOnlyList<FileAttachmentDto>>;

public record FileAttachmentDto(
    Guid Id,
    Guid PatientId,
    Guid? ConsultationId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string AttachmentType,
    string? Description,
    DateTimeOffset CreatedAt);

[ApiController]
[Route("api/files")]
[Authorize]
public class ListFilesEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FileAttachmentDto>), 200)]
    [Tags("Files Module")]
    public async Task<IActionResult> ListFiles(
        [FromQuery] Guid patientId,
        [FromQuery] FileAttachmentType? type,
        CancellationToken ct)
        => Ok(await sender.Send(new ListFilesQuery(patientId, type), ct));
}

public class ListFilesHandler(IFileAttachmentRepository repository)
    : IRequestHandler<ListFilesQuery, IReadOnlyList<FileAttachmentDto>>
{
    public async Task<IReadOnlyList<FileAttachmentDto>> Handle(ListFilesQuery query, CancellationToken ct)
    {
        var files = await repository.GetByPatientAsync(query.PatientId, query.Type, ct);

        return files.Select(f => new FileAttachmentDto(
            f.Id,
            f.PatientId,
            f.ConsultationId,
            f.FileName,
            f.ContentType,
            f.SizeBytes,
            f.AttachmentType.ToString(),
            f.Description,
            f.CreatedAt)).ToList();
    }
}
