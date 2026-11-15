-- Requesters of funds (owner, 13 Nov 2026): the requester is chosen from the staff تفصیل (report no. 2, item 6), and the
-- department is optional because Iran Nazho Co. has no departments. Requesters saved before keep their typed name until their
-- next correction.
ALTER TABLE reference.requester ADD COLUMN IF NOT EXISTS person_detail text;
ALTER TABLE reference.requester ALTER COLUMN department_id DROP NOT NULL;
