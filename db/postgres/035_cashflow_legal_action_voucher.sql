-- Voucher of the legal action on a cheque (owner's answer to Q38, 18 Nov 2026): Dr «اسناد واخواست شده حقوقی شده» / Cr «اسناد واخواست شده»
-- (or «اسناد نزد صندوق» for a cheque that was still in the cash desk). New fixed account of the voucher template.
-- Guarantee cheques and notes (answer to Q42): Dr «اسناد تضمینی حقوقی شده به نفع شرکت» / Cr «اسناد تضمینی به نفع شرکت»; one more fixed account.
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_key_known;
-- The whole list as it is now, not only the values known when this script was written: every start re-runs every script, and a
-- shorter list here would refuse rows saved with later values (19 Nov 2026: a cheque «ثبت صیادی شد» stopped db-init at this line).
ALTER TABLE cashflow.voucher_account ADD CONSTRAINT voucher_account_key_known CHECK (account_key IN (
    'ChequesInCashDesk', 'ChequesInCollection', 'ProtestedCheques', 'GuaranteeChequesForCompany', 'GuaranteeCounterpart', 'BadDebts',
    'ProtestedChequesInLegal', 'GuaranteeChequesInLegal', 'GuaranteeNotesForCompany', 'NotesInCollection', 'FundsInTransit', 'BankFees',
    'NotesPayable'));
