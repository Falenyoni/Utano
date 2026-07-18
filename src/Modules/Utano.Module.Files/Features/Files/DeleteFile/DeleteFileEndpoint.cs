using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Utano.Module.Core.Exceptions;
using Utano.Module.Files.Domain.Interfaces;

namespace Utano.Module.Files.Features.Files.DeleteFile;

public record DeleteFileCommand(Guid Id) : IRequest;

[ApiController]
[Route("api/files")]
[Authorize]
public class DeleteFileEndpoint(ISender sender) : ControllerBase
{
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [Tags("Files Module")]
    public async Task<IActionResult> DeleteFile(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteFileCommand(id), ct);
        return NoContent();
    }
}

public class DeleteFileHandler(
    IFileAttachmentRepository repository,
    IFileStorageService storage)
    : IRequestHandler<DeleteFileCommand>
{
    public async Task Handle(DeleteFileCommand command, CancellationToken ct)
    {
        var file = await repository.GetByIdAsync(command.Id, ct)
            ?? throw new UtanoDomainException("File not found.");

        file.SoftDelete();
        await repository.UpdateAsync(file, ct);

        await storage.DeleteAsync(file.ObjectKey, ct);
    }
}
