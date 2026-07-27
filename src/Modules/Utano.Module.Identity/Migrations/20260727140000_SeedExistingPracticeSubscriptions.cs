using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260727140000_SeedExistingPracticeSubscriptions")]
    public partial class SeedExistingPracticeSubscriptions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing practices get a 30-day trial from now so the trial banner
            // and subscription flow can be tested before go-live
            migrationBuilder.Sql(@"
UPDATE identity.""Practices""
SET ""SubscriptionTier""   = 'Professional',
    ""SubscriptionStatus"" = 'Trial',
    ""TrialEndsAt""        = NOW() + INTERVAL '30 days',
    ""SubscriptionExpiresAt"" = NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE identity.""Practices""
SET ""SubscriptionTier""   = 'Starter',
    ""SubscriptionStatus"" = 'Trial',
    ""TrialEndsAt""        = NULL,
    ""SubscriptionExpiresAt"" = NULL;
");
        }
    }
}
