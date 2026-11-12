-- Petty cash holders (تنخواه گردان) of accounts-svc: new-version manual item 11; MVC PettyCash, PettyCash_BehalfGroups.
-- Amounts in Rial. Purpose groups are ids of reference-svc's reference.purpose_group (checked through its API, ADR 0001).

CREATE TABLE IF NOT EXISTS accounts.petty_cash (
    id              uuid          PRIMARY KEY,
    holder_detail   text          NOT NULL,                 -- تفصیل of the holder; one petty cash per holder
    ledger_account  text          NOT NULL,
    amount          numeric(20,0) NOT NULL CHECK (amount > 0),
    settle_limit    numeric(20,0) NOT NULL CHECK (settle_limit > 0 AND settle_limit <= amount),   -- حد واریز
    pay_ceiling     numeric(20,0) NOT NULL CHECK (pay_ceiling > 0 AND pay_ceiling <= amount),     -- سقف پرداخت
    purpose_groups  uuid[]        NOT NULL DEFAULT '{}',    -- allowed payment purpose groups; empty: all
    recovery        text          NOT NULL CHECK (recovery IN ('Cash', 'Cheque')),              -- نحوه ترمیم
    is_active       boolean       NOT NULL DEFAULT true,
    version         bigint        NOT NULL DEFAULT 1,
    created_at      timestamptz   NOT NULL DEFAULT now(),
    updated_at      timestamptz   NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS petty_cash_one_per_holder ON accounts.petty_cash (holder_detail);

GRANT SELECT, INSERT, UPDATE ON accounts.petty_cash TO accounts_svc, treasury_migrator;
GRANT SELECT ON accounts.petty_cash TO treasury_readonly;
