namespace Utano.Module.Core.Services;

// Lets other modules resolve a FileAttachment they're holding a reference to (e.g. Identity's
// Practice.LogoFileId) without taking a project reference on Utano.Module.Files. Bypasses the
// Files module's practice-scoped query filter - callers are trusted to only ever hold FileIds
// they themselves set on their own tenant-scoped data.
public interface IFileAttachmentLookup
{
    /// <summary>Fresh presigned download URL for the file, or null if fileId is null or the file no longer exists.</summary>
    Task<string?> GetDownloadUrlAsync(Guid? fileId, CancellationToken ct = default);

    /// <summary>Soft-deletes the metadata record and removes the object from storage. No-ops if fileId is null or already gone.</summary>
    Task DeleteAsync(Guid? fileId, CancellationToken ct = default);

    /// <summary>
    /// Server-side upload of a practice's logo from raw bytes (for migrating legacy base64 logos
    /// into R2). Returns the new FileAttachment's Id. Regular logo uploads from the frontend go
    /// through the normal presigned-URL flow instead.
    /// </summary>
    Task<Guid> CreateLogoFromBytesAsync(Guid practiceId, byte[] data, string fileName, string contentType, CancellationToken ct = default);
}
