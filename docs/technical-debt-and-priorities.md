# Technical Debt & Priority Plan

Consolidated 2026-07-30 from everything found while building the notifications feature and the
appointments/modals UX request. **Scope boundary: this covers what was actually touched this
session** — Notifications, Appointments, ClinicalNotes, Identity/RBAC, and the Patients/Appointments
frontend. Billing, Inventory, Reports, Claims, Dispensary, and Financial haven't had a deep pass —
treat this as "everything found so far," not "everything that exists."

Each item is tagged with status: ✅ Fixed this session / ❌ Open.

---

## Tier 1 — Security-critical (real risk today, not hypothetical)

| # | Issue | Status |
|---|---|---|
| 1 | **Backend never enforces RBAC permissions.** Every endpoint checked has only `[Authorize]` (logged in) — `ICurrentUserService.HasPermission()` exists and works, but no controller/handler calls it. A Nurse can call a Billing endpoint directly (Swagger, curl, dev tools) even though the UI hides the button. This is the single largest item on this list, both in importance and in effort — it's not one fix, it's a systematic pass across every module. | ❌ Open |
| 2 | **`IsSystem` role has no protection.** Any Admin can rename or strip permissions from the built-in Admin role through the same generic `UpdateRoleHandler` used for custom roles — no guard preventing it. Real "nobody can log in or manage the practice anymore" risk. Small, cheap fix relative to its payoff. | ❌ Open |
| 3 | **Backend never enforces subscription-tier feature gating.** `IFeatureService.IsEnabledAsync` (the actual per-request check) is dead code everywhere — tier gating is 100% frontend-only, same root cause as #1. Low stakes *today* (worst case: a Starter practice's own staff sees a Professional page). Becomes an actual **money** problem the moment Phase 4 (WhatsApp sending) ships, since bypassing the frontend toggle would mean paying Meta for a Starter practice's messages. Must land before Phase 4, not necessarily before everything else on this list. | ❌ Open |

## Tier 2 — Data integrity / compounding maintenance cost

