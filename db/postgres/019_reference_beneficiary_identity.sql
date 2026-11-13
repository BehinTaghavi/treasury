-- Owner's report no. 2 (13 Nov 2026), item 7: a beneficiary is a real person (national code, کد ملی, 10 digits) or a legal
-- person (national ID, شناسه ملی, 11 digits), with postal address and 10-digit postal code. The application checks the check
-- digits and requires the code or ID; beneficiaries saved before this script have none until their next correction.
ALTER TABLE reference.beneficiary ADD COLUMN IF NOT EXISTS person_kind text CONSTRAINT beneficiary_person_kind CHECK (person_kind IN ('Natural', 'Legal'));
ALTER TABLE reference.beneficiary ADD COLUMN IF NOT EXISTS national_id text CONSTRAINT beneficiary_national_id CHECK (national_id ~ '^[0-9]{10,11}$');
ALTER TABLE reference.beneficiary ADD COLUMN IF NOT EXISTS postal_address text CONSTRAINT beneficiary_postal_address CHECK (length(postal_address) <= 300);
ALTER TABLE reference.beneficiary ADD COLUMN IF NOT EXISTS postal_code text CONSTRAINT beneficiary_postal_code CHECK (postal_code ~ '^[0-9]{10}$');
