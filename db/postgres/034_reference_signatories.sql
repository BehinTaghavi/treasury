-- Holders of authorised signatures («دارندگان امضای مجاز») and the signature rule of each kind of valuable document, in reference-svc
-- (owner, 16 Nov 2026, answers to Q41 and Q45–Q47): name, position, the staff تفصیل when there is one, national code, mobile and
-- landline numbers; for each kind of document the number of signatures needed, the compulsory and the optional signatories. One rule
-- serves every bank account and amount.
CREATE TABLE IF NOT EXISTS reference.signatory (
    id             uuid        PRIMARY KEY,
    full_name      text        NOT NULL CHECK (btrim(full_name) <> ''),
    name_key       text        NOT NULL,
    position       text        NOT NULL CHECK (btrim(position) <> ''),
    detail_code    text,
    national_code  text        CHECK (national_code ~ '^[0-9]{10}$'),
    mobile         text        CHECK (mobile ~ '^09[0-9]{9}$'),
    phone          text,
    is_active      boolean     NOT NULL DEFAULT true,
    version        bigint      NOT NULL DEFAULT 1,
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS signatory_unique_name ON reference.signatory (name_key);
CREATE UNIQUE INDEX IF NOT EXISTS signatory_unique_national_code ON reference.signatory (national_code) WHERE national_code IS NOT NULL;

-- One rule per kind of document. compulsory and optional hold signatory ids; a signatory is in one of them only, and the number of
-- signatures lies between the compulsory ones and all the signatories named.
CREATE TABLE IF NOT EXISTS reference.signature_rule (
    document        text        PRIMARY KEY CHECK (document IN ('IssuedCheque', 'IssuedNote', 'ChequeReceipt', 'NoteReceipt', 'ChequePaymentSheet', 'NotePaymentSheet')),
    id              uuid        NOT NULL UNIQUE,
    required_count  integer     NOT NULL CHECK (required_count >= 0),
    compulsory      uuid[]      NOT NULL DEFAULT '{}',
    optional        uuid[]      NOT NULL DEFAULT '{}',
    version         bigint      NOT NULL DEFAULT 1,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),
    CHECK (NOT compulsory && optional),
    CHECK (required_count BETWEEN cardinality(compulsory) AND cardinality(compulsory) + cardinality(optional))
);

GRANT SELECT, INSERT, UPDATE, DELETE ON reference.signatory TO reference_svc;
GRANT SELECT, INSERT, UPDATE ON reference.signature_rule TO reference_svc;
GRANT SELECT, INSERT, UPDATE ON reference.signatory, reference.signature_rule TO treasury_migrator;
GRANT SELECT ON reference.signatory, reference.signature_rule TO treasury_readonly;
