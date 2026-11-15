-- Payments made against approved payment requests (owner's answer to Q36 of 15 Nov 2026: an endorsed received cheque «خرج چک» pays an
-- approved request). Kept with the request (JSON of PaymentRow: key, kind, instrument, reference, amount, ISO date, user); the request
-- is «پرداخت شده» when they reach its amount.
ALTER TABLE requests.payment_request ADD COLUMN IF NOT EXISTS payments jsonb NOT NULL DEFAULT '[]'::jsonb;
