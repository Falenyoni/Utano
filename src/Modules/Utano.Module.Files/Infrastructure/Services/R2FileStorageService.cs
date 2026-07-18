using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Utano.Module.Files.Domain.Interfaces;

namespace Utano.Module.Files.Infrastructure.Services;

public class FileStorageSettings
{
    public string AccountId { get; set; } = null!;
    public string AccessKeyId { get; set; } = null!;
    public string SecretAccessKey { get; set; } = null!;
    public string BucketName { get; set; } = null!;
    public int UploadUrlExpiryMinutes { get; set; } = 5;
    public int DownloadUrlExpiryMinutes { get; set; } = 60;
}

public class R2FileStorageService : IFileStorageService
{
    private readonly AmazonS3Client _client;
    private readonly FileStorageSettings _settings;

    public R2FileStorageService(IOptions<FileStorageSettings> options)
    {
        _settings = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_settings.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };

        _client = new AmazonS3Client(_settings.AccessKeyId, _settings.SecretAccessKey, config);
    }

    public Task<string> GenerateUploadUrlAsync(string objectKey, string contentType, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(_settings.UploadUrlExpiryMinutes),
            ContentType = contentType,
        };

        return Task.FromResult(_client.GetPreSignedURL(request));
    }

    public Task<string> GenerateDownloadUrlAsync(string objectKey, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_settings.DownloadUrlExpiryMinutes),
        };

        return Task.FromResult(_client.GetPreSignedURL(request));
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        await _client.DeleteObjectAsync(_settings.BucketName, objectKey, ct);
    }
}
