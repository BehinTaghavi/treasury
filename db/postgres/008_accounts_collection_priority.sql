-- Collection priority (اولویت واگذاری) becomes a number 1–5, 1 the highest (owner's report of 11 Nov 2026, item 15).
-- Bank accounts with priority 1 come first when choosing the bank to deposit received cheques for collection.
-- Accounts that had the old yes/no priority get 1. Safe to run again.
DO $$
BEGIN
    IF (SELECT data_type FROM information_schema.columns
        WHERE table_schema = 'accounts' AND table_name = 'bank_account' AND column_name = 'collection_priority') = 'boolean' THEN
        ALTER TABLE accounts.bank_account ALTER COLUMN collection_priority DROP NOT NULL;
        ALTER TABLE accounts.bank_account ALTER COLUMN collection_priority TYPE smallint
            USING CASE WHEN collection_priority THEN 1 END;
        ALTER TABLE accounts.bank_account ADD CONSTRAINT bank_account_collection_priority_range
            CHECK (collection_priority BETWEEN 1 AND 5);
    END IF;
END
$$;
