# ADR 0002 — One command, one transaction, one aggregate, transactional outbox

Status: accepted (9 Nov 2026)

Context: the MVC version saves in several steps and undoes failures by hand; the Access program uses no transactions at all.
Decision: each use case runs in `IUnitOfWork.ExecuteAsync`; it changes one aggregate (plus the cheque book when a sheet is reserved), saves once, and writes its events to the outbox table in the same transaction. Every write carries an idempotency key whose result is stored in the same transaction.
Consequence: a failure leaves nothing behind; retries are safe; other services learn about changes only from committed events.

Amendment (14 Nov 2026, receipts): a receipt of cheques and notes (`ReceiptUseCases.RecordInstrumentsAsync`) creates the receipt
and the new instruments on it in one transaction — the design document's reason for putting Receipts and Instruments in one
service (section 5.1). Only new aggregates are created; none that already exists is changed, so there is no lost-update risk.
The alternative (save the receipt, then create the instruments from its event) would show a receipt without its cheques for a
while and needs a compensating step when a cheque is refused, which is the MVC failure this ADR removes. Any later change to an
instrument stays its own command.

Amendment (15 Nov 2026, replacement of a received cheque, owner's item 14 (d)): the cheque receipt of the new cheques, the new
instruments and the replaced cheque («تعویض شده») are saved in one transaction (`ReceiptUseCases.ReplaceAsync`). The replaced cheque is
an existing aggregate, saved with its version check, so a concurrent step on it makes the whole replacement fail and nothing is left
half-done; doing it in two commands would leave new cheques with the old one still «نزد صندوق».

Amendment (19 Nov 2026, correction of a receipt, owner's report no. 6, items 2 and 6): correcting a cheque or note receipt
(`ReceiptUseCases.CorrectInstrumentsAsync`, `CorrectNotesAsync`) saves the receipt and the cheques on it — corrected, added or taken
off — in one transaction, for the reason of the first amendment: the receipt's total, date and payer and its cheques must never
disagree. Every aggregate touched is version-checked, and a cheque is touched only while it is as it was received (no step on it), so
no other command can be working on it with a different intent. A cheque taken off the receipt is deleted (it had no life yet; schema
043 grants the delete), and the events `ReceiptCorrected`, `ReceivedInstrumentCorrected` and `ReceivedInstrumentRemoved` go to the outbox
in the same transaction, so a consumer of the earlier `ReceiptRecorded` / `InstrumentStateChanged` events can follow the correction.

Amendment (19 and 20 Nov 2026, the owner's answers to Q59, Q60 and Q67): **deleting** a receipt recorded by mistake
(`ReceiptUseCases.DeleteAsync`) marks the receipt «حذف شده» (it stays in the list; its row is never removed) and removes the cheques or
notes on it in one transaction, with `ReceiptDeleted` and `ReceivedInstrumentRemoved` in the outbox; a deleted receipt whose cheques
are still in the cash desk never stays. A cheque on which a step was recorded is not
touched by a correction and stops the deletion, so both commands still only change cheques nothing else is working on.

Amendment (20 Nov 2026, transfers between the company's own bank accounts, owner's report no. 7): three commands of
`TransferUseCases` and one of `InstrumentUseCases` save more than one aggregate in one transaction, each for the reason of the first
amendment — what the user sees as one act must never be half-done. (1) **Issuing the transfer cheque** saves the cheque book (locked,
rule R5), the new issued cheque and the new transfer, as `IssueChequeAsync` of Phase 0 already saved book and cheque together. (2) **The
button to the receipts side** creates the received cheque and saves the transfer (version-checked) that now names it; a transfer
without its received cheque, or a received cheque no transfer knows, would show the same sheet in two states. (3) **Recording the
collection** of the received transfer cheque also marks the issued cheque «وصول شده» and finishes the transfer: the two instruments
are the same sheet of paper, and nothing else works on the issued one (it has no screen of its own steps). (4) **Deleting** a cheque
transfer (only before its cheque is on the receipts side) marks the transfer «حذف شده», removes the issued cheque and frees its sheet;
**taking back** a receipts-side record removes the received cheque while it has no step. Every existing aggregate is saved with its
version check; the events (`TransferChequeIssued`, `TransferChequeReceived`, `TransferChequeCollected`, `BankTransferDeleted`,
`IssuedInstrumentRemoved`, `ChequeSheetReleased`…) go to the outbox in the same transaction.
