-- «ثبت صیادی شد» (owner, 15 Nov 2026): every cheque is recorded in the banks' Sayad system before it is deposited at the bank for
-- collection (received) or handed to its beneficiary (issued). The user ticks it; the server's date and time and the user are kept.
-- Null: not ticked yet. The rule itself is in the domain (Instrument.DepositForCollection / Deliver, Instrument.SayadNotRegistered).
ALTER TABLE cashflow.instrument ADD COLUMN IF NOT EXISTS sayad_registered_at timestamptz;
ALTER TABLE cashflow.instrument ADD COLUMN IF NOT EXISTS sayad_registered_by text;
ALTER TABLE cashflow.instrument DROP CONSTRAINT IF EXISTS instrument_sayad_registration_complete;
ALTER TABLE cashflow.instrument ADD CONSTRAINT instrument_sayad_registration_complete
    CHECK ((sayad_registered_at IS NULL) = (sayad_registered_by IS NULL));
