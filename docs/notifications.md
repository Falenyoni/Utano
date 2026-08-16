# Notifications

## Overview

An in-app notification system already exists (`Utano.Module.Notifications`) — bell icon, unread count, mark-as-read. It is generic (recipient, sender, title, message, type, optional reference) but is **not wired to anything**. No other module creates a notification automatically; the only way one is created today is a manual `POST /api/notifications` call.

This doc captures the current state, the gaps found when reviewing the module for appointment-related use cases, and the open platform decisions for reaching patients (not just staff) outside the app.

---

## Current State

### Backend — `Utano.Module.Notifications`

| Endpoint | Method | Notes |
|---|---|---|
| `/api/notifications` | POST | Creates a notification for any `RecipientUserId`. `[Authorize]` only — no permission check, no verification the recipient is in the caller's practice. |
| `/api/notifications` | GET | Returns the caller's 30 most recent notifications. |
| `/api/notifications/unread-count` | GET | Badge count. |
| `/api/notifications/{id}/read` | POST | Mark one as read. |
| `/api/notifications/mark-all-read` | POST | Mark all as read. |

`Notification` entity: `RecipientUserId`, `SenderUserId`, `SenderName`, `Title`, `Message`, `Type` (free string), `ReferenceId` (Guid?, unused by the frontend), `IsRead`.

### Frontend — `features/notifications`

`NotificationBell` polls both the list and unread-count every 30s via React Query, shows a dropdown, and marks items read on click. Clicking never navigates anywhere — `ReferenceId` is stored but not used for deep-linking.

### Not present anywhere in the solution

- ~~No background job / scheduler infrastructure~~ — Hangfire now runs `AppointmentReminderScanJob` and `AppointmentNoShowScanJob` on recurring schedules (see `Utano.Module.Appointments/Configuration/AppConfiguration.cs`). The reminder job has nothing to actually send through yet, though — see below.
- No email or SMS provider *wired into code* yet, but the account/domain/credential setup is done — Resend is configured and ready (`docs/infrastructure-setup.md`). SMS/WhatsApp provider not yet chosen.
- No patient login. Per `docs/patient-portal.md`, patient auth (magic link) is a **designed but unbuilt** future phase — patients cannot receive an in-app notification today because they have no account to receive it on.

---

## Gaps Found

1. **Zero wiring from Appointments.** `BookAppointmentHandler`, `CancelAppointmentHandler`, `RescheduleAppointmentHandler`, `CheckInAppointmentHandler`, `ReassignAppointmentHandler` never touch `INotificationRepository`. Booking, moving, or cancelling an appointment notifies no one.
2. **Tenant/authorization gap.** `CreateNotificationEndpoint` doesn't check `RecipientUserId` belongs to the caller's practice, and the module doesn't implement `IModuleDescriptor` — every other module gates itself with `{module}.{action}` permissions; this one has none.
3. **No reminders possible yet** — needs a scheduler before "appointment in 24h" style notifications can exist at all.
4. **No patient-facing channel** — today's notifications are staff-to-staff only. Reaching patients needs both patient contact info at send-time (already present on `Patient`: phone/email) and an external provider, since patients aren't logged into anything.
5. **`Type` is a free string**, not an enum — fragile as more notification types get added (`AppointmentBooked`, `AppointmentCancelled`, `AppointmentReminder`, ...).
6. **No deep-linking** on click despite `ReferenceId` existing.
7. **No tests** for the module (every other module has a `Tests` project) and this doc itself fills a gap — no `docs/notifications.md` existed before.
8. Polling only, no push, for the staff-facing bell.

---

## Proposed Architecture (phased)

