using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;
using Utano.Module.Files.Domain.Enums;

namespace Utano.Module.Files.Domain.Entities;

public class FileAttachment : AggregateRoot
{
    private FileAttachment() { }

    public Guid PatientId { get; private set; }
    public Guid? ConsultationId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ObjectKey { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public FileAttachmentType AttachmentType { get; private set; }
    public string? Description { get; private set; }
    public bool IsDeleted { get; private set; }

    public static FileAttachment Create(
        Guid practiceId,
        Guid patientId,
        string fileName,
        string objectKey,
        string contentType,
        long sizeBytes,
        FileAttachmentType attachmentType,
        Guid? consultationId = null,
        string? description = null)
    {
        if (practiceId == Guid.Empty) throw new UtanoDomainException("Practice is required.");
        if (patientId == Guid.Empty) throw new UtanoDomainException("Patient is required.");
        if (string.IsNullOrWhiteSpace(fileName)) throw new UtanoDomainException("File name is required.");
        if (string.IsNullOrWhiteSpace(objectKey)) throw new UtanoDomainException("Object key is required.");
        if (sizeBytes <= 0) throw new UtanoDomainException("File size must be greater than zero.");

        return new FileAttachment
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            PatientId = patientId,
            ConsultationId = consultationId,
            FileName = fileName.Trim(),
            ObjectKey = objectKey,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            AttachmentType = attachmentType,
            Description = description?.Trim(),
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SoftDelete()
    {
        if (IsDeleted) throw new UtanoDomainException("File is already deleted.");
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
