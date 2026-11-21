-- Transfers between the company's own bank accounts («نقل و انتقال بین بانکی», owner's report no. 7 of 20 Nov 2026).
--   bank_transfer         one row per transfer: electronic (internal, Pol, Paya, Satna) or by Sayad cheque. A cheque transfer keeps its
--                         cheque in the column cheque (JSON): the issued instrument of the payment side («انتقال دهنده»), the sheet, the
--                         Sayad ID, the due date and, once recorded on the receipts side («انتقال گیرنده»), the received instrument.
--   bank_transfer_number  last number given per fiscal year (one series for every kind of transfer).
--   transfer_limit        the company's limits of the electronic ways, in Rial — data, not code (owner: «فیلد محدودیت مبالغ را قابل
--                         کانفیگ نگهداشت و در کد ثابت نباشد»), kept on the Basic Information page «سقف‌های انتقال الکترونیکی».
-- Transactional, so LIST-partitioned by fiscal year like receipts (doc 7.3). No cross-schema keys: the bank accounts are checked
-- through accounts-svc's API (ADR 0001).

CREATE TABLE IF NOT EXISTS cashflow.bank_transfer (
    fiscal_year                 integer        NOT NULL,
    id                          uuid           NOT NULL,
    number                      integer        NOT NULL CHECK (number > 0),
    transfer_date               date           NOT NULL,
    method                      text           NOT NULL CHECK (method IN ('Internal', 'Pol', 'Paya', 'Satna', 'Cheque')),
    channel                     text           CHECK (channel IN ('InPerson', 'Remote')),
    source_account_id           uuid           NOT NULL,
    source_account_number       text           NOT NULL,
    source_bank_id              uuid           NOT NULL,
    source_bank_name            text           NOT NULL,
    destination_account_id      uuid           NOT NULL,
    destination_account_number  text           NOT NULL,
    destination_bank_id         uuid           NOT NULL,
    destination_bank_name       text           NOT NULL,
    amount                      numeric(20, 2) NOT NULL CHECK (amount > 0),
    currency                    char(3)        NOT NULL DEFAULT 'IRR',
    rate_to_rial                numeric(20, 4),
    amount_rial                 numeric(22, 0) NOT NULL CHECK (amount_rial > 0),
    tracking_number             text,
    description                 text,
    cheque                      jsonb,
    state                       text           NOT NULL,
    recorded_by                 text           NOT NULL,
    deleted_by                  text,
    deleted_at                  timestamptz,
    version                     bigint         NOT NULL DEFAULT 1,
    created_at                  timestamptz    NOT NULL DEFAULT now(),
    updated_at                  timestamptz    NOT NULL DEFAULT now(),
    PRIMARY KEY (fiscal_year, id),
    CONSTRAINT bank_transfer_number_once UNIQUE (fiscal_year, number),
    CONSTRAINT bank_transfer_two_accounts CHECK (source_account_id <> destination_account_id),
    -- A cheque transfer has its cheque and no channel; an electronic one a channel (at the branch or remote) and no cheque.
    CONSTRAINT bank_transfer_cheque_or_channel CHECK (method = 'Cheque' AND cheque IS NOT NULL AND channel IS NULL
        OR method <> 'Cheque' AND cheque IS NULL AND channel IS NOT NULL),
    CONSTRAINT bank_transfer_state_of_method CHECK (state = 'Deleted' OR (method = 'Cheque') = (state <> 'Recorded')),
    CONSTRAINT bank_transfer_rate CHECK (currency = 'IRR' AND rate_to_rial IS NULL OR currency <> 'IRR' AND rate_to_rial > 0),
    -- Pol, Paya and Satna carry Rial between two different banks; an internal transfer stays within one bank.
    CONSTRAINT bank_transfer_way CHECK (method = 'Cheque'
        OR method = 'Internal' AND source_bank_id = destination_bank_id
        OR method IN ('Pol', 'Paya', 'Satna') AND source_bank_id <> destination_bank_id AND currency = 'IRR'),
    CONSTRAINT bank_transfer_deleted CHECK ((state = 'Deleted') = (deleted_by IS NOT NULL) AND (state = 'Deleted') = (deleted_at IS NOT NULL))
) PARTITION BY LIST (fiscal_year);

