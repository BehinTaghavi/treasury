-- Receipts (رسید دریافت), owner 14 Nov 2026: cash into a cash desk, money paid into a bank account (deposit slip, transfer, POS,
-- card-to-card), and cheques and promissory notes from one payer. Old program: Subs rows with Scode 17 (دريافت نقدي) and 16
-- (واريز به حساب), D_Check for cheques; each kind numbered per year (Sno = DMax + 1).
-- Transactional, so LIST-partitioned by fiscal year like the instruments (doc 7.3).

CREATE TABLE IF NOT EXISTS cashflow.receipt (
    fiscal_year         integer        NOT NULL,
    id                  uuid           NOT NULL,
    kind                text           NOT NULL CHECK (kind IN ('Cash', 'BankDeposit', 'Instruments')),
    number              integer        NOT NULL CHECK (number > 0),
    receipt_date        date           NOT NULL,
    payer_id            uuid           NOT NULL,
    payer_name          text           NOT NULL,
    payer_detail_code   text           NOT NULL,
    purpose_group_id    uuid           NOT NULL,
    purpose_group_name  text           NOT NULL,
    purpose             text           NOT NULL CHECK (btrim(purpose) <> ''),
    reference1          text,
    reference2          text,
    department_id       uuid,
    sales_proceeds      boolean        NOT NULL DEFAULT false,
    note                text,
    recorded_by         text           NOT NULL,
    account_kind        text           CHECK (account_kind IN ('CashDesk', 'BankAccount')),
    account_id          uuid,
    deposit_method      text           CHECK (deposit_method IN ('DepositSlip', 'Transfer', 'Pos', 'CardToCard')),
    tracking_number     text,
    amount              numeric(20, 2),
    currency            char(3),
    rate_to_rial        numeric(20, 4),
    instrument_ids      uuid[]         NOT NULL DEFAULT '{}',
    total_rial          numeric(22, 0) NOT NULL CHECK (total_rial > 0),
    state               text           NOT NULL CHECK (state IN ('Recorded', 'Cancelled')),
    cancelled_on        date,
    cancel_reason       text,
    version             bigint         NOT NULL DEFAULT 1,
    created_at          timestamptz    NOT NULL DEFAULT now(),
    updated_at          timestamptz    NOT NULL DEFAULT now(),
    PRIMARY KEY (fiscal_year, id),
    CONSTRAINT receipt_number_per_kind UNIQUE (fiscal_year, kind, number),
    -- Cash goes into a cash desk, a deposit into a bank account; cheques and notes wait in the cash desk as instruments.
    CHECK (kind = 'Cash' AND account_kind = 'CashDesk' AND deposit_method IS NULL
        OR kind = 'BankDeposit' AND account_kind = 'BankAccount' AND deposit_method IS NOT NULL
        OR kind = 'Instruments' AND account_kind IS NULL AND account_id IS NULL AND cardinality(instrument_ids) > 0),
    CHECK (kind = 'Instruments' OR account_id IS NOT NULL AND amount > 0 AND currency IS NOT NULL
        AND (currency = 'IRR' AND rate_to_rial IS NULL OR currency <> 'IRR' AND rate_to_rial > 0)),
    CHECK (state = 'Recorded' AND cancelled_on IS NULL OR state = 'Cancelled' AND cancelled_on >= receipt_date AND cancel_reason IS NOT NULL)
) PARTITION BY LIST (fiscal_year);

CREATE INDEX IF NOT EXISTS receipt_by_id ON cashflow.receipt (id);
CREATE INDEX IF NOT EXISTS receipt_by_payer ON cashflow.receipt (payer_detail_code, receipt_date);

-- Last number given per year and kind. The row is locked by the UPDATE until the transaction ends, so two users never get the
-- same number; a number taken by a transaction that rolls back is given again.
CREATE TABLE IF NOT EXISTS cashflow.receipt_number (
    fiscal_year  integer NOT NULL,
    kind         text    NOT NULL CHECK (kind IN ('Cash', 'BankDeposit', 'Instruments')),
    last_number  integer NOT NULL CHECK (last_number >= 0),
    PRIMARY KEY (fiscal_year, kind)
);

