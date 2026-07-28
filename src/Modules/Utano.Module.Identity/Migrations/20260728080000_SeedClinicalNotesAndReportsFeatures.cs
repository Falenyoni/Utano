using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260728080000_SeedClinicalNotesAndReportsFeatures")]
    public partial class SeedClinicalNotesAndReportsFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ClinicalNotes moved from FeatureKey="core" to "clinical_notes" (professional)
            // Reports moved from FeatureKey="core" to "reports" (professional)
            // Seed the new feature rows for all existing practices, enabled only
            // for practices that are currently on Professional tier (Trial or Active).
            migrationBuilder.Sql(@"
INSERT INTO identity.""PracticeFeatures"" (""Id"", ""PracticeId"", ""FeatureKey"", ""IsEnabled"", ""EnabledAt"")
SELECT
    gen_random_uuid(),
    p.""Id"",
    f.""FeatureKey"",
    CASE WHEN p.""SubscriptionTier"" = 'Professional' THEN TRUE ELSE FALSE END,
    NOW()
FROM identity.""Practices"" p
CROSS JOIN (VALUES ('clinical_notes'), ('reports')) AS f(""FeatureKey"")
WHERE NOT EXISTS (
    SELECT 1 FROM identity.""PracticeFeatures"" pf
    WHERE pf.""PracticeId"" = p.""Id""
      AND pf.""FeatureKey"" = f.""FeatureKey""
);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM identity.""PracticeFeatures""
WHERE ""FeatureKey"" IN ('clinical_notes', 'reports');
");
        }
    }
}
