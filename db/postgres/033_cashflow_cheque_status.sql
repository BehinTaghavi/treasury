-- Latest status of a received cheque as the owner described it on 18 Nov 2026 (docs/domain/received-cheque-steps.md, section 5).
-- New states: registered in the banks' Sayad system (SayadRegistered, «ثبت صیادی شد») and handed to the company's lawyer (InLegal, «حقوقی»).
ALTER TABLE cashflow.instrument DROP CONSTRAINT IF EXISTS instrument_state_known;
ALTER TABLE cashflow.instrument ADD CONSTRAINT instrument_state_known CHECK (state IN (
    'InCashDesk', 'SayadRegistered', 'DepositedForCollection', 'Cleared', 'Bounced', 'InLegal', 'ReturnedToIssuer', 'Endorsed', 'ReturnedFromBank',
    'Replaced', 'BeingIssued', 'Issued', 'Delivered', 'Cancelled', 'PaidByBank', 'BouncedAtBank'));

-- «سررسید چک»: cheques still in the cash desk or at the bank are found by due date; a cheque registered in Sayad is still in the cash desk.
DROP INDEX IF EXISTS cashflow.instrument_due;
CREATE INDEX IF NOT EXISTS instrument_due ON cashflow.instrument (due_date)
    WHERE state IN ('InCashDesk', 'SayadRegistered', 'DepositedForCollection', 'Issued', 'Delivered');
