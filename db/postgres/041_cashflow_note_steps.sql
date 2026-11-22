-- Steps of a received promissory note after receipt (owner, 16 Nov 2026: واگذاری به بانک، وصول، استرداد، خرج سفته).
-- The instrument's states and steps are those of a received cheque; only the voucher template gets a new fixed account,
-- «سفته های در جریان وصول» (owner's table «واگذاری سفته به بانک»). This is the last script that defines the list of fixed accounts:
-- it checks every row.
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_key_known;
ALTER TABLE cashflow.voucher_account ADD CONSTRAINT voucher_account_key_known CHECK (account_key IN (
    'ChequesInCashDesk', 'ChequesInCollection', 'ProtestedCheques', 'GuaranteeChequesForCompany', 'GuaranteeCounterpart', 'BadDebts',
    'ProtestedChequesInLegal', 'GuaranteeChequesInLegal', 'GuaranteeNotesForCompany', 'NotesInCollection'));
