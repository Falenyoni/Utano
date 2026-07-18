# Utano — Healthcare Practice Management System (Backend)

Utano is a multi-tenant, modular monolith REST API for managing private medical practice operations.
It covers the full patient journey: registration → appointment → triage → consultation → dispensing → billing.

---

## Documentation

| Doc | Contents |
|---|---|
| [docs/file-storage.md](docs/file-storage.md) | Cloudflare R2 integration, presigned URL flow, module structure, setup guide |
| [docs/appointments.md](docs/appointments.md) | Domain rules (past-date guard, double-booking), bulk CSV import, status transitions |
| [docs/pdf-exports.md](docs/pdf-exports.md) | Invoice PDF and reports PDF — layout, libraries, key implementation patterns |
| [docs/patient-portal.md](docs/patient-portal.md) | Patient-facing portal design decisions and planned architecture |

---

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | .NET 10, ASP.NET Core Web API |
| Database | PostgreSQL via EF Core 10 |
| ORM Patterns | Global query filters (multi-tenancy), `HasConversion<string>()` for enums |
| CQRS | MediatR 14 — `IRequest` / `IRequestHandler` |
| Validation | FluentValidation (registered per module) |
| Auth | JWT Bearer (Identity module) |
| API Docs | Swashbuckle / Swagger |

---

## Architecture

### Modular Monolith — Vertical Slices

The solution is a single deployable host (`src/TemplateApi`) that registers independent modules.
Each module is a class library with no direct references to other modules.

```
src/
  TemplateApi/                        <- Host (Program.cs, AppConfiguration.cs)
  Modules/
    Utano.Module.Appointments/
    Utano.Module.Billing/
    Utano.Module.ClinicalNotes/
    Utano.Module.Doctors/
    Utano.Module.Files/
    Utano.Module.Identity/
    Utano.Module.Inventory/
    Utano.Module.Patients/
  Shared/
    Utano.Module.Core/                <- Shared interfaces (ICurrentUserService, IAuditService)
    Utano.Shared.Models/              <- Shared DTOs
```

### Feature Slice Structure

Every feature lives in one folder and is self-contained:

```
Features/FeatureName/
  FeatureNameEndpoint.cs    <- Controller: route, [Authorize], ProducesResponseType
  FeatureNameCommand.cs     <- IRequest record + IRequestHandler in the same file
```

Handlers use the DbContext directly. No generic repository abstraction — EF Core's DbContext is already a unit of work.

### Multi-Tenancy

Every DbContext applies a global query filter on `PracticeId`:

```csharp
modelBuilder.Entity<Patient>().HasQueryFilter(p => p.PracticeId == _currentUser.PracticeId);
```

`ICurrentUserService` is injected into every DbContext constructor and resolved from the JWT claims on each request. No query ever leaks data across practices. The `PracticeId` is a Guid claim embedded in the JWT token at login.

---

## Clinical Workflow

The core patient journey through the system:

```
1. RECEPTION — Patient arrives
   |
   +-- Opens a Visit  POST /api/visits
       Status: InProgress

2. NURSE — Triage
   |
   +-- Records vitals + chief complaint  PUT /api/visits/{id}/triage
       Status: InProgress -> Triaged
       Fields: BP systolic/diastolic, weight (kg), height (cm),
               temperature (C), pulse (bpm), O2 saturation (%), chief complaint

3. DOCTOR — Consultation
   |
   +-- Records clinical notes  PUT /api/visits/{id}
   |   Fields: symptoms, diagnosis, treatment, prescription notes, department
   |
   +-- Adds prescriptions  POST /api/visits/{id}/prescriptions
   |   - BillAndDispense: links to inventory stock item -> billing line item -> dispensary queue
   |   - External: patient fills at own pharmacy, no inventory/billing impact
   |
   +-- Completes the visit  PUT /api/visits/{id}/complete
       Status: Triaged -> Completed
       Auto-creates invoice via Billing module

4. DISPENSER — Dispensary queue
   |
   +-- Sees pending BillAndDispense prescriptions  GET /api/clinical/dispensary
   +-- Marks each dispensed  PUT /api/visits/{visitId}/prescriptions/{id}/dispense
       Deducts quantity from inventory stock

5. BILLING — Invoice management
   |
   +-- Invoice auto-created on visit completion
   +-- Payments recorded, payment plans supported
   +-- Medical aid claims tracked per invoice
```