-- Received cheques and notes name their receipt and carry what is written on them (branch, account, holder, Sayad ID,
-- guarantors); the payer's تفصیل code is kept next to the derived counterparty id.
ALTER TABLE cashflow.instrument ADD COLUMN IF NOT EXISTS receipt_id uuid;
ALTER TABLE cashflow.instrument ADD COLUMN IF NOT EXISTS counterparty_detail_code text;
ALTER TABLE cashflow.instrument ADD COLUMN IF NOT EXISTS details jsonb;
CREATE INDEX IF NOT EXISTS instrument_by_receipt ON cashflow.instrument (receipt_id) WHERE receipt_id IS NOT NULL;

-- A year's partitions now include the receipts.
CREATE OR REPLACE FUNCTION cashflow.ensure_fiscal_year_partitions(p_year integer)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    t text;
BEGIN
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.instrument FOR VALUES IN (%s)', 'instrument_fy' || p_year, p_year);
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.instrument_event FOR VALUES IN (%s)', 'instrument_event_fy' || p_year, p_year);
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.receipt FOR VALUES IN (%s)', 'receipt_fy' || p_year, p_year);
    -- Partitions belong to treasury_owner whoever opened the year (often an administrator), so that
    -- re-running the schema scripts can still grant on every table (apply.sh must stay safe to run again).
    FOREACH t IN ARRAY ARRAY['instrument_fy' || p_year, 'instrument_event_fy' || p_year, 'receipt_fy' || p_year]
    LOOP
        EXECUTE format('ALTER TABLE cashflow.%I OWNER TO treasury_owner', t);
    END LOOP;
END
$$;

-- Receipts of a closed year are read-only like its instrument steps (a receipt is never cancelled in a closed year).
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

    FOREACH t IN ARRAY ARRAY['instrument_event_fy' || p_year, 'receipt_fy' || p_year]
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS closed_year_guard ON cashflow.%I', t);
        EXECUTE format(
            'CREATE TRIGGER closed_year_guard BEFORE INSERT OR UPDATE OR DELETE ON cashflow.%I FOR EACH ROW EXECUTE FUNCTION cashflow.reject_change_in_closed_year(%L)',
            t, p_year);
    END LOOP;
    -- Note: the instrument partition of a closed year stays writable on purpose: a cheque received in 1404 and
    -- cleared in 1405 changes state after 1404 is closed. Its new step is written in the 1405 event partition.
END
$$;

CREATE OR REPLACE FUNCTION cashflow.archive_fiscal_year(p_year integer, p_tablespace text)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    IF (SELECT status FROM cashflow.fiscal_period WHERE fiscal_year = p_year) IS DISTINCT FROM 'Closed' THEN
        RAISE EXCEPTION 'Only a closed fiscal year can be archived (year %)', p_year;
    END IF;
    EXECUTE format('ALTER TABLE cashflow.%I SET TABLESPACE %I', 'instrument_event_fy' || p_year, p_tablespace);
    EXECUTE format('ALTER TABLE cashflow.%I SET TABLESPACE %I', 'receipt_fy' || p_year, p_tablespace);
END
$$;

-- Years opened before this script get their receipt partition now; a year closed before gets its guard.
SELECT cashflow.ensure_fiscal_year_partitions(fiscal_year) FROM cashflow.fiscal_period;
DO $$
DECLARE
    y integer;
BEGIN
    FOR y IN SELECT fiscal_year FROM cashflow.fiscal_period WHERE status = 'Closed'
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS closed_year_guard ON cashflow.%I', 'receipt_fy' || y);
        EXECUTE format(
            'CREATE TRIGGER closed_year_guard BEFORE INSERT OR UPDATE OR DELETE ON cashflow.%I FOR EACH ROW EXECUTE FUNCTION cashflow.reject_change_in_closed_year(%L)',
            'receipt_fy' || y, y);
    END LOOP;
END
$$;

GRANT SELECT, INSERT, UPDATE ON cashflow.receipt, cashflow.receipt_number TO cashflow_svc, treasury_migrator;
GRANT SELECT ON cashflow.receipt, cashflow.receipt_number TO treasury_readonly;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA cashflow TO cashflow_svc, treasury_migrator;
GRANT SELECT ON ALL TABLES IN SCHEMA cashflow TO treasury_readonly;
