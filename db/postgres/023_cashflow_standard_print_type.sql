-- Owner, 14 Nov 2026: besides the manual's six print types, a "Standard" type (استاندارد) for the uniform cheque all banks now print,
-- kept with its scan like the others.
ALTER TABLE cashflow.cheque_format DROP CONSTRAINT IF EXISTS cheque_format_print_type_check;
ALTER TABLE cashflow.cheque_format ADD CONSTRAINT cheque_format_print_type_check
    CHECK (print_type IN ('Standard', 'NewLarge', 'NewestLarge', 'OldLarge', 'NewSmall', 'NewestSmall', 'OldSmall'));