### Visit Status Transitions

```
InProgress  ->  Triaged  ->  Completed
```

- **InProgress** — visit opened, triage not yet done
- **Triaged** — nurse has recorded vitals; doctor can now proceed
- **Completed** — visit finished; no further edits allowed on any section

Status is stored as a `string` in PostgreSQL (`HasConversion<string>()`), so adding new values to the C# enum requires no column migration.

---

## Modules

### Identity (`Utano.Module.Identity`)

Authentication. Issues JWT tokens with `PracticeId`, `UserId`, `Role`, `FullName`, `Email` as claims.

**Endpoints:**
- `POST /api/auth/login` — email + password -> JWT
- `POST /api/auth/register-practice` — first-time practice and admin user setup

**Outstanding:** Currently one user = one practice. Multi-practice support is planned (see Outstanding section).

---

### Patients (`Utano.Module.Patients`)

Patient registry. All patients are scoped to the practice.

**Entities:** `Patient`, `PatientContact`, `PatientAddress`

**Endpoints:**
- `GET /api/patients` — paginated list with name/ID search
- `POST /api/patients` — register patient
- `GET /api/patients/{id}` — full detail (contacts, addresses, medical history)
- `PUT /api/patients/{id}` — update demographics and medical history
- `POST /api/patients/{id}/contacts` — add contact (type, phone, email, isPrimary)
- `PUT /api/patients/{id}/contacts/{contactId}` — edit contact
- `POST /api/patients/{id}/addresses` — add address (type, street, suburb, city, country, isPrimary)
- `PUT /api/patients/{id}/addresses/{addressId}` — edit address
- `PUT /api/patients/{id}/activate`
- `PUT /api/patients/{id}/deactivate`

**Decisions:**
- Multiple contacts and addresses per patient; one is `IsPrimary`
- Adding a new primary contact/address demotes the previous one automatically via `ExecuteUpdateAsync` (single round-trip, no load-then-update)
- Medical history (blood group, allergies, chronic conditions) lives on `Patient` directly — no separate table for these scalar values
- `medicalAidId` / `medicalAidName` are stored on the patient for defaulting into new invoices

---

### Appointments (`Utano.Module.Appointments`)

Scheduling and waiting room.

**Entity:** `Appointment`

**Types:** `Scheduled`, `WalkIn`

**Statuses:** `Scheduled -> Confirmed -> InProgress -> Completed | Cancelled | NoShow`

**Endpoints:**
- `GET /api/appointments` — paginated, filterable by date, status, doctor
- `GET /api/appointments/{id}`
- `POST /api/appointments` — book (patient, doctor, date, start/end time, type, notes)
- `PUT /api/appointments/{id}/reschedule` — new date, start time, end time
- `PUT /api/appointments/{id}/cancel` — requires cancellation reason

**Decisions:**
- Walk-ins use the same entity with `Type = WalkIn`; reception opens a Visit directly without needing a separate check-in step
- The waiting room view is a filtered appointments query (`date=today, status=Scheduled,InProgress`) — not a separate entity

---

### ClinicalNotes (`Utano.Module.ClinicalNotes`)

The largest module. Visits, triage, consultations, prescriptions, dispensary queue, and audit log.

**Entities:** `Visit`, `Prescription`, `AuditLog`

#### Visits

**Endpoints:**
- `GET /api/visits` — paginated, filterable by patient, doctor, date
- `GET /api/visits/{id}` — full detail including vitals and clinical notes
- `POST /api/visits` — open visit
- `PUT /api/visits/{id}/triage` — nurse records vitals + chief complaint; advances status to Triaged
- `PUT /api/visits/{id}` — doctor records clinical notes only (no vitals)
- `PUT /api/visits/{id}/complete` — complete visit; fires invoice creation

