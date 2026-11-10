# Getting started

## Prerequisites
.NET SDK 10, Node.js 22.21+ with pnpm 10, Python 3.11+, PostgreSQL 17 (or Docker).

## Run the system locally (no database, no login)
```bash
dotnet run --project src/Services/Cashflow/Treasury.Cashflow.Api          # http://localhost:5081, in-memory, Development
cd frontend && pnpm install
pnpm --filter @treasury/remote-receipts build && pnpm --filter @treasury/remote-receipts preview   # http://localhost:5174
pnpm --filter @treasury/shell dev                                          # http://localhost:3000 → "دریافت‌ها"
```
Development-only endpoints simulate other services: `POST /dev/payments/{id}/approve`, `POST /dev/bank-accounts/{id}/balance/{rial}`, `POST /dev/fiscal-years/{year}/open`, `GET /dev/outbox`.

## With PostgreSQL
```bash
createdb treasury && PGUSER=postgres db/postgres/apply.sh treasury
psql -d treasury -c "SELECT cashflow.open_fiscal_year(1405, '2026-03-21', '2027-03-20')"
psql -d treasury -c "ALTER ROLE cashflow_svc PASSWORD '...'"
ConnectionStrings__Treasury="Host=localhost;Database=treasury;Username=cashflow_svc;Password=..." dotnet run --project src/Services/Cashflow/Treasury.Cashflow.Api
```

## Everything in containers (compact on-premises profile)
See `deploy/docker-compose.yml`.

## Migrating old cheques
See `tools/migration/README.md`.
