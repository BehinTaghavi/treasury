-- Owner's report no. 8 (21 Nov 2026): «دارندگان امضای مجاز» becomes two pages of Basic Information.
--   1. «دارندگان امضای مجاز (اسناد بهادار)»: the position is chosen from a list (with «سایر» the user types it), the extension inside
--      the organisation, the e-mail address and the scan of the specimen signature are added, and two ticks say what the person may
--      sign (cheques, notes). Who signs the cheques of a bank account is kept with the bank account (049, accounts-svc), which marks
--      the signatory in_use through reference-svc's API.
--   2. «قاعده امضاء اسناد بهادار» (one rule per kind of document, 034) is removed completely.
--   3. «دارندگان مجوزها (فرم‌ها)»: the staff who confirm or approve treasury operations, with their roles and, per role, the operations.

-- ---- 1. signatories ----
ALTER TABLE reference.signatory ADD COLUMN IF NOT EXISTS position_kind text NOT NULL DEFAULT 'Other'
    CHECK (position_kind IN ('ManagingDirector', 'ManagingDirectorAndChairman', 'Chairman', 'ExecutiveBoardMember', 'NonExecutiveBoardMember', 'NonBoardManager', 'Other'));
ALTER TABLE reference.signatory ADD COLUMN IF NOT EXISTS extension text CHECK (extension ~ '^[0-9]{1,6}$');
ALTER TABLE reference.signatory ADD COLUMN IF NOT EXISTS email text;
-- Signatories entered before the ticks existed signed every valuable document: both ticks.
ALTER TABLE reference.signatory ADD COLUMN IF NOT EXISTS signs_cheques boolean NOT NULL DEFAULT true;
ALTER TABLE reference.signatory ADD COLUMN IF NOT EXISTS signs_notes boolean NOT NULL DEFAULT true;
ALTER TABLE reference.signatory ADD COLUMN IF NOT EXISTS signature_type text CHECK (signature_type IN ('image/png', 'image/jpeg'));
ALTER TABLE reference.signatory ADD COLUMN IF NOT EXISTS in_use boolean NOT NULL DEFAULT false;
-- Owner's answer to Q88 (22 Nov 2026): three ticks in any combination – «چک», «سفته», «قراردادها و سایر اسناد تعهدی». The first form of
-- the cheque tick was described as covering contracts too, so a signatory saved before the third tick takes it from its cheque tick.
ALTER TABLE reference.signatory ADD COLUMN IF NOT EXISTS signs_contracts boolean;
UPDATE reference.signatory SET signs_contracts = signs_cheques WHERE signs_contracts IS NULL;
ALTER TABLE reference.signatory ALTER COLUMN signs_contracts SET DEFAULT false;
ALTER TABLE reference.signatory ALTER COLUMN signs_contracts SET NOT NULL;
ALTER TABLE reference.signatory DROP CONSTRAINT IF EXISTS signatory_signs_something;
ALTER TABLE reference.signatory ADD CONSTRAINT signatory_signs_something CHECK (signs_cheques OR signs_notes OR signs_contracts);

-- Positions typed before the list existed stay as typed under «سایر»; one that is exactly a title of the list takes that choice.
UPDATE reference.signatory s SET position_kind = t.kind
FROM (VALUES ('مدیر عامل', 'ManagingDirector'), ('مدیر عامل و رییس هیئت مدیره', 'ManagingDirectorAndChairman'), ('رییس هیئت مدیره', 'Chairman'),
             ('عضو هیئت مدیره (موظف)', 'ExecutiveBoardMember'), ('عضو هیئت مدیره (غیر موظف)', 'NonExecutiveBoardMember'),
             ('مدیر و مسئول غیر عضو هیئت مدیره', 'NonBoardManager')) AS t (title, kind)
WHERE s.position_kind = 'Other' AND btrim(s.position) = t.title;

