-- Corrections to the Basic Information lists (owner's report of 11 Nov 2026, items 2, 6 and 9):
-- an entry typed by mistake may be deleted while nothing refers to it; a city may move to another province, a province to
-- another country; a currency code may be corrected while the currency is unused.
-- in_use is set by reference-svc when another service (accounts-svc) is about to save data that names the entry; it never
-- goes back to false, and only entries with in_use = false can be deleted.

ALTER TABLE reference.catalogue_item ADD COLUMN IF NOT EXISTS in_use boolean NOT NULL DEFAULT false;

-- One-time catch-up for databases where bank accounts were saved before in_use existed (e.g. the owner's test system).
-- Reads accounts-svc's table only here, in an administrator script; the services themselves never read each other's schema (ADR 0001).
UPDATE reference.catalogue_item c SET in_use = true
WHERE NOT c.in_use
  AND (c.id IN (SELECT bank_id FROM accounts.bank_account UNION SELECT city_id FROM accounts.bank_account WHERE city_id IS NOT NULL)
       OR (c.kind = 'Currency' AND c.code IN (SELECT currency FROM accounts.bank_account)));

GRANT DELETE ON reference.catalogue_item TO reference_svc;
