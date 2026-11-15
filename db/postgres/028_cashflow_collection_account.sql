-- Owner's report no. 4 (15 Nov 2026), item 9: a received cheque deposited for collection names the company bank account it was
-- deposited at (accounts-svc; no cross-schema key, ADR 0001). The account number stays in destination for older rows and reports.
ALTER TABLE cashflow.instrument ADD COLUMN IF NOT EXISTS collection_account_id uuid;
CREATE INDEX IF NOT EXISTS instrument_received_by_state ON cashflow.instrument (state, due_date) WHERE direction = 'Received';
