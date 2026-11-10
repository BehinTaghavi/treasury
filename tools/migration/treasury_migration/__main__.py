"""Command line:

  # 1. check that dbo.Checks has every column the migration reads
  python -m treasury_migration preflight --sql-server HOST --sql-user USER --database Cashnajoo86

  # 2. migrate cheques (from SQL Server, or from a CSV export of EXTRACT_SQL) and write a reconciliation report
  python -m treasury_migration cheques --sql-server HOST --sql-user USER --database Cashnajoo86 \
      --target "host=... dbname=treasury_irannajo user=treasury_migrator" --report reports/
  python -m treasury_migration cheques --csv checks.csv --source-name Cashnajoo86 --target "..." --report reports/

Passwords are read from the environment (SQLSERVER_PASSWORD, PGPASSWORD), never from the command line.
Exit code 0 = loaded and reconciled; 1 = differences found; 2 = preflight failed.
"""
from __future__ import annotations

import argparse
import csv
import json
import os
import sys
from pathlib import Path

import psycopg

from . import source, target
from .legacy_mapping import MigratedInstrument, Rejected, map_cheque


def _args(argv: list[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(prog="treasury_migration")
    sub = p.add_subparsers(dest="command", required=True)
    for name in ("preflight", "cheques"):
        s = sub.add_parser(name)
        s.add_argument("--sql-server")
        s.add_argument("--sql-user")
        s.add_argument("--database", help="old database holding dbo.Checks, normally Cashnajoo86")
        if name == "cheques":
            s.add_argument("--csv", type=Path, help="read a CSV export instead of SQL Server")
            s.add_argument("--source-name", help="name recorded in legacy_source when reading CSV")
            s.add_argument("--target", required=True, help="PostgreSQL connection string (role treasury_migrator)")
            s.add_argument("--currency-map", type=Path, help='JSON {"<Curid>": "USD", ...} for foreign-currency cheques')
            s.add_argument("--report", type=Path, default=Path("reports"))
            s.add_argument("--dry-run", action="store_true", help="map and report, but do not write to PostgreSQL")
    return p.parse_args(argv)


def main(argv: list[str] | None = None) -> int:
    a = _args(argv if argv is not None else sys.argv[1:])
    password = os.environ.get("SQLSERVER_PASSWORD", "")

    if a.command == "preflight":
        missing = source.preflight(a.sql_server, a.sql_user, password, a.database)
        print("ready" if not missing else "missing columns in dbo.Checks: " + ", ".join(missing))
        return 0 if not missing else 2

    if a.csv:
        name = a.source_name or a.csv.stem
        rows = list(source.read_csv(a.csv))
    else:
        missing = source.preflight(a.sql_server, a.sql_user, password, a.database)
        if missing:
            print("missing columns in dbo.Checks: " + ", ".join(missing))
            return 2
        name = a.database
        rows = list(source.read_sqlserver(a.sql_server, a.sql_user, password, a.database))

    currency_map = {int(k): v for k, v in json.loads(a.currency_map.read_text()).items()} if a.currency_map else {}
    results: list[MigratedInstrument | Rejected] = [map_cheque(r, name, currency_map) for r in rows]
    migrated = [r for r in results if isinstance(r, MigratedInstrument)]

    a.report.mkdir(parents=True, exist_ok=True)
    with (a.report / f"{name}_rejected.csv").open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(["legacy_source", "reason"])
        w.writerows((r.legacy_source, r.reason) for r in results if isinstance(r, Rejected))
    with (a.report / f"{name}_warnings.csv").open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(["legacy_source", "warning"])
        w.writerows((m.legacy_source, warning) for m in migrated for warning in m.warnings)

    if a.dry_run:
        print(f"dry run: {len(rows)} rows read, {len(migrated)} mappable, {len(results) - len(migrated)} rejected")
        return 0

    with psycopg.connect(a.target) as conn:
        inserted = target.load(conn, migrated)
        rec = target.reconcile(conn, name, len(rows), results)
    (a.report / f"{name}_reconciliation.md").write_text(rec.to_markdown(name), encoding="utf-8")
    print(f"{len(rows)} rows read, {inserted} inserted now, {len(rec.rejected)} rejected, reconciliation: {'MATCH' if rec.ok else 'DIFFERENCES'}")
    return 0 if rec.ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
