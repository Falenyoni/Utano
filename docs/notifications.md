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

- No background job / scheduler infrastructure (no Hangfire, no `IHostedService`, no recurring jobs) — required for any kind of reminder ("appointment tomorrow at 2pm").
- No email or SMS provider integration.
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

### Phase 3 — Reminders
- Requires picking a scheduler (`Hangfire` is the common .NET choice — persists jobs to Postgres, has a dashboard, or a lighter `IHostedService` + `PeriodicTimer` if we want to avoid a new dependency for a POC).
- A recurring job scans for appointments starting in the next N hours and creates a reminder notification (staff-facing first, since that needs no external provider).

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

### SMS / WhatsApp (matters more than email here — Zimbabwean patients are far more likely to have a phone number than a checked email inbox)

| Provider | Zimbabwe SMS reach | WhatsApp | Notes |
|---|---|---|---|
| **Africa's Talking** | Direct local carrier routes (Econet, NetOne, Telecel) | Yes | Purpose-built for African markets — likely the most reliable and cheapest way to actually land an SMS on a Zim number. Same regional-fit reasoning already applied to Indlela's payment provider choice (Flutterwave/Paynow over pure Stripe). |
| **Twilio** | Via international routes | Yes (WhatsApp Business API) | Global reach, very mature .NET SDK, but SMS deliverability/pricing into Zimbabwe is typically weaker than a local aggregator. |
| **Clickatell** | Yes | Limited | Long-standing Southern African SMS gateway. |

**Working recommendation:** don't decide this until Phase 4 is actually being built, but if forced to pick today — **Africa's Talking** for SMS/WhatsApp given carrier reach into Zimbabwe, with **Resend** as a cheap email fallback where a patient email is on file. Mirrors the "local provider for local reality" call already made for Indlela billing.

---

## Open Questions

- Do we want WhatsApp as the primary reminder channel instead of SMS? Adoption is high in Zimbabwe and it's often cheaper per message than SMS — but adds template-approval overhead with Meta.
- Should reminder timing be configurable per practice (e.g. 24h vs 2h before), or fixed for the POC?
- Opt-out — does a patient get any say in whether they receive SMS reminders, or is it implicit from booking?
- Who owns the provider account/billing for SMS once we're past POC stage?

---

## Implementation Order

1. Phase 1 hardening (descriptor, tenant check, enum, deep-link)
2. Phase 2 — wire notifications into the 5 Appointments handlers
3. Phase 3 — pick a scheduler, add a reminder job (staff-facing)
4. Phase 4 — pick a provider, send patient-facing reminders
