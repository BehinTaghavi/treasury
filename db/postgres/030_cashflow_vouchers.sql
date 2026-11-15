-- Treasury vouchers (owner's report no. 4, item 8; his template table and answers of 15 Nov 2026).
-- voucher_account: the company's codes of the fixed accounts of the template («شابلون اسناد خزانه داری»).
-- voucher: vouchers the treasurer confirmed, one per receipt (step 0) or per step of a received cheque; lines kept as confirmed.
CREATE TABLE IF NOT EXISTS cashflow.voucher_account (
    account_key          text        PRIMARY KEY CHECK (account_key IN ('ChequesInCashDesk', 'ChequesInCollection', 'ProtestedCheques',
                                                                       'GuaranteeChequesForCompany', 'GuaranteeCounterpart')),
    ledger_account_code  text        NOT NULL CHECK (btrim(ledger_account_code) <> ''),
    updated_at           timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS cashflow.voucher (
    fiscal_year   integer        NOT NULL,
    id            uuid           NOT NULL,
    source        text           NOT NULL CHECK (source IN ('Receipt', 'ChequeStep')),
    source_id     uuid           NOT NULL,
    step          integer        NOT NULL CHECK (step >= 0),
    operation     text           NOT NULL,
    voucher_date  date           NOT NULL,
    lines         jsonb          NOT NULL,
    total_rial    numeric(22, 0) NOT NULL,
    confirmed_by  text           NOT NULL,
    confirmed_at  timestamptz    NOT NULL,
    version       bigint         NOT NULL DEFAULT 1,
    PRIMARY KEY (fiscal_year, id),
    CONSTRAINT voucher_once UNIQUE (fiscal_year, source, source_id, step),
    CHECK ((source = 'Receipt') = (step = 0))
) PARTITION BY LIST (fiscal_year);
CREATE INDEX IF NOT EXISTS voucher_by_source ON cashflow.voucher (source_id, step);

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
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.voucher FOR VALUES IN (%s)', 'voucher_fy' || p_year, p_year);
    FOREACH t IN ARRAY ARRAY['instrument_fy' || p_year, 'instrument_event_fy' || p_year, 'receipt_fy' || p_year, 'voucher_fy' || p_year]
    LOOP
        EXECUTE format('ALTER TABLE cashflow.%I OWNER TO treasury_owner', t);
    END LOOP;
END
$$;

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

    FOREACH t IN ARRAY ARRAY['cashflow.instrument_event_fy' || p_year, 'cashflow.receipt_fy' || p_year, 'cashflow.voucher_fy' || p_year,
                             'requests.payment_request_fy' || p_year, 'requests.request_file_fy' || p_year]
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

SELECT cashflow.ensure_fiscal_year_partitions(fiscal_year) FROM cashflow.fiscal_period;

GRANT SELECT, INSERT, UPDATE, DELETE ON cashflow.voucher_account TO cashflow_svc;
-- A confirmed voucher is never changed (the default privileges of 002 would allow UPDATE).
REVOKE UPDATE, DELETE ON cashflow.voucher FROM cashflow_svc;
GRANT SELECT, INSERT ON cashflow.voucher TO cashflow_svc;
GRANT SELECT, INSERT, UPDATE ON cashflow.voucher_account, cashflow.voucher TO treasury_migrator;
GRANT SELECT ON cashflow.voucher_account, cashflow.voucher TO treasury_readonly;
