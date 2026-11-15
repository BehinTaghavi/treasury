-- Owner's report no. 3 (12 Nov 2026), item 3: all banks print the same standard cheque, so a cheque type can be defined once for
-- all bank accounts (bank_account_id empty) and chosen for any account's cheque books. Each print type once per account, and
-- once among the standard types (NULLS NOT DISTINCT, PostgreSQL 15+).
ALTER TABLE cashflow.cheque_format ALTER COLUMN bank_account_id DROP NOT NULL;
DROP INDEX IF EXISTS cashflow.cheque_format_one_per_type;
CREATE UNIQUE INDEX IF NOT EXISTS cheque_format_one_per_type_v2 ON cashflow.cheque_format (bank_account_id, print_type) NULLS NOT DISTINCT;