**Decision — triage and consultation as separate endpoints:**
Previously both vitals and clinical notes were in one `PUT /api/visits/{id}`. Split because:
- Different staff roles act on each section at different times in the workflow
- The UI needed independent save buttons
- Splitting enables role-based enforcement per endpoint in the future
- The `Visit.Triage()` domain method validates that a completed visit cannot be re-triaged

#### Prescriptions

**Types:**
- `BillAndDispense` — from practice stock; goes into the dispensary queue; creates invoice line item on completion
- `External` — patient fills at their own pharmacy; no inventory or billing impact

**Statuses:** `Pending -> Dispensed`

**Endpoints:**
- `GET /api/visits/{visitId}/prescriptions`
- `POST /api/visits/{visitId}/prescriptions`
- `PUT /api/visits/{visitId}/prescriptions/{id}/dispense`
- `DELETE /api/visits/{visitId}/prescriptions/{id}` — only while Pending

#### Dispensary Queue

- `GET /api/clinical/dispensary` — all pending `BillAndDispense` prescriptions across all visits, joined with visit context

**Decision:** The dispensary is a read-only projection (not a separate entity). It joins `Prescriptions` with `Visits`. The visit side of the join uses `IgnoreQueryFilters()` — prescriptions are already scoped by `PracticeId`; applying the filter to both sides would break the join with no security benefit.

The dispensary queue auto-refreshes every 30 seconds on the frontend.

#### Audit Log

**Entity:** `AuditLog` — schema `clinical.AuditLogs`

**Fields:** `Id`, `PracticeId`, `UserId`, `UserName`, `EntityType`, `EntityId`, `Action`, `Description`, `Timestamp`

**Currently logged:**
- Visit triaged (action: `Triaged`)
- Visit completed (action: `Completed`)

**Interface:** `IAuditService` is defined in `Utano.Module.Core.Services` so any module can inject it without creating a dependency on ClinicalNotes. Implementation (`AuditService`) lives in `ClinicalNotes.Infrastructure`.

**Endpoint:**
- `GET /api/admin/audit-log` — paginated, filterable by entityType, entityId, action, dateFrom, dateTo

**Decision — explicit audit logging, not via EF interceptors:**
Only significant clinical events should appear in the audit log. Interceptor-based logging would log every database write, creating noise and making the log unusable for clinical review.

> **Action required — pending migration:**
> The `AuditLog` table must be created before the audit log endpoints will work.
>
> In Package Manager Console:
> ```
> Add-Migration AddAuditLog -Project Utano.Module.ClinicalNotes -StartupProject TemplateApi
> Update-Database -Project Utano.Module.ClinicalNotes -StartupProject TemplateApi
> ```

---

### Billing (`Utano.Module.Billing`)

Invoice lifecycle and payment tracking.

**Entities:** `Invoice`, `InvoiceLineItem`, `Payment`, `PaymentPlan`

**Invoice statuses:** `Draft -> Issued -> PartiallyPaid -> Paid | Void`

**Endpoints:**
- `GET /api/billing/invoices` — paginated, filterable by patient, status, date range
- `GET /api/billing/invoices/{id}` — detail with line items and payments
- `POST /api/billing/invoices` — create invoice manually (patient, optional medical aid)
- `POST /api/billing/invoices/{id}/line-items` — add line item
- `DELETE /api/billing/invoices/{id}/line-items/{lineItemId}`
- `POST /api/billing/invoices/{id}/issue` — move to Issued
- `POST /api/billing/invoices/{id}/payments` — record payment
- `POST /api/billing/invoices/{id}/payment-plan` — set up installments
- `POST /api/billing/invoices/{id}/void`
- `GET /api/billing/reports` — revenue summary

**Decision — auto-invoice on visit completion:**
Completing a visit triggers invoice creation with line items for all `BillAndDispense` prescriptions.
This is done via a billing service interface injected into the ClinicalNotes module — not a direct project reference between modules.

