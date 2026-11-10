# Open questions for the domain expert

> Farsi version: [open-questions.fa.md](open-questions.fa.md). Keep both in step when either changes.

Each item is a behaviour the code currently implements one way, with the reason. Please answer by number; the answer becomes a test.

| # | Question | Current behaviour | Why |
|---|---|---|---|
| Q1 | Should a received cheque with the same bank and number as an existing one be **blocked**? | Saved, with a warning (`Instrument.PossibleDuplicate`). | The check exists in the old program's `Form_D_Check` but is commented out, so the old program allows it. |
| Q2 | What can happen to a **bounced received cheque** afterwards: legal action, return to the drawer, replacement (تعویض) by a new cheque, re-deposit? | Nothing; "Bounced" is final. Legal action is kept as a note on migrated rows (`GoInlaw`). | The old program uses flags (`GoInlaw`, `Backtohim`) rather than steps; the MVC version has a replacement step (PlaningTypeID 21). |
| Q3 | When is a **cheque book** deactivated? | When no unused sheet remains. | The old program deactivates it when the *last sheet number* is printed, even if earlier sheets were skipped. Skipped sheets are now voided with a reason. |
| Q4 | May an issued cheque be **cancelled after it was delivered** to the payee? | No. | The old program cancels from the print form only (before delivery). |
| Q5 | Is Iran Nazho Co.'s **fiscal year** the same as the Jalali calendar year (1 Farvardin – 29/30 Esfand)? | Assumed yes (`Coyearinfo` lists one database per Jalali year). | Partitions and year-end rules depend on it. |
| Q6 | New rule N1: a step may not be **dated before the previous step** of the same instrument. Keep it? | Enforced. Migrated rows that break it are corrected to the previous date and listed in the warnings report. | Not in the old program; prevents impossible histories. |
| Q7 | Meaning and use of the old flags **Signed, Hazfhavale, Payit, Calcit, Kasrtime, Delivervag**. | Not migrated (except where listed in the mapping). | Needed to decide whether they are business data or old UI state. |
| Q8 | **Cash payments** stored in `Checks` with `Checktype = 9`: migrate as payments? | Rejected by the cheque migration and listed in the report. | They are not instruments; the Disbursements context will take them. |
| Q9 | Please map the MVC version's **PlaningType** codes to the states in `instrument-lifecycle.md`. | — | Needed to migrate clients who already use the MVC version. |
| Q10 | Is a **due date required** for received instalments and promissory notes? | Required for every kind except guarantee items. | Old `D_Check` requires a due date ("سررسید نامشخص است"). |
| Q11 | Rule N2 says a cheque cannot be **cleared** before its due date. Does the same apply to a **bounce** (can the bank return a cheque before its due date)? | A bounce may be dated before the due date. | Asked 10 Nov 2026 together with N2. |
