-- Steps of received cheques after receipt (owner's item 14, answers to Q30–Q35 of 15 Nov 2026; docs/domain/received-cheque-steps.md).
-- New states: back from the bank (ReturnedFromBank) and replaced (Replaced); a received cheque can now be cancelled.
ALTER TABLE cashflow.instrument DROP CONSTRAINT IF EXISTS instrument_state_check;
-- NOT VALID: a later script widens this list, and every start runs all scripts again; rows that already use the later values must
-- not stop this script. The last script that defines the list checks every row.
-- (033_cashflow_cheque_status.sql adds the states of 18 Nov 2026.)
ALTER TABLE cashflow.instrument DROP CONSTRAINT IF EXISTS instrument_state_known;
ALTER TABLE cashflow.instrument ADD CONSTRAINT instrument_state_known CHECK (state IN (
    'InCashDesk', 'DepositedForCollection', 'Cleared', 'Bounced', 'ReturnedToIssuer', 'Endorsed', 'ReturnedFromBank', 'Replaced',
    'BeingIssued', 'Issued', 'Delivered', 'Cancelled', 'PaidByBank', 'BouncedAtBank')) NOT VALID;

-- What a step recorded besides date, reference and note: the bank account of a deposit, the receiver of an endorsement, the date of
-- management's order for a cancellation, the receipt of a replacement (JSON of StepDetailsSnapshot). Null for older steps.
ALTER TABLE cashflow.instrument_event ADD COLUMN IF NOT EXISTS details jsonb;

-- A cheque receipt that replaces a received cheque (تعویض) names it.
ALTER TABLE cashflow.receipt ADD COLUMN IF NOT EXISTS replaced_instrument_id uuid;

-- New fixed account of the voucher template: «مطالبات سوخت شده» (cancelled received cheques).
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_account_key_check;
ALTER TABLE cashflow.voucher_account DROP CONSTRAINT IF EXISTS voucher_account_key_known;
-- NOT VALID for the same reason (035, 039 and 041 add fixed accounts).
ALTER TABLE cashflow.voucher_account ADD CONSTRAINT voucher_account_key_known CHECK (account_key IN (
    'ChequesInCashDesk', 'ChequesInCollection', 'ProtestedCheques', 'GuaranteeChequesForCompany', 'GuaranteeCounterpart', 'BadDebts')) NOT VALID;
