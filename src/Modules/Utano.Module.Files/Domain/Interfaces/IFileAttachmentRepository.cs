using Utano.Module.Files.Domain.Entities;
using Utano.Module.Files.Domain.Enums;

namespace Utano.Module.Files.Domain.Interfaces;

public interface IFileAttachmentRepository
{
    Task<FileAttachment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FileAttachment>> GetByPatientAsync(Guid patientId, FileAttachmentType? type = null, CancellationToken ct = default);
    Task AddAsync(FileAttachment file, CancellationToken ct = default);
    Task UpdateAsync(FileAttachment file, CancellationToken ct = default);
}