-- The scan is kept beside the row, like the company's logo: lists never carry the picture.
CREATE TABLE IF NOT EXISTS reference.signatory_signature (
    signatory_id  uuid   PRIMARY KEY REFERENCES reference.signatory (id) ON DELETE CASCADE,
    data          bytea  NOT NULL CHECK (octet_length(data) BETWEEN 1 AND 1048576)
);

-- ---- 2. the signature rule per kind of document is gone ----
DROP TABLE IF EXISTS reference.signature_rule;

-- ---- 3. permission holders ----
CREATE TABLE IF NOT EXISTS reference.permission_holder (
    id              uuid        PRIMARY KEY,
    full_name       text        NOT NULL CHECK (btrim(full_name) <> ''),
    name_key        text        NOT NULL,
    job_title       text        NOT NULL CHECK (btrim(job_title) <> ''),
    detail_code     text,
    national_code   text        CHECK (national_code ~ '^[0-9]{10}$'),
    mobile          text        CHECK (mobile ~ '^09[0-9]{9}$'),
    phone           text,
    extension       text        CHECK (extension ~ '^[0-9]{1,6}$'),
    email           text,
    signature_type  text        CHECK (signature_type IN ('image/png', 'image/jpeg')),
    is_active       boolean     NOT NULL DEFAULT true,
    version         bigint      NOT NULL DEFAULT 1,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS permission_holder_unique_name ON reference.permission_holder (name_key);
CREATE UNIQUE INDEX IF NOT EXISTS permission_holder_unique_national_code ON reference.permission_holder (national_code) WHERE national_code IS NOT NULL;

-- One row per ticked role: its title (the fixed one, or the text typed for «سایر») and the operations ticked under it.
CREATE TABLE IF NOT EXISTS reference.permission_holder_role (
    holder_id   uuid    NOT NULL REFERENCES reference.permission_holder (id) ON DELETE CASCADE,
    role        text    NOT NULL CHECK (role IN ('FirstConfirmer', 'SecondConfirmer', 'FirstApprover', 'SecondApprover', 'Other')),
    title       text    NOT NULL CHECK (btrim(title) <> ''),
    operations  text[]  NOT NULL CHECK (cardinality(operations) > 0),
    PRIMARY KEY (holder_id, role)
);

-- Owner, 22 Nov 2026: the fixed roles follow each other in the approval flow (first and second confirmer, first and second approver);
-- a role under «سایر» is given its place among them, and only such a role has one. Rows written before the column come last.
ALTER TABLE reference.permission_holder_role ADD COLUMN IF NOT EXISTS place text
    CHECK (place IN ('BeforeFirstConfirmer', 'AfterFirstConfirmer', 'AfterSecondConfirmer', 'AfterFirstApprover', 'AfterSecondApprover'));
UPDATE reference.permission_holder_role SET place = 'AfterSecondApprover' WHERE role = 'Other' AND place IS NULL;
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'permission_holder_role_place_of_other') THEN
        ALTER TABLE reference.permission_holder_role ADD CONSTRAINT permission_holder_role_place_of_other CHECK ((role = 'Other') = (place IS NOT NULL));
    END IF;
END
$$;

CREATE TABLE IF NOT EXISTS reference.permission_holder_signature (
    holder_id  uuid   PRIMARY KEY REFERENCES reference.permission_holder (id) ON DELETE CASCADE,
    data       bytea  NOT NULL CHECK (octet_length(data) BETWEEN 1 AND 1048576)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON reference.signatory_signature, reference.permission_holder, reference.permission_holder_role,
    reference.permission_holder_signature TO reference_svc;
GRANT SELECT, INSERT, UPDATE ON reference.signatory_signature, reference.permission_holder, reference.permission_holder_role,
    reference.permission_holder_signature TO treasury_migrator;
GRANT SELECT ON reference.signatory_signature, reference.permission_holder, reference.permission_holder_role,
    reference.permission_holder_signature TO treasury_readonly;
