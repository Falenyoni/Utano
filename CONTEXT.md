# Utano

Practice management SaaS for medical practices (Zimbabwe-focused) — appointments, clinical
visits, billing, inventory, and RBAC, multi-tenant per practice.

## Language

**Patient Identifier**:
The identifying document recorded for a patient at registration. Has a type — National ID,
Passport, or Pending — and a value, present for National ID and Passport, absent for Pending.
Exactly one identifier is recorded per patient; it is not a set of documents, just the one used to
identify them.
_Avoid_: National ID (as the general term — National ID is only one identifier type, not the
concept itself)

**Pending** (Patient Identifier type):
No identifying document was available at registration — covers a newborn awaiting a birth
certificate, a walk-in without ID on them at the time, and someone with no document at all, all the
same way. Staff may update it to a real National ID or Passport later; nothing requires them to.
_Avoid_: Unknown, missing, N/A, no ID