-- The owner's answers of 21 Nov 2026 (Q77, Q79, Q82): the bank's fee of an electronic transfer, the approved payment request every
-- transfer pays (requests-svc; only its id, number and the key of the payment recorded on it are kept, ADR 0001), and the state
-- 'Cancelled' of a cheque transfer whose cheque was cancelled before it was registered in Sayad. Added with ALTER so a database that
-- already has the table of the first version of this script gets them too; the list of states is the whole list.
ALTER TABLE cashflow.bank_transfer ADD COLUMN IF NOT EXISTS fee_rial numeric(22, 0);
ALTER TABLE cashflow.bank_transfer ADD COLUMN IF NOT EXISTS payment_request_id uuid;
ALTER TABLE cashflow.bank_transfer ADD COLUMN IF NOT EXISTS payment_request_number integer;
ALTER TABLE cashflow.bank_transfer ADD COLUMN IF NOT EXISTS payment_key text;
ALTER TABLE cashflow.bank_transfer DROP CONSTRAINT IF EXISTS bank_transfer_state_check;
ALTER TABLE cashflow.bank_transfer DROP CONSTRAINT IF EXISTS bank_transfer_state_known;
ALTER TABLE cashflow.bank_transfer ADD CONSTRAINT bank_transfer_state_known
    CHECK (state IN ('Recorded', 'ChequeIssued', 'ChequeReceived', 'Collected', 'Deleted', 'Cancelled'));
ALTER TABLE cashflow.bank_transfer DROP CONSTRAINT IF EXISTS bank_transfer_fee;
ALTER TABLE cashflow.bank_transfer ADD CONSTRAINT bank_transfer_fee CHECK (fee_rial IS NULL OR fee_rial > 0 AND method <> 'Cheque');
ALTER TABLE cashflow.bank_transfer DROP CONSTRAINT IF EXISTS bank_transfer_request;
ALTER TABLE cashflow.bank_transfer ADD CONSTRAINT bank_transfer_request
    CHECK ((payment_request_id IS NULL) = (payment_request_number IS NULL) AND (payment_request_id IS NULL) = (payment_key IS NULL));

CREATE INDEX IF NOT EXISTS bank_transfer_by_id ON cashflow.bank_transfer (id);
-- The day's orders of one source account by one way, for the daily limits.
CREATE INDEX IF NOT EXISTS bank_transfer_by_source_day ON cashflow.bank_transfer (source_account_id, transfer_date, method);

-- The row is locked by the UPDATE until the transaction ends, so two users never get the same number; a number taken by a
-- transaction that rolls back is given again.
CREATE TABLE IF NOT EXISTS cashflow.bank_transfer_number (
    fiscal_year  integer PRIMARY KEY,
    last_number  integer NOT NULL CHECK (last_number >= 0)
);

