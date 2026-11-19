-- Payment requests of the kind «ثبت انتقال (خرج چک)» (owner, 19 Nov 2026): a received cheque that is «ثبت صیادی شد» is handed on to the
-- beneficiary. The request names the cheque instead of the account paid from and shows its number, due date and who gave it to the
-- company; the cheque belongs to cashflow-svc, so only its id and what was written on it at the time are kept (ADR 0001).
ALTER TABLE requests.payment_request ADD COLUMN IF NOT EXISTS cheque_id uuid;
ALTER TABLE requests.payment_request ADD COLUMN IF NOT EXISTS cheque_number text;
ALTER TABLE requests.payment_request ADD COLUMN IF NOT EXISTS cheque_due_date date;
ALTER TABLE requests.payment_request ADD COLUMN IF NOT EXISTS cheque_payer text;
ALTER TABLE requests.payment_request ADD COLUMN IF NOT EXISTS cheque_bank text;
ALTER TABLE requests.payment_request ADD COLUMN IF NOT EXISTS cheque_sayad_id text;

-- The whole list of payment methods (every start re-runs every script; a later script that adds a method must carry the whole list too).
ALTER TABLE requests.payment_request DROP CONSTRAINT IF EXISTS payment_request_method_check;
ALTER TABLE requests.payment_request ADD CONSTRAINT payment_request_method_check
    CHECK (method IN ('CurrentCheque', 'PostDatedCheque', 'GuaranteeCheque', 'Cash', 'PromissoryNote', 'GuaranteeNote', 'Transfer', 'CardToCard', 'ChequeTransfer'));

ALTER TABLE requests.payment_request DROP CONSTRAINT IF EXISTS payment_request_cheque;
ALTER TABLE requests.payment_request ADD CONSTRAINT payment_request_cheque
    CHECK ((method = 'ChequeTransfer') = (cheque_id IS NOT NULL)
       AND (cheque_id IS NULL OR (cheque_number IS NOT NULL AND cheque_payer IS NOT NULL AND pay_from_id IS NULL)));

-- A cheque is promised to one open request at a time (the application refuses a second one; the index finds it).
CREATE INDEX IF NOT EXISTS payment_request_cheque ON requests.payment_request (cheque_id) WHERE cheque_id IS NOT NULL;
