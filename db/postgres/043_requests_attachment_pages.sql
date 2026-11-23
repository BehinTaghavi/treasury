-- Owner's report no. 9 (22 Nov 2026), item 4: «تعداد ضمائم» of a payment request is no longer typed; it is the number of pages of the
-- scans attached to it. Each scan keeps its pages (counted by the domain: Treasury.Requests.Domain.PaymentRequests.ScanPages);
-- payment_request.attachments keeps their sum, written by requests-svc with every save.
-- Scans attached before this script count as one page each (their pages were not counted then); remove and attach one again to
-- count it.
ALTER TABLE requests.request_file ADD COLUMN IF NOT EXISTS pages integer NOT NULL DEFAULT 1;
ALTER TABLE requests.request_file DROP CONSTRAINT IF EXISTS request_file_pages_valid;
ALTER TABLE requests.request_file ADD CONSTRAINT request_file_pages_valid CHECK (pages BETWEEN 1 AND 5000);

-- Twenty scans of many pages can pass the old limit of 999 typed attachments.
ALTER TABLE requests.payment_request DROP CONSTRAINT IF EXISTS payment_request_attachments_check;
ALTER TABLE requests.payment_request DROP CONSTRAINT IF EXISTS payment_request_attachments_valid;
ALTER TABLE requests.payment_request ADD CONSTRAINT payment_request_attachments_valid CHECK (attachments >= 0);

-- Requests saved before: the number typed by hand becomes the pages of their scans (years still open; a closed year is not changed).
UPDATE requests.payment_request p
   SET attachments = s.pages
  FROM (SELECT r.id, coalesce((SELECT sum(f.pages) FROM requests.request_file f WHERE f.request_id = r.id), 0)::integer AS pages
          FROM requests.payment_request r) s
 WHERE s.id = p.id
   AND p.attachments IS DISTINCT FROM s.pages
   AND p.fiscal_year IN (SELECT fiscal_year FROM cashflow.fiscal_period WHERE status <> 'Closed');
