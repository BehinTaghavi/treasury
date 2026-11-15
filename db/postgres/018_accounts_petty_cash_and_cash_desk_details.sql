-- Owner's report no. 2 (13 Nov 2026) on petty cash (تنخواه گردان) and cash desks (صندوق):
--   item 4:  payment ceiling <= settlement limit < amount (was: both at most the amount);
--   item 11: the date the petty cash or cash desk was set up (تاریخ ایجاد);
--   item 12: how the holder keeps the money: bank account (name, number), cash and/or bank card (holder, number), as JSON;
--   item 13: top-up onto a bank card (BankCard).
-- Rows saved before this script keep empty dates and keeping; the screen asks for them at the next correction.
ALTER TABLE accounts.petty_cash ADD COLUMN IF NOT EXISTS created_on date;
ALTER TABLE accounts.petty_cash ADD COLUMN IF NOT EXISTS keeping jsonb;
ALTER TABLE accounts.cash_desk ADD COLUMN IF NOT EXISTS created_on date;

ALTER TABLE accounts.petty_cash DROP CONSTRAINT IF EXISTS petty_cash_recovery_check;
ALTER TABLE accounts.petty_cash ADD CONSTRAINT petty_cash_recovery_check CHECK (recovery IN ('Cash', 'Cheque', 'BankCard'));

-- The old limit checks (settle_limit <= amount, pay_ceiling <= amount) are replaced by the owner's rule. Older rows that break it
-- stay readable; the check below is NOT VALID so it applies to new and changed rows only.
ALTER TABLE accounts.petty_cash DROP CONSTRAINT IF EXISTS petty_cash_settle_limit_check;
ALTER TABLE accounts.petty_cash DROP CONSTRAINT IF EXISTS petty_cash_pay_ceiling_check;
ALTER TABLE accounts.petty_cash DROP CONSTRAINT IF EXISTS petty_cash_limits;
ALTER TABLE accounts.petty_cash ADD CONSTRAINT petty_cash_limits
    CHECK (pay_ceiling > 0 AND pay_ceiling <= settle_limit AND settle_limit < amount) NOT VALID;
