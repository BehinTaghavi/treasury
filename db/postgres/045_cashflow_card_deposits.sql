-- «واریز به کارت بانک» (owner's report no. 6, item 12; his answers to Q68–Q70 of 20 Nov 2026): a fifth kind of receipt, 'CardDeposit',
-- numbered in its own series, for money a payer pays onto the bank card of one of the company's petty-cash holders (account_kind
-- 'PettyCashCard'; account_id is the petty cash of accounts-svc) or onto a company bank card that has no bank account ('CompanyCard';
-- account_id is accounts.company_card, script 044). The money comes card to card, with a deposit slip or through the company's
-- payment gateway. No cross-schema keys: the card is checked through accounts-svc's API (ADR 0001).
--
-- The checks that list the kinds of a receipt are written, with the whole list as it is now, in 033_cashflow_note_receipts.sql, which
-- drops and adds them again at every start; 033 runs before this script, so they already allow 'CardDeposit' here. What is left is the
-- check on account_kind, written inline in 025 and so named by PostgreSQL.
ALTER TABLE cashflow.receipt DROP CONSTRAINT IF EXISTS receipt_account_kind_check;
ALTER TABLE cashflow.receipt DROP CONSTRAINT IF EXISTS receipt_account_kind_known;
ALTER TABLE cashflow.receipt ADD CONSTRAINT receipt_account_kind_known CHECK (account_kind IN ('CashDesk', 'BankAccount', 'PettyCashCard', 'CompanyCard'));
