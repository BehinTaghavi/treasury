-- «ذینفعان / متعهدین / ضامنین» (owner, 19 Nov 2026): the beneficiaries' form also keeps the obligors and guarantors of promissory
-- notes, told apart by three ticks (one person may be in more than one group); a mobile number; and the area code of the phone number
-- in a column of its own. Entries saved before this script are beneficiaries.
ALTER TABLE reference.beneficiary ADD COLUMN IF NOT EXISTS is_beneficiary boolean NOT NULL DEFAULT true;
ALTER TABLE reference.beneficiary ADD COLUMN IF NOT EXISTS is_obligor boolean NOT NULL DEFAULT false;
ALTER TABLE reference.beneficiary ADD COLUMN IF NOT EXISTS is_guarantor boolean NOT NULL DEFAULT false;
ALTER TABLE reference.beneficiary ADD COLUMN IF NOT EXISTS mobile text CONSTRAINT beneficiary_mobile CHECK (mobile ~ '^09[0-9]{9}$');
ALTER TABLE reference.beneficiary ADD COLUMN IF NOT EXISTS phone_area_code text CONSTRAINT beneficiary_phone_area_code CHECK (phone_area_code ~ '^0[0-9]{1,3}$');

ALTER TABLE reference.beneficiary DROP CONSTRAINT IF EXISTS beneficiary_has_group;
ALTER TABLE reference.beneficiary ADD CONSTRAINT beneficiary_has_group CHECK (is_beneficiary OR is_obligor OR is_guarantor);
