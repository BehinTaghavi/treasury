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
| Q11 | Rule N2 says a cheque cannot be **cleared** before its due date. Does the same apply to a **bounce** (can the bank return a cheque before its due date)? | **Answered 10 Nov 2026: yes.** A bounce before the due date is refused (`Instrument.BouncedBeforeDueDate`). | See `docs/decisions.md`. |

## Receipts (owner, 14 Nov 2026)

Q12–Q18 are about cheque printing, in [cheque-printing.md](cheque-printing.md).

| # | Question | Current behaviour | Why |
|---|---|---|---|
| Q19 | **Receipt numbers:** does each kind (cash, bank deposit, cheques and notes) keep its own series starting at 1 each year, or should all receipts share one series? | **Answered 14 Nov 2026: own series per kind (cash, bank deposit, cheques and notes), from 1 each year.** | Old `Subs.Sno` is counted per `Scode` (17 cash, 16 deposit) and year. |
| Q20 | **Correcting a receipt:** a cash or bank receipt typed wrongly is **cancelled with a reason** and entered again; a receipt of cheques is not cancelled, each cheque is **returned** instead. Is that right, or should a receipt be editable until it is confirmed («قطعی»), as in the old program? | **Answered 14 Nov 2026: yes: cancel with a reason, no editing; cheques are returned.** | Cancelling keeps a clear trail for the auditor; the old `Final` flag only locked editing. |
| Q21 | Is the cheque's 16-digit **Sayad ID** required on every received cheque? | **Answered 14 Nov 2026: yes, required on every received cheque (`Receipt.SayadIdRequired`).** | Old cheques (before 1397) and some bank cheques may not show it. |
| Q22 | Which **payers** may be chosen: any تفصیل, or only some types (customers, persons and companies…)? | **Answered 14 Nov 2026: only customers and persons/companies (`Receipt.PayerTypeNotAllowed`); the role is read from the تفصیل type («مشتریان», «اشخاص و شرکتها»).** | The old form lists all تفصیل (`Openaccs 15/16`). |
| Q23 | Must POS and card-to-card receipts carry a **tracking number**? | **Answered 14 Nov 2026: optional.** | Old `D_variz` requires only the deposit type. |
| Q24 | Does one payer ever pay **cash and cheques together** so that they need **one** receipt number? | **Answered 14 Nov 2026: no: each kind has its own receipt, even when paid at the same time.** | The old program has separate forms and series. |
