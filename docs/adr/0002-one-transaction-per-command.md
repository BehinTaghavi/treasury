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
042 grants the delete), and the events `ReceiptCorrected`, `ReceivedInstrumentCorrected` and `ReceivedInstrumentRemoved` go to the outbox
in the same transaction, so a consumer of the earlier `ReceiptRecorded` / `InstrumentStateChanged` events can follow the correction.
