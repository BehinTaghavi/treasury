-- Departments (بخش‌ها / واحد سازمانی), requesters of funds (درخواست کنندگان وجوه) and beneficiaries (ذینفعان) of reference-svc:
-- new-version manual items 12–14; MVC Beneficiaries, BeneficiariesDetailInfo, Beneficiaries_TELS.
-- تفصیل codes are codes of reference.detail_account (checked by the application; the copy is re-imported, never deleted).

CREATE TABLE IF NOT EXISTS reference.department (
    id            uuid        PRIMARY KEY,
    number        integer     NOT NULL CHECK (number > 0),
    name          text        NOT NULL CHECK (length(name) BETWEEN 1 AND 100),
    name_key      text        NOT NULL,
    cost_centers  text[]      NOT NULL DEFAULT '{}',       -- تفصیل codes of its cost centres (each in one department only)
    is_active     boolean     NOT NULL DEFAULT true,
    version       bigint      NOT NULL DEFAULT 1,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS department_unique_number ON reference.department (number);
CREATE UNIQUE INDEX IF NOT EXISTS department_unique_name ON reference.department (name_key);

CREATE TABLE IF NOT EXISTS reference.requester (
    id             uuid        PRIMARY KEY,
    number         integer     NOT NULL CHECK (number > 0),
    full_name      text        NOT NULL CHECK (length(full_name) BETWEEN 1 AND 100),
    department_id  uuid        NOT NULL CONSTRAINT requester_department REFERENCES reference.department (id),
    cost_center    text        NOT NULL,
    is_active      boolean     NOT NULL DEFAULT true,
    in_use         boolean     NOT NULL DEFAULT false,      -- a payment request names it: never deleted
    version        bigint      NOT NULL DEFAULT 1,
    created_at     timestamptz NOT NULL DEFAULT now(),
    updated_at     timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS requester_unique_number ON reference.requester (number);

CREATE TABLE IF NOT EXISTS reference.beneficiary (
    id              uuid        PRIMARY KEY,
    detail_code     text        NOT NULL,
    legal_name      text        NOT NULL CHECK (length(legal_name) BETWEEN 1 AND 150),
    bank_id         uuid        CONSTRAINT beneficiary_bank REFERENCES reference.catalogue_item (id),
    branch_name     text,
    account_number  text,
    sheba           text        CHECK (sheba ~ '^IR[0-9]{24}$'),
    phone           text,
    is_active       boolean     NOT NULL DEFAULT true,
    in_use          boolean     NOT NULL DEFAULT false,
    version         bigint      NOT NULL DEFAULT 1,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),
    CHECK (account_number IS NULL OR bank_id IS NOT NULL)
);
CREATE UNIQUE INDEX IF NOT EXISTS beneficiary_unique_detail ON reference.beneficiary (detail_code);

GRANT SELECT, INSERT, UPDATE, DELETE ON reference.department, reference.requester, reference.beneficiary TO reference_svc;
GRANT SELECT, INSERT, UPDATE ON reference.department, reference.requester, reference.beneficiary TO treasury_migrator;
GRANT SELECT ON reference.department, reference.requester, reference.beneficiary TO treasury_readonly;
