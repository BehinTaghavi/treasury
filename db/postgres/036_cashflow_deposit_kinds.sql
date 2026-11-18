-- Kinds of a direct deposit into a bank account (owner, 18 Nov 2026): Paya and Satna transfers with the Sheba they came from, an internal
-- transfer between accounts, cash paid in at a branch (DepositSlip), card to card and the payment gateway of the company's account.
-- 'Transfer' (Paya, Satna and internal transfers as one kind) stays for receipts recorded before; 'Pos' stays.
DO $$
DECLARE
    c text;
BEGIN
    -- The check of 025 was written inline, so PostgreSQL named it; find it by what it checks.
    FOR c IN SELECT conname FROM pg_constraint
             WHERE conrelid = 'cashflow.receipt'::regclass AND contype = 'c' AND pg_get_constraintdef(oid) LIKE '%deposit_method%DepositSlip%'
    LOOP
        EXECUTE format('ALTER TABLE cashflow.receipt DROP CONSTRAINT %I', c);
    END LOOP;
END
$$;
ALTER TABLE cashflow.receipt ADD CONSTRAINT receipt_deposit_method_known CHECK (deposit_method IN (
    'DepositSlip', 'Transfer', 'Pos', 'CardToCard', 'Paya', 'Satna', 'InternalTransfer', 'PaymentGateway'));

ALTER TABLE cashflow.receipt ADD COLUMN IF NOT EXISTS payer_sheba text;
ALTER TABLE cashflow.receipt DROP CONSTRAINT IF EXISTS receipt_payer_sheba;
ALTER TABLE cashflow.receipt ADD CONSTRAINT receipt_payer_sheba CHECK (
    (payer_sheba IS NULL OR payer_sheba ~ '^IR[0-9]{24}$')
    AND (payer_sheba IS NOT NULL) = (deposit_method IS NOT NULL AND deposit_method IN ('Paya', 'Satna')));
