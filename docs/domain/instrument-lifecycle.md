# Instrument lifecycle (cheques, notes, instalments)

> Farsi version: [instrument-lifecycle.fa.md](instrument-lifecycle.fa.md). Keep both in step when either changes.

Source of truth in code: `src/Services/Cashflow/Treasury.Cashflow.Domain/Instruments/InstrumentLifecycle.cs`.
Old program: dbo.Checks in the shared `Cashnajoo86` database, status in `Stcode` plus flags; forms of `Lcash.mdb` named below.

## Received (old Dp = 1)

| Old | New state | Reached by | From | Old form | Kinds |
|---|---|---|---|---|---|
| 11 | InCashDesk | Receive | — | D_Check | all |
| 12 | DepositedForCollection | DepositForCollection (needs collection account) | a cheque: SayadRegistered (rule N3; in the old data: InCashDesk); an ordinary promissory note: InCashDesk (a note is not in Sayad) | D_Check_Vag_m; owner, 16 and 18 Nov 2026 | ordinary and bank cheque; ordinary promissory note (owner, 16 Nov 2026) |
| 13 | Cleared | RecordCleared | DepositedForCollection | D_Check_vos_m | all |
| 14 | Bounced | RecordBounced | DepositedForCollection | D_Check_vos_m, Checkbacked | all |
| 15 | ReturnedToIssuer | ReturnToIssuer | InCashDesk | D_Check_Od_m | all |
| 16 | Endorsed | Endorse (receiver تفصیل, payment purpose group, purpose, Sayad tracking no.) | InCashDesk | D_Check_Kh_m | ordinary and bank cheque; ordinary promissory note (owner, 16 Nov 2026) |
| — | ReturnedFromBank (عودت شده از بانک) | ReturnFromBank | DepositedForCollection | owner's item 14 (c) | all |
| 12 | DepositedForCollection | DepositForCollection again | ReturnedFromBank | owner's item 14, Q33 | ordinary and bank cheque; ordinary promissory note (owner, 16 Nov 2026) |
| 14 | Bounced | RecordBounced (bounce notice no. required; its date is the step's date) | DepositedForCollection, ReturnedFromBank | owner's item 14 (e), Q33 | all |
| 14 | Bounced | RecordEndorsementReturned (bounce notice no.) | Endorsed | owner's item 14 (h) | all |
| 15 | ReturnedToIssuer | ReturnToIssuer | ReturnedFromBank | manual | all |
| — | Replaced (تعویض چک) | Replace (on a cheque receipt of the new cheques, same payer) | InCashDesk, SayadRegistered, ReturnedFromBank, Bounced, InLegal | owner's item 14 (d), Q31; owner, 18 Nov 2026 | not guarantee |
| — | Cancelled (ابطال شده) | Cancel (management's order no. and date, reason) | InCashDesk, SayadRegistered, ReturnedFromBank, Bounced, InLegal | owner's item 14 (f), Q30 | all |
| — | SayadRegistered (ثبت صیادی شد) | RegisterSayad (date; tracking no. of the registration, optional) | InCashDesk | owner, 18 Nov 2026 | cheques |
| 15, 16 | ReturnedToIssuer, Endorsed | the other steps of a cheque in the cash desk (ReturnToIssuer, Endorse) | SayadRegistered | owner, 18 Nov 2026 | as from InCashDesk |
| 14 + GoInlaw | InLegal (اقدام حقوقی) | SendToLegal (date and description of the action, on the screen «اقدام حقوقی»; scans attached afterwards) | Bounced | owner, 18 Nov 2026 | all |
| — | InLegal (اقدام حقوقی) | SendToLegal: guarantee cheques and guarantee notes; ordinary cheques as the exceptional case | InCashDesk, SayadRegistered | owner's answer to Q38 and 18 Nov 2026 | cheques and guarantee notes (not an ordinary note that has not bounced) |
| — | InCashDesk (نزد صندوق) | ReturnFromLegal (the cheque became collectable: back to the cash desk, then like a new cheque) | InLegal | owner's answer to Q38 (2), 18 Nov 2026 | all |

Owner's item 14 (15 Nov 2026) and his answers Q30–Q35: `docs/domain/received-cheque-steps.md`. Bounced and Endorsed are no longer final.
The bank's paper called "bounce notice" in this file and in the code is «گواهی عدم پرداخت» (certificate of non-payment) on the screens
(owner, 18 Nov 2026); the legal action keeps its number, date and tracking code.

Latest status as the owner described it on 18 Nov 2026 (`received-cheque-steps.md`, section 5): a cheque registered in Sayad stays in
the cash desk, so every step of «نزد صندوق» stays open; a bounced cheque handed to the lawyer can still be replaced or cancelled.
**«سررسید چک» is not a state**: it is a mark on a post-dated cheque (due after the day it was received) that is still in the cash
desk or at the bank on or after its due date (`ReceivedChequeStatus.HasReachedDueDate`); no step is recorded for it.

**Promissory notes (owner, 16 Nov 2026: «مراحل سفته پس از دریافت شامل واگذاری به بانک، وصول، استرداد، خرج سفته (ظهرنویسی و انتقال به غیر) و …»).** A received note follows the same states and steps as a received cheque, with other kinds allowed: an **ordinary** note (`PromissoryNote`) may now be deposited at the bank and endorsed (old rule R3 allowed ordinary and bank cheques only). **A note is not in the Sayad system**: the step «ثبت صیادی» is for cheques only, so rule N3 (18 Nov 2026) does not hold a note back and an ordinary note goes to the bank straight from the cash desk — the one line of the table where a note and a cheque start from different states. A **guarantee or trust** note (`GuaranteeNote`) is only handed back («استرداد», ReturnToIssuer) or cancelled, or gets a legal action; a protested ordinary note may get a legal action too (owner, 18 Nov 2026). For a note the bounce is its protest («واخواست», with the protest's number), the return to the payer is «استرداد به متعهد», and an endorsement has no Sayad transfer. A note payable on demand has no due date, so rule N2 does not hold it back. Screen: «عملیات سفته‌های دریافتی». Confirmed by the owner's answers to QN13–QN16; converting a guarantee note to an ordinary one and replacing a note are not needed.

## Issued (old Dp = 2)

| Old | New state | Reached by | From | Old form |
|---|---|---|---|---|
| 20 | BeingIssued | PrepareIssue (approved payment) | — | Porder |
| 21 | Issued | Issue (reserves a sheet of the cheque book) | BeingIssued | Check_Print_single |
| 22 | Delivered | Deliver | Issued | Check_deliv_m |
| 26 | Cancelled | Cancel (needs reason) | BeingIssued, Issued | Check_Print_single |
| 21/22 + Vos, RegVos | PaidByBank | RecordPaidByBank | Issued, Delivered | Check_p_vos_m |
| 21/22 + Backed, RegVos | BouncedAtBank | RecordBouncedAtBank | Issued, Delivered | Check_p_vos_m |
| Ctocash | (same state, IsMatured) | RecordMatured — post-dated items only, once | Issued, Delivered | Check_longtocash_m |

## Kinds (old Checktype)

| Old | Received | Issued |
|---|---|---|
| 0 | OrdinaryCheque (چک عادی) | OrdinaryCheque (چک نقدی) |
| 1 | GuaranteeCheque (چک انتظامی) | GuaranteeCheque |
| 2 | BankCheque (چک رمزدار/بانکی) | PostDatedCheque (چک مدت دار) |
| 3 | Instalment | Instalment |
| 4 | PromissoryNote | PromissoryNote |
| 5 | Other | Other |
| 6 | GuaranteeNote | GuaranteeNote |
| 9 | — | cash payment: not an instrument (Q8) |

## Dates of each step in the old data (used by the migration)
Received: D1 receipt, D3 sent for collection / returned / endorsed, D4 collected or bounced. A bounced cheque with the flag `GoInlaw`
is migrated as «اقدام حقوقی» (InLegal); the old program kept no date for it, so the step takes the date of the bounce.
Issued: D1 issue, D2 delivery (or cancellation), D3 maturity reclassification, D4 paid or returned by our bank.

Every step belongs to the fiscal year in which it happens; the instrument belongs to the year it was created in (doc 7.4).

## Date rules for new operations
- **N1** (new, Q6): a step may not be dated before the previous step of the same instrument.
- **N3** (CEO, 18 Nov 2026): **«ثبت صیادی» is required**: a cheque is deposited at the bank only after it is registered in Sayad
  (`Instrument.SayadRegistrationRequired`). A cheque the bank gave back was registered before its first deposit and is deposited
  again without a new registration. Not in the old program, which sent a cheque straight from the cash desk (D_Check_Vag_m);
  migrated history is kept as recorded (`LEGACY_TRANSITIONS` in the migration tool, outside the table the two copies share).
- **N2** (CEO, 10 Nov 2026): a received cheque cannot be **cleared** before its due date (`Instrument.ClearedBeforeDueDate`). Depositing it for collection before the due date stays allowed. Not in the old program. Migrated history that breaks it is kept as recorded and listed in the warnings report. The same applies to **bounces** (`Instrument.BouncedBeforeDueDate`; answer to Q11, 10 Nov 2026).

