# Appointments — Domain Rules & Features

## Domain Rules

All booking invariants are enforced in `Appointment.Book()` and `Appointment.Reschedule()` in the domain entity. Application-layer validators (FluentValidation) duplicate the most common checks to return clean 400 responses before the domain is reached.

### 1. Past-date Guard

Appointments cannot be booked or rescheduled to a date before today.

**Enforcement:**
- `BookAppointmentValidator` — `.Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))` fires first, returns a validation error.
- `Appointment.Book()` — throws `UtanoDomainException("Appointment date cannot be in the past.")` as a domain-level safety net.
- `Appointment.Reschedule()` — same check applied when rescheduling to a new date.

**Exception — Bulk Import:**  
`BulkImportAppointmentsHandler` passes `allowPastDate: true` to `Appointment.Book()`. Bulk import is a trusted admin operation for migrating or seeding historical data; it explicitly bypasses this guard.

### 2. Double-booking Prevention

A doctor cannot have two overlapping appointments on the same date.

**Overlap condition:**  
`existingStart < newEnd && existingEnd > newStart`  
This catches all cases: exact same slot, partial overlaps, and new slot wrapping an existing one. Back-to-back slots (where one ends exactly when the next starts) are **not** blocked.

**Excluded statuses:**  
`Cancelled` and `Completed` appointments no longer hold their slot and are excluded from the conflict check.

**Enforcement — Booking:**  
`BookAppointmentHandler` calls `IAppointmentReadRepository.HasConflictAsync(practiceId, doctorId, date, startTime, endTime)` before calling `Appointment.Book()`. If a conflict exists, throws `UtanoDomainException("The doctor already has an appointment in that time slot.")`.

**Enforcement — Rescheduling:**  
`RescheduleAppointmentHandler` calls the same `HasConflictAsync`, passing `excludeAppointmentId: command.Id` so the appointment being moved is not considered a conflict with its own existing slot.

```csharp
// IAppointmentReadRepository
Task<bool> HasConflictAsync(
    Guid practiceId,
    Guid doctorId,
    DateOnly date,
    TimeOnly startTime,
    TimeOnly endTime,
    Guid? excludeAppointmentId = null,
    CancellationToken cancellationToken = default);
```

### 3. Other Invariants (existing)

- End time must be after start time.
- Patient must be active (`patientStatusChecker.IsActiveAsync`) — normal booking only, not bulk import.
- Cannot cancel a `Completed` or already `Cancelled` appointment.
- Cannot reschedule a `Completed` or `Cancelled` appointment.

---

## Bulk CSV Import

Allows staff to import many appointments at once from a CSV file, bypassing the normal per-patient validation (since bulk import uses name-based rows, not existing patient records).

### Backend

**Endpoint:** `POST /api/appointments/import`  
**Handler:** `BulkImportAppointmentsHandler`  
**Location:** `Utano.Module.Appointments/Features/Appointments/BulkImport/`

The handler:
- Accepts a list of `BulkImportRow` objects (patient name, doctor name, date, start/end time, type, notes).
- Generates a new `Guid` for both `PatientId` and `DoctorId` — these are placeholder IDs since the import is name-based.
- Skips `patientStatusChecker` — trusted import scenario.
- Passes `allowPastDate: true` — historical data is allowed.
- Collects per-row errors and returns a `BulkImportResult` with counts; a row failure does not abort the whole batch.

```csharp
public record BulkImportRow(
    string PatientName, string DoctorName,
    DateOnly AppointmentDate, TimeOnly StartTime, TimeOnly EndTime,
    string Type, string? Notes);

public record BulkImportResult(int Imported, int Failed, List<string> Errors);
```

### Frontend

**Component:** `ImportAppointmentsModal` (`src/features/appointments/ImportAppointmentsModal.tsx`)

Uses the shared `BulkUploadModal` component (same as patient and user imports). The modal submits one row at a time to `/api/appointments/import` so that per-row progress is shown in real time.

**Progress UI:**
- Idle → file drop zone + column reference + CSV template download
- Preview → first 5 rows in a table, validation error highlighting, row count
- Importing → animated progress bar (`done / total`), per-row ⏳ → ✅ / ❌ live status
- Done → green/amber summary banner

**Accepted file formats:** CSV, XLSX, XLS

**Expected columns:**

| Column | Required | Format |
|---|---|---|
| `PatientName` | ✅ | Full name |
| `DoctorName` | ✅ | Full name |
| `AppointmentDate` | ✅ | `YYYY-MM-DD` |
| `StartTime` | ✅ | `HH:MM` |
| `EndTime` | ✅ | `HH:MM` |
| `Type` | ✅ | See appointment types below |
| `Notes` | — | Free text |

**Appointment types:** `Consultation`, `FollowUp`, `Procedure`, `Vaccination`, `LabCollection`, `WalkIn`, `Other`

### Seed Data

Sample datasets are in `C:\POC\seed-data\`:

| File | Contents |
|---|---|
| `utano-patients.csv` | 20 seed patients |
| `utano-patients-additional.csv` | 30 additional patients (total 50) |
| `utano-appointments-bulk.csv` | 92 appointments for all 50 patients, 2026-07-01 to 2026-08-13 |

The appointments file covers all three doctors (`Dr. Tendai Moyo`, `Dr. Rudo Chikwanda`, `Dr. Farai Ncube`), all appointment types, and includes clinically realistic notes matched to each patient's conditions. No two appointments share the same doctor and time slot on the same date.

---

## Appointment Statuses

```
Scheduled → Confirmed → CheckedIn → InProgress → Completed
                                                ↘ NoShow
         → Cancelled (from any non-terminal status)
```

Cancellation requires a reason. Completed and Cancelled appointments are terminal — they cannot be rescheduled or cancelled again.
