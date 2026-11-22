-- «مشخصات شرکت», the owner's request of 16 Nov 2026 joined to the page of 18 Nov (037_reference_company_profile.sql): the company's
-- logo (آرم; PNG/JPEG, at most 1 MB), shown with its name at the top of every screen, and its official details for printed forms —
-- شناسه ملی (11 digits), کد اقتصادی (11 or 12 digits), شماره ثبت, تلفن, آدرس سایت, ایمیل. All optional; the application checks the
-- national ID's check digit and the shape of the website and e-mail address. The address and the postal code are those of 037.
ALTER TABLE reference.company_profile ADD COLUMN IF NOT EXISTS logo_type text CONSTRAINT company_profile_logo_type CHECK (logo_type IN ('image/png', 'image/jpeg'));
ALTER TABLE reference.company_profile ADD COLUMN IF NOT EXISTS national_id text CONSTRAINT company_profile_national_id CHECK (national_id ~ '^[0-9]{11}$');
ALTER TABLE reference.company_profile ADD COLUMN IF NOT EXISTS economic_code text CONSTRAINT company_profile_economic_code CHECK (economic_code ~ '^[0-9]{11,12}$');
ALTER TABLE reference.company_profile ADD COLUMN IF NOT EXISTS registration_number text CONSTRAINT company_profile_registration_number CHECK (registration_number ~ '^[0-9]{1,10}$');
ALTER TABLE reference.company_profile ADD COLUMN IF NOT EXISTS phone text CONSTRAINT company_profile_phone CHECK (phone ~ '^\+?[0-9-]{6,20}$');
ALTER TABLE reference.company_profile ADD COLUMN IF NOT EXISTS website text CONSTRAINT company_profile_website CHECK (length(website) BETWEEN 1 AND 100);
ALTER TABLE reference.company_profile ADD COLUMN IF NOT EXISTS email text CONSTRAINT company_profile_email CHECK (length(email) BETWEEN 1 AND 100 AND email LIKE '%@%');

-- The logo is kept apart so reading the name never loads the picture. One row, like the profile it belongs to.
CREATE TABLE IF NOT EXISTS reference.company_profile_logo (
    only_one  boolean  PRIMARY KEY DEFAULT true REFERENCES reference.company_profile (only_one) ON DELETE CASCADE,
    data      bytea    NOT NULL CHECK (octet_length(data) BETWEEN 1 AND 1048576)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON reference.company_profile_logo TO reference_svc;
GRANT SELECT, INSERT, UPDATE ON reference.company_profile_logo TO treasury_migrator;
GRANT SELECT ON reference.company_profile_logo TO treasury_readonly;
