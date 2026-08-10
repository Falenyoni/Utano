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

    /// <summary>
    /// Uploads bytes directly from the server. Only for server-side migrations (e.g. moving a
    /// legacy base64 blob into R2) - regular uploads go through GenerateUploadUrlAsync so file
    /// bytes never pass through the API.
    /// </summary>
    Task UploadAsync(string objectKey, byte[] data, string contentType, CancellationToken ct = default);
}
