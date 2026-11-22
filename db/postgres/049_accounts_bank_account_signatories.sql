-- Owner's report no. 8 (21 Nov 2026): the authorised signatories of each bank account, set on the bank account form –
-- «دارندگان امضای مجاز هر حساب بانکی ممکن است متفاوت از دارندگان امضای مجاز حساب بانک بعدی باشد».
--   required_signatures: «تعداد امضاء», one digit; 0 while it is not set.
--   bank_account_signatory: one row per signatory, «ثابت» (signs every cheque) or «متغیر» (chosen among the variable ones to complete
--   the number). The signatory lives in reference-svc («دارندگان امضای مجاز»); accounts-svc keeps its id only (ADR 0001) and marks it
--   in use there before saving.
ALTER TABLE accounts.bank_account ADD COLUMN IF NOT EXISTS required_signatures smallint NOT NULL DEFAULT 0
    CHECK (required_signatures BETWEEN 0 AND 9);

CREATE TABLE IF NOT EXISTS accounts.bank_account_signatory (
    bank_account_id  uuid     NOT NULL REFERENCES accounts.bank_account (id) ON DELETE CASCADE,
    signatory_id     uuid     NOT NULL,
    kind             text     NOT NULL CHECK (kind IN ('Fixed', 'Variable')),
    position         integer  NOT NULL,
    PRIMARY KEY (bank_account_id, signatory_id)
);
CREATE INDEX IF NOT EXISTS bank_account_signatory_by_signatory ON accounts.bank_account_signatory (signatory_id);

GRANT SELECT, INSERT, UPDATE, DELETE ON accounts.bank_account_signatory TO accounts_svc;
GRANT SELECT, INSERT, UPDATE ON accounts.bank_account_signatory TO treasury_migrator;
GRANT SELECT ON accounts.bank_account_signatory TO treasury_readonly;
