-- Steps of a received promissory note after receipt (owner, 16 Nov 2026: واگذاری به بانک، وصول، استرداد، خرج سفته).
-- The instrument's states and steps are those of a received cheque; only the voucher template gets a new fixed account,
-- «سفته های در جریان وصول» (owner's table «واگذاری سفته به بانک»).
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_key_known;
-- The whole list as it is now, not only the values known when this script was written: every start re-runs every script, and a
-- shorter list here would refuse rows saved with later values (19 Nov 2026: a cheque «ثبت صیادی شد» stopped db-init at this line).
ALTER TABLE cashflow.voucher_account ADD CONSTRAINT voucher_account_key_known CHECK (account_key IN (
    'ChequesInCashDesk', 'ChequesInCollection', 'ProtestedCheques', 'GuaranteeChequesForCompany', 'GuaranteeCounterpart', 'BadDebts',
    'ProtestedChequesInLegal', 'GuaranteeChequesInLegal', 'GuaranteeNotesForCompany', 'NotesInCollection', 'FundsInTransit'));
