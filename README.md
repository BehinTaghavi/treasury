# Treasury (Accounts Receivable / Payable) — new version

Replacement of the Access program `Lcash.mdb` and the unfinished MVC version, built as DDD microservice modules (.NET 10),
PostgreSQL 17 partitioned by fiscal year, and Nuxt 4 micro-frontends. Pilot client: Iran Nazho Co.

**Start here:** `CLAUDE.md` (rules and commands) → `docs/plan.md` (what is next) → `docs/decisions.md` → `docs/architecture.md` → `docs/getting-started.md`. Design document: `docs/assessment.md` (Farsi: `docs/assessment.fa.md`). Old systems for reference: `legacy/`.

## Status — Phase 0, first package (9 Nov 2026)

| Part | State | Verified how |
|---|---|---|
| Instrument lifecycle + cheque book (domain) | Built | 49 unit tests, each tied to the old program's behaviour |
| Use cases (receive, state changes, issue cheque with sheet reservation) | Built | 12 tests incl. 40 parallel issues from one book |
| Clean Architecture rules | Enforced | 6 architecture tests |
| Cashflow HTTP API (in-memory storage) | Built | Run and called over HTTP |
| PostgreSQL schema, fiscal-year partitions, closed-year freeze, roles | Built | 10 tests on PostgreSQL 16 (same partitioning features as 17) |
| PostgreSQL adapters (Npgsql) | Written, type-checked against an API stub | **Not yet compiled with the real package or run against a database** |
| Keycloak token validation in the API | Written | **Not yet run** (needs NuGet + a Keycloak realm) |
| Migration of old cheques + reconciliation report | Built | 21 tests incl. end-to-end load into PostgreSQL; Jalali dates checked day by day against .NET for 1380–1420 |
| Nuxt 4 shell + receipts micro-frontend (Module Federation) | Built | 7 UI-kit tests; browser test: receive, send for collection, duplicate warning, invalid date |
| Docker Compose (compact on-premises profile) | Written | **Not yet run** (no Docker daemon where it was written) |

Open business questions: `docs/domain/open-questions.md`.
