-- Steps of received cheques after receipt (owner's item 14, answers to Q30–Q35 of 15 Nov 2026; docs/domain/received-cheque-steps.md).
-- New states: back from the bank (ReturnedFromBank) and replaced (Replaced); a received cheque can now be cancelled.
ALTER TABLE cashflow.instrument DROP CONSTRAINT IF EXISTS instrument_state_check;
ALTER TABLE cashflow.instrument DROP CONSTRAINT IF EXISTS instrument_state_known;
-- The whole list as it is now, not only the values known when this script was written: every start re-runs every script, and a
-- shorter list here would refuse rows saved with later values (19 Nov 2026: a cheque «ثبت صیادی شد» stopped db-init at this line).
ALTER TABLE cashflow.instrument ADD CONSTRAINT instrument_state_known CHECK (state IN (
    'InCashDesk', 'SayadRegistered', 'DepositedForCollection', 'Cleared', 'Bounced', 'InLegal', 'ReturnedToIssuer', 'Endorsed', 'ReturnedFromBank',
    'Replaced', 'BeingIssued', 'Issued', 'Delivered', 'Cancelled', 'PaidByBank', 'BouncedAtBank'));

-- What a step recorded besides date, reference and note: the bank account of a deposit, the receiver of an endorsement, the date of
-- management's order for a cancellation, the receipt of a replacement (JSON of StepDetailsSnapshot). Null for older steps.
ALTER TABLE cashflow.instrument_event ADD COLUMN IF NOT EXISTS details jsonb;

-- A cheque receipt that replaces a received cheque (تعویض) names it.
ALTER TABLE cashflow.receipt ADD COLUMN IF NOT EXISTS replaced_instrument_id uuid;

-- New fixed account of the voucher template: «مطالبات سوخت شده» (cancelled received cheques).
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_account_key_check;
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_key_known;
-- The whole list as it is now, not only the values known when this script was written: every start re-runs every script, and a
-- shorter list here would refuse rows saved with later values (19 Nov 2026: a cheque «ثبت صیادی شد» stopped db-init at this line).
ALTER TABLE cashflow.voucher_account ADD CONSTRAINT voucher_account_key_known CHECK (account_key IN (
    'ChequesInCashDesk', 'ChequesInCollection', 'ProtestedCheques', 'GuaranteeChequesForCompany', 'GuaranteeCounterpart', 'BadDebts',
    'ProtestedChequesInLegal', 'GuaranteeChequesInLegal', 'GuaranteeNotesForCompany', 'NotesInCollection', 'FundsInTransit', 'BankFees',
    'NotesPayable'));
