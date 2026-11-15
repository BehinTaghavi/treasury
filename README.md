# Treasury (Accounts Receivable / Payable) — new version

Replacement of the Access program `Lcash.mdb` and the unfinished MVC version, built as DDD microservice modules (.NET 10),
PostgreSQL 17 partitioned by fiscal year, and Nuxt 4 micro-frontends. Pilot client: Iran Nazho Co.

**Start here:** `CLAUDE.md` (rules and commands) → `docs/plan.md` (what is next) → `docs/decisions.md` → `docs/architecture.md` → `docs/getting-started.md`. Design document: `docs/assessment.md` (Farsi: `docs/assessment.fa.md`). Old systems for reference: `legacy/`.

## Status — Phase A, first real build (10 Nov 2026)

| Part | State | Verified how |
|---|---|---|
| Instrument lifecycle + cheque book (domain) | Built | 49 unit tests, each tied to the old program's behaviour |
| Use cases (receive, state changes, issue cheque with sheet reservation) | Built | 12 tests incl. 40 parallel issues from one book |
| Clean Architecture rules | Enforced | 7 architecture tests (incl. the PostgreSQL adapter assembly) |
| Cashflow HTTP API | Built | In memory and with PostgreSQL, called over HTTP through the gateway |
| Chart of accounts and تفصیل copied from the accounting system (Excel import with preview, allowed تفصیل types) | Built | 22 domain + 6 use-case + 9 Excel-reader tests, a PostgreSQL scenario with 10,000 تفصیل; checked locally with Iran Nazho Co.'s real exports (not in the repository) |
| The company's bank accounts (`accounts-svc`, manual section 5) | Built | 15 domain + 13 use-case + 5 PostgreSQL tests (incl. duplicate numbers in Persian digits, one backup per main account); browser check with the real chart of accounts |
| Basic information lists (`reference-svc`): currencies, country/province/city, bank names | Built | 20 domain + 13 use-case + 7 PostgreSQL tests (incl. concurrent duplicate names, Arabic/Persian letters); browser check of the Basic Information screen |
| PostgreSQL schema, fiscal-year partitions, closed-year freeze, roles | Built | 10 tests on PostgreSQL 17 |
| PostgreSQL adapters (Npgsql) | Built | 14 integration tests on PostgreSQL 17 as the `cashflow_svc` role, incl. 25 parallel issues from one book |
| Keycloak token validation in the API | Compiled | **Not yet run** (needs the `treasury` realm, Phase B); Keycloak itself runs in Docker Compose |
| Migration of old cheques + reconciliation report | Built | 21 tests incl. end-to-end load into PostgreSQL; Jalali dates checked day by day against .NET for 1380–1420 |
| Nuxt 4 shell + receipts micro-frontend (Module Federation) | Built | 7 UI-kit tests; browser test: receive, send for collection, duplicate warning, invalid date |
| Docker Compose (compact on-premises profile) | Runs | `deploy/smoke-test.sh` + browser check; CI job `compose` on every pull request |

Open business questions: `docs/domain/open-questions.md`.