Medical aid name is denormalised onto the invoice so list views require no join to the medical aids table.

---

### Inventory (`Utano.Module.Inventory`)

Stock management for dispensable items (medications, consumables, equipment).

**Entities:** `StockItem`, `StockTransaction`

**Transaction types:** `Received`, `Dispensed`, `Adjusted`

**Endpoints:**
- `GET /api/inventory/stock` — paginated, filterable by category, low stock flag, active, search
- `GET /api/inventory/stock/{id}` — detail with recent transactions
- `POST /api/inventory/stock` — create item (name, SKU, category, unit, cost/selling price, reorder level)
- `PUT /api/inventory/stock/{id}` — update item details
- `POST /api/inventory/stock/{id}/receive` — receive stock (increases on-hand quantity)
- `POST /api/inventory/stock/{id}/adjust` — manual correction (positive or negative)
- `PUT /api/inventory/stock/{id}/deactivate`

**Decision:** `quantityOnHand` is a computed property from the transaction ledger, not a stored column. This makes the full stock movement history auditable. Low stock is computed as `quantityOnHand <= reorderLevel`.

When a prescription is dispensed, the `Dispensed` stock transaction is created by the inventory service, keeping the stock ledger consistent with clinical activity.

---

### Files (`Utano.Module.Files`)

Patient file attachments stored on Cloudflare R2. Files are uploaded directly from the browser via presigned PUT URLs — the server never proxies file bytes.

**Supported types:** JPEG, PNG, WebP, GIF, PDF, DICOM. Max 50 MB per file.

**Attachment types:** `XRay`, `LabResult`, `ClinicalNote`, `Referral`, `Prescription`, `ConsentForm`, `InsuranceDocument`, `Other`

**Endpoints:**
- `POST /api/files/upload-url` — register metadata + return a 5-min presigned PUT URL for direct browser upload
- `GET /api/files/{id}/url` — return a 60-min presigned GET URL for download/display
- `GET /api/files?patientId={}&type={}` — list file metadata for a patient
- `DELETE /api/files/{id}` — soft-delete record + remove object from R2

See [docs/file-storage.md](docs/file-storage.md) for the full integration guide, upload flow, and R2 setup.

---

### Doctors (`Utano.Module.Doctors`)

Staff registry. Used to populate doctor/staff selectors.

**Endpoints:**
- `GET /api/doctors`
- `POST /api/doctors`

**Outstanding:** Will evolve into a full Staff + Roles module when RBAC is implemented.

---

### Core Shared (`Utano.Module.Core`)

Shared abstractions only. No business logic, no DbContext.

- `ICurrentUserService` — `PracticeId`, `UserId`, `FullName`, `Email`, `Role`
- `IAuditService` — `LogAsync(entityType, entityId, action, description?, ct)`
- `UtanoDomainException` — domain rule violation base exception
- Pagination helpers

---

## Database Schema Layout

| Module | PostgreSQL Schema |
|---|---|
| Patients | `patients.*` |
| Appointments | `appointments.*` |
| ClinicalNotes | `clinical.*` |
| Billing | `billing.*` |
| Inventory | `inventory.*` |
| Identity | `identity.*` |

No cross-schema joins in queries. Cross-module data (e.g. patient name on an invoice) is denormalised at write time.

---

## Key Decisions Summary

| Decision | Rationale |
|---|---|
| Modular monolith over microservices | Team size and operational simplicity; modules can be extracted later |
| MediatR in every module | Decouples HTTP endpoint from business logic; easy to test handlers in isolation |
| No generic repository pattern | EF Core DbContext is already a unit of work; a wrapper adds abstraction with no benefit at this scale |
| Triage and consultation as separate endpoints | Nurse and doctor act at different times; enables role enforcement per endpoint |
| Explicit audit logging (not interceptors) | Only clinical events of significance should appear; automated logging creates unusable noise |
| Dispensary as a projection | Derived from prescriptions — no extra entity or sync job needed |
| Enums stored as strings | Adding new status values costs zero migrations; values are human-readable in the DB |
| `IAuditService` in Core | Any module can emit audit events without depending on ClinicalNotes |
| Medical aid name denormalised | Avoids join on every invoice list query; kept in sync at write time |

