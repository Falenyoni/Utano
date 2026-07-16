using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Utano.Module.Identity.DatabaseMappings;

#nullable disable

namespace Utano.Module.Identity.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260715210000_SeedExistingPracticeRoles")]
    public partial class SeedExistingPracticeRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // For every practice that has no roles yet (pre-RBAC data):
            //   1. Seed the 5 default system roles with their full permission sets.
            //   2. Assign each existing user to the role that matches their Role column.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    p                    RECORD;
    admin_role_id        UUID;
    doctor_role_id       UUID;
    nurse_role_id        UUID;
    receptionist_role_id UUID;
    billing_role_id      UUID;
    u                    RECORD;
    matched_role_id      UUID;
    now_ts               TIMESTAMPTZ := NOW();
BEGIN
    FOR p IN
        SELECT ""Id"" FROM identity.""Practices""
        WHERE ""Id"" NOT IN (SELECT DISTINCT ""PracticeId"" FROM identity.""Roles"")
    LOOP
        admin_role_id        := gen_random_uuid();
        doctor_role_id       := gen_random_uuid();
        nurse_role_id        := gen_random_uuid();
        receptionist_role_id := gen_random_uuid();
        billing_role_id      := gen_random_uuid();

        -- ── Roles ────────────────────────────────────────────────────────────
        INSERT INTO identity.""Roles"" (""Id"", ""Name"", ""Description"", ""IsSystem"", ""IsActive"", ""PracticeId"", ""CreatedAt"", ""UpdatedAt"")
        VALUES
            (admin_role_id,        'Admin',        'Full system access',                       TRUE, TRUE, p.""Id"", now_ts, now_ts),
            (doctor_role_id,       'Doctor',       'Patient care and clinical documentation',  TRUE, TRUE, p.""Id"", now_ts, now_ts),
            (nurse_role_id,        'Nurse',        'Patient care and appointment management',  TRUE, TRUE, p.""Id"", now_ts, now_ts),
            (receptionist_role_id, 'Receptionist', 'Patient registration and scheduling',      TRUE, TRUE, p.""Id"", now_ts, now_ts),
            (billing_role_id,      'Billing',      'Financial management and reporting',       TRUE, TRUE, p.""Id"", now_ts, now_ts);

        -- ── Admin permissions (all 17) ────────────────────────────────────────
        INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"") VALUES
            (admin_role_id, 'patients.view'),
            (admin_role_id, 'patients.create'),
            (admin_role_id, 'patients.edit'),
            (admin_role_id, 'patients.delete'),
            (admin_role_id, 'appointments.view'),
            (admin_role_id, 'appointments.create'),
            (admin_role_id, 'appointments.edit'),
            (admin_role_id, 'appointments.cancel'),
            (admin_role_id, 'clinical_notes.view'),
            (admin_role_id, 'clinical_notes.create'),
            (admin_role_id, 'clinical_notes.edit'),
            (admin_role_id, 'inventory.view'),
            (admin_role_id, 'inventory.manage'),
            (admin_role_id, 'billing.view'),
            (admin_role_id, 'billing.manage'),
            (admin_role_id, 'reports.view'),
            (admin_role_id, 'settings.users');

        -- ── Doctor permissions ────────────────────────────────────────────────
        INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"") VALUES
            (doctor_role_id, 'patients.view'),
            (doctor_role_id, 'appointments.view'),
            (doctor_role_id, 'appointments.create'),
            (doctor_role_id, 'appointments.edit'),
            (doctor_role_id, 'clinical_notes.view'),
            (doctor_role_id, 'clinical_notes.create'),
            (doctor_role_id, 'clinical_notes.edit'),
            (doctor_role_id, 'billing.view'),
            (doctor_role_id, 'reports.view');

        -- ── Nurse permissions ─────────────────────────────────────────────────
        INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"") VALUES
            (nurse_role_id, 'patients.view'),
            (nurse_role_id, 'appointments.view'),
            (nurse_role_id, 'appointments.create'),
            (nurse_role_id, 'clinical_notes.view'),
            (nurse_role_id, 'clinical_notes.create'),
            (nurse_role_id, 'inventory.view');

        -- ── Receptionist permissions ──────────────────────────────────────────
        INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"") VALUES
            (receptionist_role_id, 'patients.view'),
            (receptionist_role_id, 'patients.create'),
            (receptionist_role_id, 'appointments.view'),
            (receptionist_role_id, 'appointments.create'),
            (receptionist_role_id, 'appointments.edit'),
            (receptionist_role_id, 'appointments.cancel'),
            (receptionist_role_id, 'billing.view');

        -- ── Billing permissions ───────────────────────────────────────────────
        INSERT INTO identity.""RolePermissions"" (""RoleId"", ""PermissionKey"") VALUES
            (billing_role_id, 'patients.view'),
            (billing_role_id, 'appointments.view'),
            (billing_role_id, 'billing.view'),
            (billing_role_id, 'billing.manage'),
            (billing_role_id, 'reports.view');

        -- ── Assign users to their matching role ───────────────────────────────
        -- Only touches users who already have no UserRoles entry.
        FOR u IN
            SELECT ""Id"", ""Role""
            FROM identity.""Users""
            WHERE ""PracticeId"" = p.""Id""
              AND ""Id"" NOT IN (SELECT ""UserId"" FROM identity.""UserRoles"")
        LOOP
            matched_role_id := CASE u.""Role""
                WHEN 'Admin'        THEN admin_role_id
                WHEN 'Doctor'       THEN doctor_role_id
                WHEN 'Nurse'        THEN nurse_role_id
                WHEN 'Receptionist' THEN receptionist_role_id
                WHEN 'Billing'      THEN billing_role_id
                ELSE                     admin_role_id
            END;

            INSERT INTO identity.""UserRoles"" (""UserId"", ""RoleId"", ""AssignedAt"")
            VALUES (u.""Id"", matched_role_id, now_ts);
        END LOOP;
    END LOOP;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove role assignments for any user whose role was seeded by this migration
            // (identified as roles that are IsSystem=true and the practice had no roles before).
            // Because we cannot perfectly distinguish seeded vs user-created roles after the fact,
            // Down() is intentionally left as a no-op — re-running is safe (NOT EXISTS guards above).
        }
    }
}
