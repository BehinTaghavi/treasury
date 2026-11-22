-- Receipts of promissory notes (owner's report no. 4, item 13, and his form «فرم دریافت سفته» of 16 Nov 2026).
-- A fourth kind of receipt, 'Notes', numbered in its own series; it names the notes it brought in like a cheque receipt and keeps
-- the receipt-level part of the form (reason, paper or electronic, due date or on demand, custody, attachments…) as JSON of
-- NoteTermsSnapshot in note_terms. Each note is a row of cashflow.instrument whose details carry the date written on it, «عندالمطالبه»
-- and what was checked on it.
ALTER TABLE cashflow.receipt ADD COLUMN IF NOT EXISTS note_terms jsonb;

-- The checks that list the kinds are replaced (whatever name PostgreSQL gave them), so the script stays safe to run again.
DO $$
DECLARE
    c record;
BEGIN
    FOR c IN
        SELECT conrelid::regclass AS tbl, conname
        FROM pg_constraint
        WHERE conrelid IN ('cashflow.receipt'::regclass, 'cashflow.receipt_number'::regclass)
          AND contype = 'c' AND pg_get_constraintdef(oid) LIKE '%Instruments%'
    LOOP
        EXECUTE format('ALTER TABLE %s DROP CONSTRAINT %I', c.tbl, c.conname);
    END LOOP;
END
$$;

ALTER TABLE cashflow.receipt ADD CONSTRAINT receipt_kind_known CHECK (kind IN ('Cash', 'BankDeposit', 'Instruments', 'Notes'));
-- Cash goes into a cash desk, a deposit into a bank account; cheques and notes wait in the cash desk as instruments.
ALTER TABLE cashflow.receipt ADD CONSTRAINT receipt_kind_account CHECK (
    kind = 'Cash' AND account_kind = 'CashDesk' AND deposit_method IS NULL
    OR kind = 'BankDeposit' AND account_kind = 'BankAccount' AND deposit_method IS NOT NULL
    OR kind IN ('Instruments', 'Notes') AND account_kind IS NULL AND account_id IS NULL AND cardinality(instrument_ids) > 0);
ALTER TABLE cashflow.receipt ADD CONSTRAINT receipt_kind_amount CHECK (
    kind IN ('Instruments', 'Notes') OR account_id IS NOT NULL AND amount > 0 AND currency IS NOT NULL
    AND (currency = 'IRR' AND rate_to_rial IS NULL OR currency <> 'IRR' AND rate_to_rial > 0));
-- Only a note receipt has note terms, and it always has them.
ALTER TABLE cashflow.receipt DROP CONSTRAINT IF EXISTS receipt_note_terms;
ALTER TABLE cashflow.receipt ADD CONSTRAINT receipt_note_terms CHECK ((kind = 'Notes') = (note_terms IS NOT NULL));
ALTER TABLE cashflow.receipt_number ADD CONSTRAINT receipt_number_kind_known CHECK (kind IN ('Cash', 'BankDeposit', 'Instruments', 'Notes'));

-- New fixed account of the voucher template: «سفته انتظامی به نفع شرکت» (owner's table «دریافت سفته انتظامی»), after the accounts of
-- the legal action (035).
-- NOT VALID: a later script widens this list, and every start runs all scripts again; rows that already use the later values must
-- not stop this script. The last script that defines the list checks every row.
-- (041 adds «سفته های در جریان وصول».)
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_key_known;
ALTER TABLE cashflow.voucher_account ADD CONSTRAINT voucher_account_key_known CHECK (account_key IN (
    'ChequesInCashDesk', 'ChequesInCollection', 'ProtestedCheques', 'GuaranteeChequesForCompany', 'GuaranteeCounterpart', 'BadDebts',
    'ProtestedChequesInLegal', 'GuaranteeChequesInLegal', 'GuaranteeNotesForCompany')) NOT VALID;