-- bank_key: the bank's id (in the bank-names list of reference-svc) as text, or '' for the general limit of the way.
CREATE TABLE IF NOT EXISTS cashflow.transfer_limit (
    method                    text           NOT NULL CHECK (method IN ('Internal', 'Pol', 'Paya', 'Satna')),
    bank_key                  text           NOT NULL DEFAULT '',
    min_per_order             numeric(22, 0) CHECK (min_per_order > 0),
    max_per_order_in_person   numeric(22, 0) CHECK (max_per_order_in_person > 0),
    max_per_order_remote      numeric(22, 0) CHECK (max_per_order_remote > 0),
    max_per_day_in_person     numeric(22, 0) CHECK (max_per_day_in_person > 0),
    max_per_day_remote        numeric(22, 0) CHECK (max_per_day_remote > 0),
    max_count_per_day         integer        CHECK (max_count_per_day > 0),
    updated_at                timestamptz    NOT NULL DEFAULT now(),
    PRIMARY KEY (method, bank_key),
    CONSTRAINT transfer_limit_bank_key CHECK (bank_key = '' OR bank_key ~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$')
);

-- A company starts with the figures of the owner's report no. 7 (20 Nov 2026): Pol 3 milliard Rial a day at the branch and 2 milliard
-- remotely, at most 5 orders a day; Paya 5 milliard an order at the branch and 3 milliard remotely, at most 5 orders a day; Satna at
-- least 2 milliard an order; internal: none (every bank has its own). The general rows are never deleted — an emptied limit is a row
-- of NULLs — so this insert, which runs at every start, never brings back a figure the company changed or emptied.
INSERT INTO cashflow.transfer_limit (method, bank_key, min_per_order, max_per_order_in_person, max_per_order_remote, max_per_day_in_person, max_per_day_remote, max_count_per_day)
VALUES ('Internal', '', NULL, NULL, NULL, NULL, NULL, NULL),
       ('Pol',      '', NULL, NULL, NULL, 3000000000, 2000000000, 5),
       ('Paya',     '', NULL, 5000000000, 3000000000, NULL, NULL, 5),
       ('Satna',    '', 2000000000, NULL, NULL, NULL, NULL, NULL)
ON CONFLICT (method, bank_key) DO NOTHING;

-- The voucher template gets «وجوه در راه» (FundsInTransit): debited when a transfer cheque is issued, credited when it is received;
-- «هزینه کارمزد خدمات بانکی» (BankFees, answer to Q77) and «اسناد پرداختنی» (NotesPayable, answer to Q81).
-- The list is the whole list as it is now (038) plus the new accounts; it matches the enum FixedAccount of the .NET domain.
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_key_known;
ALTER TABLE cashflow.voucher_account ADD CONSTRAINT voucher_account_key_known CHECK (account_key IN (
    'ChequesInCashDesk', 'ChequesInCollection', 'ProtestedCheques', 'GuaranteeChequesForCompany', 'GuaranteeCounterpart', 'BadDebts',
    'ProtestedChequesInLegal', 'GuaranteeChequesInLegal', 'GuaranteeNotesForCompany', 'NotesInCollection', 'FundsInTransit', 'BankFees',
    'NotesPayable'));

-- Confirmed vouchers of a transfer: source 'Transfer', step 0 for an electronic transfer or the payer's side of a cheque transfer
-- («انتقال دهنده»), step 1 for the receiver's side («انتقال گیرنده»), step 2 for the maturity of a post-dated cheque («سررسید»: Dr
-- «اسناد پرداختنی» / Cr the bank; answer to Q81), steps 3 and 4 for the payer's and the receiver's side of a cancelled cheque (answer
-- to Q79). The two unnamed checks of 030 (the list of sources; a receipt's
-- voucher is step 0) are replaced by named ones.
DO $$
DECLARE c text;
BEGIN
    FOR c IN SELECT conname FROM pg_constraint
             WHERE conrelid = 'cashflow.voucher'::regclass AND contype = 'c'
               AND conname NOT IN ('voucher_source_known', 'voucher_step_of_source')
               AND (pg_get_constraintdef(oid) LIKE '%source = ANY%' OR pg_get_constraintdef(oid) LIKE '%source = ''Receipt''%')
    LOOP
        EXECUTE format('ALTER TABLE cashflow.voucher DROP CONSTRAINT %I', c);
    END LOOP;
END $$;
ALTER TABLE cashflow.voucher DROP CONSTRAINT IF EXISTS voucher_source_known;
ALTER TABLE cashflow.voucher ADD CONSTRAINT voucher_source_known CHECK (source IN ('Receipt', 'ChequeStep', 'Transfer'));
ALTER TABLE cashflow.voucher DROP CONSTRAINT IF EXISTS voucher_step_of_source;
ALTER TABLE cashflow.voucher ADD CONSTRAINT voucher_step_of_source CHECK (
    source = 'Receipt' AND step = 0 OR source = 'ChequeStep' AND step >= 1 OR source = 'Transfer' AND step BETWEEN 0 AND 4);

