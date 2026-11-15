-- Cash desks (صندوق) of accounts-svc: new-version manual item 10; MVC CashDesk. The holder's تفصیل and the معین are codes of
-- reference-svc's copy of the chart of accounts (checked through its API, no cross-schema keys: ADR 0001).

CREATE TABLE IF NOT EXISTS accounts.cash_desk (
    id              uuid        PRIMARY KEY,
    name            text        NOT NULL CHECK (length(name) BETWEEN 1 AND 100),
    name_key        text        NOT NULL,                 -- PersianText.Key(name), computed by the application
    ledger_account  text        NOT NULL,
    holder_detail   text        NOT NULL,                 -- تفصیل of the holder (دارنده صندوق)
    detail2         text,
    currency        char(3)     NOT NULL CHECK (currency ~ '^[A-Z]{3}$'),
    is_active       boolean     NOT NULL DEFAULT true,
    version         bigint      NOT NULL DEFAULT 1,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS cash_desk_unique_name ON accounts.cash_desk (name_key);

GRANT SELECT, INSERT, UPDATE ON accounts.cash_desk TO accounts_svc, treasury_migrator;
GRANT SELECT ON accounts.cash_desk TO treasury_readonly;
