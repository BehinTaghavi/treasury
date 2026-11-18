-- The fixed accounts of the voucher template after the promissory notes (033 and 035_cashflow_note_steps, owner 16 Nov 2026) and the
-- legal action (035_cashflow_legal_action_voucher, owner's answers to Q38 and Q42, 18 Nov 2026) were joined. Both 035 scripts rewrote
-- the list on their own branch, so whichever ran last dropped the other's accounts; this list has all of them and matches the enum
-- FixedAccount of the .NET domain (checked by test_the_voucher_template_accepts_every_fixed_account_of_the_domain).
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_key_known;
ALTER TABLE cashflow.voucher_account ADD CONSTRAINT voucher_account_key_known CHECK (account_key IN (
    'ChequesInCashDesk', 'ChequesInCollection', 'ProtestedCheques', 'GuaranteeChequesForCompany', 'GuaranteeCounterpart', 'BadDebts',
    'ProtestedChequesInLegal', 'GuaranteeChequesInLegal', 'GuaranteeNotesForCompany', 'NotesInCollection'));
