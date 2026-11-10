-- Schema of cashflow-svc (Receipts, Disbursements, Negotiable Instruments, Petty Cash).
-- Transactional tables are LIST-partitioned by Jalali fiscal year (doc section 7.3).
-- The partition key is part of every primary key, as PostgreSQL requires.

CREATE SCHEMA IF NOT EXISTS cashflow AUTHORIZATION treasury_owner;

-- Local copy of the fiscal years, kept up to date from reference-svc events (no cross-schema reads).
CREATE TABLE IF NOT EXISTS cashflow.fiscal_period (
    fiscal_year  integer PRIMARY KEY,
    starts_on    date    NOT NULL,
    ends_on      date    NOT NULL,
    status       text    NOT NULL CHECK (status IN ('Open', 'Closing', 'Closed'))
);

-- Cheque books (old: dbo.Bundle). Small, not partitioned.
CREATE TABLE IF NOT EXISTS cashflow.cheque_book (
    id               uuid    PRIMARY KEY,
    bank_account_id  uuid    NOT NULL,
    serial           text    NOT NULL DEFAULT '',
    first_sheet      bigint  NOT NULL CHECK (first_sheet > 0),
    last_sheet       bigint  NOT NULL,
    is_active        boolean NOT NULL DEFAULT true,
    version          bigint  NOT NULL DEFAULT 1,
    CHECK (last_sheet >= first_sheet AND last_sheet - first_sheet < 1000)
);

-- One row per used sheet. The primary key makes it impossible, even for two concurrent transactions,
-- to hand out the same sheet twice (fixes the MVC race on LastUsedPaperNo).
CREATE TABLE IF NOT EXISTS cashflow.cheque_sheet (
    cheque_book_id  uuid        NOT NULL REFERENCES cashflow.cheque_book (id),
    sheet           bigint      NOT NULL,
    sheet_use       text        NOT NULL CHECK (sheet_use IN ('Issued', 'Voided')),
    reason          text,
    used_at         timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (cheque_book_id, sheet)
);

-- Instruments: received and issued cheques, notes, instalments (old: dbo.Checks in Cashnajoo86).
-- Partitioned by the year the instrument was created in; it stays there for its whole life.
CREATE TABLE IF NOT EXISTS cashflow.instrument (
    origin_fiscal_year  integer       NOT NULL,
    id                  uuid          NOT NULL,
    direction           text          NOT NULL CHECK (direction IN ('Received', 'Issued')),
    kind                text          NOT NULL CHECK (kind IN ('OrdinaryCheque', 'GuaranteeCheque', 'BankCheque', 'PostDatedCheque', 'Instalment', 'PromissoryNote', 'GuaranteeNote', 'Other')),
    number              text,
    bank_name           text,
    bank_account_id     uuid,
    cheque_book_id      uuid,
    payment_id          uuid,
    amount              numeric(20, 2) NOT NULL CHECK (amount > 0),
    currency            char(3)       NOT NULL DEFAULT 'IRR',
    rate_to_rial        numeric(20, 4),
    amount_rial         numeric(20, 0) NOT NULL CHECK (amount_rial > 0),
    issue_date          date          NOT NULL,
    due_date            date,
    counterparty_id     uuid          NOT NULL,
    counterparty_name   text          NOT NULL,
    purpose             text          NOT NULL,
    destination         text,
    state               text          NOT NULL CHECK (state IN ('InCashDesk', 'DepositedForCollection', 'Cleared', 'Bounced', 'ReturnedToIssuer', 'Endorsed', 'BeingIssued', 'Issued', 'Delivered', 'Cancelled', 'PaidByBank', 'BouncedAtBank')),
    legacy_source       text,          -- e.g. 'Cashnajoo86.Checks:12345' for migrated rows
    version             bigint        NOT NULL DEFAULT 1,
    created_at          timestamptz   NOT NULL DEFAULT now(),
    updated_at          timestamptz   NOT NULL DEFAULT now(),
    PRIMARY KEY (origin_fiscal_year, id),
    CHECK (currency = 'IRR' AND rate_to_rial IS NULL OR currency <> 'IRR' AND rate_to_rial > 0)
) PARTITION BY LIST (origin_fiscal_year);

CREATE INDEX IF NOT EXISTS instrument_due ON cashflow.instrument (due_date) WHERE state IN ('InCashDesk', 'DepositedForCollection', 'Issued', 'Delivered');
CREATE INDEX IF NOT EXISTS instrument_bank_number ON cashflow.instrument (bank_name, number) WHERE direction = 'Received';
CREATE INDEX IF NOT EXISTS instrument_id ON cashflow.instrument (id);

-- Every step of every instrument (replaces the old fixed columns D1–D5 / G1–G5).
-- Partitioned by the year in which the step happens, so a cheque received in 1404 and cleared in 1405
-- has its clearing step — and its voucher — in 1405 (doc section 7.4).
CREATE TABLE IF NOT EXISTS cashflow.instrument_event (
    fiscal_year         integer     NOT NULL,
    id                  bigint      GENERATED ALWAYS AS IDENTITY,
    instrument_year     integer     NOT NULL,
    instrument_id       uuid        NOT NULL,
    seq                 integer     NOT NULL,
    action              text        NOT NULL,
    from_state          text,
    to_state            text        NOT NULL,
    event_date          date        NOT NULL,
    user_id             text        NOT NULL,
    reference           text,
    note                text,
    recorded_at         timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (fiscal_year, id),
    FOREIGN KEY (instrument_year, instrument_id) REFERENCES cashflow.instrument (origin_fiscal_year, id)
) PARTITION BY LIST (fiscal_year);

CREATE INDEX IF NOT EXISTS instrument_event_by_instrument ON cashflow.instrument_event (instrument_id, seq);

-- Transactional outbox: written in the same transaction as the change, published afterwards.
CREATE TABLE IF NOT EXISTS cashflow.outbox (
    id            uuid        PRIMARY KEY,
    type          text        NOT NULL,
    payload       jsonb       NOT NULL,
    occurred_at   timestamptz NOT NULL,
    published_at  timestamptz
);
CREATE INDEX IF NOT EXISTS cashflow_outbox_unpublished ON cashflow.outbox (occurred_at) WHERE published_at IS NULL;

-- Results of commands by idempotency key (double clicks and retries do not repeat a payment).
CREATE TABLE IF NOT EXISTS cashflow.idempotency (
    key         text        PRIMARY KEY,
    result      text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now()
);

GRANT USAGE ON SCHEMA cashflow TO cashflow_svc, treasury_migrator, treasury_readonly;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA cashflow TO cashflow_svc, treasury_migrator;
GRANT DELETE ON cashflow.idempotency, cashflow.outbox TO cashflow_svc;
GRANT SELECT ON ALL TABLES IN SCHEMA cashflow TO treasury_readonly;
ALTER DEFAULT PRIVILEGES FOR ROLE treasury_owner IN SCHEMA cashflow GRANT SELECT, INSERT, UPDATE ON TABLES TO cashflow_svc, treasury_migrator;
ALTER DEFAULT PRIVILEGES FOR ROLE treasury_owner IN SCHEMA cashflow GRANT SELECT ON TABLES TO treasury_readonly;
