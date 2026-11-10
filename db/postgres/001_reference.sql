-- Schema of reference-svc (Organization & Reference Data, Fiscal Periods).
-- Only the fiscal-year master is defined here for now; branches, currencies, banks follow in Phase 1.

CREATE SCHEMA IF NOT EXISTS reference AUTHORIZATION treasury_owner;

CREATE TABLE IF NOT EXISTS reference.fiscal_year (
    id          integer     PRIMARY KEY CHECK (id BETWEEN 1300 AND 1500),  -- Jalali year, e.g. 1405
    starts_on   date        NOT NULL,
    ends_on     date        NOT NULL,
    status      text        NOT NULL DEFAULT 'Open' CHECK (status IN ('Open', 'Closing', 'Closed')),
    opened_at   timestamptz NOT NULL DEFAULT now(),
    closed_at   timestamptz,
    CHECK (ends_on > starts_on)
);

-- Years may not overlap.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fiscal_year_no_overlap') THEN
        ALTER TABLE reference.fiscal_year
            ADD CONSTRAINT fiscal_year_no_overlap EXCLUDE USING gist (daterange(starts_on, ends_on, '[]') WITH &&);
    END IF;
END
$$;

-- Events of reference-svc (FiscalYearOpened, FiscalYearClosed, ...) — same transaction as the change.
CREATE TABLE IF NOT EXISTS reference.outbox (
    id            uuid        PRIMARY KEY,
    type          text        NOT NULL,
    payload       jsonb       NOT NULL,
    occurred_at   timestamptz NOT NULL,
    published_at  timestamptz
);
CREATE INDEX IF NOT EXISTS outbox_unpublished ON reference.outbox (occurred_at) WHERE published_at IS NULL;

GRANT USAGE ON SCHEMA reference TO reference_svc, treasury_migrator, treasury_readonly;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA reference TO reference_svc, treasury_migrator;
GRANT SELECT ON ALL TABLES IN SCHEMA reference TO treasury_readonly;
