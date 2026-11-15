-- Cheque types (معرفی انواع چک و چاپ آنها) of cashflow-svc: new-version manual item 7; MVC ChequeFormatTypes.
-- One per bank account and print type, with a scan of the cheque (PNG/JPEG, at most 2 MB). Cheque books name their type.

CREATE TABLE IF NOT EXISTS cashflow.cheque_format (
    id               uuid        PRIMARY KEY,
    number           integer     NOT NULL CHECK (number > 0),                -- شماره چاپ
    bank_account_id  uuid        NOT NULL,                                   -- accounts-svc bank account (checked through its API)
    print_type       text        NOT NULL CHECK (print_type IN ('NewLarge', 'NewestLarge', 'OldLarge', 'NewSmall', 'NewestSmall', 'OldSmall')),
    image_type       text        CHECK (image_type IN ('image/png', 'image/jpeg')),
    is_active        boolean     NOT NULL DEFAULT true,
    version          bigint      NOT NULL DEFAULT 1
);
CREATE UNIQUE INDEX IF NOT EXISTS cheque_format_unique_number ON cashflow.cheque_format (number);
CREATE UNIQUE INDEX IF NOT EXISTS cheque_format_one_per_type ON cashflow.cheque_format (bank_account_id, print_type);

CREATE TABLE IF NOT EXISTS cashflow.cheque_format_image (
    format_id  uuid   PRIMARY KEY REFERENCES cashflow.cheque_format (id) ON DELETE CASCADE,
    data       bytea  NOT NULL CHECK (octet_length(data) BETWEEN 1 AND 2097152)
);

ALTER TABLE cashflow.cheque_book ADD COLUMN IF NOT EXISTS format_id uuid CONSTRAINT cheque_book_format REFERENCES cashflow.cheque_format (id);

GRANT SELECT, INSERT, UPDATE, DELETE ON cashflow.cheque_format, cashflow.cheque_format_image TO cashflow_svc;
GRANT SELECT ON cashflow.cheque_format, cashflow.cheque_format_image TO treasury_readonly;
