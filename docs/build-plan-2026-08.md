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
- [ ] **#32** — ⏸ Deferred by explicit decision (2026-08-04, "let's skip this we will do it later"). Forgot Password flow, needs an email-provider decision (Resend was the leaning choice) — same decision Phase 10/#14 needs, make it once here whenever it's picked back up.

## Phase 5 — File storage chain
*Dependent — build in this exact order.*

- [ ] **#35** — Extend the R2 object-key scheme for practice-level (non-patient) assets; make `FileAttachment.PatientId` optional.
- [ ] **#33** — Move the practice logo onto R2; stop embedding base64 in login/branding responses.
- [ ] **#17** — Patient document upload/viewer UI (dropzone, list, viewer modal) — biggest of the three, reuses the infra the first two steps extend.

## Phase 6 — Cheap frontend-only batch
*Good for a lighter week — zero backend work for any of these.*

- [x] **#25** — Billing invoices: quick month-preset filter buttons. *(fixed 2026-08-04)*
- [x] **#26** — Claims page: date range + medical-aid-scheme filters. *(fixed 2026-08-04 — also had to add the `MedicalAidId` backend filter param, which turned out not to already exist)*
- [x] **#22** — Moved Low Stock's PDF export onto the Inventory page; dropped the redundant Reports tab. *(fixed 2026-08-04)*
- [x] **#23** — Dashboard stat cards navigate to filtered views instead of unfiltered lists. *(fixed 2026-08-04, all 4 cards — Overdue via #18, remaining 3 this phase)*

**Phase 6 status: fully built and live-verified 2026-08-04.**

## Phase 7 — Bigger, unscoped features
*Design pass needed before building — not urgent.*

- [ ] **#27** — Stock Take / physical count reconciliation workflow.
- [ ] **#28** — Price adjustment strategy (bulk repricing, margin-based pricing, price history).
- [ ] **#21** — Revenue Summary by-doctor/by-service breakdown. **⏸ Stays paused until #24 lands** — Bongani's explicit sequencing call, invoice data will look different once auto-invoicing exists.

## Phase 8 — Patient model change
*Bigger domain change — touches registration form + duplicate-check logic. Own focused session.*

- [ ] **#29** — Make `NationalId` optional, add a placeholder/alternative-identifier path (newborns, foreign patients, ID-less walk-ins).

## Phase 9 — Modal cleanup
*No correctness risk — whenever there's a lighter week.*

- [ ] **#11** — Shared modal primitives (`ModalBackdrop`, `inputClass`, `labelClass`) instead of duplicated across 6+ files.
- [ ] **#12** — Convert `NewPatientPage`, `PatientDetailPage`, `WalkInPage`, `NewAppointmentPage` from pages to modals.

## Phase 10 — Deferred by explicit decision
*Last, per Bongani's own earlier call.*

- [ ] **#13** — Broader audit trail (Billing, Inventory, Patients, Identity).
- [ ] **#14** — Phase 4: actual WhatsApp/email sending. After #3 and #32's email infra both exist.

## Fold in opportunistically
*Not worth a dedicated pass — fix when nearby code gets touched anyway.*

- [ ] **#15** — `CreatePracticeHandler`'s hardcoded system-role list.
- [ ] **#16** — Dead code: `Visit.UpdateVitals()`.

---

## Already done (for reference — not part of this plan)

`#0` (MediatR licensing — resolved, key registration still pending), `#6`–`#10` (visitId + the 3 appointment UX bugs, all verified live 2026-07-31).

---

**Suggested starting point:** Phase 1, since it's the highest security value and unblocks the most other items (#30, #34 both depend on it). Phase 2's items are cheap enough to knock out first as a warm-up if preferred — they don't block or get blocked by anything else.
