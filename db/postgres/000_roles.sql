-- Treasury on PostgreSQL 17 — database roles.
-- One database per client company; one schema per service; each service logs in with its own role
-- and can only use its own schema (doc section 7.2). Passwords are set outside these scripts
-- (from the secret store), never written here.

DO $$
DECLARE
    r text;
BEGIN
    FOREACH r IN ARRAY ARRAY['treasury_owner', 'reference_svc', 'accounts_svc', 'cashflow_svc', 'treasury_migrator', 'treasury_readonly']
    LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = r) THEN
            EXECUTE format('CREATE ROLE %I NOLOGIN', r);
        END IF;
    END LOOP;
END
$$;

-- Service roles may log in; the owner and the read-only role are group roles.
ALTER ROLE reference_svc LOGIN;
ALTER ROLE accounts_svc LOGIN;
ALTER ROLE cashflow_svc LOGIN;
ALTER ROLE treasury_migrator LOGIN;

-- Nobody gets anything through PUBLIC.
REVOKE ALL ON SCHEMA public FROM PUBLIC;