### Phase 1 — Harden what exists ✅ Built 2026-07-30
- Added `NotificationsModuleDescriptor` (`notifications.view`, granted to every role via a seed migration — it only ever gates a user's own notifications, so there's nothing to restrict per role).
- `CreateNotificationEndpoint`: verifies `RecipientUserId` belongs to the caller's practice via a new `IUserPracticeValidator` (Core abstraction, implemented in Identity — same "interface in Core, implementation in owning module" pattern as `IPatientStatusChecker`).
- `Type` converted from a free string to a `NotificationType` enum (`Domain/Enums/NotificationType.cs`), stored as a string via EF `HasConversion<string>()` so the DB column is unaffected. Endpoint DTOs still carry `Type` as a plain string on the wire (parsed/rendered at the boundary) — same convention `BookAppointmentCommand` already uses for `AppointmentType`.
- Frontend: clicking a notification with a `referenceId` and an `Appointment*` type now navigates to `/appointments` (there's no per-appointment detail route yet, so this is as far as "deep link" goes today).

### Phase 2 — Wire into Appointments (staff-facing, in-app only) ✅ Built 2026-07-30

**Wiring style: domain events, not a direct call.** `Appointment` implements a new opt-in `IHasDomainEvents` interface (Core) — deliberately *not* added to the shared `AggregateRoot` base class, since that would give every entity in every module an event queue whether it needs one or not. Only entities that choose to participate implement the interface.

- `Appointment.Book()/Reschedule()/Cancel()/Reassign()` each queue a domain event (`AppointmentBookedEvent`, `AppointmentRescheduledEvent`, `AppointmentCancelledEvent`, `AppointmentReassignedEvent` — all in `Core/Domain/Events/Appointments/`).
- A new `DomainEventDispatchInterceptor` (Core, EF `SaveChangesInterceptor`) runs after `AppointmentsDbContext` saves, publishes queued events via MediatR, then clears them. Registered per-module (`AddInterceptors(...)` in `AddAppointmentsModule`), so any other module (e.g. Billing) gets the same capability by implementing `IHasDomainEvents` on its own aggregate and registering the same interceptor on its own `DbContext` — no changes needed to the interceptor itself.
- `Utano.Module.Notifications` reacts via `INotificationHandler<T>` for each event (`Features/AppointmentEventHandlers/`), creating a notification for the affected doctor (both old and new doctor on reassignment). Each handler wraps its body in try/catch + logging — a notification failure must never surface as an error on the original booking/reschedule/cancel request, since by the time the event fires the appointment write has already committed.
- Chosen over a direct call from Appointments straight into `INotificationRepository` because Billing already carries `AppointmentId`/`VisitId` on invoices — a second consumer reacting to appointment lifecycle is a real near-term possibility, not a hypothetical. Events mean Billing (or anything else) can subscribe later without Appointments being touched at all; a direct call would need a new call site added to the handler for every new consumer.
- No message bus needed for this — it's all in-process, single deployable. A bus (or at minimum a background job) only becomes relevant once notifications mean an actual external send (Phase 4) that shouldn't block the request thread.

**Second adopter (2026-07-30):** `Utano.Module.ClinicalNotes` now uses the same `IHasDomainEvents`/`DomainEventDispatchInterceptor` infra for its audit log — `Visit.Complete()`/`Triage()` raise `VisitCompletedEvent`/`VisitTriagedEvent`, and `VisitCompletedAuditHandler`/`VisitTriagedAuditHandler` write the `AuditLog` entry instead of `CompleteVisitHandler`/`TriageVisitHandler` calling `IAuditService` inline. No changes were needed to the interceptor itself — confirms it's genuinely reusable across modules, not just built for this one case.

### Phase 3 — Reminders ✅ Built 2026-07-30

**Scheduler: Hangfire, not a bare `IHostedService`.** Chosen specifically because Phase 4 will call external APIs (email/SMS/WhatsApp) that can fail, rate-limit, or time out — Hangfire persists jobs to Postgres (the existing `UtanoDb`), gives retry-on-failure for free, survives restarts, and has a dashboard for "did that actually send" visibility. A hand-rolled timer would mean building that retry/backoff logic manually for exactly the failure modes external providers actually have. `/hangfire` dashboard is Development + localhost-only (`LocalRequestsOnlyAuthorizationFilter`) — the app's JWT-in-header auth doesn't carry over to a directly browser-navigated page, so a real "Admin-only" gate would need a cookie-based admin session; out of scope for now.

- `Appointment.RemindedAt` (nullable, new column) tracks whether a reminder already fired — makes the recurring scan idempotent regardless of run cadence. Cleared on `Reschedule()` so a moved appointment gets reminded again for its new time.
- `AppointmentReminderScanJob` (Hangfire recurring, every 15 min) queries appointments entering the window via `IAppointmentReadRepository.GetAppointmentsNeedingReminderAsync` — deliberately uses `IgnoreQueryFilters()` since a background job has no `HttpContext`/`ICurrentUserService.PracticeId` to scope to; it must see every practice, not one.
- Lead time is `AppointmentReminderSettings.HoursBefore` (config-bound, default 24) — a plain config value, not a structural decision, so it's configurable from day one rather than hardcoded (unlike the domain-events-vs-direct-call choice, there's no premature-abstraction argument against making a number configurable).
- `Appointment.MarkReminded()` raises `AppointmentReminderDueEvent` the same way every other appointment event does — the scan job calling `writeRepository.UpdateAsync()` triggers the *existing* `DomainEventDispatchInterceptor` on save, same as Booked/Cancelled/etc. No new dispatch mechanism needed.
- `AppointmentReminderNotificationHandler` (Notifications) reacts to it. Unlike the other appointment handlers, there's no real "actor" (background job, no `ICurrentUserService` identity) — the notification's `SenderName` is hardcoded `"System"` rather than trying to attribute it to a user.

**Preferences, so opt-in/out is real:** `NotificationPreference` (Notifications module) — one row per user, `InAppEnabled`/`EmailEnabled`/`SmsEnabled`/`WhatsAppEnabled` flags (defaults: in-app `true`, others `false` until Phase 4 exists), `ConsentRecordedAt` stamped the first time any external channel gets turned on. `GET/PUT /api/notifications/preferences` lets a user toggle these now, even though only `InAppEnabled` is actually consulted yet (`AppointmentReminderNotificationHandler` checks it, defaulting to `true` if no row exists). This is the seam Phase 4's email/SMS/WhatsApp handlers plug into later — each new channel is a new handler on the same `AppointmentReminderDueEvent` checking its own preference flag, not a rewrite of the scan job or the event.

