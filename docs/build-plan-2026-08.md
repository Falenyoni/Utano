# Build Plan — August 2026

Written 2026-08-02, right after a full module-by-module audit pass (Notifications, Appointments, ClinicalNotes, Identity/RBAC, Patients, Reports, Billing, Claims, Inventory, Settings, Financial) turned up 19 new items on top of what was already known. Full details, code references, and reasoning for every item live in `technical-debt-and-priorities.md` — this doc is just the at-a-glance roadmap, checkable off as work lands.

Items are grouped into phases by **dependency and effort**, not just severity — everything in one phase genuinely belongs in the same work session, since they touch the same files or mechanism. Work top to bottom unless noted otherwise.

---

## Phase 1 — RBAC pass
*One connected effort — same authorization mechanism, same files, do together.*

- [x] **#1** — Build `IRequirePermission` + `PermissionAuthorizationBehavior` + `UtanoForbiddenException`. First real usage: `UpdateRoleCommand`/`CreateRoleCommand`. *(built 2026-08-03, build-verified)*
- [x] **#2 (correction)** — Revised `UpdateRoleHandler`: remove the permission-editing lock on system roles, generalize the Admin-deactivation check to "practice must keep ≥1 active role with `settings.roles`." *(built 2026-08-03)*
- [x] **#30** — Wire `ResetUserPasswordCommand` to the new mechanism. Highest priority in this phase — it's a live account-takeover path today. *(built 2026-08-03)*
- [x] **#34** — Split `settings.practice` into 4 distinct permissions (Practice / Branding / Medical Aid Schemes / Subscription). *(built 2026-08-03, incl. migration)*
- [x] **#4 + #5** — RBAC seeding reconciliation + permissions catalog table. *(built 2026-08-03 — `Permission` entity + FK + `PermissionReconciler`; migration generated + seeded)*
- [x] **#3** — Subscription-tier server-side enforcement. Must land before Phase 4/#14 regardless. *(built 2026-08-03 — `SubscriptionTierBehavior`, assembly-matched to module `Plan`; confirmed Trial correctly gets full Professional access)*
- [x] **#37 (new, found during Phase 1)** — Trial extension was impossible (`StartTrial` only ever called once). Fixed: `Practice.ExtendTrial()` + `POST /api/admin/practices/{id}/extend-trial`. *(built 2026-08-03)*

