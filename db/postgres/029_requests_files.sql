-- Owner's report no. 4 (15 Nov 2026), item 12: scanned supporting documents attached to a payment request. Kept in requests-svc's own
-- schema until a documents service exists (design document: documents-svc). PDF, JPEG or PNG, at most 5 MB (checked by the domain and here).
-- Partitioned by the request's fiscal year like the requests themselves.
CREATE TABLE IF NOT EXISTS requests.request_file (
    fiscal_year   integer     NOT NULL,
    file_id       uuid        NOT NULL,
    request_id    uuid        NOT NULL,
    file_name     text        NOT NULL,
    content_type  text        NOT NULL CHECK (content_type IN ('application/pdf', 'image/jpeg', 'image/png')),
    size          integer     NOT NULL CHECK (size > 0 AND size <= 5242880),
    uploaded_by   text        NOT NULL,
    uploaded_at   timestamptz NOT NULL,
    content       bytea       NOT NULL,
    PRIMARY KEY (fiscal_year, file_id),
    CHECK (octet_length(content) = size)
) PARTITION BY LIST (fiscal_year);
CREATE INDEX IF NOT EXISTS request_file_by_request ON requests.request_file (request_id, uploaded_at);

CREATE OR REPLACE FUNCTION requests.ensure_fiscal_year_partitions(p_year integer)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    t text;
BEGIN
    EXECUTE format('CREATE TABLE IF NOT EXISTS requests.%I PARTITION OF requests.payment_request FOR VALUES IN (%s)', 'payment_request_fy' || p_year, p_year);
    EXECUTE format('CREATE TABLE IF NOT EXISTS requests.%I PARTITION OF requests.request_file FOR VALUES IN (%s)', 'request_file_fy' || p_year, p_year);
    FOREACH t IN ARRAY ARRAY['payment_request_fy' || p_year, 'request_file_fy' || p_year]
    LOOP
        EXECUTE format('ALTER TABLE requests.%I OWNER TO treasury_owner', t);
    END LOOP;
END
$$;

SELECT requests.ensure_fiscal_year_partitions(fiscal_year) FROM cashflow.fiscal_period;

GRANT SELECT, INSERT, DELETE ON requests.request_file TO requests_svc;
GRANT SELECT, INSERT ON requests.request_file TO treasury_migrator;
GRANT SELECT ON requests.request_file TO treasury_readonly;

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

    FOREACH t IN ARRAY ARRAY['cashflow.instrument_event_fy' || p_year, 'cashflow.receipt_fy' || p_year, 'requests.payment_request_fy' || p_year,
                             'requests.request_file_fy' || p_year]
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
