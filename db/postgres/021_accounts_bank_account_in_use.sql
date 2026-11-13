-- Owner's report no. 3 (12 Nov 2026), item 1: a bank account nothing uses can be deleted. cashflow-svc marks an account "in use"
-- through accounts-svc's API before it saves a cheque book, cheque type or cheque naming it (ADR 0001); the catch-up below marks
-- accounts already named by cashflow data. It reads cashflow's schema once, from this administrator script (as 007 did); the
-- services never read each other's schema.
ALTER TABLE accounts.bank_account ADD COLUMN IF NOT EXISTS in_use boolean NOT NULL DEFAULT false;
DO $$
BEGIN
    IF to_regclass('cashflow.cheque_book') IS NOT NULL THEN
        UPDATE accounts.bank_account b SET in_use = true
        WHERE NOT in_use AND (EXISTS (SELECT 1 FROM cashflow.cheque_book c WHERE c.bank_account_id = b.id)
                              OR EXISTS (SELECT 1 FROM cashflow.cheque_format f WHERE f.bank_account_id = b.id)
                              OR EXISTS (SELECT 1 FROM cashflow.instrument i WHERE i.bank_account_id = b.id));
    END IF;
END $$;
GRANT DELETE ON accounts.bank_account TO accounts_svc;
