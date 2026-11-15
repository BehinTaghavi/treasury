-- Fiscal-year partition management for the cashflow schema.
--   cashflow.open_fiscal_year(1406, '2027-03-21', '2028-03-19')  -- creates the year's partitions
--   cashflow.close_fiscal_year(1404)                            -- makes that year's rows read-only
--   cashflow.archive_fiscal_year(1390, 'archive_ts')            -- moves a closed year to cheaper storage
-- There is deliberately no DEFAULT partition: a row for a year that was never opened is an error, not a surprise.

CREATE OR REPLACE FUNCTION cashflow.ensure_fiscal_year_partitions(p_year integer)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.instrument FOR VALUES IN (%s)',
        'instrument_fy' || p_year, p_year);
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.instrument_event FOR VALUES IN (%s)',
        'instrument_event_fy' || p_year, p_year);
    -- Partitions belong to treasury_owner whoever opened the year (often an administrator), so that
    -- re-running the schema scripts can still grant on every table (apply.sh must stay safe to run again).
    EXECUTE format('ALTER TABLE cashflow.%I OWNER TO treasury_owner', 'instrument_fy' || p_year);
    EXECUTE format('ALTER TABLE cashflow.%I OWNER TO treasury_owner', 'instrument_event_fy' || p_year);
END
$$;

CREATE OR REPLACE FUNCTION cashflow.open_fiscal_year(p_year integer, p_starts date, p_ends date)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO cashflow.fiscal_period (fiscal_year, starts_on, ends_on, status)
    VALUES (p_year, p_starts, p_ends, 'Open')
    ON CONFLICT (fiscal_year) DO UPDATE SET starts_on = excluded.starts_on, ends_on = excluded.ends_on;
    PERFORM cashflow.ensure_fiscal_year_partitions(p_year);
END
$$;

-- Fiscal year of a date, from the local copy of the fiscal periods.
CREATE OR REPLACE FUNCTION cashflow.fiscal_year_of(p_date date)
RETURNS integer
LANGUAGE sql
STABLE
AS $$
    SELECT fiscal_year FROM cashflow.fiscal_period WHERE p_date BETWEEN starts_on AND ends_on
$$;

-- Blocks any change to rows of a closed year, whoever tries it (application, migration, manual SQL).
CREATE OR REPLACE FUNCTION cashflow.reject_change_in_closed_year()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Fiscal year % is closed; % on % is not allowed', TG_ARGV[0], TG_OP, TG_TABLE_NAME
        USING ERRCODE = 'P0001', HINT = 'Record a correcting entry in the open year instead.';
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

    FOREACH t IN ARRAY ARRAY['instrument_event_fy' || p_year]
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
END
$$;
