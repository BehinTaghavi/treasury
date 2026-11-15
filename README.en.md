# Treasury (Accounts Receivable / Payable) — new version

The repository's home page is the Farsi [README.md](README.md). This is its English copy; update both in the same change.

## What this repository is

The new **Treasury** module of the ERP: received and issued cheques and notes, receipts, payments, petty cash, payment requests, and posting vouchers to the general ledger.

It replaces two earlier programs:

- the Access program `Lcash.mdb`, in production for more than 20 years;
- the ASP.NET MVC version, which was never finished.

The MVC version is not repaired. The system is rebuilt, and each part runs beside the old program until it is accepted. Business rules are taken from the old program (`legacy/`) and every rule has its own test. Pilot client: **Iran Nazho Co.**

| Topic | Choice |
|---|---|
| Server side | .NET 10, Clean Architecture and DDD, one folder per service with Domain, Application, Infrastructure and Api layers |
| Database | PostgreSQL 17, one database per client company, one schema per service, transactional tables partitioned by fiscal year |
| User interface | Nuxt 4 shell and Vue micro-frontends loaded at run time with Module Federation 2.0 |
| Identity | Keycloak |
| Deployment | Docker Compose on the client's own server (or ArvanCloud) |

## How the project is run

- The product owner is the company's CEO, who confirms the business rules. Every confirmed decision is recorded in `docs/decisions.md`.
- Claude Code writes the code and one engineer reviews every pull request on GitHub. `main` is protected: one approval and green CI.
- Every pull request description has two parts: "What changed, in plain words" for the owner and "Technical notes" for the reviewer.
- Documents are written in English, which is the master. What the domain expert reads also has a Farsi copy (`*.fa.md`).
- No real client data, database backup or password enters this repository.

## Latest changes

By 15 Nov 2026 six pull requests were merged into `main`. The latest CI run on `main` after these merges is green.

