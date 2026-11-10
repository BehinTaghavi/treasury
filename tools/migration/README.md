# Migration of old cheques (Access era) into PostgreSQL

Reads `dbo.Checks` of the shared old database (`Cashnajoo86` for Iran Nazho Co.), maps every row to the new instrument model
with its history rebuilt from the old date columns, loads it into the `cashflow` schema, and writes a reconciliation report.

```bash
pip install -r requirements.txt
export SQLSERVER_PASSWORD=...   PGPASSWORD=...
python -m treasury_migration preflight --sql-server HOST --sql-user USER --database Cashnajoo86
python -m treasury_migration cheques --sql-server HOST --sql-user USER --database Cashnajoo86 \
    --target "host=... dbname=treasury user=treasury_migrator" --report reports/ [--dry-run] [--currency-map currencies.json]
# or from a CSV export of EXTRACT_SQL (treasury_migration/source.py):
python -m treasury_migration cheques --csv checks.csv --source-name Cashnajoo86 --target "..." --report reports/
```

Reports: `<db>_reconciliation.md` (counts and Rial totals per direction/state/kind, expected vs loaded),
`<db>_rejected.csv` (rows not migrated, with the reason), `<db>_warnings.csv` (migrated rows with data problems).
Ids are deterministic (source database + Checkid), so the migration can be re-run safely.

Mapping rules: `docs/domain/instrument-lifecycle.md`. Open points (Q1–Q10): `docs/domain/open-questions.md`.

Not yet covered: cash and bank transactions (`Subs`), vouchers, cheque books (`Bundle`), counterparties (names are kept; ids are
placeholders until the Counterparties context is migrated), the MVC version's data (needs Q9).

The column names come from the old program's VBA code; `preflight` checks them against the real table before anything is read.
