-- Basic Information lists of reference-svc: currencies, countries, provinces, cities, bank names.
-- New-version manual "اطلاعات پایه" items 1–4; old tables Bank (Access), BasicBanks, City, ExchangeTypes (MVC).
-- Entries are never deleted, only deactivated (other services and old data refer to them).

CREATE TABLE IF NOT EXISTS reference.catalogue_item (
    id          uuid        PRIMARY KEY,
    kind        text        NOT NULL CHECK (kind IN ('Currency', 'Country', 'Province', 'City', 'Bank')),
    parent_id   uuid        REFERENCES reference.catalogue_item (id),
    name        text        NOT NULL CHECK (length(name) BETWEEN 1 AND 100),
    -- Comparison key computed by the application (PersianText.Key: Arabic ي/ك = Persian ی/ک, no spaces).
    name_key    text        NOT NULL,
    code        char(3)     CHECK (code ~ '^[A-Z]{3}$'),
    is_active   boolean     NOT NULL DEFAULT true,
    version     bigint      NOT NULL DEFAULT 1,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now(),
    CHECK ((kind = 'Currency') = (code IS NOT NULL)),
    CHECK ((kind IN ('Province', 'City')) = (parent_id IS NOT NULL))
);

-- The same name only once per list and parent (two provinces may each have a city of the same name).
CREATE UNIQUE INDEX IF NOT EXISTS catalogue_item_unique_name
    ON reference.catalogue_item (kind, parent_id, name_key) NULLS NOT DISTINCT;
CREATE UNIQUE INDEX IF NOT EXISTS catalogue_item_unique_currency ON reference.catalogue_item (code) WHERE code IS NOT NULL;
CREATE INDEX IF NOT EXISTS catalogue_item_by_parent ON reference.catalogue_item (parent_id) WHERE parent_id IS NOT NULL;

-- Results of commands by idempotency key (double clicks and retries do not add an entry twice).
CREATE TABLE IF NOT EXISTS reference.idempotency (
    key         text        PRIMARY KEY,
    result      text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now()
);

GRANT SELECT, INSERT, UPDATE ON reference.catalogue_item, reference.idempotency TO reference_svc, treasury_migrator;
GRANT DELETE ON reference.idempotency, reference.outbox TO reference_svc;
GRANT SELECT ON reference.catalogue_item TO treasury_readonly;
