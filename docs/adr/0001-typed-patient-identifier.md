# Typed patient identifier instead of nullable National ID

Patient registration required a Zimbabwean National ID unconditionally, which blocked newborns
(no ID issued yet), foreign patients (only a passport, never a National ID), and walk-ins without
ID on them at the time. The simpler fix — make `NationalId` nullable — was rejected because it
would erase real data: a foreign patient's passport number is a genuine, checkable identifier, not
an absence of one. Instead, `Patient` carries a typed `PatientIdentifier` (`NationalId` | `Passport`
| `Pending`), with a value present for the first two and absent for `Pending`. Duplicate-detection
runs whenever a real value is present (either type) and is skipped for `Pending`, since there's
nothing reliable to compare.
