using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Files.Domain.Entities;
using Utano.Module.Files.Domain.Enums;
using Utano.Module.Files.Domain.Interfaces;

namespace Utano.Module.Files.Features.Files.RequestUploadUrl;

public record RequestUploadUrlCommand(
    Guid PatientId,
    string FileName,
    string ContentType,
    long SizeBytes,
    FileAttachmentType AttachmentType,
    Guid? ConsultationId,
    string? Description) : IRequest<RequestUploadUrlResponse>;

public record RequestUploadUrlResponse(
    Guid FileId,
    string UploadUrl,
    string ContentType);

[ApiController]
[Route("api/files")]
[Authorize]
public class RequestUploadUrlEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("upload-url")]
    [ProducesResponseType(typeof(RequestUploadUrlResponse), 200)]
    [Tags("Files Module")]
    public async Task<IActionResult> RequestUploadUrl(
        [FromBody] RequestUploadUrlCommand command,
        CancellationToken ct)
        => Ok(await sender.Send(command, ct));
}

public class RequestUploadUrlHandler(
    IFileAttachmentRepository repository,
    IFileStorageService storage,
    ICurrentUserService currentUser)
    : IRequestHandler<RequestUploadUrlCommand, RequestUploadUrlResponse>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg", "image/png", "image/webp", "image/gif",
        "application/pdf",
        "application/dicom", "image/dicom"
    ];

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public async Task<RequestUploadUrlResponse> Handle(
        RequestUploadUrlCommand cmd, CancellationToken ct)
    {
        if (!AllowedContentTypes.Contains(cmd.ContentType.ToLowerInvariant()))
            throw new UtanoDomainException($"File type '{cmd.ContentType}' is not allowed. Accepted types: JPEG, PNG, WebP, GIF, PDF, DICOM.");

        if (cmd.SizeBytes > MaxFileSizeBytes)
            throw new UtanoDomainException($"File exceeds the maximum allowed size of 50 MB.");

        var ext = Path.GetExtension(cmd.FileName).ToLowerInvariant();
        var objectKey = $"{currentUser.PracticeId}/{cmd.PatientId}/{cmd.AttachmentType.ToString().ToLower()}/{Guid.NewGuid()}{ext}";

        var file = FileAttachment.Create(
            currentUser.PracticeId,
            cmd.PatientId,
            cmd.FileName,
            objectKey,
            cmd.ContentType,
            cmd.SizeBytes,
            cmd.AttachmentType,
            cmd.ConsultationId,
            cmd.Description);

        await repository.AddAsync(file, ct);

        var uploadUrl = await storage.GenerateUploadUrlAsync(objectKey, cmd.ContentType, ct);

        return new RequestUploadUrlResponse(file.Id, uploadUrl, cmd.ContentType);
    }
}
