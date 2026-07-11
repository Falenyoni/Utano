using Microsoft.EntityFrameworkCore;
using Utano.Module.Billing.Domain.Entities;
using Utano.Module.Core.Services;

namespace Utano.Module.Billing.DatabaseMappings;

public class BillingDbContext(DbContextOptions<BillingDbContext> options, ICurrentUserService currentUser)
    : DbContext(options)
{
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentPlan> PaymentPlans { get; set; }
    public DbSet<PaymentPlanInstallment> PaymentPlanInstallments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Invoice>()
            .HasQueryFilter(i => i.PracticeId == currentUser.PracticeId);

        modelBuilder.Entity<Payment>()
            .HasQueryFilter(p => p.PracticeId == currentUser.PracticeId);

        modelBuilder.Entity<PaymentPlan>()
            .HasQueryFilter(pp => pp.PracticeId == currentUser.PracticeId);
    }
}
