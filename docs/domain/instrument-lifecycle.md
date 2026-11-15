# Instrument lifecycle (cheques, notes, instalments)

> Farsi version: [instrument-lifecycle.fa.md](instrument-lifecycle.fa.md). Keep both in step when either changes.

Source of truth in code: `src/Services/Cashflow/Treasury.Cashflow.Domain/Instruments/InstrumentLifecycle.cs`.
Old program: dbo.Checks in the shared `Cashnajoo86` database, status in `Stcode` plus flags; forms of `Lcash.mdb` named below.

## Received (old Dp = 1)

| Old | New state | Reached by | From | Old form | Kinds |
|---|---|---|---|---|---|
| 11 | InCashDesk | Receive | — | D_Check | all |
| 12 | DepositedForCollection | DepositForCollection (needs collection account; a cheque must be ticked «ثبت صیادی شد» first) | InCashDesk | D_Check_Vag_m | ordinary, bank cheque |
| 13 | Cleared | RecordCleared | DepositedForCollection | D_Check_vos_m | all |
| 14 | Bounced | RecordBounced | DepositedForCollection | D_Check_vos_m, Checkbacked | all |
| 15 | ReturnedToIssuer | ReturnToIssuer | InCashDesk | D_Check_Od_m | all |
| 16 | Endorsed | Endorse (receiver تفصیل, payment purpose group, purpose, Sayad tracking no.) | InCashDesk | D_Check_Kh_m | ordinary, bank cheque |
| — | ReturnedFromBank (عودت شده از بانک) | ReturnFromBank | DepositedForCollection | owner's item 14 (c) | all |
| 12 | DepositedForCollection | DepositForCollection again | ReturnedFromBank | owner's item 14, Q33 | ordinary, bank cheque |
| 14 | Bounced | RecordBounced (bounce notice no. required; its date is the step's date) | DepositedForCollection, ReturnedFromBank | owner's item 14 (e), Q33 | all |
| 14 | Bounced | RecordEndorsementReturned (bounce notice no.) | Endorsed | owner's item 14 (h) | all |
| 15 | ReturnedToIssuer | ReturnToIssuer | ReturnedFromBank | manual | all |
| — | Replaced (تعویض شده) | Replace (on a cheque receipt of the new cheques, same payer) | InCashDesk, ReturnedFromBank, Bounced | owner's item 14 (d), Q31 | not guarantee |
| — | Cancelled (ابطال شده) | Cancel (management's order no. and date, reason) | InCashDesk, ReturnedFromBank, Bounced | owner's item 14 (f), Q30 | all |

Owner's item 14 (15 Nov 2026) and his answers Q30–Q35: `docs/domain/received-cheque-steps.md`. Bounced and Endorsed are no longer final.

## Issued (old Dp = 2)

| Old | New state | Reached by | From | Old form |
|---|---|---|---|---|
| 20 | BeingIssued | PrepareIssue (approved payment) | — | Porder |
| 21 | Issued | Issue (reserves a sheet of the cheque book) | BeingIssued | Check_Print_single |
| 22 | Delivered | Deliver (a cheque must be ticked «ثبت صیادی شد» first) | Issued | Check_deliv_m |
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
Received: D1 receipt, D3 sent for collection / returned / endorsed, D4 collected or bounced.
Issued: D1 issue, D2 delivery (or cancellation), D3 maturity reclassification, D4 paid or returned by our bank.

Every step belongs to the fiscal year in which it happens; the instrument belongs to the year it was created in (doc 7.4).

## Date rules for new operations
- **N1** (new, Q6): a step may not be dated before the previous step of the same instrument.
- **N2** (CEO, 10 Nov 2026): a received cheque cannot be **cleared** before its due date (`Instrument.ClearedBeforeDueDate`). Depositing it for collection before the due date stays allowed. Not in the old program. Migrated history that breaks it is kept as recorded and listed in the warnings report. The same applies to **bounces** (`Instrument.BouncedBeforeDueDate`; answer to Q11, 10 Nov 2026).

## «ثبت صیادی شد» (owner, 15 Nov 2026)
Every cheque is recorded in the banks' Sayad system before it goes to the bank or to its beneficiary. The user ticks «ثبت صیادی شد» on the cheque and the system keeps the computer's date and time and the user. It is **not a state and not a step** (the table of transitions is unchanged, no voucher): it is a condition of two steps.
- **Received cheque**, current or post-dated: no `DepositForCollection` before the tick (`Instrument.SayadNotRegistered`); the step is not offered until then.
- **Issued cheque**: no `Deliver` before the tick. The tick is possible once the cheque has its number (state Issued).
- Only cheques (ordinary, guarantee, bank, post-dated); notes and instalments are not in Sayad.
- The tick can be removed only while the step it guards is still ahead (in the cash desk or back from the bank; issued and not delivered). A cheque back from the bank keeps its tick.
- Not in the old program (`Lcash.mdb` is older than Sayad). Migrated cheques arrive without a tick; those still in the cash desk must be ticked before they are deposited (Q39).

