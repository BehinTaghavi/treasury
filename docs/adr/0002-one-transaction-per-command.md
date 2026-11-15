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
