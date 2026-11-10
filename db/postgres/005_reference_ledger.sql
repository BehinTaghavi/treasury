-- Copy of the accounting system's accounts (owner, 10 Nov 2026), owned by reference-svc until posting-svc links the ERP.
-- Fixed part: four levels گروه / کل / معین / معین در معین, code lengths per company. Floating part: تفصیل with a type.
-- Each list is one aggregate (ChartOfAccounts, DetailAccountDirectory): its header row carries the version.

CREATE TABLE IF NOT EXISTS reference.ledger_chart (
    id              uuid        PRIMARY KEY,
    group_length    smallint    NOT NULL,
    general_length  smallint    NOT NULL,
    sub_length      smallint    NOT NULL,
    subsub_length   smallint    NOT NULL,
    detail_length   smallint    NOT NULL,
    version         bigint      NOT NULL DEFAULT 1,
    CHECK (0 < group_length AND group_length < general_length AND general_length < sub_length AND sub_length < subsub_length AND subsub_length <= 15),
    CHECK (detail_length BETWEEN 1 AND 15)
);
-- One chart per company database.
CREATE UNIQUE INDEX IF NOT EXISTS ledger_chart_single ON reference.ledger_chart ((true));

CREATE TABLE IF NOT EXISTS reference.ledger_account (
    code            text        PRIMARY KEY CHECK (code ~ '^[0-9]{1,15}$'),
    name            text        NOT NULL,
    level           smallint    NOT NULL CHECK (level BETWEEN 1 AND 4),
    parent_code     text        REFERENCES reference.ledger_account (code),
    is_active       boolean     NOT NULL,
    is_postable     boolean     NOT NULL,
    -- Types of تفصیل allowed in each position (owner, 10 Nov 2026: several types may be allowed; others are refused).
    detail1_types   text[]      NOT NULL DEFAULT '{}',
    detail2_types   text[]      NOT NULL DEFAULT '{}',
    updated_at      timestamptz NOT NULL DEFAULT now(),
    CHECK ((level = 1) = (parent_code IS NULL)),
    CHECK (cardinality(detail2_types) = 0 OR cardinality(detail1_types) > 0)
);
CREATE INDEX IF NOT EXISTS ledger_account_postable ON reference.ledger_account (code) WHERE is_postable AND is_active;

CREATE TABLE IF NOT EXISTS reference.detail_directory (
    id          uuid    PRIMARY KEY,
    version     bigint  NOT NULL DEFAULT 1
);
CREATE UNIQUE INDEX IF NOT EXISTS detail_directory_single ON reference.detail_directory ((true));

CREATE TABLE IF NOT EXISTS reference.detail_account (
    code            text        PRIMARY KEY CHECK (code ~ '^[0-9]{1,15}$'),
    name            text        NOT NULL,
    -- PersianText.Key of the name, computed by the application: search finds «بانك» and «بانک» alike.
    name_key        text        NOT NULL,
    type            text,
    is_cost_center  boolean     NOT NULL,
    is_active       boolean     NOT NULL,
    updated_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS detail_account_by_type ON reference.detail_account (type) WHERE is_active;

GRANT SELECT, INSERT, UPDATE ON reference.ledger_chart, reference.ledger_account, reference.detail_directory, reference.detail_account
    TO reference_svc, treasury_migrator;
GRANT SELECT ON reference.ledger_chart, reference.ledger_account, reference.detail_directory, reference.detail_account TO treasury_readonly;