---

## RBAC — Permission-Based Access Control

### Decision

We do not use `[Authorize(Roles = "...")]` on controllers. Instead we use a **permission-based system** where roles define defaults but the admin controls who has what at runtime via a UI — no deployment required to change access.

The core principle: the developer defines what permissions exist and what each endpoint requires. The admin decides which roles (or individual users) are granted those permissions.

### Why Not Hardcoded Roles on Controllers

`[Authorize(Roles = "Admin,Nurse")]` works but has a hard limitation: any change to who can do what requires a code change and deployment. In a clinical environment, access needs can change (e.g. a solo GP who triages their own patients, a receptionist who should temporarily cover billing) and the practice admin should be able to handle this without involving a developer.

### Data Model

```
Permission              RolePermission              UserPermission
-----------             --------------              --------------
Id (Guid)               RoleId (string)             UserId (Guid)
Name (string, unique)   PermissionId (Guid, FK)     PermissionId (Guid, FK)
Description             GrantedAt                   GrantedAt
                        GrantedByUserId             GrantedByUserId
```

- **Permission** — seeded from code on startup. The developer defines the permission names; they cannot be created by the admin.
- **RolePermission** — admin-editable mapping. Seeded with sensible defaults (e.g. Nurse gets `visits.triage`, Doctor gets `visits.consult` and `visits.complete`).
- **UserPermission** — optional per-user grants that override or extend the role defaults (e.g. a doctor in a solo practice also gets `patients.register`).

### Permission Names (Defined in Code)

```
patients.read
patients.register
patients.edit

appointments.read
appointments.book
appointments.manage          <- reschedule, cancel

visits.open
visits.triage
visits.consult               <- update clinical notes
visits.complete
visits.prescribe

dispensary.view
dispensary.dispense

billing.view
billing.manage               <- create invoices, record payments

inventory.view
inventory.manage             <- add items, receive stock, adjust

reports.view
audit.view
settings.manage
```

### Role Defaults (Seeded, Admin Can Change)

| Permission | Admin | Receptionist | Nurse | Doctor | Dispenser |
|---|---|---|---|---|---|
| patients.read | ✓ | ✓ | ✓ | ✓ | — |
| patients.register / edit | ✓ | ✓ | — | — | — |
| appointments.read | ✓ | ✓ | ✓ | ✓ | — |
| appointments.book / manage | ✓ | ✓ | — | — | — |
| visits.open | ✓ | ✓ | — | — | — |
| visits.triage | ✓ | — | ✓ | ✓ | — |
| visits.consult | ✓ | — | — | ✓ | — |
| visits.complete | ✓ | — | — | ✓ | — |
| visits.prescribe | ✓ | — | — | ✓ | — |
| dispensary.view / dispense | ✓ | — | — | — | ✓ |
| billing.view / manage | ✓ | ✓ | — | — | — |
| inventory.view / manage | ✓ | — | — | — | ✓ |
| reports.view | ✓ | ✓ | — | ✓ | — |
| audit.view | ✓ | — | — | — | — |
| settings.manage | ✓ | — | — | — | — |

Doctor has `visits.triage` by default because in small or solo practices the doctor triages their own patients. A practice with a dedicated nurse can leave this as-is — the nurse also has it and will always do it first in practice.

### How It Works at Runtime

```
1. On startup
   └─ All Permission records seeded from code (if not already present)
   └─ Default RolePermission mappings seeded (if not already present)

2. Admin UI (/settings/permissions)
   └─ Shows all permissions grouped by feature
   └─ Toggle per role (checkboxes)
   └─ Optional: grant directly to a specific user

3. User logs in
   └─ Identity module resolves effective permissions:
      role defaults + user-level overrides
   └─ Permission list embedded in JWT as a claim

4. Request arrives at endpoint decorated with [RequirePermission("visits.triage")]
   └─ Custom IAuthorizationHandler reads permissions claim from JWT
   └─ Allows or rejects — no DB hit per request
```

