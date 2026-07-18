namespace Utano.Module.Files.Domain.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Returns a presigned PUT URL the client uploads to directly. Expires in minutes configured in FileStorage:UploadUrlExpiryMinutes.
    /// </summary>
    Task<string> GenerateUploadUrlAsync(string objectKey, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Returns a short-lived presigned GET URL for downloading a file.
    /// </summary>
    Task<string> GenerateDownloadUrlAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Permanently deletes the object from storage.
    /// </summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}
