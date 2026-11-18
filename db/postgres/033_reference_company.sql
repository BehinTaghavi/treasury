-- «اطلاعات شرکت» of reference-svc (owner, 16 Nov 2026): the name and logo (آرم) of the company that owns this database, entered in
-- Basic Information and shown at the top of every screen. One database per client company: the table holds at most one row.
-- The logo (PNG/JPEG, at most 1 MB) is kept apart so reading the name never loads the picture.

CREATE TABLE IF NOT EXISTS reference.company (
    id          uuid        PRIMARY KEY,
    single      boolean     NOT NULL DEFAULT true CONSTRAINT company_single UNIQUE CHECK (single),
    name        text        NOT NULL CHECK (length(name) BETWEEN 1 AND 100),
    logo_type   text        CHECK (logo_type IN ('image/png', 'image/jpeg')),
    version     bigint      NOT NULL DEFAULT 1,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS reference.company_logo (
    company_id  uuid   PRIMARY KEY REFERENCES reference.company (id) ON DELETE CASCADE,
    data        bytea  NOT NULL CHECK (octet_length(data) BETWEEN 1 AND 1048576)
);

GRANT SELECT, INSERT, UPDATE ON reference.company TO reference_svc;
GRANT SELECT, INSERT, UPDATE, DELETE ON reference.company_logo TO reference_svc;
GRANT SELECT, INSERT, UPDATE ON reference.company, reference.company_logo TO treasury_migrator;
GRANT SELECT ON reference.company, reference.company_logo TO treasury_readonly;
