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
