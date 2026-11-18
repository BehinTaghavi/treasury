-- The company's own particulars («مشخصات شرکت», owner, 18 Nov 2026: name, address, and the legal address with its city and postal code)
-- were first a table of their own, reference.company_profile. Since the owner joined «مشخصات شرکت» with «اطلاعات شرکت» (18 Nov 2026)
-- they are columns of reference.company (033, 034); 039_reference_company_joined.sql adds them, moves a profile entered before into
-- reference.company and drops the old table. This script no longer creates it.
SELECT 1;
