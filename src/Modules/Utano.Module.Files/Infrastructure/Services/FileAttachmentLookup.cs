using Utano.Module.Core.Services;
using Utano.Module.Files.Domain.Entities;
using Utano.Module.Files.Domain.Enums;
using Utano.Module.Files.Domain.Interfaces;

namespace Utano.Module.Files.Infrastructure.Services;

public class FileAttachmentLookup(
    IFileAttachmentRepository repository,
    IFileStorageService storage) : IFileAttachmentLookup
{
    public async Task<string?> GetDownloadUrlAsync(Guid? fileId, CancellationToken ct = default)
    {
        if (fileId is null) return null;

        var file = await repository.GetByIdIgnoringTenantAsync(fileId.Value, ct);
        if (file is null) return null;

        return await storage.GenerateDownloadUrlAsync(file.ObjectKey, ct);
    }

    public async Task DeleteAsync(Guid? fileId, CancellationToken ct = default)
    {
        if (fileId is null) return;

        var file = await repository.GetByIdIgnoringTenantAsync(fileId.Value, ct);
        if (file is null) return;

        file.SoftDelete();
        await repository.UpdateAsync(file, ct);
        await storage.DeleteAsync(file.ObjectKey, ct);
    }

    public async Task<Guid> CreateLogoFromBytesAsync(Guid practiceId, byte[] data, string fileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var objectKey = $"{practiceId}/_practice/{FileAttachmentType.Logo.ToString().ToLower()}/{Guid.NewGuid()}{ext}";

        await storage.UploadAsync(objectKey, data, contentType, ct);

        var file = FileAttachment.Create(
            practiceId,
            patientId: null,
            fileName,
            objectKey,
            contentType,
            data.LongLength,
            FileAttachmentType.Logo);

        await repository.AddAsync(file, ct);

        return file.Id;
    }
}
