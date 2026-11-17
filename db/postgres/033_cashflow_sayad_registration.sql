-- «ثبت صیادی شد» (owner, 15 Nov 2026): every cheque is recorded in the banks' Sayad system before it is deposited at the bank for
-- collection (received) or handed to its beneficiary (issued). The user ticks it; the server's date and time and the user are kept.
-- Null: not ticked yet. The rule itself is in the domain (Instrument.DepositForCollection / Deliver, Instrument.SayadNotRegistered).
--
-- Answer to Q39 (owner, 16 Nov 2026): received cheques that are already in the system when the tick arrives are assumed to be
-- recorded in Sayad, so they are ticked once, here, with the moment of the upgrade and 'system:assumed' in place of a user
-- (SayadRegistration.AssumedBy). This happens only when the columns are created: running the script again ticks nothing, so cheques
-- received afterwards wait for the user's tick.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_schema = 'cashflow' AND table_name = 'instrument' AND column_name = 'sayad_registered_at') THEN
        ALTER TABLE cashflow.instrument ADD COLUMN sayad_registered_at timestamptz;
        ALTER TABLE cashflow.instrument ADD COLUMN sayad_registered_by text;
        UPDATE cashflow.instrument
           SET sayad_registered_at = now(), sayad_registered_by = 'system:assumed'
         WHERE direction = 'Received' AND kind IN ('OrdinaryCheque', 'GuaranteeCheque', 'BankCheque');
    END IF;
END $$;
ALTER TABLE cashflow.instrument DROP CONSTRAINT IF EXISTS instrument_sayad_registration_complete;
ALTER TABLE cashflow.instrument ADD CONSTRAINT instrument_sayad_registration_complete
    CHECK ((sayad_registered_at IS NULL) = (sayad_registered_by IS NULL));
