-- Correction («ویرایش») of a saved receipt before its voucher is confirmed (owner's report no. 6, items 2 and 6, 18 Nov 2026). A cheque
-- or note typed by mistake is taken off its receipt while the receipt is corrected: its row and its only step (the receipt) are deleted.
-- The service refuses it once the cheque has any other step (Instrument.CorrectionAfterStep); rows of a closed fiscal year stay frozen by
-- the trigger closed_year_guard. Until now the service could only insert and update these two tables.
GRANT DELETE ON cashflow.instrument, cashflow.instrument_event TO cashflow_svc;

-- Deletion («حذف») of a receipt recorded by mistake, before its voucher is confirmed (owner's answer to Q59, 19 Nov 2026): the receipt's
-- row is deleted with the cheques on it. Its number stays taken (cashflow.receipt_number is not turned back). The service refuses it for
-- a cancelled receipt, a confirmed voucher, a replacement receipt and a receipt with a cheque that has a step recorded on it.
GRANT DELETE ON cashflow.receipt TO cashflow_svc;
