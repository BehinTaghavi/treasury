-- Legal action on a bounced cheque (owner, 18 Nov 2026): the screen «اقدام حقوقی» keeps the scans of the action's papers with the cheque.
-- Kept in cashflow-svc's own schema until a documents service exists, like the scans of payment requests (029). PDF, JPEG or PNG, at
-- most 5 MB (checked by the domain and here). Partitioned by the fiscal year the scan was added in.
CREATE TABLE IF NOT EXISTS cashflow.instrument_file (
    fiscal_year    integer     NOT NULL,
    file_id        uuid        NOT NULL,
    instrument_id  uuid        NOT NULL,
    step           integer     NOT NULL CHECK (step >= 1),   -- the step of the cheque's history the scan belongs to (the legal action)
    file_name      text        NOT NULL,
    content_type   text        NOT NULL CHECK (content_type IN ('application/pdf', 'image/jpeg', 'image/png')),
    size           integer     NOT NULL CHECK (size > 0 AND size <= 5242880),
    uploaded_by    text        NOT NULL,
    uploaded_at    timestamptz NOT NULL,
    content        bytea       NOT NULL,
    PRIMARY KEY (fiscal_year, file_id),
    CHECK (octet_length(content) = size)
) PARTITION BY LIST (fiscal_year);
CREATE INDEX IF NOT EXISTS instrument_file_by_instrument ON cashflow.instrument_file (instrument_id, uploaded_at);

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
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.instrument_file FOR VALUES IN (%s)', 'instrument_file_fy' || p_year, p_year);
    FOREACH t IN ARRAY ARRAY['instrument_fy' || p_year, 'instrument_event_fy' || p_year, 'receipt_fy' || p_year, 'voucher_fy' || p_year,
                             'instrument_file_fy' || p_year]
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
                             'cashflow.instrument_file_fy' || p_year, 'requests.payment_request_fy' || p_year, 'requests.request_file_fy' || p_year]
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

-- Years opened before this script get their partition now; a year closed before gets its guard.
SELECT cashflow.ensure_fiscal_year_partitions(fiscal_year) FROM cashflow.fiscal_period;
DO $$
DECLARE
    y integer;
BEGIN
    FOR y IN SELECT fiscal_year FROM cashflow.fiscal_period WHERE status = 'Closed'
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS closed_year_guard ON cashflow.%I', 'instrument_file_fy' || y);
        EXECUTE format(
            'CREATE TRIGGER closed_year_guard BEFORE INSERT OR UPDATE OR DELETE ON cashflow.%I FOR EACH ROW EXECUTE FUNCTION cashflow.reject_change_in_closed_year(%L)',
            'instrument_file_fy' || y, y);
    END LOOP;
END
$$;

-- A scan is added or removed, never changed (the default privileges of 002 would allow UPDATE).
REVOKE UPDATE ON cashflow.instrument_file FROM cashflow_svc;
GRANT SELECT, INSERT, DELETE ON cashflow.instrument_file TO cashflow_svc;
GRANT SELECT, INSERT ON cashflow.instrument_file TO treasury_migrator;
GRANT SELECT ON cashflow.instrument_file TO treasury_readonly;
