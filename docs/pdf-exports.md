# PDF Exports

PDF generation runs entirely in the browser using `jspdf` and `jspdf-autotable`. No server involvement — the PDF is generated client-side and downloaded directly.

**Libraries:**
- `jspdf` v4 — `import { jsPDF } from 'jspdf'`
- `jspdf-autotable` — `import autoTable from 'jspdf-autotable'`

---

## Invoice PDF

**Location:** `src/features/billing/InvoiceDetailModal.tsx`  
**Trigger:** `↓ PDF` button in the invoice modal header

### Layout

```
[Practice Logo]          [Practice Name         ]
                         [Address               ]
                         [Phone · Email         ]
                         [AdHoc No · BP No      ]
─────────────────────────────────────────────────
INVOICE                  #INV-0001
                         Date: 2026-07-18
                         Due:  2026-08-17
BILLED TO
Patient Name
Dr. Doctor Name

┌────────────┬──────┬─────┬───────────┬───────┬──────────┐
│ Description│ Type │ Qty │ Unit Price│ Disc% │ Amount   │
├────────────┼──────┼─────┼───────────┼───────┼──────────┤
│ ...        │      │     │           │       │          │
├────────────┴──────┴─────┴───────────┴───────┤          │
│                                  Subtotal   │  150.00  │
│                                  Discount   │   15.00  │
│                                  VAT (15%)  │   20.25  │
│                                  ─────────────────────  │
│                                  Total      │  155.25  │
│                                  Paid       │   50.00  │
│                                  Balance Due│  105.25  │
└─────────────────────────────────────────────┴──────────┘

PAYMENTS
┌──────────────┬──────────────┬──────────┐
│ Date         │ Method       │ Amount   │
│ 2026-07-15   │ Cash         │   50.00  │
└──────────────┴──────────────┴──────────┘
```

### Key Implementation Details

**Practice data:** Fetched async on button click — cache-first (`useQuery`), falls back to `getPractice()` API call if the React Query cache is empty. Ensures the logo and company details are always available even if the modal was opened before the cache warmed up.

**Totals alignment:** Subtotals, VAT, Total, Paid, and Balance Due are embedded as `foot` rows in the same `autoTable` as line items. This guarantees pixel-perfect column alignment — a separate table would have independent column widths and would not align.

**VAT display:** VAT is always shown, even when zero — `VAT (0%): $0.00`. The rate is computed from `taxAmount / subTotal`.

**Discount row:** Only rendered when `discountAmount > 0`.

**Separator line:** A thin horizontal rule is drawn above the Total row using `willDrawCell` hook at `data.cell.y` (the cell's top edge) — not with `doc.line()` coordinates which would overlap the text.

**Balance Due colour:** Red when `balanceDue > 0`, green when settled.

**Payments table:** Rendered below the main table if `invoice.payments.length > 0`.

**File name:** `{invoiceNumber}.pdf` (e.g. `INV-0001.pdf`).

---

## Reports PDF

**Location:** `src/features/reports/ReportsPage.tsx`  
**Trigger:** `↓ Export PDF` button on each report panel

Five reports each have an `exportPdf()` function and an export button placed in a `flex justify-between` row alongside the date filters.

| Report | Tables exported |
|---|---|
| **Revenue** | Summary metrics (total invoiced, collected, outstanding, invoice count) + monthly breakdown |
| **Outstanding** | Invoice list with patient, doctor, amount, status, due date |
| **Visits by Doctor** | Doctor stats: visit count, completed, pending, cancellations |
| **Demographics** | Gender split + age group distribution + visits per doctor |
| **Low Stock** | Inventory items below reorder threshold |

All reports share the same styling: dark navy header row (`[30, 41, 59]`), alternating row shading, right-aligned numeric columns.

---

## Shared Patterns

**Formatting helper:**
```ts
const fv = (n: number) => `${n.toFixed(2)}`
```

**Y-position after a table:**
```ts
const y = ((doc as any).lastAutoTable?.finalY ?? fallback) + gap
```

**autoTable foot rows for totals:**
```ts
// Foot rows must match the number of body columns exactly.
// Empty string cells in leading columns push label and value to the right.
const foot = [
  ['', '', '', '', 'Subtotal', fv(invoice.subTotal)],
  ['', '', '', '', 'VAT (15%)', fv(invoice.taxAmount)],
  ['', '', '', '', 'Total', fv(invoice.totalAmount)],
]
autoTable(doc, { ..., foot, showFoot: 'lastPage' })
```
