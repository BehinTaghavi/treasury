# Received cheques after receipt: steps, confirmation and vouchers

> Farsi version: [received-cheque-steps.fa.md](received-cheque-steps.fa.md). Keep both in step when either changes.

Owner's item 14 (15 Nov 2026): for each state of a received cheque after it is received and recorded, the step is recorded and
confirmed, its accounting voucher is made, and the cheque's latest state is updated, "with a mechanism you propose". States named
by the owner: (a) deposit at the bank for collection, (b) collected by the bank, after the treasurer checks the bank statement,
(c) returned to the company by the bank, (d) replacement of a returned cheque by a new one, (e) bounce with the bank's bounce notice
number and date, (f) cancellation in any state by management's order, done by the treasurer, (g) endorsement (خرج چک) to pay the
company's debts, (h) return of an endorsed cheque. The old program had one form per step (manual: «ثبت وصول / برگشت / عودت
چک‌های واگذاری», «خرج چک‌های دریافتی», «عودت چک نزد صندوق», «ابطال»).

Status: **built** (15 Nov 2026) with the owner's answers to Q30–Q35 (`open-questions.md`): cancellation by the treasurer with
management's order number and date (Q30); replacement cheques may differ in amount and number (Q31); endorsement to any تفصیل with the
Sayad tracking number (Q32); a cheque back from the bank can be deposited again or get its bounce notice (Q33); «اسناد واخواست شده»
(Q34); one voucher per cheque (Q35). An endorsement pays an approved payment request (Q36): the request's beneficiary receives the cheque; a returned endorsed cheque takes the payment back. Not built yet: undoing an
unconfirmed step. **Section 5** adds the owner's description of the latest status of 18 Nov 2026 («ثبت صیادی شد», «اقدام حقوقی», «سررسید چک»,
the «وضعیت» column of the receipts list), built the same day.

## 1. Mechanism

1. **One screen «عملیات چک‌های دریافتی»** (it replaces the current tab «واگذاری و وصول چک»), with one tab per place of the cheque,
   like the old program's forms: **نزد صندوق**, **نزد بانک**, **عودت از بانک، برگشتی و اقدام حقوقی**, **خرج‌شده**. Each tab lists its cheques by due
   date with search; the user ticks cheques and chooses one of the steps allowed there, enters the step's fields and records it.
2. **Recording** a step changes the cheque's **latest state** at once and writes it in the cheque's history (date, user, fields).
   A step can only follow the allowed order (table below); its date is not before the previous step (rule N1).
3. **Confirming:** each step makes its **voucher**, shown as a draft under the step. The treasurer checks it (for "collected":
   against the bank statement) and presses **«تایید سند»**. The confirmed voucher is kept unchanged and later sent to the
   accounting system (Phase F).
4. **Cancellation** (f): the treasurer cancels the cheque and types the **number and date of management's order** and the reason
   (answer to Q30); the order cannot be dated after the cancellation.
5. **A cheque card («کارت چک»):** opening a cheque shows its whole history, its latest state and every voucher with its state.

## 2. States and allowed steps