-- The functions of 034 with the new partitioned table (this script runs last, so its definitions stand).
CREATE OR REPLACE FUNCTION cashflow.ensure_fiscal_year_partitions(p_year integer)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    t text;
BEGIN
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.instrument FOR VALUES IN (%s)', 'instrument_fy' || p_year, p_year);
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.instrument_event FOR VALUES IN (%s)', 'instrument_event_fy' || p_year, p_year);
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.receipt FOR VALUES IN (%s)', 'receipt_fy' || p_year, p_year);
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.voucher FOR VALUES IN (%s)', 'voucher_fy' || p_year, p_year);
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.instrument_file FOR VALUES IN (%s)', 'instrument_file_fy' || p_year, p_year);
    EXECUTE format('CREATE TABLE IF NOT EXISTS cashflow.%I PARTITION OF cashflow.bank_transfer FOR VALUES IN (%s)', 'bank_transfer_fy' || p_year, p_year);
    FOREACH t IN ARRAY ARRAY['instrument_fy' || p_year, 'instrument_event_fy' || p_year, 'receipt_fy' || p_year, 'voucher_fy' || p_year,
                             'instrument_file_fy' || p_year, 'bank_transfer_fy' || p_year]
    LOOP
        EXECUTE format('ALTER TABLE cashflow.%I OWNER TO treasury_owner', t);
    END LOOP;
END
$$;

CREATE OR REPLACE FUNCTION cashflow.close_fiscal_year(p_year integer)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    t text;
BEGIN
    UPDATE cashflow.fiscal_period SET status = 'Closed' WHERE fiscal_year = p_year;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Fiscal year % does not exist', p_year;
    END IF;

    FOREACH t IN ARRAY ARRAY['cashflow.instrument_event_fy' || p_year, 'cashflow.receipt_fy' || p_year, 'cashflow.voucher_fy' || p_year,
                             'cashflow.instrument_file_fy' || p_year, 'cashflow.bank_transfer_fy' || p_year,
                             'requests.payment_request_fy' || p_year, 'requests.request_file_fy' || p_year]
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS closed_year_guard ON %s', t);
        EXECUTE format(
            'CREATE TRIGGER closed_year_guard BEFORE INSERT OR UPDATE OR DELETE ON %s FOR EACH ROW EXECUTE FUNCTION cashflow.reject_change_in_closed_year(%L)',
            t, p_year);
    END LOOP;
    -- Note: the instrument partition of a closed year stays writable on purpose: a cheque received in 1404 and
    -- cleared in 1405 changes state after 1404 is closed. Its new step is written in the 1405 event partition.
END
$$;

-- Years opened before this script get their partition now; a year closed before gets its guard.
SELECT cashflow.ensure_fiscal_year_partitions(fiscal_year) FROM cashflow.fiscal_period;
DO $$
DECLARE
    y integer;
BEGIN
    FOR y IN SELECT fiscal_year FROM cashflow.fiscal_period WHERE status = 'Closed'
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS closed_year_guard ON cashflow.%I', 'bank_transfer_fy' || y);
        EXECUTE format(
            'CREATE TRIGGER closed_year_guard BEFORE INSERT OR UPDATE OR DELETE ON cashflow.%I FOR EACH ROW EXECUTE FUNCTION cashflow.reject_change_in_closed_year(%L)',
            'bank_transfer_fy' || y, y);
    END LOOP;
END
$$;

-- The service never deletes a transfer: one recorded by mistake is kept as 'Deleted' so its number is seen to be taken. A bank's own
-- limits can be removed again. The sheet of a deleted cheque transfer is freed (the record never happened), so the service may
-- delete an issued sheet; voided sheets stay (the domain refuses to free them).
GRANT SELECT, INSERT, UPDATE ON cashflow.bank_transfer, cashflow.bank_transfer_number TO cashflow_svc, treasury_migrator;
REVOKE DELETE ON cashflow.bank_transfer FROM cashflow_svc;
GRANT SELECT, INSERT, UPDATE, DELETE ON cashflow.transfer_limit TO cashflow_svc;
GRANT SELECT, INSERT, UPDATE ON cashflow.transfer_limit TO treasury_migrator;
GRANT DELETE ON cashflow.cheque_sheet TO cashflow_svc;
GRANT SELECT ON cashflow.bank_transfer, cashflow.bank_transfer_number, cashflow.transfer_limit TO treasury_readonly;