### Endpoint Decoration

Controllers use a custom attribute instead of `[Authorize(Roles = "...")]`:

```csharp
[RequirePermission("visits.triage")]
public async Task<IActionResult> Triage(...) { ... }
```

The attribute triggers a registered `IAuthorizationRequirement` + `IAuthorizationHandler` that reads the `permissions` JWT claim.

### Stale Permissions

Permissions are embedded in the JWT at login. If an admin changes permissions, existing token holders do not see the change until their token expires.

**Decision: short token expiry matching a work shift (8 hours).** At the start of each shift, staff log in and get a fresh token with current permissions. This is acceptable for a clinical practice environment where permission changes are rare and not time-critical within a session. A server-side permission cache (Redis) can be added later if finer control is needed.

### Implementation Checklist

- [ ] `Permission` entity + seeder in Identity module
- [ ] `RolePermission` entity + default seeder
- [ ] `UserPermission` entity (optional grants per user)
- [ ] Permission resolution service — merges role defaults + user overrides
- [ ] Embed permissions list in JWT at login
- [ ] `RequirePermissionAttribute` custom attribute
- [ ] `PermissionAuthorizationHandler` implementing `IAuthorizationHandler`
- [ ] Register policy in `AppConfiguration`
- [ ] Replace any existing `[Authorize(Roles = "...")]` with `[RequirePermission("...")]`
- [ ] Admin UI: `/settings/permissions` page (see frontend README)

---

## Outstanding Work

### Must Do Before Production

| Item | Detail |
|---|---|
| AuditLog migration | Run `Add-Migration AddAuditLog` and `Update-Database` for ClinicalNotes module |
| Permission-based access control | Design finalised — see RBAC section above. Not yet implemented. Replaces static `[Authorize(Roles="...")]` with a dynamic `[RequirePermission("...")]` system where the admin grants access at runtime without deployments. |
| Input validation | FluentValidation registered but validators not written for all endpoints |
| Error handling consistency | Some endpoints return raw exceptions; all should return RFC 7807 problem details |

### Planned Features

| Feature | Detail |
|---|---|
| Multi-practice support | Owner with N practices logs in once, selects a practice context, switches without re-logging in. Requires `UserPractice` join table with role per practice, login returning practice list, scoped JWT per selected practice |
| Medical Aid Claims | Track claim submission, status, and response per invoice |
| Full Reports module | Revenue by doctor / period / medical aid; patient visit frequency; stock turnover |
| Appointment reminders | SMS or email to patients before scheduled appointments |
| Document uploads | ✅ Scaffolded — see `Utano.Module.Files` and [docs/file-storage.md](docs/file-storage.md). Needs frontend integration into visit/patient detail pages. |
| Notification system | Alert dispenser when a new BillAndDispense prescription is added; alert reception when visit is completed |
| Password reset / user management | Self-service password change; admin can reset staff passwords |
| Patient portal | Patients view their own visit history, invoices, prescriptions |
| Unit and integration tests | No tests written yet; EF Core integration tests against a real DB are the priority |
| Cross-practice reporting | For practice owners — revenue and patient totals aggregated across all their practices |

---

## Design Decisions — Pending Implementation

### Doctor External Hospital Sessions

**Problem:** Doctors registered under a practice also consult at external hospitals (e.g., private ward rounds, surgical assists). They need to bill those hospitals and track payment — this is a separate financial relationship from patient billing.

**Decision: Session-based model (not per-patient)**
External hospital work is logged as a session, not patient-by-patient. Hospitals pay per session or procedure list — registering individual patients from a hospital ward round adds friction with no billing benefit.

