-- Cheque books screen (new-version manual item 6; owner, 11 Nov 2026): a book none of whose sheets was used may be deleted
-- or corrected (series, first and last sheet). The application checks it; the delete also re-checks for used sheets.
GRANT DELETE ON cashflow.cheque_book TO cashflow_svc;
CREATE INDEX IF NOT EXISTS cheque_book_by_account ON cashflow.cheque_book (bank_account_id);
