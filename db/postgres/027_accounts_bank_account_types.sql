-- Owner's report no. 4 (15 Nov 2026), items 1, 2 and 4: the kind of each bank account and its company bank cards.
--   current Qard-al-hasan (the only kind with cheque books), savings Qard-al-hasan, short-term deposit (ordinary, 3-month, 6-month),
--   long-term deposit. Accounts saved before are current Qard-al-hasan accounts (they were all current accounts with cheque books).
ALTER TABLE accounts.bank_account ADD COLUMN IF NOT EXISTS account_type text NOT NULL DEFAULT 'CurrentQardHasan'
    CHECK (account_type IN ('CurrentQardHasan', 'SavingsQardHasan', 'ShortTermDeposit', 'LongTermDeposit'));
ALTER TABLE accounts.bank_account ADD COLUMN IF NOT EXISTS term text CHECK (term IN ('Ordinary', 'ThreeMonths', 'SixMonths'));
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'bank_account_type_rules') THEN
        ALTER TABLE accounts.bank_account ADD CONSTRAINT bank_account_type_rules CHECK (
            (account_type = 'ShortTermDeposit') = (term IS NOT NULL)
            AND (account_type <> 'ShortTermDeposit' OR currency = 'IRR'));
    END IF;
END
$$;

-- A bank card belongs to one account; one account may have several (e.g. one per petty-cash holder).
CREATE TABLE IF NOT EXISTS accounts.bank_card (
    card_number      text    PRIMARY KEY CHECK (card_number ~ '^[0-9]{16}$'),
    bank_account_id  uuid    NOT NULL REFERENCES accounts.bank_account (id) ON DELETE CASCADE,
    holder_name      text    NOT NULL CHECK (btrim(holder_name) <> ''),
    position         integer NOT NULL
);
CREATE INDEX IF NOT EXISTS bank_card_by_account ON accounts.bank_card (bank_account_id, position);

GRANT SELECT, INSERT, UPDATE, DELETE ON accounts.bank_card TO accounts_svc;
GRANT SELECT, INSERT, UPDATE ON accounts.bank_card TO treasury_migrator;
GRANT SELECT ON accounts.bank_card TO treasury_readonly;
