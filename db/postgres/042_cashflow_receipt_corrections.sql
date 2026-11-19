-- Correction («ویرایش») of a saved receipt before its voucher is confirmed (owner's report no. 6, items 2 and 6, 18 Nov 2026). A cheque
-- or note typed by mistake is taken off its receipt while the receipt is corrected: its row and its only step (the receipt) are deleted.
-- The service refuses it once the cheque has any other step (Instrument.CorrectionAfterStep); rows of a closed fiscal year stay frozen by
-- the trigger closed_year_guard. Until now the service could only insert and update these two tables.
GRANT DELETE ON cashflow.instrument, cashflow.instrument_event TO cashflow_svc;