| Latest state | Allowed next steps |
|---|---|
| نزد صندوق (in the cash desk) | ثبت صیادی (required before the deposit, rule N3) · (d) replace · (f) cancel · (g) endorse · return to the payer · اقدام حقوقی (guarantee cheques and notes; ordinary cheques as the exceptional case, section 6) |
| ثبت صیادی شد (registered in Sayad, still in the cash desk) — **18 Nov** | (a) deposit at the bank (only from here, rule N3) · (d) replace · (f) cancel · (g) endorse · return to the payer · اقدام حقوقی (exceptional) |
| در جریان وصول (at the bank) | (b) collected · (c) returned to the company · (e) bounced |
| عودت چک از بانک (back from the bank, not collected) | (a) deposit again · (e) bounced (Q33) · (d) replace · (f) cancel · return to the payer (all confirmed by the owner, Q40) |
| برگشت چک (bounced, with notice no. and date) | اقدام حقوقی (its own screen, section 6) · (d) replace · (f) cancel |
| اقدام حقوقی (with the company's lawyer) — **18 Nov** | تحویل به صندوق (the cheque became collectable; then like a new cheque) · (d) replace · (f) cancel · scans of the action added or removed |
| خرج شده (endorsed) | (h) returned by the endorsee → **برگشت چک** |
| وصول چک · تعویض چک · ابطال شده · عودت از صندوق | none (final) |

Guarantee cheques are not deposited or endorsed (rule R3); they can be cancelled or returned.

## 3. Fields and vouchers of each step

Accounts in «» are the fixed accounts of «شابلون اسناد خزانه داری»; "payer" is the تفصیل of the cheque's payer, "bank" the تفصیل
of the company bank account.

| Step | Fields | Debit | Credit | Source |
|---|---|---|---|---|
| (a) deposit | bank account, deposit slip no., date | «چک‌های در جریان وصول» · bank | «اسناد نزد صندوق» · payer | owner's table — built |
| (b) collected | date (after checking the statement), reference | bank account's معین · bank | «چک‌های در جریان وصول» · bank | owner's table — built |
| (c) returned to the company | date, bank's note | «اسناد نزد صندوق» · payer | «چک‌های در جریان وصول» · bank | owner's table «عودت چک واگذاری توسط بانک» |
| (e) bounced | **bounce notice no. and date** (required; on screens «گواهی عدم پرداخت»), its tracking code (optional), reason | «اسناد واخواست شده» · payer | «چک‌های در جریان وصول» · bank (from «عودت شده»: «اسناد نزد صندوق») | owner's table «برگشت چک واگذاری توسط بانک» (account name corrected by the owner, Q34) |
| (d) replaced | the new cheque(s) of the same payer (as on the cheque receipt form) | «اسناد نزد صندوق» · payer (new cheque) | where the old cheque is: «اسناد نزد صندوق», «اسناد واخواست شده» or, in legal action, «اسناد واخواست شده حقوقی شده» · payer | Q31 |
| (f) cancelled | management's order no. and date, date, reason | new fixed account «مطالبات سوخت شده» · payer (guarantee: «طرف حساب انتظامی») | where the cheque is: «اسناد نزد صندوق» or «اسناد واخواست شده» (guarantee: «چک انتظامی به نفع شرکت») | owner's table «ابطال چک دریافتنی نزد صندوق» |
| (g) endorsed | an approved payment request (its beneficiary, payment purpose group and purpose), Sayad transfer tracking no. (required) | payment purpose group's account · endorsee | «اسناد نزد صندوق» · payer | manual: «خرج چک: بد بستانکاران / حساب‌های پرداختنی» (Q32) |
| (h) endorsement returned | bounce notice no. and date | «اسناد واخواست شده» · payer | the account debited at (g) · endorsee | owner's table «عودت چک‌های خرج شده … (پس از برگشت)» |
| عودت از صندوق (return to the payer) — **18 Nov** | date, note | the account the cheque's receipt credited (its purpose group's account) · payer (guarantee: «طرف حساب انتظامی») | «اسناد نزد صندوق» · payer (guarantee: «چک انتظامی به نفع شرکت») | owner's table «عودت چک‌های نزد صندوق: بدهکاران یا ...»; confirmed by the owner (Q41) |
| ثبت صیادی — **18 Nov** | date, tracking no. of the registration (optional) | no voucher: only the latest status changes (owner's answer to Q37) | | owner, 18 Nov 2026 |
| اقدام حقوقی — **18 Nov** | date of the action and its description (both required); number, date and tracking code of the certificate of non-payment («گواهی عدم پرداخت»); scans of its papers | new fixed account «اسناد واخواست شده حقوقی شده» · payer (guarantee: new fixed account «اسناد تضمینی حقوقی شده به نفع شرکت») | «اسناد واخواست شده» · payer (a cheque still in the cash desk: «اسناد نزد صندوق»; guarantee: «چک انتظامی به نفع شرکت») | owner's answers to Q38 and Q42, 18 Nov 2026 |
| تحویل به صندوق پس از اقدام حقوقی — **18 Nov** | date, note | «اسناد نزد صندوق» · payer (guarantee: «چک انتظامی به نفع شرکت») | «اسناد واخواست شده حقوقی شده» · payer (guarantee: «اسناد تضمینی حقوقی شده به نفع شرکت») | owner's answers to Q38 (2) and Q43, 18 Nov 2026 |

One voucher per step and cheque (answer to Q35), also when several cheques are deposited on one slip.

## 4. What changes in the code (for the reviewer)

- `InstrumentLifecycle`: new states `ReturnedFromBank`, `Replaced` and `Cancelled` for received cheques; new actions
  `ReturnFromBank`, `Replace`, `RecordEndorsementReturned`, and `Cancel` for received cheques (`Instrument.CancelReceived`); the
  migration copy (`legacy_mapping.py`) and `instrument-lifecycle.md` follow. `Bounced` and `Endorsed` are no longer final.
- Schema 031: `instrument_event.details` (jsonb), `receipt.replaced_instrument_id`, the new states and the fixed account `BadDebts`.
- The history entry keeps the bank account of each deposit (a cheque can be deposited again at another account), so every step's
  voucher keeps its own bank.
- Replacement saves the old cheque and the new cheque(s) with a cheque receipt in one transaction (as receipts already do, ADR 0002
  amendment).

## 5. Latest status of a received cheque (owner, 18 Nov 2026)

The owner described the field «وضعیت» (it means the **latest** status) and asked for it in the last column of the receipts list and
in every part of the system. His statuses, in his words, and what the system does:

| Status (as shown) | The owner's meaning | In the system |
|---|---|---|
| نزد صندوق | the cheque is received and recorded | state after the receipt |
| ثبت صیادی شد | the cheque is registered in the banks' Sayad system | **new** step «ثبت صیادی» on a cheque in the cash desk; the cheque stays in the cash desk. **Required before the deposit at the bank** (rule N3, owner 18 Nov 2026) |
| در جریان وصول | deposited at the bank for collection | step «واگذاری به بانک» |
| وصول چک | collected by the bank at the due date, into the company's account | step «وصول چک» (not before the due date, rule N2) |
| عودت از صندوق | a cheque in the cash desk is given back to its owner | step «عودت از صندوق»; **now with its voucher** |
| عودت چک از بانک | the bank gives the cheque back without a bounce notice; it goes back to the cash desk | step «عودت چک از بانک» |
| برگشت چک | the bank issues its bounce notice | step «برگشت چک» with the notice's number and date; from the bank or after «عودت چک از بانک» |
| اقدام حقوقی | the bounced cheque is handed to the company's lawyer | **new** step on a bounced cheque, recorded on its own screen «اقدام حقوقی» (section 6) |
| تعویض چک | from «نزد صندوق», «برگشت چک» or «اقدام حقوقی» the cheque goes back to its owner and a new cheque is received | step «تعویض چک» (a cheque receipt of the new cheques); **now also from «اقدام حقوقی»** |
| سررسید چک | a post-dated cheque in the cash desk or at the bank has reached its due date | **new** mark, shown automatically beside the status, with what the cheque waits for |

- **«سررسید چک»** is shown from the day of the due date on a post-dated cheque (due after the day it was received) that is «نزد صندوق»,
  «ثبت صیادی شد» or «در جریان وصول». At the bank it waits for «وصول چک», «عودت چک از بانک» or «برگشت چک»; in the cash desk for
  «واگذاری به بانک» or «تعویض چک». Nothing is recorded for it; the screen «عملیات چک‌های دریافتی» can list only these cheques.
- **Receipts list:** the last column «وضعیت» shows, for a cheque receipt, the latest status of its cheques (with the number of sheets
  when they differ, and the mark «سررسید چک»); for a cash or bank receipt «ثبت شده» or «ابطال شده». The list can be searched by status.
- **One list of names** (`frontend/packages/ui-kit/src/chequeStatus.ts`) serves the receipts list, the opened receipt, the screen
  «عملیات چک‌های دریافتی» and the cheque card, so a cheque is called the same everywhere. «خرج شده» and «ابطال شده» (item 14) stay.
- **Vouchers:** every step that moves an account has its voucher (table in section 3). The return to the payer got its voucher
  (the owner's table «عودت چک‌های نزد صندوق»; debit confirmed by the owner, Q41). The legal action has its voucher with the new
  template account «اسناد واخواست شده حقوقی شده» (answer to Q38). «ثبت صیادی» and «سررسید چک» have no voucher: the latest status
  changes, or the mark is shown, and that is enough (answers to Q37 and Q39).
- A cheque back from the bank keeps all five steps: return to its owner, bounce, deposit again (Q33), replacement and cancellation
  (the owner's answer to Q40: "all three stay").
- Code: states `SayadRegistered`, `InLegal`; steps `RegisterSayad`, `SendToLegal`; `ReceivedChequeStatus.HasReachedDueDate`; schema
  033; the migration maps the old flag `GoInlaw` to «اقدام حقوقی».

## 6. The screen «اقدام حقوقی» (owner, 18 Nov 2026)

The owner asked for a screen of its own for the legal action, and for the name «اقدام حقوقی» "as before" for this status. It is the
sixth tab of «دریافت‌ها»; the button «اقدام حقوقی» on a bounced cheque's row of «عملیات چک‌های دریافتی» opens it with that cheque.

1. The cheque is chosen from **a list box of the bounced cheques** (searched by number, bank or payer).
2. **Its particulars are shown**: number, Sayad ID, bank and branch, account number and holder, payer, amount, purpose, date received,
   due date, the bank's bounce notice (number and date), the reason of the bounce and the account it was deposited at.
3. **«تاریخ اقدام»** and **«شرح اقدام حقوقی»** are entered (both required; `Instrument.LegalActionDescriptionRequired`). The date is
   not before the bounce (rule N1).
4. **«اسکن ضمائم اقدام حقوقی»**: PDF, JPG or PNG files, at most 5 MB each and 20 a cheque (the limits of a payment request's scans).
5. «ثبت اقدام حقوقی» records the step: the cheque's latest status becomes **«اقدام حقوقی»** everywhere, then the scans are stored.
   A scan that is refused (wrong type, too large) is named in the message; the action itself stays recorded.

Below the form the cheques in legal action are listed with the date and description of their action; opening one shows its scans,
which can be opened, added to and removed while the cheque is in legal action. Replacement and cancellation of such a cheque stay on
«عملیات چک‌های دریافتی». The lawyer's name and case number of the first version are gone: the description carries them.

Code: `Instrument.SendToLegal(date, user, description)`; the scans are `InstrumentAttachment` (its own small aggregate, one command
per file, kept in `cashflow.instrument_file`, schema 034, partitioned by the fiscal year the scan is added in);
`LegalActionUseCases`; `frontend/remotes/receipts/src/LegalAction.vue`.

**Voucher (owner's answer to Q38, 18 Nov 2026):** Dr «اسناد واخواست شده حقوقی شده» / Cr «اسناد واخواست شده», the payer's تفصیل on both
sides. The account is a new line of «شابلون اسناد خزانه داری» (`FixedAccount.ProtestedChequesInLegal`, schema 035); the voucher is
shown under the opened cheque on this screen with «تایید سند» and cannot be confirmed before the account is set. "If, unlikely as it
is, legal action is taken on a cheque that is «نزد صندوق»", the credit is «اسناد نزد صندوق»: a tick box on the screen adds the
cheques still in the cash desk (also those registered in Sayad; not guarantee cheques or notes) to the list box. From then on the
cheque is in «اسناد واخواست شده حقوقی شده», so its replacement or cancellation credits that account (confirmed by the owner).

**After the legal action (owner's answer to Q38 (2)):** "if the cheque or note becomes collectable during the legal action or at its
end, it is handed to the cash desk at the start of the cycle («نزد صندوق») and from there treated like a new cheque; or it is replaced
or cancelled". The step «تحویل به صندوق (قابل وصول)» is on the opened cheque of this screen (and on its row of the steps screen): the
latest status becomes «نزد صندوق» again and the cheque goes through «ثبت صیادی», the deposit and so on. Its voucher takes the cheque
out of the legal account: Dr «اسناد نزد صندوق» / Cr «اسناد واخواست شده حقوقی شده» (confirmed by the owner, Q43). The scans stay with
the cheque.

**«گواهی عدم پرداخت» (owner, 18 Nov 2026):** the bank's paper is called so on every screen and in the voucher texts (in the code and in
this English file: bounce notice). The legal-action screen has its **number, date and tracking code**; number and date are filled
from the bounce and can be corrected. **For a bounced ordinary cheque or note the three are required**
(`Instrument.NonPaymentCertificateRequired`, owner 18 Nov 2026); for a guarantee item, or the exceptional cheque taken to law from the
cash desk, which has no certificate, they stay optional. The step «برگشت چک» (and the return of an endorsed cheque) also takes the
**tracking code** of the certificate, optional there, so the legal-action screen finds it filled.

**Guarantee cheques, guarantee notes and bounced notes (owner, 18 Nov 2026)** may get legal action too. The list box therefore holds
the bounced cheques and notes and the guarantee cheques and notes in the cash desk; ordinary cheques in the cash desk appear only with
the exceptional-case tick. A guarantee cheque or note is kept in the memorandum accounts; its legal action (owner's answer to Q42)
credits «اسناد تضمینی به نفع شرکت» (the template's «چک انتظامی به نفع شرکت») and debits the new template account «اسناد تضمینی حقوقی
شده به نفع شرکت» (`FixedAccount.GuaranteeChequesInLegal`). A bounced note is credited out of «اسناد واخواست شده» like a cheque.

## 7. The receipt screens (owner, 18 Nov 2026)

- **Button «فهرست دریافت‌ها» on every receipt screen**: under each receipt form (cheques, cash, direct bank deposits) and under the
  cheque screens (steps, legal action) the button opens the receipts of that screen's kind for the fiscal year in use, **newest
  first**, with the latest status of each, in a box that scrolls in the free space under the form; the list can be searched (direct
  deposits also by kind of deposit) and **printed** (the rows shown, A4 landscape, with the total). The cheque screens show the cheque
  receipts. `frontend/remotes/receipts/src/ReceiptsPanel.vue`.
- **Switching the tab of «عملیات چک‌های دریافتی»** closes the cheque card, the step being entered and the ticks of the tab left behind
  (the card of a cheque at the bank used to stay on the screen after switching to «نزد صندوق»).
- **Kinds of a direct deposit** («نوع واریز», to be chosen, no default): حواله پایا با شماره شبا، حواله ساتنا با شماره شبا، انتقال داخلی حساب
  به حساب، واریز نقدی در شعبه، کارت به کارت (only when the bank account has a bank card)، واریز به درگاه پرداخت متصل به حساب شرکت. Paya
  and Satna are recorded with the Sheba the transfer came from (IR and 24 digits, check digits verified). Kept besides, as the owner confirmed (Q44): «کارتخوان
  (POS)» of 14 Nov; the Sheba is the payer's (Q45); the old combined «حواله / انتقال» is shown on earlier receipts and no longer chosen. Schema 036.
