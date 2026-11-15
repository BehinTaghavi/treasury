-- Opening balances (موجودی اول دوره) of bank accounts, cash desks and petty cash (owner, 14 Nov 2026; Access Form_Accamounts).
-- One row per account and fiscal (Jalali) year: the first year on the start date (may be mid-year), later years on 1 Farvardin
-- when a correction is entered (otherwise the balance is carried from the year before). Foreign-currency accounts keep the Rial
-- equivalent. Few rows per year, so the table is not partitioned. accounts_svc may delete a balance typed by mistake.
CREATE TABLE IF NOT EXISTS accounts.opening_balance (
    id               uuid          PRIMARY KEY,
    account_kind     text          NOT NULL CHECK (account_kind IN ('BankAccount', 'CashDesk', 'PettyCash')),
    account_id       uuid          NOT NULL,
    fiscal_year      smallint      NOT NULL CHECK (fiscal_year BETWEEN 1300 AND 1500),
    balance_date     date          NOT NULL,
    amount           numeric(22,2) NOT NULL,
    rial_equivalent  numeric(22,0),
    version          bigint        NOT NULL DEFAULT 1,
    created_at       timestamptz   NOT NULL DEFAULT now(),
    updated_at       timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT opening_balance_cash_not_negative CHECK (account_kind = 'BankAccount' OR amount >= 0)
);
CREATE UNIQUE INDEX IF NOT EXISTS opening_balance_once_per_year ON accounts.opening_balance (account_kind, account_id, fiscal_year);

GRANT SELECT, INSERT, UPDATE, DELETE ON accounts.opening_balance TO accounts_svc;
GRANT SELECT, INSERT, UPDATE ON accounts.opening_balance TO treasury_migrator;
GRANT SELECT ON accounts.opening_balance TO treasury_readonly;
