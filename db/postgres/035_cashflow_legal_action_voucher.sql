-- Voucher of the legal action on a cheque (owner's answer to Q38, 18 Nov 2026): Dr «اسناد واخواست شده حقوقی شده» / Cr «اسناد واخواست شده»
-- (or «اسناد نزد صندوق» for a cheque that was still in the cash desk). New fixed account of the voucher template.
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_key_known;
ALTER TABLE cashflow.voucher_account ADD CONSTRAINT voucher_account_key_known CHECK (account_key IN (
    'ChequesInCashDesk', 'ChequesInCollection', 'ProtestedCheques', 'GuaranteeChequesForCompany', 'GuaranteeCounterpart', 'BadDebts',
    'ProtestedChequesInLegal'));
