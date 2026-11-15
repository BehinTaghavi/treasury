-- Schema of accounts-svc (Treasury Accounts): the company's bank accounts (manual section 5); cash desks and cheque books follow.
-- Banks, cities, currencies and ledger accounts belong to reference-svc: only their ids / codes are stored here (no
-- cross-schema foreign keys, ADR 0001); accounts-svc checks them through reference-svc's API when a bank account is saved.

CREATE SCHEMA IF NOT EXISTS accounts AUTHORIZATION treasury_owner;

CREATE TABLE IF NOT EXISTS accounts.bank_account (
    id                    uuid        PRIMARY KEY,
    account_number        text        NOT NULL,
    -- Digits of the number only (application: BankAccount.NumberKeyOf). Form_Bankacc: "شماره حساب تكراري است".
    number_key            text        NOT NULL UNIQUE CHECK (number_key ~ '^[0-9]+$'),
    bank_id               uuid        NOT NULL,
    branch_name           text,
    branch_code           text,
    city_id               uuid,
    opening_date          date,
    currency              char(3)     NOT NULL CHECK (currency ~ '^[A-Z]{3}$'),
    main_account_id       uuid        REFERENCES accounts.bank_account (id),
    ledger_account        text        NOT NULL,
    detail1               text,
    detail2               text,
    collection_account    text,
    bounced_account       text,
    collection_priority   boolean     NOT NULL,
    is_facility           boolean     NOT NULL,
    is_active             boolean     NOT NULL,
    version               bigint      NOT NULL DEFAULT 1,
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),
    CHECK (main_account_id IS DISTINCT FROM id),
    CHECK (detail2 IS NULL OR detail1 IS NOT NULL)
);
-- MVC BanksController: a main account has at most one backup account.
CREATE UNIQUE INDEX IF NOT EXISTS bank_account_one_backup ON accounts.bank_account (main_account_id) WHERE main_account_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS accounts.outbox (
    id            uuid        PRIMARY KEY,
    type          text        NOT NULL,
    payload       jsonb       NOT NULL,
    occurred_at   timestamptz NOT NULL,
    published_at  timestamptz
);
CREATE INDEX IF NOT EXISTS accounts_outbox_unpublished ON accounts.outbox (occurred_at) WHERE published_at IS NULL;

CREATE TABLE IF NOT EXISTS accounts.idempotency (
    key         text        PRIMARY KEY,
    result      text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now()
);

GRANT USAGE ON SCHEMA accounts TO accounts_svc, treasury_migrator, treasury_readonly;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA accounts TO accounts_svc, treasury_migrator;
GRANT DELETE ON accounts.idempotency, accounts.outbox TO accounts_svc;
GRANT SELECT ON ALL TABLES IN SCHEMA accounts TO treasury_readonly;