**Entities to add:**
- `ExternalFacility` — name, type (Hospital/Clinic), billing contact, address. Managed under Settings.
- `DoctorSession` — doctor, facility, date, session type (WardRound/SurgicalAssist/OutpatientClinic), patient count, fee, notes.
- `FacilityInvoice` — same structure as a patient invoice but `PayerType = Facility`, linked to `ExternalFacilityId`. Line items are session fees, not per-patient charges.
- Extend `Claims` module: add `ClaimType` enum (`MedicalAid` | `Facility`). Facility claims track submission to the hospital, approval, and payment.

**What stays the same:** All regular patient invoices and medical aid claims are unaffected. This is an additive module.

**Status:** Discussed — not yet designed in detail. Decision pending: confirm session-based approach with users before building.

---

### Billing — Discounts and Write-offs

**Current state:** Per-line-item `discountPercent` exists on `InvoiceLineItem`. No invoice-level discount. No write-off mechanism.

**Gaps identified:**

| Gap | Description |
|---|---|
| Invoice-level discount | Apply a percentage or fixed amount off the whole invoice (e.g., staff discount, pensioner rate, hardship) |
| Discount reason | Mandatory audit field — why was the discount applied? (Staff / Courtesy / Hardship / Dispute settlement) |
| Write-off | Separate action to void an uncollectable balance. Hits a bad-debt ledger, not a regular discount. Requires mandatory reason and should be irreversible. |

**Fields to add to `Invoice`:**
```
InvoiceDiscountPercent  decimal?
InvoiceDiscountAmount   decimal?   -- only one of percent/amount applies
DiscountReason          string?
IsWrittenOff            bool
WriteOffReason          string?
WriteOffDate            DateTime?
WriteOffByUserId        Guid?
```

**Business rule:** `InvoiceDiscountPercent` and `InvoiceDiscountAmount` are mutually exclusive. The calculated `TotalAmount` reflects the invoice-level discount applied after line item subtotals.

**Status:** Discussed — not yet implemented.

---

### Medical Aid Shortfalls and Reconciliation

**Problem:** Medical aids rarely pay the full claimed amount. Practices need to:
1. Know what the medical aid actually paid vs what was claimed
2. Transfer the unpaid portion (shortfall) to the patient's balance or write it off
3. Process bulk payments from Remittance Advice (RA) documents without opening each invoice individually

**Current state:** The system tracks `MedAidClaimAmount` and payments tagged `MedicalAid`. Shortfall is displayed as `MedAidClaimAmount - MedAidPaymentsTotal` but has no explicit workflow — it sits as an unpaid amount with no owner.

**Gaps:**

| Gap | Description |
|---|---|
| Shortfall owner | When medical aid underpays, the balance must be explicitly assigned: bill to patient, write off, or dispute |
| Rejection workflow | `ClaimRejected` → reason recorded → options: bill patient / write off / dispute & resubmit |
| Bulk RA processing | Medical aids send one Remittance Advice covering many claims. Need a single screen to match and post all payments at once |
| Dispute tracking | Rejected or underpaid claims can be disputed with the medical aid. Need `Disputed` status and resubmission date |

**Entities to add:**

- `RemittanceAdvice` — date, medical aid, total paid, reference number, imported by user
- `RemittanceAdviceLine` — links to `Invoice`, amount claimed, amount approved, rejection reason, status
- `InvoiceShortfallAction` — per invoice: `BilledToPatient` | `WrittenOff` | `Disputed`, date, notes

**Workflow:**
```
RA received from medical aid
  └─ Enter/import RA lines
  └─ System matches lines to submitted claims by invoice reference
  └─ Bulk post: records MedicalAid payment per matched invoice
  └─ For each shortfall: prompt "Bill to patient / Write off / Dispute"
  └─ BillToPatient → shortfall amount added to patient's balance (new line item or balance transfer)
  └─ WrittenOff → balance cleared, reason recorded
  └─ Disputed → status set, follow-up date set, RA line marked for resubmission
```

**Status:** Discussed — design agreed. Not yet implemented. Priority: after invoice-level discounts are done.
