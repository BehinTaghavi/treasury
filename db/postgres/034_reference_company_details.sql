-- «اطلاعات شرکت», the owner's answer to Q38 (16 Nov 2026): the company's official details for printed forms — شناسه ملی (11 digits),
-- کد اقتصادی (11 or 12 digits), شماره ثبت, نشانی, کد پستی (10 digits), تلفن, آدرس سایت, ایمیل. All optional; the application checks
-- the national ID's check digit and the shape of the website and e-mail address.
ALTER TABLE reference.company ADD COLUMN IF NOT EXISTS national_id text CONSTRAINT company_national_id CHECK (national_id ~ '^[0-9]{11}$');
ALTER TABLE reference.company ADD COLUMN IF NOT EXISTS economic_code text CONSTRAINT company_economic_code CHECK (economic_code ~ '^[0-9]{11,12}$');
ALTER TABLE reference.company ADD COLUMN IF NOT EXISTS registration_number text CONSTRAINT company_registration_number CHECK (registration_number ~ '^[0-9]{1,10}$');
ALTER TABLE reference.company ADD COLUMN IF NOT EXISTS address text CONSTRAINT company_address CHECK (length(address) BETWEEN 1 AND 300);
ALTER TABLE reference.company ADD COLUMN IF NOT EXISTS postal_code text CONSTRAINT company_postal_code CHECK (postal_code ~ '^[0-9]{10}$');
ALTER TABLE reference.company ADD COLUMN IF NOT EXISTS phone text CONSTRAINT company_phone CHECK (phone ~ '^\+?[0-9-]{6,20}$');
ALTER TABLE reference.company ADD COLUMN IF NOT EXISTS website text CONSTRAINT company_website CHECK (length(website) BETWEEN 1 AND 100);
ALTER TABLE reference.company ADD COLUMN IF NOT EXISTS email text CONSTRAINT company_email CHECK (length(email) BETWEEN 1 AND 100 AND email LIKE '%@%');
