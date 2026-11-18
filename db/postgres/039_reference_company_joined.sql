-- «اطلاعات شرکت» (name, logo and official details, owner 16 Nov 2026) and «مشخصات شرکت» (address, legal address with its city and
-- postal code, owner 18 Nov 2026) are one page and one row since the owner joined them (18 Nov 2026). The city of the legal address
-- decides «خارج از مرکز» on a received cheque; the postal code of 034 is the legal address's.
ALTER TABLE reference.company ADD COLUMN IF NOT EXISTS legal_address text CONSTRAINT company_legal_address CHECK (length(legal_address) BETWEEN 1 AND 300);
-- A city of the Basic Information list (reference.catalogue_item, kind City). Null only for a row entered before the join; the
-- application asks for it at the next correction.
ALTER TABLE reference.company ADD COLUMN IF NOT EXISTS city_id uuid;

-- A profile entered on the page «مشخصات شرکت» before the join (037 of the first version) moves into reference.company.
DO $$
BEGIN
    IF to_regclass('reference.company_profile') IS NOT NULL THEN
        IF EXISTS (SELECT 1 FROM reference.company) THEN
            UPDATE reference.company c
               SET address       = COALESCE(c.address, left(p.address, 300)),
                   legal_address = COALESCE(c.legal_address, left(p.legal_address, 300)),
                   city_id       = COALESCE(c.city_id, p.city_id),
                   postal_code   = COALESCE(c.postal_code, p.postal_code),
                   version       = c.version + 1,
                   updated_at    = now()
              FROM reference.company_profile p;
        ELSE
            INSERT INTO reference.company (id, name, address, legal_address, city_id, postal_code)
            SELECT gen_random_uuid(), left(p.name, 100), left(p.address, 300), left(p.legal_address, 300), p.city_id, p.postal_code
              FROM reference.company_profile p;
        END IF;
        DROP TABLE reference.company_profile;
    END IF;
END
$$;
