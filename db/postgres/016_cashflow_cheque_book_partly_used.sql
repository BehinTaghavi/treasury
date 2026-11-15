-- Cheque book sizes (owner, 12 Nov 2026): a new book has 25, 50 or 100 sheets; a book already partly used when the company starts
-- with Treasury is entered with its unused sheets only, any number up to 100 (checked by the application). Books registered before
-- this rule with another sheet count are marked partly used so that they can still be corrected.
ALTER TABLE cashflow.cheque_book ADD COLUMN IF NOT EXISTS partly_used boolean NOT NULL DEFAULT false;
UPDATE cashflow.cheque_book SET partly_used = true WHERE NOT partly_used AND last_sheet - first_sheet + 1 NOT IN (25, 50, 100);
