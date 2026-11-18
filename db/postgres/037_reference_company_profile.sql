-- The company's own particulars («مشخصات شرکت», Basic Information; owner, 18 Nov 2026): name, address, and the legal address
-- («آدرس قانونی») with its city and postal code. A received cheque is «خارج از مرکز» when the city of its bank is not this city.
-- One row per client database.
CREATE TABLE IF NOT EXISTS reference.company_profile (
    only_one       boolean     PRIMARY KEY DEFAULT true CHECK (only_one),
    name           text        NOT NULL CHECK (btrim(name) <> ''),
    address        text,
    legal_address  text,
    city_id        uuid        NOT NULL,                              -- city of the legal address
    postal_code    text        CHECK (postal_code ~ '^[0-9]{10}$'),   -- postal code of the legal address
    version        bigint      NOT NULL DEFAULT 1,
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now()
);

GRANT SELECT, INSERT, UPDATE ON reference.company_profile TO reference_svc, treasury_migrator;
GRANT SELECT ON reference.company_profile TO treasury_readonly;
