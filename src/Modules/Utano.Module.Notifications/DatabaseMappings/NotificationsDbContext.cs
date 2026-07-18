using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.Domain.Entities;

namespace Utano.Module.Notifications.DatabaseMappings;

public class NotificationsDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public NotificationsDbContext(
        DbContextOptions<NotificationsDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        // Scope to practice only — recipient filter applied in repository queries
        modelBuilder.Entity<Notification>()
            .HasQueryFilter(n => n.PracticeId == _currentUserService.PracticeId);
    }
}
