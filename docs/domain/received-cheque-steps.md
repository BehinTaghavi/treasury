# Received cheques after receipt: steps, confirmation and vouchers (proposal)

> Farsi version: [received-cheque-steps.fa.md](received-cheque-steps.fa.md). Keep both in step when either changes.

Owner's item 14 (15 Nov 2026): for each state of a received cheque after it is received and recorded, the step is recorded and
confirmed, its accounting voucher is made, and the cheque's latest state is updated, "with a mechanism you propose". States named
by the owner: (a) deposit at the bank for collection, (b) collected by the bank, after the treasurer checks the bank statement,
(c) returned to the company by the bank, (d) replacement of a returned cheque by a new one, (e) bounce with the bank's bounce notice
number and date, (f) cancellation in any state by management's order, done by the treasurer, (g) endorsement (خرج چک) to pay the
company's debts, (h) return of an endorsed cheque. The old program had one form per step (manual: «ثبت وصول / برگشت / عودت
چک‌های واگذاری», «خرج چک‌های دریافتی», «عودت چک نزد صندوق», «ابطال»).

Status: **proposal, waiting for the owner's answers to Q30–Q35** (`open-questions.md`). Steps (a) and (b) and the bounce without its
notice number are already built.

## 1. Mechanism

1. **One screen «عملیات چک‌های دریافتی»** (it replaces the current tab «واگذاری و وصول چک»), with one tab per place of the cheque,
   like the old program's forms: **نزد صندوق**, **نزد بانک**, **عودت‌شده و برگشتی**, **خرج‌شده**. Each tab lists its cheques by due
   date with search; the user ticks cheques and chooses one of the steps allowed there, enters the step's fields and records it.
2. **Recording** a step changes the cheque's **latest state** at once and writes it in the cheque's history (date, user, fields).
   A step can only follow the allowed order (table below); its date is not before the previous step (rule N1).
3. **Confirming:** each step makes its **voucher**, shown as a draft under the step. The treasurer checks it (for "collected":
   against the bank statement) and presses **«تایید سند»**. The confirmed voucher is kept unchanged and later sent to the
   accounting system (Phase F). An unconfirmed step can be undone («برگشت از آخرین عملیات») by the treasurer; a confirmed one only
   by a new step.
4. **Cancellation** (f) needs management's approval: the treasurer records **«درخواست ابطال»** with the reason; a manager presses
   **«تایید ابطال»**; only then the cheque becomes «ابطال شده» and its voucher is made (Q30). Until login and roles exist anyone can
   approve; then only the roles the owner names.
5. **A cheque card («کارت چک»):** opening a cheque shows its whole history, its latest state and every voucher with its state.

## 2. States and allowed steps

| Latest state | Allowed next steps |
|---|---|
| نزد صندوق (in the cash desk) | (a) deposit at the bank · (d) replace · (f) cancel · (g) endorse · return to the payer |
| در جریان وصول نزد بانک (at the bank) | (b) collected · (c) returned to the company · (e) bounced |
| عودت شده از بانک (back from the bank, not collected) — **new** | (a) deposit again · (e) bounced (Q33) · (d) replace · (f) cancel · return to the payer |
| برگشت شده (bounced, with notice no. and date) | (d) replace · (f) cancel · legal action later (Q2) |
| خرج شده (endorsed) | (h) returned by the endorsee → **برگشت شده** |
| وصول شده · تعویض شده (new) · ابطال شده (new) · عودت به پرداخت‌کننده | none (final) |

Guarantee cheques are not deposited or endorsed (rule R3); they can be cancelled or returned.

## 3. Fields and vouchers of each step

Accounts in «» are the fixed accounts of «شابلون اسناد خزانه داری»; "payer" is the تفصیل of the cheque's payer, "bank" the تفصیل
of the company bank account.

| Step | Fields | Debit | Credit | Source |
|---|---|---|---|---|
| (a) deposit | bank account, deposit slip no., date | «چک‌های در جریان وصول» · bank | «اسناد نزد صندوق» · payer | owner's table — built |
| (b) collected | date (after checking the statement), reference | bank account's معین · bank | «چک‌های در جریان وصول» · bank | owner's table — built |
| (c) returned to the company | date, bank's note | «اسناد نزد صندوق» · payer | «چک‌های در جریان وصول» · bank | owner's table «عودت چک واگذاری توسط بانک» |
| (e) bounced | **bounce notice no. and date** (required), reason | «اسناد واخواست شده» · payer | «چک‌های در جریان وصول» · bank (from «عودت شده»: «اسناد نزد صندوق») | owner's table «برگشت چک واگذاری توسط بانک» (Q34) |
| (d) replaced | the new cheque(s) of the same payer (as on the cheque receipt form) | «اسناد نزد صندوق» · payer (new cheque) | where the old cheque is: «اسناد نزد صندوق» or «اسناد واخواست شده» · payer | Q31 |
| (f) cancelled | management's approval, date, reason | new fixed account «مطالبات سوخت شده» · payer (guarantee: «طرف حساب انتظامی») | where the cheque is: «اسناد نزد صندوق» or «اسناد واخواست شده» (guarantee: «چک انتظامی به نفع شرکت») | owner's table «ابطال چک دریافتنی نزد صندوق» |
| (g) endorsed | endorsee (ذینفع), payment purpose group and purpose, optional payment request, Sayad transfer tracking no. | payment purpose group's account · endorsee | «اسناد نزد صندوق» · payer | manual: «خرج چک: بد بستانکاران / حساب‌های پرداختنی» (Q32) |
| (h) endorsement returned | bounce notice no. and date | «اسناد واخواست شده» · payer | the account debited at (g) · endorsee | owner's table «عودت چک‌های خرج شده … (پس از برگشت)» |

One voucher per step and cheque today; one voucher per slip or per operation if the owner prefers (Q35).

## 4. What changes in the code (for the reviewer)

- `InstrumentLifecycle`: new states `ReturnedFromBank`, `Replaced`, `Cancelled` for received cheques; new actions
  `ReturnFromBank`, `Replace`, `RequestCancel`/`ApproveCancel`, `RecordEndorsementReturned`; the migration copy
  (`legacy_mapping.py`) and `instrument-lifecycle.md` follow.
- The history entry keeps the bank account of each deposit (a cheque can be deposited again at another account), so every step's
  voucher keeps its own bank.
- Replacement saves the old cheque and the new cheque(s) with a cheque receipt in one transaction (as receipts already do, ADR 0002
  amendment).
