-- «ثبت صیادی شد» on issued cheques (owner, 15 Nov 2026): every cheque is recorded in the banks' Sayad system before it is handed to its
-- beneficiary. The user ticks it; the server's date and time and the user are kept. Null: not ticked yet. The rule itself is in the
-- domain (Instrument.Deliver, Instrument.SayadNotRegistered).
-- A received cheque does not use these columns: since the owner's rule N3 of 18 Nov 2026 it is registered with its own step and the
-- state 'SayadRegistered' (033_cashflow_cheque_status.sql), so no received cheque is ticked here, now or at an upgrade.
ALTER TABLE cashflow.instrument ADD COLUMN IF NOT EXISTS sayad_registered_at timestamptz;
ALTER TABLE cashflow.instrument ADD COLUMN IF NOT EXISTS sayad_registered_by text;
ALTER TABLE cashflow.instrument DROP CONSTRAINT IF EXISTS instrument_sayad_registration_complete;
ALTER TABLE cashflow.instrument ADD CONSTRAINT instrument_sayad_registration_complete
    CHECK ((sayad_registered_at IS NULL) = (sayad_registered_by IS NULL));
