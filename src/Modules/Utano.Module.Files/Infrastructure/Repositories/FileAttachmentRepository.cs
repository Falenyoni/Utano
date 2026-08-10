using Microsoft.EntityFrameworkCore;
using Utano.Module.Files.DatabaseMappings;
using Utano.Module.Files.Domain.Entities;
using Utano.Module.Files.Domain.Enums;
using Utano.Module.Files.Domain.Interfaces;

namespace Utano.Module.Files.Infrastructure.Repositories;

public class FileAttachmentRepository(FilesDbContext context) : IFileAttachmentRepository
{
    public async Task<FileAttachment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.FileAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    // Bypasses the practice-scoped query filter. Only for cross-module lookups (e.g. resolving a
    // practice's logo at login, before a JWT/ICurrentUserService.PracticeId exists) where the
    // caller already trusts the FileId it's holding (it was only ever set to a file that same
    // practice created).
    public async Task<FileAttachment?> GetByIdIgnoringTenantAsync(Guid id, CancellationToken ct = default)
        => await context.FileAttachments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<FileAttachment>> GetByPatientAsync(
        Guid patientId,
        FileAttachmentType? type = null,
        CancellationToken ct = default)
    {
        var query = context.FileAttachments
            .AsNoTracking()
            .Where(f => f.PatientId == patientId);

        if (type.HasValue)
            query = query.Where(f => f.AttachmentType == type.Value);

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(FileAttachment file, CancellationToken ct = default)
    {
        await context.FileAttachments.AddAsync(file, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(FileAttachment file, CancellationToken ct = default)
    {
        context.FileAttachments.Update(file);
        await context.SaveChangesAsync(ct);
    }
}
