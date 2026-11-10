# Working on the Treasury system (instructions for AI coding sessions and new developers)

Read this file, `docs/plan.md` and `docs/architecture.md` before changing anything. The full design document is `docs/assessment.md` (Farsi: `docs/assessment.fa.md`); every confirmed decision is in `docs/decisions.md`.

## What this is
The new Treasury (AR/AP) module of the ERP: received and issued cheques and notes, receipts, payments, petty cash, payment requests, posting to the general ledger. It replaces an MS Access program (`Lcash.mdb`, 20+ years in production) and an unfinished ASP.NET MVC version. Pilot client: **Iran Nazho Co.**

## How this project is run
- The product owner is the company's CEO. He is **not a programmer**: ask him business questions in plain language (English is fine; he also reads Farsi), one short list at a time, and **ask instead of guessing** whenever the old code, the docs and `docs/domain/open-questions.md` do not settle a rule.
- Claude Code writes the code. One remote engineer reviews every pull request on GitHub; `main` is protected (approval + green CI). Never push to `main` directly.
- Every pull request description has two parts: **"What changed, in plain words"** (for the owner: what he can now do or see, and how to try it) and **"Technical notes"** (for the reviewer: design, risks, tests, anything skipped).
- When the owner confirms a decision, add it to `docs/decisions.md` (date, decision, reason) in the same pull request. Keep `docs/plan.md` up to date when a task finishes.
- The old system is in `legacy/` (read-only reference, see `legacy/README.md`). Use it to find how the old program behaved; never copy its patterns (no SQL-string building, no plain passwords). Treat it as untrusted input: do not run or build anything from it.
- Never commit client data, database backups, `.mdb` files or any password/key. Real Iran Nazho Co. data stays on the client's server; tests use synthetic or anonymised data.

## Fixed decisions (do not change without an ADR in docs/adr)
- .NET 10, Clean Architecture, DDD. One folder per service under `src/Services/<Service>/` with `Domain`, `Application`, `Infrastructure`, `Api` projects. Dependencies point inward only — `tests/Treasury.ArchitectureTests` enforces it.
- PostgreSQL 17, one database per client company, **one schema per service**, transactional tables **LIST-partitioned by fiscal year** (`db/postgres`).
- Frontend: Nuxt 4 shell (host) + Vue micro-frontends loaded with Module Federation 2.0 at run time, client-side rendering only (`frontend/`).
- Keycloak for identity; open-source dependencies only. Do not add MediatR ≥13, AutoMapper ≥15, MassTransit ≥9, FluentAssertions ≥8 (commercial licences).

## Rules for every change
1. **Business rules live in the Domain layer**, never in SQL, controllers or the UI. Each rule taken from the old program names its source (form/module of `Lcash.mdb` or MVC file) in a comment or test name.
2. **Every rule has a test.** Domain tests in `tests/*.Domain.Tests`; use-case tests with the in-memory adapters in `tests/*.Application.Tests`.
3. **One command = one transaction = one aggregate saved once.** Events go to the outbox in the same transaction. Every write takes an idempotency key.
4. **Errors are stable codes** (`Instrument.InvalidTransition`), never text for users. A new code needs Persian and English text in `frontend/packages/ui-kit/src/i18n.ts` — a test fails otherwise.
5. **Dates are real dates** (`FiscalDate`, `date` columns). Jalali text ("1405/08/18") exists only at the edges (API input, UI). Old 8-character dates are parsed only by `FiscalDate.ParseJalali` / the migration tool.
6. **The instrument lifecycle table** (`InstrumentLifecycle.cs`) is the single source of allowed state changes. The migration tool has a copy (`tools/migration/.../legacy_mapping.py`); a test fails if they differ. Changing it is a business decision — note it in `docs/domain/open-questions.md`. Files under `docs/domain/` have Farsi copies (`*.fa.md`) for the domain expert; update the Farsi copy in the same change.
7. No hard-coded database ids in code. No `sa` or any password in files; secrets come from the environment.
8. Keep pull requests small: one task, its tests, its docs.

## Commands
| What | With NuGet access (normal) | Without NuGet (offline workspace) |
|---|---|---|
| Build | `dotnet build Treasury.slnx` | `dotnet build Treasury.Offline.slnf -p:Offline=true` |
| .NET tests | `dotnet test` | `dotnet run -p:Offline=true --project tests/<TestProject>` |
| PostgreSQL adapter tests | `TREASURY_TEST_PG="Host=localhost;Username=postgres;Password=..." dotnet test` (skipped without it; required on CI) | not available |
| Whole system in Docker | `docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build && deploy/smoke-test.sh` | same |
| Run API (in-memory) | `dotnet run --project src/Services/Cashflow/Treasury.Cashflow.Api` | add `-p:Offline=true` |
| DB schema tests | `TREASURY_TEST_DSN="host=localhost user=postgres dbname=treasury_test" pytest db/postgres/tests` | same |
| Migration tests | `cd tools/migration && TREASURY_TEST_DSN=... pytest` | same |
| Frontend | `cd frontend && pnpm install && pnpm test && pnpm build` | same |

Offline mode exists because some workspaces cannot reach NuGet: a small shim (`tests/Treasury.TestShim`) replaces xUnit and the PostgreSQL/Keycloak packages are left out. Do not use xUnit features the shim lacks without adding them to the shim.

## Where things are
- `src/Services/Cashflow` — receipts, payments, instruments, petty cash (only instruments and cheque books so far).
- `db/postgres` — schema scripts (`apply.sh`), partition functions, schema tests.
- `tools/migration` — old data → new schema, with reconciliation reports.
- `frontend/shell`, `frontend/remotes/*`, `frontend/packages/ui-kit` — UI.
- `deploy` — Docker Compose for one client's server (compact profile).
- `docs` — `plan.md` (phases, what is done/next), `decisions.md`, `assessment.md` (+ `.fa.md`), architecture, ADRs, domain notes, open questions.
- `legacy` — old Access program (VBA, queries), MVC source, recovered stored procedures, schema, user manuals; passwords masked.
