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

## Payment requests (owner, 15 Nov 2026)

| # | Question | Current behaviour | Why |
|---|---|---|---|
| Q25 | **Who may approve** a payment request («تایید پرداخت»)? May the requester approve his own request? Are there **amount limits** (e.g. above a sum the CEO approves)? | Anyone using the system may approve, including the requester; no limits. Login and permissions (Keycloak) come next. | The manual names "the person allowed to approve requests" and lets the treasurer enter and approve when there are no requests. |
| Q26 | The **beneficiary** is chosen from Basic Information → «ذینفعان» (with bank account and Sheba). Is that right, or should any تفصیل of the accounting system be allowed, chosen by its type as the manual describes? | Only beneficiaries defined in Basic Information. | The beneficiary list keeps the Sheba and national ID that transfers and cheques need. |
| Q27 | May an **approved** request be corrected (the manual allows the approver to edit fields without *)? | No: the approval is withdrawn («برداشتن تایید»), the request corrected, then approved again. | Keeps what was approved exactly what is paid. |
| Q28 | **Attachments** («اسکن ضمائم»): should the scans be uploaded with the request now, or is the number of attachments enough until the documents service is built? | Only the number of attachments. | Scans need a documents store (planned `documents-svc`). |
| Q29 | When a request is paid by **several cheques or from several accounts** (manual: «پرداخت یک درخواست وجه از طریق چند فقره چک»), and for «پرداخت جمعی», are the details entered at payment time? | Yes, planned for the payment screens (Phase E). | As the manual describes. |

## Received cheque steps (owner's item 14, 15 Nov 2026)

Proposal: `received-cheque-steps.md`.

| # | Question | Proposed behaviour | Why |
|---|---|---|---|
| Q30 | **Cancelling a received cheque** «به دستور مدیریت»: should a manager **approve it in the system** (the treasurer records «درخواست ابطال», the manager presses «تایید ابطال»), or does the treasurer cancel it and type the order's number and date? Who may approve? | **Answered 15 Nov 2026: the treasurer cancels it and types the number and date of management's order** (`Instrument.CancelOrderRequired`); one step, no approval in the system. | Keeps management's order in the system with the cheque. |
| Q31 | **Replacing a cheque («تعویض»):** may the new cheques differ from the old one in **amount** or **number** (one cheque replaced by two, or with a late-payment amount)? Does the replacement get a **cheque receipt number**? | **Answered 15 Nov 2026: yes, the new cheques may differ in amount and number.** Built as a cheque receipt marked «تعویض چک» (cheque receipt numbers); the difference goes to the purpose group. | The voucher moves exactly the old cheque's amount. |
| Q32 | **Endorsing («خرج چک»):** is the receiver chosen from Basic Information → «ذینفعان» or any تفصیل? Must it pay an **approved payment request** (which then becomes paid)? Record the **Sayad transfer** tracking number? (The new manual says endorsement is no longer possible; your item 14 keeps it.) | **Answered 15 Nov 2026: any تفصیل; the Sayad transfer's tracking number is recorded (required); endorsement is kept.** The link to a payment request is asked again in Q36. | The manual's voucher: debit «بستانکاران / حساب‌های پرداختنی» of the receiver. |
| Q33 | After the bank **returns** a cheque to the company (c), can it be **deposited again**, and can the company **later ask for a bounce notice** (e) for it? | **Answered 15 Nov 2026: both are possible.** | The manual lists «عودت شده از نزد بانک» as a state of its own. |
| Q34 | Your table says «اسناد **درخ**واست شده» for a bounce and «اسناد **واخ**واست شده» for a returned endorsed cheque. Is it **one account** (واخواست = protested)? | **Answered 15 Nov 2026: one account, «اسناد واخواست شده»** (the table's «درخواست شده» was a typing error; corrected everywhere). | Probably a typing difference. |
| Q35 | When several cheques are deposited together on **one slip** (or collected on one statement date), is it **one voucher** for the slip or one per cheque? | **Answered 15 Nov 2026: one voucher per cheque.** | Your answer "one voucher for each operation" can mean either. |
| Q36 | **Endorsement and payment requests:** must every endorsement («خرج چک») pay an **approved payment request** (the request then becomes paid), or is the request optional? (Your answer repeated the question.) | **Answered 15 Nov 2026: yes, every endorsement pays an approved payment request** (in full or in part; the request is paid when its payments reach its amount; a returned endorsed cheque takes its payment back). | Decides whether the endorsement screen must choose an approved request. |

## «ثبت صیادی شد» (owner, 15 Nov 2026)

Built as the owner described: a received cheque is not deposited at the bank, and an issued cheque is not handed to its beneficiary, before «ثبت صیادی شد» is ticked; the tick keeps the computer's date and time (`docs/decisions.md`).

| # | Question | Current behaviour | Why |
|---|---|---|---|
| Q37 | **Endorsing («خرج چک»):** must a received cheque also be ticked «ثبت صیادی شد» before it is endorsed to someone else? | **Answered 16 Nov 2026: no, the tick is required only before the deposit at the bank.** An endorsement keeps needing the tracking number of the Sayad transfer. | You named the deposit at the bank; in the Sayad system a cheque can only be transferred after it was confirmed there. |
| Q38 | **A tick set by mistake:** may the user remove it? | **Answered 16 Nov 2026: yes.** While the cheque is still in the cash desk (issued cheque: not yet delivered); after the deposit or delivery it cannot be changed. Each tick and removal is kept with user and time in the system's event log. | A tick on the wrong cheque must be correctable; after the cheque left, the tick is part of its history. |
| Q39 | **Cheques already in the cash desk** (on the test server now, and the old program's cheques at go-live): must each one be ticked before it is deposited, or should they count as already recorded in Sayad? | **Answered 16 Nov 2026: all of them are ticked, since they can be assumed to be recorded in Sayad.** Schema script 033 ticks the received cheques already in the database once, and the migration tool ticks the old program's received cheques; the screen shows «از قبل ثبت‌شده فرض شد» with the date of the upgrade or migration. Cheques received afterwards wait for the user's tick. | Your rule covers every cheque; at go-live there may be many cheques in the cash desk that are already in Sayad. |
