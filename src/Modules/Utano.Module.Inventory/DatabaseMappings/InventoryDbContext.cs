using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Services;
using Utano.Module.Inventory.Domain.Entities;

namespace Utano.Module.Inventory.DatabaseMappings;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options, ICurrentUserService currentUser)
    : DbContext(options)
{
    public DbSet<StockItem> StockItems { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StockItem>()
            .HasQueryFilter(s => s.PracticeId == currentUser.PracticeId);

        modelBuilder.Entity<StockTransaction>()
            .HasQueryFilter(t => t.PracticeId == currentUser.PracticeId);
    }
}
