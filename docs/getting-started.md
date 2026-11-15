# Getting started

## Prerequisites
.NET SDK 10, Node.js 22.21+ with pnpm 10, Python 3.11+, PostgreSQL 17 (or Docker).

## Run the system locally (no database, no login)
```bash
dotnet run --project src/Services/Cashflow/Treasury.Cashflow.Api          # http://localhost:5081, in-memory, Development
dotnet run --project src/Services/Reference/Treasury.Reference.Api        # http://localhost:5082, Basic Information lists
cd frontend && pnpm install
pnpm --filter @treasury/remote-receipts build && pnpm --filter @treasury/remote-receipts preview   # http://localhost:5174
pnpm --filter @treasury/remote-basic-info build && pnpm --filter @treasury/remote-basic-info preview # http://localhost:5175
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

## Tests of the PostgreSQL adapters
They create and drop their own database (`treasury_adapter_test_<pid>`) on the server you point them at:
```bash
docker run -d --name pg17 -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:17
TREASURY_TEST_PG="Host=localhost;Username=postgres;Password=postgres" dotnet test
```
Without `TREASURY_TEST_PG` they are skipped (on CI, where `CI` is set, they fail instead).

## Everything in containers (compact on-premises profile)
```bash
cp deploy/.env.example deploy/.env      # then set real passwords
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build
deploy/smoke-test.sh                    # checks shell, receipts screen, Keycloak and API + database through the gateway
```
Open http://localhost:8080 → "دریافت‌ها". The smoke test writes one test cheque: run it on test servers only.

On a Windows PC without the command line (the product owner's test server): `docs/test-server-windows.md`.

## Migrating old cheques
See `tools/migration/README.md`.

## In a Claude Code cloud session
The environment's setup script installs the .NET 10 SDK (from Ubuntu's archive), NuGet packages, frontend and Python packages.
Docker is installed but not started, and the sandbox's network proxy needs two local workarounds (never commit them):
```bash
dockerd > /tmp/dockerd.log 2>&1 &                 # start Docker
# Build images trust the proxy's certificate: shadow the two base images locally with copies that include it.
d=$(mktemp -d) && cp /root/.ccr/ca-bundle.crt $d/proxy-ca.crt
printf 'FROM node:22-alpine\nCOPY proxy-ca.crt /usr/local/share/ca-certificates/proxy-ca.crt\nRUN cat /usr/local/share/ca-certificates/proxy-ca.crt >> /etc/ssl/certs/ca-certificates.crt\nENV NODE_EXTRA_CA_CERTS=/usr/local/share/ca-certificates/proxy-ca.crt\n' > $d/node.Dockerfile
printf 'FROM mcr.microsoft.com/dotnet/sdk:10.0\nCOPY proxy-ca.crt /usr/local/share/ca-certificates/proxy-ca.crt\nRUN update-ca-certificates\n' > $d/dotnet.Dockerfile
docker build -t node:22-alpine -f $d/node.Dockerfile $d && docker build -t mcr.microsoft.com/dotnet/sdk:10.0 -f $d/dotnet.Dockerfile $d
# quay.io is not reachable: take Keycloak's identical image from Docker Hub.
docker pull keycloak/keycloak:26.4 && docker tag keycloak/keycloak:26.4 quay.io/keycloak/keycloak:26.4
```
