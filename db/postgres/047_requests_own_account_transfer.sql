-- Payment requests of the kind «نقل و انتقال بین بانک‌های شرکت» (owner, 21 Nov 2026, answer to Q82: «برای نقل و انتقال از هر نوع لازم است
-- درخواست وجه صادر و مجوز آن اخذ شود»). Such a request names the bank account the money leaves (pay_from_id, «انتقال دهنده») and the
-- company bank account it reaches (transfer_to_account_id, «انتقال گیرنده») instead of a beneficiary and a purpose group: the columns
-- beneficiary_* keep the receiving account's id and name, purpose_group_id is the zero uuid and purpose_group_name the words «نقل و
-- انتقال بین بانک‌های شرکت» (the owner's name for this kind, 21 Nov 2026), so lists and printouts read the same way for every kind. The accounts belong to accounts-svc (ADR 0001).
-- cashflow-svc pays the request with a transfer (payment kind 'BankTransfer' in the column payments, 032).
ALTER TABLE requests.payment_request ADD COLUMN IF NOT EXISTS transfer_to_account_id uuid;

-- The whole list of payment methods (also written in 041: every start re-runs every script).
ALTER TABLE requests.payment_request DROP CONSTRAINT IF EXISTS payment_request_method_check;
ALTER TABLE requests.payment_request ADD CONSTRAINT payment_request_method_check
    CHECK (method IN ('CurrentCheque', 'PostDatedCheque', 'GuaranteeCheque', 'Cash', 'PromissoryNote', 'GuaranteeNote', 'Transfer', 'CardToCard', 'ChequeTransfer',
                      'OwnAccountTransfer'));

ALTER TABLE requests.payment_request DROP CONSTRAINT IF EXISTS payment_request_own_transfer;
ALTER TABLE requests.payment_request ADD CONSTRAINT payment_request_own_transfer
    CHECK ((method = 'OwnAccountTransfer') = (transfer_to_account_id IS NOT NULL)
       AND (transfer_to_account_id IS NULL OR (pay_from_kind = 'BankAccount' AND pay_from_id IS NOT NULL AND pay_from_id <> transfer_to_account_id)));