**Phase 1 status: fully built, migrated, and live-verified 2026-08-03/04** — RBAC enforcement, tier enforcement, trial extension, and self-signup (#38, added mid-phase) all confirmed working end-to-end against the real DB.

## Phase 2 — Quick, high-value bug fixes
*Small enough to do anytime — even as a warm-up before Phase 1.*

- [x] **#36** — Financial page currency label (hardcoded "R", should be "$"). *(fixed 2026-08-04, 21 occurrences)*
- [x] **#20** — Demographics report data bug. *(fixed 2026-08-04 by threading `patientGender`/`patientDateOfBirth` through nav state onto `Visit` — turned out not to actually work on real data; correctly re-fixed 2026-08-05 with a live `IPatientDemographicsLookup` call in `VisitDemographicsHandler` instead, and all the 2026-08-04 threading deleted as unnecessary. See technical-debt doc for the full story.)*
- [x] **#31** — Login brute-force/lockout protection. *(fixed + live-verified 2026-08-04 — account lockout + IP rate limit, migration `AddLoginLockout`)*

**Phase 2 status: fully built and live-verified 2026-08-04.**

## Phase 3 — Needs a decision before it can be scoped
*Bring these back for a quick decision, then they slot into a later phase.*

- [x] **#19** — Consultations "+ Open Visit" button: removed entirely, routes everyone through Walk-In. *(decided + built 2026-08-04)*
- [x] **#18** — Overdue/No-show handling: automatic `NoShow` after 1hr grace period (Hangfire scan job), computed `IsOverdue` field, "Overdue" UI badge on Appointments/Waiting Room/Dashboard, stuck `CheckedIn`/`InProgress` flagged not auto-transitioned. *(decided + built + partially live-verified 2026-08-04 — see technical-debt doc for the production-Hangfire-race caveat on full end-to-end verification)*

**Phase 3 status: fully built 2026-08-04.**

## Phase 4 — Revenue-integrity work
*Real money risk — prioritize ahead of general polish.*

- [x] **#24** — Turned out to be already fully built (direct `IBillingService` calls from `OpenVisitHandler`/`CompleteVisitHandler`/procedures/prescriptions, not a `VisitCompletedEvent` handler as assumed). Live-verified 2026-08-04; only real gap was frontend visibility (visit↔invoice link), fixed same day — see technical-debt doc.
- [x] **#32** — Forgot Password flow. Built 2026-08-16: `IEmailSender`/`ResendEmailSender`, `PasswordResetToken`, forgot/reset-password endpoints + frontend pages. **Migrated and live-verified 2026-08-16** — Bongani tested end-to-end. Same-day follow-up: security-notification emails added to `ChangePasswordHandler`/`ResetUserPasswordHandler` too.

**Phase 4 status: fully built and live-verified 2026-08-16.**

## Phase 5 — File storage chain
*Dependent — build in this exact order.*

- [x] **#35** — Extended the R2 object-key scheme for practice-level (non-patient) assets (`{practiceId}/_practice/{type}/{uuid}.{ext}`); `FileAttachment.PatientId` now nullable; added `IFileAttachmentLookup` (Core cross-module interface, mirrors `IVisitLookup`/`IPatientDemographicsLookup`) and permission checks on all 4 Files endpoints. Built 2026-08-10. **Also found and fixed a real bug**: the module's original migration was scaffolded into a mistaken duplicated path (`src/Modules/Modules/...`) back on 2026-07-18 and was never actually picked up by the real project — `FileAttachments` had never been created in any database. Regenerated cleanly as `InitFilesModule` in the correct location.
- [x] **#33** — Practice logo moved onto R2 (`Practice.LogoFileId` replacing `LogoBase64` for new uploads); login/branding/practice responses now resolve a fresh presigned `LogoUrl` instead of embedding base64. One-off API-key-gated `POST /api/admin/practices/migrate-branding-logos` backfills any practice's existing base64 logo into R2 — `LogoBase64` column deliberately kept for now, drop it in a follow-up migration once the backfill's been checked live. Built 2026-08-10.
- [x] **#17** — Patient document upload/viewer UI: `PatientDocuments.tsx` (upload modal with type selector, list, delete) wired into `PatientDetailModal`; viewer shows images inline, PDFs via `<iframe>`, other types as a download link. Built 2026-08-10, `npx tsc --noEmit` clean.

**Phase 5 status: fully built, migrated, and live-verified 2026-08-16** — R2 credentials configured, CORS fixed (browser drag-drop hijack + missing bucket CORS policy, both found and fixed live), logo upload and patient/visit document upload both confirmed working end-to-end.

## Phase 6 — Cheap frontend-only batch
*Good for a lighter week — zero backend work for any of these.*

- [x] **#25** — Billing invoices: quick month-preset filter buttons. *(fixed 2026-08-04)*
- [x] **#26** — Claims page: date range + medical-aid-scheme filters. *(fixed 2026-08-04 — also had to add the `MedicalAidId` backend filter param, which turned out not to already exist)*
- [x] **#22** — Moved Low Stock's PDF export onto the Inventory page; dropped the redundant Reports tab. *(fixed 2026-08-04)*
- [x] **#23** — Dashboard stat cards navigate to filtered views instead of unfiltered lists. *(fixed 2026-08-04, all 4 cards — Overdue via #18, remaining 3 this phase)*

**Phase 6 status: fully built and live-verified 2026-08-04.**

## Phase 7 — Bigger, unscoped features
*Design pass needed before building — not urgent.*

- [x] **#27** — Stock Take / physical count reconciliation workflow. Built 2026-08-22 — new `StockTake`/`StockTakeLine` aggregate, scope confirmed first (whole inventory or one category, partial counts allowed). Finalize applies the existing `StockItem.Adjust()` per counted line with variance, so `StockTransaction` stays the single source of truth. New `StockTakesPage`/`StockTakeDetailPage`. Migration `AddStockTakes` (2 new tables, purely additive). **Live-tested clean 2026-08-23.**
- [x] **#28** — Price adjustment strategy. Built 2026-08-22 — `StockItem.AdjustPricing()`, `POST /api/inventory/stock/bulk-reprice` (category + Selling/Cost/Both + Percent/Fixed), margin calculator on Add/Edit Stock Item forms, price-change delta folded into the existing audit entry (reuses #13's `AuditLog`, no new table). No migration. **Live-tested clean 2026-08-23.**
- [x] **#21** — Revenue Summary by-doctor/by-service breakdown. Built 2026-08-22 — no schema change, both dimensions (`Invoice.DoctorId`/`DoctorName`, `InvoiceLineItem.Type`) already existed. Two new tables on the Revenue report + PDF export. Build clean.

**Phase 7 status: fully built 2026-08-22, live-tested clean 2026-08-23.**

## Phase 8 — Patient model change
*Bigger domain change — touches registration form + duplicate-check logic. Own focused session.*

- [x] **#29** — Replaced `NationalId` with a typed `PatientIdentifier` (NationalId | Passport | Pending), covering newborns, foreign patients, and ID-less walk-ins. Built 2026-08-10 (see `docs/adr/0001-typed-patient-identifier.md`). Migration run, live-tested by Bongani 2026-08-10 — confirmed working.

## Phase 9 — Modal cleanup
*No correctness risk — whenever there's a lighter week.*

- [x] **#11** — Shared modal primitives (`ModalBackdrop`, `inputClass`, `labelClass`) extracted to `shared/components/ModalBackdrop.tsx` and `shared/constants/formStyles.ts`; applied across all files with genuine style drift. Built 2026-08-10.
- [x] **#12** — Converted `NewPatientPage`→`NewPatientModal`, `PatientDetailPage`→`PatientDetailModal`, `WalkInPage`→`WalkInModal`, `NewAppointmentPage`→`NewAppointmentModal`. The two patient modals use route-driven nesting (`patients/new`, `patients/:id` as children of `patients`, rendered via `<Outlet />`) to stay deep-linkable; the other two use local component state. Built 2026-08-10. `npx tsc --noEmit` clean.

## Phase 10 — Deferred by explicit decision
*Last, per Bongani's own earlier call.*

- [x] **#13** — Broader audit trail (Billing, Inventory, Patients, Identity). Built 2026-08-22 — reused the existing `IAuditService`/`AuditLog` mechanism (no new infrastructure needed), inline fire-and-forget calls in each handler. Scope confirmed with Bongani first: Patients (Register/Update/Activate/Deactivate), Billing (Create/Issue/Void invoice, Record Payment, Claims, Payment Plan), Inventory (stock item metadata only — quantity changes stay covered by `StockTransaction`), Identity (Create/Update/Deactivate user, Assign roles, Reset password). Frontend filter dropdowns updated to match. No migration. **Live-tested clean 2026-08-23.**
- [x] **#14** — Email half done and **live-verified** 2026-08-16: appointment reminder emails to both doctor (if opted in) and patient (if they have an email on file), reusing `IEmailSender`. WhatsApp/SMS still needs a provider account — not started.

## Fold in opportunistically
*Not worth a dedicated pass — fix when nearby code gets touched anyway.*

- [x] **#15** — `CreatePracticeHandler`'s hardcoded system-role list. Fixed 2026-08-23 — replaced 6 hand-written `SeedRole(...)` calls (a parallel list to `SystemRoles.All`, easy to forget updating) with a loop over `SystemRoles.All` keyed against a `RoleDescriptions` dictionary; a role missing its description now throws immediately at practice-creation time instead of silently seeding nothing. No migration, no behavior change for existing roles. Build clean.
- [x] **#16** — Dead code: `Visit.UpdateVitals()`. Confirmed zero callers anywhere in the codebase (superseded by the triage flow), deleted 2026-08-23. Build clean.
- [x] **#39** — `NoShow` appointments have no available actions and no way to undo one. Found 2026-08-10, fixed 2026-08-22 — Reschedule/Reassign/Cancel now surfaced for `NoShow` in the UI (backend always permitted them), plus a new "Undo No-Show" action (`NoShow` → `CheckedIn`) for the wrongly-auto-marked case. No migration. **Live-tested clean 2026-08-23** (full batched QA pass).
- [x] **#40** — Login couldn't disambiguate two accounts sharing one email (per-practice uniqueness let this happen, `CreateUserCommand` didn't check globally like `CreatePracticeCommand` did). Found and fixed 2026-08-13 — app-layer check + DB constraint restored globally unique. Cleanup script run, migration applied, code pushed — **live 2026-08-14.**
- [x] **#41** — **Cross-tenant data leak**: Audit Log page showed every practice's audit trail to every practice — `AuditLog` was the one entity missing from `ClinicalNotesDbContext`'s tenant query filter. Also had zero permission check (`[Authorize]` only). Found 2026-08-21, fixed same day — query filter added, `IRequirePermission` added. No migration. **Deployed and live-verified clean 2026-08-23** — each practice now correctly sees only its own audit trail.

---

## Already done (for reference — not part of this plan)

`#0` (MediatR licensing — resolved, key registration still pending), `#6`–`#10` (visitId + the 3 appointment UX bugs, all verified live 2026-07-31).

---

**Suggested starting point:** Phase 1, since it's the highest security value and unblocks the most other items (#30, #34 both depend on it). Phase 2's items are cheap enough to knock out first as a warm-up if preferred — they don't block or get blocked by anything else.