**Known dependency note:** `Hangfire.Core` pulled in `Newtonsoft.Json` 11.0.1 transitively, which had a known high-severity advisory (`GHSA-5crp-9r3c-p9vr`) — pinned to 13.0.3 directly in `Utano.Module.Appointments.csproj` to resolve it (not just suppressed).

**Frontend (`utano-frontend`):** "Notification Preferences" added to the existing user avatar menu in `Navbar.tsx`, next to "Change Password" — same modal pattern, no new page/route. In-app toggle is live; Email/SMS/WhatsApp are shown but disabled with "Coming soon," so opting in doesn't silently do nothing without explanation.

### Phase 4 — Patient-facing reminders (external channel)
- Depends on Phase 3 (something has to trigger the send) and needs an actual delivery provider — see platform comparison below. This does **not** depend on the patient portal/login work (that's for patients to view/manage their own appointments); sending them a reminder only needs their phone/email, which `Patient` already stores.

---

## Integration Platform Options (open decision)

Nothing is chosen yet — flagging tradeoffs so we can decide before Phase 4.

### Staff-facing real-time (replaces 30s polling)
No external platform needed — **SignalR** (built into ASP.NET Core) would let the backend push new notifications to the bell instantly instead of polling. Purely a later nice-to-have, not a blocker for anything above.

### Email

| Provider | Notes |
|---|---|
| **Resend** | Modern API, simple, generous free tier — good default for a POC. |
| **Postmark** | Best-in-class deliverability reputation for transactional email; no meaningful free tier. |
| **SendGrid** (Twilio) | Mature, well-known, 100/day free tier, mature .NET SDK. |
| **AWS SES** | Cheapest at real scale; more setup overhead (domain verification, sandbox mode limits) — worth it only once volume justifies it. |

### SMS — dropped from scope (2026-07-30)

Bongani's call: SMS costs real money per message with no meaningful free tier on any provider, and patient messaging is generally moving to WhatsApp anyway. Not worth building. `NotificationPreference.SmsEnabled` still exists as a DB column (harmless, unused — not worth a migration to remove it) but the frontend no longer shows an SMS toggle at all, and `PUT /api/notifications/preferences` always sends `smsEnabled: false`.

### WhatsApp (the actual plan for patient-facing reminders)

**Meta's own WhatsApp Cloud API, not a reseller.** First 1,000 conversations/month are free *per practice* — at a single small clinic's reminder volume, this likely covers everything for $0. Africa's Talking/Twilio can also send WhatsApp (as resellers on top of Meta's API), but there's no reason to pay a markup when Meta's direct API has its own free tier and .NET has straightforward HTTP client support for it.

### Cost model — who pays

**Utano (the SaaS operator) pays the provider directly** — one shared Meta/Resend account across all practices, never "bring your own API key." Cost is absorbed into the existing subscription price, not metered separately:

- **Starter (free tier):** in-app + email. Both cost ~$0 at this scale regardless of provider, no reason to gate them.
- **Professional (paid tier):** adds WhatsApp. Even fully loaded, WhatsApp's free tier likely covers a small practice's entire volume — the marginal cost only shows up once a practice is large enough that the Professional subscription revenue already covers it many times over. Revisit metered/pass-through billing only if real usage data ever says otherwise (YAGNI applies to billing complexity the same as code).

**Gating built (2026-07-30):** the WhatsApp toggle in the frontend Notification Preferences modal is disabled with an upgrade prompt for Starter-tier practices (`subscription.tier !== 'Professional'`), shown as "Coming soon" for Professional practices since the channel itself isn't built yet. This is **frontend-only enforcement** — while wiring it, checked how every other Professional-gated module (Billing, Inventory, ClinicalNotes) enforces its tier restriction server-side, and found `IFeatureService.IsEnabledAsync` (the actual per-request gate) is never called anywhere in the backend. Tier gating is frontend-only across the *entire app* today, not a new gap introduced here — matched the existing convention rather than building a stricter one-off backend check for just this feature. Worth fixing app-wide later, alongside the permission-enforcement gap noted in `project_utano_poc` memory.

**Decided (2026-07-30):** WhatsApp via Meta's Cloud API directly (not a reseller) + Resend for email. SMS dropped entirely — see above.

---

## Open Questions

- Reminder timing is already configurable (`AppointmentReminders:HoursBefore`, default 24) — per-practice override not built, revisit if a practice actually asks.
- Meta's WhatsApp templates need pre-approval for outbound messages outside a user-initiated conversation window — factor the approval lead time into whenever Phase 4 actually gets scheduled.
- Server-side Professional-tier enforcement (`IFeatureService.IsEnabledAsync` is currently dead code app-wide) — worth fixing before WhatsApp sending goes live, since at that point a Starter practice bypassing the frontend gate would actually cost real money, unlike today's cosmetic-only Professional features.

---

## Implementation Order

1. Phase 1 hardening (descriptor, tenant check, enum, deep-link)
2. Phase 2 — wire notifications into the 5 Appointments handlers
3. Phase 3 — pick a scheduler, add a reminder job (staff-facing)
4. Phase 4 — pick a provider, send patient-facing reminders
