-- Schema of requests-svc (Payment Requests, درخواست وجه), owner 15 Nov 2026. New-version manual «ب-صدور درخواست وجه»; old Access
-- form Porder. Requester, beneficiary, purpose group, department and the account paid from belong to reference-svc and accounts-svc:
-- only their ids and names at the time of the request are kept here (no cross-schema foreign keys, ADR 0001).
-- Transactional, so LIST-partitioned by fiscal year (doc 7.3).

CREATE SCHEMA IF NOT EXISTS requests AUTHORIZATION treasury_owner;

CREATE TABLE IF NOT EXISTS requests.payment_request (
    fiscal_year             integer        NOT NULL,
    id                      uuid           NOT NULL,
    number                  integer        NOT NULL CHECK (number > 0),
    request_date            date           NOT NULL,
    requester_id            uuid           NOT NULL,
    requester_name          text           NOT NULL,
    requester_department_id uuid,
    cost_center_code        text,
    beneficiary_id          uuid           NOT NULL,
    beneficiary_detail_code text           NOT NULL,
    beneficiary_name        text           NOT NULL,
    purpose_group_id        uuid           NOT NULL,
    purpose_group_name      text           NOT NULL,
    purpose                 text           NOT NULL CHECK (btrim(purpose) <> ''),
    reference1              text,
    reference2              text,
    amount                  numeric(20, 2) NOT NULL CHECK (amount > 0),
    currency                char(3)        NOT NULL CHECK (currency ~ '^[A-Z]{3}$'),
    method                  text           NOT NULL CHECK (method IN ('CurrentCheque', 'PostDatedCheque', 'GuaranteeCheque', 'Cash', 'PromissoryNote',
                                                                      'GuaranteeNote', 'Transfer', 'CardToCard')),
    priority                smallint       NOT NULL CHECK (priority BETWEEN 1 AND 3),
    transfer_account        text,
    card_number             text,
    pay_from_kind           text           CHECK (pay_from_kind IN ('BankAccount', 'CashDesk')),
    pay_from_id             uuid,
    due_date                date,
    department_id           uuid,
    has_main_beneficiaries  boolean        NOT NULL DEFAULT false,
    attachments             integer        NOT NULL DEFAULT 0 CHECK (attachments BETWEEN 0 AND 999),
    note                    text,
    state                   text           NOT NULL CHECK (state IN ('Registered', 'Approved', 'Paid', 'Cancelled')),
    recorded_by             text           NOT NULL,
    approved_by             text,
    approved_at             timestamptz,
    cancel_reason           text,
    version                 bigint         NOT NULL DEFAULT 1,
    created_at              timestamptz    NOT NULL DEFAULT now(),
    updated_at              timestamptz    NOT NULL DEFAULT now(),
    PRIMARY KEY (fiscal_year, id),
    CONSTRAINT payment_request_number UNIQUE (fiscal_year, number),
    CHECK (currency <> 'IRR' OR amount = trunc(amount)),
    CHECK ((method = 'Transfer') = (transfer_account IS NOT NULL)),
    CHECK ((method = 'CardToCard') = (card_number IS NOT NULL)),
    CHECK ((pay_from_kind IS NULL) = (pay_from_id IS NULL)),
    CHECK (state <> 'Approved' OR approved_by IS NOT NULL),
    CHECK (state <> 'Cancelled' OR cancel_reason IS NOT NULL)
) PARTITION BY LIST (fiscal_year);

CREATE INDEX IF NOT EXISTS payment_request_by_id ON requests.payment_request (id);
CREATE INDEX IF NOT EXISTS payment_request_approved ON requests.payment_request (method, priority) WHERE state = 'Approved';

-- Last number given per year; the row is locked by the UPDATE until the transaction ends.
CREATE TABLE IF NOT EXISTS requests.request_number (
    fiscal_year  integer PRIMARY KEY,
    last_number  integer NOT NULL CHECK (last_number >= 0)
);

CREATE TABLE IF NOT EXISTS requests.outbox (
    id            uuid        PRIMARY KEY,
    type          text        NOT NULL,
    payload       jsonb       NOT NULL,
    occurred_at   timestamptz NOT NULL,
    published_at  timestamptz
);
CREATE INDEX IF NOT EXISTS requests_outbox_unpublished ON requests.outbox (occurred_at) WHERE published_at IS NULL;

CREATE TABLE IF NOT EXISTS requests.idempotency (
    key         text        PRIMARY KEY,
    result      text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE OR REPLACE FUNCTION requests.ensure_fiscal_year_partitions(p_year integer)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    EXECUTE format('CREATE TABLE IF NOT EXISTS requests.%I PARTITION OF requests.payment_request FOR VALUES IN (%s)', 'payment_request_fy' || p_year, p_year);
    EXECUTE format('ALTER TABLE requests.%I OWNER TO treasury_owner', 'payment_request_fy' || p_year);
END
$$;

-- Opening a fiscal year (cashflow.open_fiscal_year, called by the administrator and by deploy) now creates the requests partition too,
-- so a year is opened once for every service. Same body as 003, plus the requests partition.
CREATE OR REPLACE FUNCTION cashflow.open_fiscal_year(p_year integer, p_starts date, p_ends date)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO cashflow.fiscal_period (fiscal_year, starts_on, ends_on, status)
    VALUES (p_year, p_starts, p_ends, 'Open')
    ON CONFLICT (fiscal_year) DO UPDATE SET starts_on = excluded.starts_on, ends_on = excluded.ends_on;
    PERFORM cashflow.ensure_fiscal_year_partitions(p_year);
    PERFORM requests.ensure_fiscal_year_partitions(p_year);
END
$$;

-- Requests of a closed year are read-only, like its receipts and instrument steps.
CREATE OR REPLACE FUNCTION cashflow.close_fiscal_year(p_year integer)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    t text;
BEGIN
    UPDATE cashflow.fiscal_period SET status = 'Closed' WHERE fiscal_year = p_year;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Fiscal year % does not exist', p_year;
    END IF;

    FOREACH t IN ARRAY ARRAY['cashflow.instrument_event_fy' || p_year, 'cashflow.receipt_fy' || p_year, 'requests.payment_request_fy' || p_year]
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS closed_year_guard ON %s', t);
        EXECUTE format(
            'CREATE TRIGGER closed_year_guard BEFORE INSERT OR UPDATE OR DELETE ON %s FOR EACH ROW EXECUTE FUNCTION cashflow.reject_change_in_closed_year(%L)',
            t, p_year);
    END LOOP;
    -- Note: the instrument partition of a closed year stays writable on purpose: a cheque received in 1404 and
    -- cleared in 1405 changes state after 1404 is closed. Its new step is written in the 1405 event partition.
END
$$;

-- Years opened before this script get their requests partition now.
SELECT requests.ensure_fiscal_year_partitions(fiscal_year) FROM cashflow.fiscal_period;

GRANT USAGE ON SCHEMA requests TO requests_svc, treasury_migrator, treasury_readonly;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA requests TO requests_svc, treasury_migrator;
GRANT DELETE ON requests.idempotency, requests.outbox TO requests_svc;
GRANT SELECT ON ALL TABLES IN SCHEMA requests TO treasury_readonly;
ALTER DEFAULT PRIVILEGES FOR ROLE treasury_owner IN SCHEMA requests GRANT SELECT, INSERT, UPDATE ON TABLES TO requests_svc, treasury_migrator;
ALTER DEFAULT PRIVILEGES FOR ROLE treasury_owner IN SCHEMA requests GRANT SELECT ON TABLES TO treasury_readonly;
