#!/usr/bin/env bash
# Applies the Treasury schema scripts to one client database. Safe to run again (scripts are idempotent).
# Usage: PGHOST=... PGPORT=5432 PGUSER=postgres ./apply.sh <database>
# Roles are created by an administrator; every object is then created as treasury_owner.
set -euo pipefail
db="${1:?database name required}"
here="$(cd "$(dirname "$0")" && pwd)"

psql -v ON_ERROR_STOP=1 -q -d "$db" -f "$here/000_roles.sql"
psql -v ON_ERROR_STOP=1 -q -d "$db" -c "GRANT CREATE ON DATABASE \"$db\" TO treasury_owner;"
for f in "$here"/0[0-9][0-9]_*.sql; do
  [[ "$(basename "$f")" == 000_roles.sql ]] && continue
  echo "applying $(basename "$f")"
  PGOPTIONS='-c role=treasury_owner -c client_min_messages=warning' psql -v ON_ERROR_STOP=1 -q -d "$db" -f "$f"
done
