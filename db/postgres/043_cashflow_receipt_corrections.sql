-- Correction («ویرایش») of a saved receipt before its voucher is confirmed (owner's report no. 6, items 2 and 6, 18 Nov 2026). A cheque
-- or note typed by mistake is taken off its receipt while the receipt is corrected: its row and its only step (the receipt) are deleted.
-- The service refuses it once the cheque has any other step (Instrument.CorrectionAfterStep); rows of a closed fiscal year stay frozen by
-- the trigger closed_year_guard. Until now the service could only insert and update these two tables.
GRANT DELETE ON cashflow.instrument, cashflow.instrument_event TO cashflow_svc;

-- Deletion («حذف») of a receipt recorded by mistake, before its voucher is confirmed (owner's answers to Q59 and Q67, 19 and 20 Nov
-- 2026). The receipt's row is kept in the state 'Deleted' — the receipts list shows it marked «حذف شده», so its number is seen to be
-- taken — with who deleted it and when; the cheques or notes on it are deleted (above). The service never deletes a receipt row.
REVOKE DELETE ON cashflow.receipt FROM cashflow_svc;
ALTER TABLE cashflow.receipt ADD COLUMN IF NOT EXISTS deleted_by text;
ALTER TABLE cashflow.receipt ADD COLUMN IF NOT EXISTS deleted_at timestamptz;

-- The two unnamed checks of 025 (the list of states; a cancellation's date and reason) are replaced by named ones with the whole list.
DO $$
DECLARE c text;
BEGIN
    FOR c IN SELECT conname FROM pg_constraint
             WHERE conrelid = 'cashflow.receipt'::regclass AND contype = 'c'
               AND conname NOT IN ('receipt_state_known', 'receipt_state_marks')
               AND (pg_get_constraintdef(oid) LIKE '%state = ANY%Recorded%Cancelled%' OR pg_get_constraintdef(oid) LIKE '%cancelled_on IS NULL%')
    LOOP
        EXECUTE format('ALTER TABLE cashflow.receipt DROP CONSTRAINT %I', c);
    END LOOP;
END $$;
ALTER TABLE cashflow.receipt DROP CONSTRAINT IF EXISTS receipt_state_known;
ALTER TABLE cashflow.receipt ADD CONSTRAINT receipt_state_known CHECK (state IN ('Recorded', 'Cancelled', 'Deleted'));
ALTER TABLE cashflow.receipt DROP CONSTRAINT IF EXISTS receipt_state_marks;
ALTER TABLE cashflow.receipt ADD CONSTRAINT receipt_state_marks CHECK (
    state = 'Recorded' AND cancelled_on IS NULL AND deleted_at IS NULL
 OR state = 'Cancelled' AND cancelled_on >= receipt_date AND cancel_reason IS NOT NULL AND deleted_at IS NULL
 OR state = 'Deleted' AND cancelled_on IS NULL AND deleted_at IS NOT NULL AND deleted_by IS NOT NULL);