| # | Issue | Status |
|---|---|---|
| 4 | **No RBAC seeding reconciliation.** Every permission change requires a brand-new hand-written SQL migration, forever — 11 of these exist already, one already needed a manual delete-and-reinsert cleanup (`SeedSettingsPermissions`) because nothing keeps existing practices in sync with code. Recommended fix: auto-reconcile additive permission changes on app startup (diff DB against `IModuleDescriptor.GetPermissionsForRole`, insert what's missing); keep deletions as deliberate reviewed migrations. | ❌ Open |
| 5 | **No canonical `Permissions` catalog table.** `RolePermissions.PermissionKey` is free-text `varchar(100)` with no referential integrity — a typo'd or orphaned permission key inserts silently and just never matches anything. Pairs naturally with #4 (same reconciliation pass could populate/sync this table). | ❌ Open |
| 6 | **`AppointmentSummary` has no `visitId`.** Blocks the appointments UX fixes below (#9–11) — "open the existing visit" can't work without it. Needs a small Core-abstraction + ClinicalNotes-implementation pair, same shape as the existing `IAppointmentLinker`. | ✅ Fixed 2026-07-31 — `IVisitLookup` (Core, impl in ClinicalNotes), batch-queried in `GetAppointmentsHandler`/`GetAppointmentByIdHandler`. Verified live against real data. |

## Tier 3 — UX correctness bugs (users hit these today)

| # | Issue | Status |
|---|---|---|
| 7 | Appointments list: Reschedule/Reassign/Cancel still show when an appointment is `InProgress` — should be replaced by a single "Open" → straight to the visit. Depends on #6. | ❌ Open |
| 8 | Grid view: clicking any active-status block (including `InProgress`) always opens the Reschedule modal. Depends on #6. | ❌ Open |
| 9 | Waiting Room: `InProgress` → "View Visit" navigates to the generic consultations list, not the specific visit — same underlying gap as #7/#8. Depends on #6. | ❌ Open |
| 10 | Minor, found in passing: "Open Visit" currently shows for Scheduled/Confirmed too, not just CheckedIn — arguably was already loose before this request. Worth a decision, not urgent. | ❌ Open |

## Tier 4 — UX modernization (no correctness risk, quality-of-life)

| # | Issue | Status |
|---|---|---|
| 11 | Modal boilerplate (`ModalBackdrop`, `inputClass`, `labelClass`) duplicated across 6+ files instead of a shared component. | ❌ Open |
| 12 | Convert `NewPatientPage`, `PatientDetailPage`, `WalkInPage`, `NewAppointmentPage` from full pages to modals, following the modal pattern already established elsewhere in the codebase (`ChangePasswordModal`, `PatientDetailPage`'s own contact/address edit modals). | ❌ Open |
| 17 | **File upload/viewer UI doesn't exist on the frontend at all.** The backend (`Utano.Module.Files`) is fully built — Cloudflare R2, presigned URLs, all 4 CRUD endpoints, documented in `docs/file-storage.md` — but nothing in `utano-frontend` calls any of it. Needs: an upload dropzone (patient detail / consultation context), a document list, and a viewer modal (image `<img>`, PDF via `<iframe>` or a viewer component per the doc's own frontend-integration notes). Storage provider choice doesn't need revisiting — R2's zero-egress model is still the right fit for repeatedly-viewed medical images. | ❌ Open |

## Tier 5 — Deferred by explicit decision (not forgotten, just sequenced)

| # | Issue | Status |
|---|---|---|
| 13 | Broader audit trail — Billing, Inventory, Patients, Identity have none (only ClinicalNotes does). Bongani's call: do this last, once everything else on the active list is done. | ❌ Deferred |
| 14 | Phase 4 — actual WhatsApp/email sending. Blocked on #3 (tier enforcement) landing first, plus a provider integration pass. | ❌ Deferred |

## Tier 6 — Minor / fix opportunistically (low stakes, cheap when touching nearby code)

| # | Issue | Status |
|---|---|---|
| 15 | `CreatePracticeHandler`'s system-role list is hardcoded instead of reading `SystemRoles.All` — cosmetic duplication risk. | ❌ Open |
| 16 | `Visit.UpdateVitals()` is dead code — defined, never called from anywhere. | ❌ Open |

## Already fixed this session

| Issue | Fix |
|---|---|
| `ICurrentUserService.UserId`/`.Email` always resolved to `Guid.Empty`/`""` (JWT claim remapping) | `MapInboundClaims = false` |
| No global exception handling — every domain exception raw-500'd with a stack trace | `GlobalExceptionHandler : IExceptionHandler` |
| `Hangfire.Core` pulled a vulnerable `Newtonsoft.Json` transitively | Pinned to 13.0.3 directly |
| ClinicalNotes audit log missed diagnosis/treatment/prescription edits | `VisitClinicalNotesUpdatedEvent` added |
| Notifications: zero wiring, no tenant check, no RBAC descriptor, free-string type, no tests, no docs | All of Phase 1–3 of `docs/notifications.md` |

---

## Recommended order

1. **#2 (`IsSystem` guard)** — cheapest fix on this whole list, closes a real self-lockout risk, do it first almost regardless of anything else.
2. **#6 (`visitId` on appointments)** — small, unblocks the three UX bugs (#7–9) that are actively confusing to use every day.
3. **#7–9 (appointment UX bugs)** — quick once #6 lands.
4. **#1 (backend permission enforcement)** — biggest item, start it as a systematic pass (one shared authorization mechanism, applied module by module) rather than trying to land it all at once. Highest security value on the list.
5. **#4 + #5 (RBAC reconciliation + permissions catalog)** — natural to do alongside #1, since both touch the same authorization surface.
6. **#3 (tier enforcement)** — must land before Phase 4 specifically; doesn't need to block anything else.
7. **#11 + #12 (modal cleanup/conversion)** — no correctness risk, do whenever there's a lighter week.
8. **#10, #15, #16** — fold into whichever nearby task touches that file; not worth a dedicated pass.
9. **#13 (broader audit trail)** — last, as decided.
10. **#14 (Phase 4)** — after #3.
