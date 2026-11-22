-- Payment sheets («برگ پرداخت») have a serial number of their own, from 1 each fiscal year (owner's answer to Q57, 19 Nov 2026). The
-- first payment that gets one is the transfer of a received cheque («ثبت انتقال / خرج چک»); the number and the authorised signatories of
-- the sheet are kept with the transfer step (cashflow.instrument_event.details: sheetNumber, signers). The payment screens of Phase E
-- share this series.
CREATE TABLE IF NOT EXISTS cashflow.payment_sheet_number (
    fiscal_year  integer PRIMARY KEY,
    last_number  integer NOT NULL CHECK (last_number >= 0)
);

GRANT SELECT, INSERT, UPDATE ON cashflow.payment_sheet_number TO cashflow_svc, treasury_migrator;
GRANT SELECT ON cashflow.payment_sheet_number TO treasury_readonly;
