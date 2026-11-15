-- References (عطف), purpose groups (گروه بابت) and purposes (بابت) of reference-svc: new-version manual items 8 and 9;
-- old tables Babs (Access: groups GBid = 0, purposes GBid <> 0), MVC BehalfGroup, Behalfs, ReferenceType, ReferenceTypes_By_BehalfGroup.

-- References are a list of the Basic Information catalogue (kind 'Reference').
ALTER TABLE reference.catalogue_item DROP CONSTRAINT IF EXISTS catalogue_item_kind_check;
ALTER TABLE reference.catalogue_item ADD CONSTRAINT catalogue_item_kind_check
    CHECK (kind IN ('Currency', 'Country', 'Province', 'City', 'Bank', 'Reference'));

CREATE TABLE IF NOT EXISTS reference.purpose_group (
    id              uuid        PRIMARY KEY,
    number          integer     NOT NULL CHECK (number > 0),
    direction       text        NOT NULL CHECK (direction IN ('Receipt', 'Payment')),
    name            text        NOT NULL CHECK (length(name) BETWEEN 1 AND 100),
    name_key        text        NOT NULL,                 -- PersianText.Key(name), computed by the application
    ledger_account  text        NOT NULL,                 -- معین / معین در معین of reference.ledger_account (checked by the application)
    reference1_id   uuid        CONSTRAINT purpose_group_reference1 REFERENCES reference.catalogue_item (id),
    reference2_id   uuid        CONSTRAINT purpose_group_reference2 REFERENCES reference.catalogue_item (id),
    is_active       boolean     NOT NULL DEFAULT true,
    in_use          boolean     NOT NULL DEFAULT false,   -- a receipt or payment names it: never deleted
    version         bigint      NOT NULL DEFAULT 1,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),
    CHECK (reference2_id IS NULL OR reference1_id IS NOT NULL),
    CHECK (reference1_id IS NULL OR reference1_id IS DISTINCT FROM reference2_id)
);
CREATE UNIQUE INDEX IF NOT EXISTS purpose_group_unique_number ON reference.purpose_group (number);
CREATE UNIQUE INDEX IF NOT EXISTS purpose_group_unique_name ON reference.purpose_group (direction, name_key);

CREATE TABLE IF NOT EXISTS reference.purpose (
    id          uuid        PRIMARY KEY,
    group_id    uuid        NOT NULL CONSTRAINT purpose_group REFERENCES reference.purpose_group (id),
    name        text        NOT NULL CHECK (length(name) BETWEEN 1 AND 150),
    name_key    text        NOT NULL,
    is_active   boolean     NOT NULL DEFAULT true,
    in_use      boolean     NOT NULL DEFAULT false,
    version     bigint      NOT NULL DEFAULT 1,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS purpose_unique_name ON reference.purpose (group_id, name_key);

GRANT SELECT, INSERT, UPDATE, DELETE ON reference.purpose_group, reference.purpose TO reference_svc;
GRANT SELECT, INSERT, UPDATE ON reference.purpose_group, reference.purpose TO treasury_migrator;
GRANT SELECT ON reference.purpose_group, reference.purpose TO treasury_readonly;