| Pull request | Subject | What it added |
|---|---|---|
| [#6](https://github.com/BehinTaghavi/treasury/pull/6) | Shared command plumbing | The three services each had their own copy of the same code: idempotency key, one transaction per command, rule violations turned into error codes. It now lives once in `src/BuildingBlocks/Treasury.BuildingBlocks.Application`. No behaviour change. |
| [#5](https://github.com/BehinTaghavi/treasury/pull/5) | Basic information, step 3: the company's bank accounts | New `accounts-svc` and its screen: account number, bank, branch, currency, the bank's معین with تفصیل اول/دوم, collection and bounced-cheque accounts, and a backup account. A duplicate account number is refused (Persian and Latin digits count as the same). A main account has at most one backup, from the same bank. Accounts are switched off, not deleted. |
| [#4](https://github.com/BehinTaghavi/treasury/pull/4) | Basic information, step 2: chart of accounts and تفصیل | Account structure with four fixed levels (گروه، کل، معین، معین در معین) and a floating تفصیل; code lengths are set per company. Accounts and تفصیل are imported from the accounting system's Excel exports, with a preview before saving. Re-importing never deletes. Each account lists its allowed تفصیل types. Checked with Iran Nazho Co.'s real exports: 634 accounts and 9,628 تفصیل, no rejected row (the files are not in the repository). Voucher rules written down: `docs/domain/posting-rules.md`. |
| [#3](https://github.com/BehinTaghavi/treasury/pull/3) | Basic information, step 1: simple lists | New `reference-svc`: currencies, country/province/city and bank names, with default lists. New "Basic Information" micro-frontend. |
| [#2](https://github.com/BehinTaghavi/treasury/pull/2) | Owner's review of the receipts screen | Main menu order: Dashboard, Basic information, Payment requests, Payments, Receipts, Reports. Rule N2: a received cheque cannot be cleared or bounced before its due date; depositing it for collection earlier stays allowed. |
| [#1](https://github.com/BehinTaghavi/treasury/pull/1) | Phase A: first real build | Full build with NuGet packages and no warnings, PostgreSQL adapter tests on PostgreSQL 17, the whole system run with Docker Compose and `deploy/smoke-test.sh`, and CI with four jobs: `dotnet`, `database-and-migration`, `frontend`, `compose`. Test server on the owner's Windows PC, started with a double-click: `docs/test-server-windows.md`. |

Before these, the first package (Phase 0) was loaded on 9 Nov 2026: repository skeleton, the cheque and cheque-book domain model, the database schema, the migration tool, the Nuxt shell with the receipts screen, the design document, and the old programs' source for reference.

## Current status

Phase A is almost finished and Phase B (basic information and login) has started.

| Part | What is built | Tests |
|---|---|---|
| `cashflow-svc` | Lifecycle of received and issued cheques and notes, cheque books with sheet reservation on issue, rule N2, and its API | 53 domain tests, 12 use-case tests (incl. 40 parallel issues from one book), 14 PostgreSQL tests |
| `reference-svc` | Currencies, country/province/city, bank names; code lengths, chart of accounts and تفصیل with Excel import; allowed تفصیل types | 43 domain tests, 19 use-case tests, 9 Excel-reader tests, 8 PostgreSQL tests |
| `accounts-svc` | The company's bank accounts (manual section 5) | 15 domain tests, 13 use-case tests, 5 PostgreSQL tests |
| Architecture rules | Layers depend inward only; no service references another service's code | 14 architecture tests |
| Database | One schema per service, fiscal-year partitions, closed-year freeze, and one role per service that cannot see another service's schema | 14 tests on PostgreSQL 17 |
| Migration tool | Old cheques moved to the new schema with a reconciliation report; Jalali dates checked day by day against .NET for 1380–1420 | 23 tests |
| User interface | Nuxt 4 shell and two micro-frontends: Receipts and Basic Information | 9 ui-kit tests and browser checks |
| Deployment | Docker Compose for one client's server, and the Windows test server | `deploy/smoke-test.sh` and the CI job `compose` on every pull request |
| Login with Keycloak | Token validation in the API is compiled | **Not yet run**; comes later in Phase B |

There are 205 .NET tests in total. 27 need PostgreSQL and run on CI. One runs only locally, with a client's real files.

## Next steps

The full plan and phases A–G are in `docs/plan.md`.

- **Left in Phase A:** migration dry run on an anonymised copy of Iran Nazho Co.'s data (`Cashnajoo86`), and deployment notes for the pilot server.
- **Rest of Phase B:** references (عطف), purpose groups and purposes, cash desks and cheque books in Basic Information; then the receipts form uses them; then login with Keycloak.
- **Later phases:** payment requests (C), receipts and the full cheque lifecycle (D), payments and cheque printing (E), accounting vouchers (F), reports and the full pilot (G).
- **Open business questions:** Q1–Q10 in `docs/domain/open-questions.md` are not answered yet.

## Repository layout

| Path | Contents |
|---|---|
| `src/Services/Cashflow` | Cheques and notes, cheque books (later: receipts, payments, petty cash) |
| `src/Services/Reference` | Basic Information lists and the copy of the chart of accounts |
| `src/Services/Accounts` | The company's bank accounts |
| `src/BuildingBlocks` | The only code services share: `FiscalDate`, `PersianText`, `Result`, `CommandRunner` |
| `tests` | Domain, use-case, PostgreSQL adapter and architecture tests |
| `db/postgres` | Schema scripts, partition functions and schema tests |
| `tools/migration` | Old data moved to the new schema, with reconciliation reports |
| `frontend` | Shell (`shell`), micro-frontends (`remotes`) and shared parts (`packages/ui-kit`) |
| `deploy` | Docker Compose, gateway and Windows scripts |
| `docs` | Plan, decisions, architecture, ADRs, domain notes and the design document |
| `legacy` | The old Access program and the MVC version, for reference only; passwords masked |

## Build and test

Prerequisites: .NET SDK 10, Node.js 22 with pnpm 10, Python 3.11+, and PostgreSQL 17 or Docker. Details: `docs/getting-started.md`.

```bash
dotnet build Treasury.slnx
dotnet test
cd frontend && pnpm install && pnpm test && pnpm build
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build && deploy/smoke-test.sh
```

## For the reviewer: where to start

1. `CLAUDE.md`: the rules for every change, and the commands.
2. `docs/plan.md`: phases, what is done, what is next.
3. `docs/decisions.md`: confirmed decisions with date and reason.
4. `docs/architecture.md` and `docs/adr/`: architecture summary and architecture decisions.
5. `docs/domain/`: instrument lifecycle, posting rules and open questions (each with a Farsi copy).
6. `docs/assessment.md`: the full design document (Farsi: `docs/assessment.fa.md`).
