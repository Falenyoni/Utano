using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Files.Domain.Entities;

namespace Utano.Module.Files.DatabaseMappings;

public class FilesDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public FilesDbContext(
        DbContextOptions<FilesDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<FileAttachment> FileAttachments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FilesDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileAttachment>()
            .HasQueryFilter(f =>
                f.PracticeId == _currentUserService.PracticeId &&
                !f.IsDeleted);
    }
}
