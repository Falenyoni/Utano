# Patient Portal

## Overview

A patient-facing portal allowing patients to view their profile, manage appointments, and update their contact details — without access to any staff, clinical, or billing data.

---

## Current State

- Rescheduling by **staff** is already implemented (`RescheduleAppointment` feature + Appointments page modal). ✅
- Patients currently have **no login credentials**. The `Patient` entity is a clinical record only — no `passwordHash`, no auth.
- All existing API endpoints are staff-scoped (require a staff JWT).

---

## Decisions

### 1. Deployment — Same app, separate route section

The patient portal lives under `/portal/...` within the existing Utano frontend.  
A separate layout (no sidebar, no staff nav — minimal branded header only) is rendered for all `/portal` routes.

A fully separate app is cleaner long-term but doubles the maintenance surface for a POC. Revisit when the portal grows.

### 2. Authentication — Magic link (phase 1), password login (phase 2)

**Phase 1 — Magic link**  
When an appointment is booked, the system emails the patient a secure, time-limited link (`/portal/auth?token=...`).  
Clicking the link authenticates them for that session — no password required.  
Appropriate for infrequent use (appointment management, not a daily app).

**Phase 2 — Password login** _(deferred)_  
Add `PasswordHash` + `PasswordResetToken` to the `Patient` entity.  
Add registration, forgot-password, and email-verification flows.  
Build when patient return usage justifies the complexity.

### 3. What patients can see and do

| Area | Access |
|---|---|
| Their own profile (name, DOB, contact, address) | View + Edit contact/address only |
| Upcoming appointments | View + Reschedule + Cancel |
| Past appointment dates | View (date and doctor name only) |
| Clinical notes / diagnoses | ❌ Not accessible |
| Billing / invoices | ❌ Not accessible |
| Other patients' data | ❌ Blocked at API level |
| Staff data | ❌ Blocked at API level |

---

## Architecture

### Backend

**New entity fields on `Patient`** _(phase 2 only)_
```
PasswordHash       string?
PasswordResetToken string?
PasswordResetExpiry DateTimeOffset?
```

**New feature module: `PatientAuth`** (inside `Utano.Module.Patients` or a new `Utano.Module.PatientPortal`)

| Endpoint | Method | Description |
|---|---|---|
| `/api/portal/auth/magic-link` | POST | Request a magic link (email input) |
| `/api/portal/auth/verify` | GET | Verify token, return patient JWT |
| `/api/portal/auth/login` | POST | _(phase 2)_ Password login |
| `/api/portal/me` | GET | Return own patient profile |
| `/api/portal/me` | PUT | Update contact details / address |
| `/api/portal/appointments` | GET | Own upcoming + past appointments |
| `/api/portal/appointments/:id/reschedule` | PUT | Reschedule own appointment |
| `/api/portal/appointments/:id/cancel` | PUT | Cancel own appointment |

**JWT claims for portal token**
```
role       = "Patient"
patientId  = "<guid>"
practiceId = "<guid>"
```

Middleware checks `role == "Patient"` and enforces that all queries are filtered to `patientId` from the token claim — patients can never query another patient's records.

### Frontend

**New routes (same Vite app)**
```
/portal/login          — magic link request page
/portal/auth           — token verification + redirect
/portal/dashboard      — upcoming appointments overview
/portal/profile        — view + edit own details
/portal/appointments   — full appointment list + reschedule/cancel
```

**New layout component**: `PortalLayout` — minimal header with practice logo and branding, no sidebar, logout button only.

**Shared API client**: Reuses `apiFetch` but with a separate localStorage key for the portal JWT (`utano_patient_auth`) so staff and patient sessions don't collide if used on the same device.

---

## Implementation Order

1. Backend: Patient JWT auth (magic link endpoint + token verification)
2. Backend: `/api/portal/me` GET + PUT
3. Backend: `/api/portal/appointments` GET + reschedule/cancel
4. Frontend: `PortalLayout` + `/portal/login` + `/portal/auth` pages
5. Frontend: Dashboard + Profile + Appointments pages
6. Integration: Trigger magic link email on appointment booking
7. _(Phase 2)_ Password login + forgot password flow

---

## Open Questions

- Which email provider will send the magic link? (SMTP, SendGrid, etc.)
- Should patients be able to **cancel** appointments or only reschedule? (Clinic policy decision)
- How far in advance can a patient reschedule? (e.g. no changes within 24 hours)
- Should past visit summaries (doctor name + date only, no clinical notes) be visible to patients?
- Is the portal public-facing or invitation-only (patients only get access when booked)?
