-- The company's bank cards that have no bank account in the system (owner's report no. 6, item 12, and his answer to Q69 of 20 Nov
-- 2026): a list of its own in Basic Information with the card number, the name on the card, the issuing bank and the معین and تفصیل
-- that a deposit onto the card debits («واریز به کارت بانک», cashflow-svc). A card of one of the company's bank accounts stays on
-- that account (accounts.bank_card). The bank is an entry of reference-svc's bank-names list and the معین and تفصیل are codes of its
-- copy of the chart of accounts (checked through its API, no cross-schema keys: ADR 0001).

CREATE TABLE IF NOT EXISTS accounts.company_card (
    id              uuid        PRIMARY KEY,
    card_number     text        NOT NULL CHECK (card_number ~ '^[0-9]{16}$'),
    holder_name     text        NOT NULL CHECK (length(holder_name) BETWEEN 1 AND 100),   -- عنوان دارندهٔ کارت
    bank_id         uuid        NOT NULL,
    ledger_account  text        NOT NULL,
    detail1         text,
    is_active       boolean     NOT NULL DEFAULT true,
    version         bigint      NOT NULL DEFAULT 1,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);
-- A card is entered once.
CREATE UNIQUE INDEX IF NOT EXISTS company_card_unique_number ON accounts.company_card (card_number);

GRANT SELECT, INSERT, UPDATE ON accounts.company_card TO accounts_svc, treasury_migrator;
GRANT SELECT ON accounts.company_card TO treasury_readonly;
