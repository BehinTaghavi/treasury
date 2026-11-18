# Notes written for the reviewer

The "Technical notes" of pull requests 1 to 14, collected on 18 Nov 2026 (27 Aban 1405). Farsi copy: [reviewer-notes.fa.md](reviewer-notes.fa.md).

> This is a snapshot of 18 Nov 2026. Later pull requests are not in it; their notes are on each pull request's page on GitHub.

## 1. What this document is

Every pull request in this project has a part called **"Technical notes"**, written for the reviewer: design choices, risks, tests, and what was skipped or not checked. This document collects those notes from all 14 pull requests, in order, in one place.

- The English text in part 4 is copied from GitHub without changes. The Farsi text is a translation of it. If the two differ, the English text on GitHub is the reference.
- Checked on GitHub on 18 Nov 2026: none of the 14 pull requests has a review, a comment or a line comment. So the "Technical notes" are everything written to the reviewer on GitHub so far.
- Two longer guides for the reviewer are already in the repository (both in Farsi):
    - [docs/reviewer-guide.fa.md](https://github.com/BehinTaghavi/treasury/blob/main/docs/reviewer-guide.fa.md): how the code is organised and what to check in each pull request;
    - [docs/reviewer-run-guide.fa.md](https://github.com/BehinTaghavi/treasury/blob/main/docs/reviewer-run-guide.fa.md): how to build and run any version with Git and Docker.

## 2. How to get the latest version

The repository is public, so these links work without an invitation.

| What | Link or command |
|---|---|
| The project (latest approved version, branch `main`) | <https://github.com/BehinTaghavi/treasury> |
| Pull requests waiting for review | <https://github.com/BehinTaghavi/treasury/pulls> |
| The same version as a ZIP file | <https://github.com/BehinTaghavi/treasury/archive/refs/heads/main.zip> |

**First time, in a terminal** (on Windows use Git Bash):

```bash
git clone -c core.autocrlf=false https://github.com/BehinTaghavi/treasury.git
cd treasury
```

**Every time after that**, to get the newest `main`:

```bash
git switch main
git pull
```

**To look at a pull request that is not merged yet** (number 14 as an example):

```bash
git fetch origin pull/14/head:pr-14
git switch pr-14
```

**To build and run what is on disk** (Docker needed; details in the run guide):

```bash
cp deploy/.env.example deploy/.env
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build
deploy/smoke-test.sh
```

Then open <http://localhost:8080>.

**Where the newest work is.** `main` holds pull requests 1 to 9 (last merge: 15 Nov 2026). The work after that is in the five open pull requests 10 to 14 and is not in `main` yet. To see it, check out the pull request as shown above.

**If the reviewer works with an AI coding assistant** (for example Claude Code), this one prompt brings it up to date:

```text
Clone https://github.com/BehinTaghavi/treasury.git (or run "git pull" on main
if it is already cloned). Read CLAUDE.md, README.en.md, docs/plan.md and
docs/reviewer-guide.fa.md. Then list the open pull requests at
https://github.com/BehinTaghavi/treasury/pulls and, for each one, show me its
"Technical notes" and the files it changes. Do not push, merge or comment
on anything.
```

## 3. Read this first

These points are taken from the notes in part 4 and from the state of GitHub on 18 Nov 2026.

**3.1 No review is recorded yet.** Pull requests 1 to 9 are merged, and GitHub shows no review on any of them. The largest is pull request 8 (45 commits, 251 files, about 24,600 added lines); the notes of pull request 9 say its review is still to come.

**3.2 Five pull requests are open, and four of them overlap.** The notes of pull requests 12 and 14 describe the overlaps:

| Pull requests | The overlap | What the notes say |
|---|---|---|
| 11 and 14 | Both build the rule "recorded in Sayad before the bank". 11 does it as a tick on the cheque (owner, 15 Nov); 14 as a step and a status, rule N3 (owner, 18 Nov). | Only one should be merged. 14 follows the owner's later rule. |
| 12 and 14 | Both keep the company's name and address in `reference-svc` («اطلاعات شرکت» in 12, «مشخصات شرکت» in 14). | They need to become one page before both are merged. |
| 10 and 14 | Both change the same lifecycle, receipt and voucher code. A trial merge gave 26 conflicted files, several of them real design conflicts. | 10 has to be adapted to 14, or the other way round. |
| 10, 11, 12, 14 | Schema scripts 033 and later, and question numbers Q37 and later, are used by more than one of them. | Whichever merges later renumbers its questions (and preferably its scripts). |

Pull request 13 (version stamp, gateway fix) is not named in any of these overlaps. Pull request 14 was tested locally merged with 13.

*A possible order, as a suggestion only (the decision is the reviewer's and the owner's):* 13 first; then 14; close 11 if 14 is accepted; make 12 and the company page of 14 one page; then adapt 10 to the result.

**3.3 CI on the open pull requests** (GitHub, 18 Nov 2026): all four jobs are green on 10, 11, 12 and 13. On 14, the job `compose` failed and the other three passed ([the failed run](https://github.com/BehinTaghavi/treasury/actions/runs/44247305117/job/134609249577)). The notes of 14 do not mention this failure.

**3.4 Things the notes say were not tested or not done:**

- **Real Windows.** The `.cmd` files, the automatic start of Docker Desktop and Windows PowerShell 5.1 were not run on a real Windows machine (1). The Windows script was not run for the version stamp (13).
- **Login.** There is no login yet; the endpoints of all services are open. Test data only (3, 5, 12).
- **Pull request 8.** The tests were not re-run when it was opened; CI on the pull request was their first run. CI result quoted in 9: 510 .NET tests, 509 passed, 1 skipped.
- **PostgreSQL 16 instead of 17 on the local machine,** and Docker Compose not run by hand (10, 11, 12). CI ran both on PostgreSQL 17 and in the `compose` job.
- **Two known gaps in the PostgreSQL adapter** (1): a rejection by the closed-year trigger comes out as HTTP 500, not a stable code; the `ChequeBook.SheetAlreadyUsed` path from a unique violation is not covered by a test.
- **Promissory notes** (10): endorsing a note and the voucher for handing an ordinary note back are covered by tests only, not seen in a browser. The wording swap in `ChequeOperations.vue` is done by text replacement.
- **The run guide** (9): its commands were not re-run by hand after it was updated.

## 4. Notes of each pull request

| No. | Title | Status |
|---|---|---|
| [1](#pull-request-1) | Phase A 1–4 and 6: first real build, database tests, Docker Compose, CI, test server on the owner's PC | Merged, 15 Nov 2026 |
| [2](#pull-request-2) | Owner review of receipts: main menu order and rule N2 (no clearing or bounce before the due date) | Merged, 15 Nov 2026 |
| [3](#pull-request-3) | Basic information, step 1: currencies, country/province/city, bank names (reference-svc) | Merged, 15 Nov 2026 |
| [4](#pull-request-4) | Basic information, step 2: chart of accounts and تفصیل from the accounting system (Excel import) | Merged, 15 Nov 2026 |
| [5](#pull-request-5) | Basic information step 3: the company's bank accounts (accounts-svc) | Merged, 15 Nov 2026 |
| [6](#pull-request-6) | Share the command plumbing of the three services (no behaviour change) | Merged, 15 Nov 2026 |
| [7](#pull-request-7) | Docs: reviewer's guide in Farsi | Merged, 15 Nov 2026 |
| [8](#pull-request-8) | Work of 11–15 Nov: Basic Information completed, opening balances, receipts, payment requests, vouchers, cheque steps | Merged, 15 Nov 2026 |
| [9](#pull-request-9) | Docs: Farsi home page and the reviewer's build-and-run guide, brought up to today's main | Merged, 15 Nov 2026 |
| [10](#pull-request-10) | Promissory notes: receipt form with printed receipt, steps after receipt, and authorised signatories | Open since 16 Nov 2026, CI green |
| [11](#pull-request-11) | «ثبت صیادی شد»: a cheque goes to the bank only after it is ticked as recorded in Sayad | Open since 17 Nov 2026, CI green |
| [12](#pull-request-12) | «اطلاعات شرکت»: the company's name and logo at the top of every screen, with its official details | Open since 17 Nov 2026, CI green |
| [13](#pull-request-13) | Show the running version at the top of the screen and in the «Start Treasury» window | Open since 17 Nov 2026, CI green |
| [14](#pull-request-14) | Legal action on bounced cheques, rule N3 («ثبت صیادی» before the bank), certificate of non-payment, receipt lists, company particulars | Open since 18 Nov 2026, CI: **`compose` failed** |

### Pull request 1

**Phase A 1–4 and 6: first real build, database tests, Docker Compose, CI, test server on the owner's PC**

Merged, 15 Nov 2026 · branch `claude/adoring-knuth-2m722k` · https://github.com/BehinTaghavi/treasury/pull/1

**Build / .NET**
- SDK: `dotnet-sdk-10.0` 10.0.114 from Ubuntu's archive (`builds.dotnet.microsoft.com` is blocked in the cloud sandbox; NuGet is not). It satisfies `global.json` (10.0.100, `latestFeature`). CI uses `setup-dotnet` as before.
- The first build with NuGet had 0 warnings, so `TreatWarningsAsErrors` is back on for `Infrastructure.Postgres`. That project now uses `Microsoft.Extensions.DependencyInjection.Abstractions` instead of the whole `Microsoft.AspNetCore.App` framework. New architecture test: no `Treasury.Cashflow.Infrastructure*.dll` (found by file scan, so it works offline and online) references ASP.NET Core or the Api.
- **Bug:** `InstrumentUseCases.GetAsync` called the repository outside `IUnitOfWork.ExecuteAsync`. The Npgsql adapters need the ambient transaction, so they threw `InvalidOperationException`. In-memory hid it. GetAsync now runs in a unit of work. I checked it: reverting the fix makes 4 adapter tests fail.

**PostgreSQL adapter tests** (`tests/Treasury.Cashflow.Infrastructure.Postgres.Tests`)
- An xUnit v3 assembly fixture creates `treasury_adapter_test_<pid>`, applies `db/postgres/0??_*.sql` the way `apply.sh` does (roles as admin, objects as `treasury_owner`), and opens years 1403–1405. The adapters run with `Options=-c role=cashflow_svc`, so missing grants would fail. The database is dropped at the end.
- Connection comes from `TREASURY_TEST_PG` (Npgsql format). Without it the tests are **skipped** locally and **fail** when `CI` is set, so CI cannot silently skip them. On CI: 82 succeeded, 0 skipped.
- Coverage: field round trip (incl. foreign currency, rate and Rial amount), history, outbox jsonb with ISO dates, idempotency row, duplicate warning, steps in the partition of the year they happen in (1404 → 1405), rollback on rule violation, optimistic concurrency, closed-year trigger as last defence, sheet order, book closing on its last sheet, 25 concurrent issues under real row locks, a refused issue leaving its sheet free.
- **Not covered:** the `cheque_sheet` unique-violation → `ChequeBook.SheetAlreadyUsed` path. It can't be reached while the `FOR UPDATE` lock holds (the FK insert would block on it). Also, a closed-year trigger rejection comes out of the use case as a raw `PostgresException` (HTTP 500), not a stable code. That is acceptable for now because the application's fiscal-period check runs first. Worth mapping when posting-svc arrives.

**Schema (restart bug)**
- `apply.sh` failed on its second run: `002_cashflow.sql:114 permission denied for table instrument_fy1405`. Partitions created by `open_fiscal_year` run as `postgres` (db-init, tests) were owned by `postgres`, so `GRANT … ON ALL TABLES` as `treasury_owner` failed. db-init re-runs on every `docker compose up`, so a stopped stack never came back.
- `ensure_fiscal_year_partitions` now does `ALTER TABLE … OWNER TO treasury_owner`. New test `test_the_scripts_can_run_again_after_years_were_opened_by_an_administrator` re-runs `apply.sh` and checks the owners; it fails without the fix.
- Existing databases with `postgres`-owned partitions need a one-time `ALTER TABLE … OWNER TO treasury_owner`. None exist outside test machines.

**Docker Compose** (all 7 services up, `db-init` exits 0)
- Added `.dockerignore` (root) and `frontend/.dockerignore`. Without them, host `bin/obj` and `node_modules` were sent into the builds.
- `gateway.conf`: `Host $http_host` + `X-Forwarded-Host`. With `$host` the port was lost: the Keycloak issuer was `http://localhost/auth/...` instead of `:8080`.
- The gateway port is now bound to `${GATEWAY_ADDRESS:-127.0.0.1}`. The API runs in Development without login, so the default must not expose it. A server sets `GATEWAY_ADDRESS=0.0.0.0` once Keycloak is configured (`.env.example` documents it).
- `frontend/nginx.conf`: `absolute_redirect off`. `/receipts` redirected to `http://localhost/receipts/`, dropping the port.
- Receipts remote: `dts: false`. Plain `tsc` can't compile `.vue`, so every build logged `TYPE-001`, and nothing consumes the types. Removed the tracked `.mf/diagnostics/latest.json` (a build log of that error) and ignored `.mf/`.
- db-init calls `bash /db/apply.sh` (no reliance on the exec bit from a Windows bind mount). `.gitattributes`: `*.sh eol=lf`, and `*.cmd -text` stored as CRLF.
- `deploy/smoke-test.sh`: shell, `/receipts` redirect, MF manifest and entry, Keycloak issuer (host and port), receive → read → deposit through the gateway. It fails against the old `gateway.conf`. A Playwright run also submitted the receipts form with no console errors.
- Sandbox-only workarounds, **not committed** (described in `docs/getting-started.md`): local base images with the proxy CA, and Keycloak pulled from Docker Hub and tagged as `quay.io/...`, because quay.io is blocked here. GitHub runners need neither.

**Windows test server** (`deploy/windows`, `docs/test-server-windows.md`)
- `treasury.ps1` (Windows PowerShell 5.1 syntax) with `start` / `stop` / `reset`, wrapped by three CRLF `.cmd` files using `-ExecutionPolicy Bypass`.
- Settings are generated once into `%LOCALAPPDATA%\Treasury\treasury.env` (outside the unpacked folder, so updating = unpack the new ZIP anywhere). The compose project name `treasury` keeps the `pgdata` volume across versions.
- The script starts Docker Desktop if needed and waits for `docker info`. Native-command checks run with `ErrorActionPreference=Continue` (PS 5.1 otherwise turns redirected stderr into terminating errors). It uses `curl.exe` explicitly (`curl` is an alias in PS 5.1). Readiness = API 404 on an unknown id + shell 200; then it opens the browser.
- **Tested** with PowerShell 7.6 on Linux against the real stack: fresh start, smoke test, stop → start with data kept (this found the restart bug), declined and confirmed reset, start empty. The script parses with 0 errors. **Not tested on real Windows** (no Windows machine here): the `.cmd` wrappers, Docker Desktop auto-start and the PS 5.1 runtime. The owner's first run is that test.

**CI** (`.github/workflows/ci.yml`)
- The `dotnet` job has a `postgres:17` service and `TREASURY_TEST_PG`, and also builds `Treasury.Offline.slnf -p:Offline=true` so offline mode can't rot.
- New `compose` job: throw-away passwords, `up -d --build`, smoke test, **stop → up → smoke test again**, logs on failure, `down -v`.
- Triggers changed to `pull_request` + `push` to `main` (was every push, which ran each PR twice). Added `concurrency` cancellation.
- `tools/migration/pytest.ini` (`pythonpath = .`): plain `pytest` could not import `treasury_migration`, so that CI step could never have passed.
- `pnpm/action-setup` now gets `package_json_file: frontend/package.json`. It looked for a root `package.json` (there is none), so the frontend job failed before installing anything.

**Also verified:** offline build and shim runs still pass (49 + 12 + 7). Docs updated: `docs/plan.md` (tasks 1–4 and 6, done log), `docs/decisions.md`, README status table, `docs/getting-started.md`, CLAUDE.md commands. No `docs/domain` changes, so no Farsi copies needed.

### Pull request 2

**Owner review of receipts: main menu order and rule N2 (no clearing or bounce before the due date)**

Merged, 15 Nov 2026 · branch `claude/menu-and-clearing-date-rule` · https://github.com/BehinTaghavi/treasury/pull/2

- **Domain:** `RecordCleared` and `RecordBounced` check the rule through a new optional `validate` callback on `Apply`, via a shared `RequireNotBeforeDueDate` helper. It runs after the lifecycle transition, kind and N1 checks and before any state change, so an invalid transition still reports `Instrument.InvalidTransition` first. Instruments without a due date (guarantee items) are unaffected. Issued cheques (`RecordBouncedAtBank`, `RecordPaidByBank`) are unchanged.
- **New codes:** `Instrument.ClearedBeforeDueDate` and `Instrument.BouncedBeforeDueDate`, with fa/en texts in `ui-kit/src/i18n.ts` (enforced by `codes.test.ts`).
- **Tests:** 4 domain tests (clearing refused before the due date, allowed on it; bounce refused and allowed; invalid transition reported first). The adapter test for clearing across fiscal years now uses a realistic due date (1405/01/10 instead of the default 1405/09/01, which N2 rightly refused).
- **Migration:** `map_cheque` keeps old early clearings and bounces as recorded, with a warning `… before the due date … (rule N2); kept as recorded`. 2 new tests.
- **Lifecycle table unchanged.** This is a date rule, not a transition, so `legacy_mapping.py` `TRANSITIONS` and the parity test are untouched.
- **Shell:** menu order in `layouts/default.vue`; new `pages/payment-requests.vue` placeholder (prerendered).
- **Verified:** 86 .NET tests (incl. 14 adapter tests on PostgreSQL 17), 11 DB tests, 23 migration tests, 7 UI-kit tests, shell build. Browser check on the Docker stack: menu order, and the Persian message for an early clearing with the cheque left in `DepositedForCollection`.
- **Docs:** decisions (7 rows), plan (Phase B reordered: basic information first), Q11 asked and answered, lifecycle date rules N1/N2, in English and Farsi.

### Pull request 3

**Basic information, step 1: currencies, country/province/city, bank names (reference-svc)**

Merged, 15 Nov 2026 · branch `claude/basic-info-reference-lists` · https://github.com/BehinTaghavi/treasury/pull/3

**New service `reference-svc`** (`src/Services/Reference/`, ADR 0001 module with its own projects, schema, role and outbox):
- **Domain:** one aggregate `CatalogueItem` with `CatalogueKind` (Currency, Country, Province, City, Bank).
  - Rules: parent kind (Province→Country, City→Province), parent active; ISO-4217 currency code, upper-cased and immutable; Rial (`IRR`) cannot be deactivated; name cleaned, ≤ 100 characters; deactivate instead of delete.
  - Events: `CatalogueItemChanged`.
  - `PersianText` (BuildingBlocks): `Clean` (trim, collapse spaces, keep ZWNJ) and `Key` (ي/ى→ی, ك→ک, Persian/Arabic digits → ASCII, spaces and ZWNJ removed, lower-case) for comparisons.
- **Application:** `CatalogueUseCases` (Add, Rename, SetActive with expected version, List in `fa-IR` order).
  - Same pattern as cashflow: idempotency key, one transaction, failed command saves nothing, `Concurrency.Conflict` on a stale version.
  - `Result<T>`, the ports and `ConcurrencyConflictException` are duplicated from cashflow rather than shared. A small shared application kernel could follow later.
- **Default lists** (`DefaultLists.SeedAsync`, run at API start): only for a list with no entries at all, through the use cases with fixed idempotency keys, so they get the same validation, normalisation and outbox events as typed entries. The migration tool will bring old clients' own lists later.
- **PostgreSQL** `db/postgres/004_reference_catalogue.sql`:
  - `reference.catalogue_item` with checks: kind, code only for currencies, parent only for province/city.
  - Unique `(kind, parent_id, name_key) NULLS NOT DISTINCT` and unique currency code. `reference.idempotency`. Grants to `reference_svc`.
  - The Npgsql adapter maps unique violations from concurrent adds to `Catalogue.NameTaken` / `Catalogue.CodeTaken`.
- **API** `/api/reference/{currencies|countries|provinces|cities|banks}`: GET (`parentId`, `includeInactive`), POST, PUT `{id}`, POST `{id}/activate|deactivate`. Problem details carry the stable code. Keycloak wiring is the same as cashflow (login comes after basic information).
- **Deploy:**
  - `reference-api` and `remote-basic-info` services; gateway routes `/api/reference/` and `/remotes/basic-info/`.
  - `REFERENCE_DB_PASSWORD` in `.env.example`, CI and db-init.
  - The Windows script now **adds missing settings to an existing settings file** and never changes existing ones. Tested from an old file.
- **Frontend:** new micro-frontend `remotes/basic-info` (exposes `./BasicInfo`, `dts: false`), with a shared list editor driven by a `ListSpec`.
  - ui-kit gets `put()` and fa/en texts for the 12 new codes plus `Concurrency.Conflict`; `codes.test.ts` enforces this.
  - Shell page `basic-info` loads the remote; dev remotes and dev proxy updated.
- **Tests:**
  - 20 domain, 13 use-case (incl. defaults seeded once and not over a client's own list), 7 PostgreSQL adapter (as `reference_svc`, incl. 8 concurrent adds of the same bank → one success, seven `Catalogue.NameTaken`), 2 schema, 1 UI-kit.
  - Architecture: layer rules checked for every service; no service references another.
  - The PostgreSQL fixture moved to `tests/Support` (linked into both adapter test projects) and takes a server-wide advisory lock during setup, because the two projects run in parallel and share the cluster's roles.
  - Test shim: condition-based `Contains`/`DoesNotContain`/`Single` and `[InlineData(null)]`, so the offline build keeps compiling.
- **Verified locally:**
  - 128 .NET tests (0 skipped), 13 DB, 23 migration, 8 UI-kit; offline build plus 107 shim tests; frontend build.
  - Docker stack via the Windows script from an old settings file, plus the extended smoke test.
  - Browser walk-through of the screen: add, Arabic-letter duplicate refused, city needs a province, switch off, Rial refused, English.
- **Not in this PR:** bank accounts, cheque books UI, references, purpose groups, cash desks; migration of the old lists (`Bank`, `BasicBanks`, `City`).

### Pull request 4

**Basic information, step 2: chart of accounts and تفصیل from the accounting system (Excel import)**

Merged, 15 Nov 2026 · branch `claude/ledger-accounts-import` · https://github.com/BehinTaghavi/treasury/pull/4

- **Domain** (`Treasury.Reference.Domain/Ledger`):
  - `AccountCodeFormat`: per-company lengths that must grow level by level; level by code length; parent = code prefix.
  - **`ChartOfAccounts`** and **`DetailAccountDirectory`** are single aggregates, a copy of the ERP. An import is one command, one transaction, one event.
  - `Import`/`Preview` handle: parents before children whatever the file order; code-length, duplicate, missing-parent and empty-name rows rejected with the Excel row number; missing accounts deactivated and reactivated when they return; `IsPostable` = Subsidiary without an active child, or SubSubsidiary.
  - A file with nothing acceptable is refused (`Ledger.NothingToImport`), so a wrong file cannot switch off the chart. Code lengths are locked once accounts exist.
  - **Allowed تفصیل types are sets per position** (owner, 10 Nov 2026). `SetDetailTypes` only on postable accounts, types must exist in the تفصیل list, detail 2 needs detail 1. `LedgerAccount.CheckDetails` gives `Ledger.DetailRequired` / `DetailNotAllowed` / `DetailTypeNotAllowed`, ready for bank accounts and vouchers.
- **Application:** `LedgerUseCases` (format, preview/import for both lists, detail types, account and تفصیل queries via `ILedgerQueries`, search capped at 200).
  - New shared `CommandRunner` (idempotency, one transaction, failure saves nothing); the catalogue use cases now use it too.
  - `ILedgerExportReader` port.
- **Excel** (`Treasury.Reference.Infrastructure.Excel`, ExcelDataReader 3.9.0, MIT; not in the offline build, where imports return `Excel.NotAvailable`):
  - Columns are found by header text (PersianText keys) and separator/footer rows are skipped.
  - cp1256 fallback: the old exports have no CODEPAGE record. Codes from doubles; Persian digits normalised.
  - Clear codes `Excel.UnknownLayout` / `Excel.Unreadable`.
- **PostgreSQL** `005_reference_ledger.sql`:
  - Single-row header tables (`ledger_chart`, `detail_directory`) carry the aggregate version.
  - `ledger_account` has a self-FK (checked per statement, so parent and child go in one upsert) and `detail1_types`/`detail2_types text[]`. `detail_account` has `name_key` for letter-insensitive search.
  - Only changed rows are written, with one `INSERT … SELECT FROM unnest(…) ON CONFLICT` per list. Text-array sets travel as unit-separator-joined strings (nested arrays cannot be unnested).
- **API** `/api/reference/ledger/*`: format, `chart/import` and `details/import` (multipart, `?preview=true`, antiforgery off for this token-based API, 20 MB cap), accounts, `accounts/{code}/detail-types`, details search, detail-types.
- **Frontend:** `ImportBox` (preview → confirm), `ChartAccounts`, `DetailAccounts`; ui-kit `upload()` and fa/en texts for every Ledger/Excel code, including import reasons.
- **Tests:**
  - 22 ledger domain (format, tree, reasons by row, reimport, preview, empty file, lock, allowed type sets and `CheckDetails`), 6 use-case.
  - PostgreSQL scenario as `reference_svc`: 10,000 تفصیل in one transaction in about 1 s, letter-insensitive search, `%` escaped, type sets round trip, reimport keeps types.
  - 9 Excel tests: **made-up** `.xls` fixtures in the exports' layout, generated by `Fixtures/make_fixtures.py`.
  - An optional `TREASURY_REAL_EXPORTS` test that I ran with the client files: 0 rejected rows. Skipped on CI.
  - 1 UI-kit test; shim `Assert.All`.
- **Verified:**
  - 167 .NET tests (1 skipped = optional real-exports test), 13 DB, 23 migration, 9 UI-kit, offline build plus shim tests, frontend build.
  - Docker stack via the Windows script on an existing database (005 applied on update), smoke test, browser walk-through with the real files.
- **Not in this PR:** the direct ERP link (Phase F); بانک accounts (#5).
  - The reference copy lives in reference-svc until `posting-svc` exists. Moving it is a module move, ADR 0001.

### Pull request 5

**Basic information step 3: the company's bank accounts (accounts-svc)**

Merged, 15 Nov 2026 · branch `claude/bank-accounts` · https://github.com/BehinTaghavi/treasury/pull/5

- **New service `accounts-svc`** under `src/Services/Accounts` (Domain / Application / Infrastructure / Infrastructure.Postgres / Api), on port 5083 locally.
  - It has its own schema `accounts` and role `accounts_svc` (`db/postgres/006_accounts.sql`), plus its own outbox and idempotency tables.
  - There are no cross-schema foreign keys.
- **ADR 0001:** banks, cities, currencies, ledger accounts and تفصیل are read from `reference-svc` over HTTP (`IReferenceLookup` / `HttpReferenceLookup`).
  - reference-svc gained `GET /api/reference/{list}/{id}`, `/ledger/accounts/{code}` and `/ledger/details/{code}`.
  - If reference-svc is down, a command fails with `Reference.Unavailable` (HTTP 503). Nothing is saved half-checked.
- **Domain:** `BankAccount` aggregate.
  - The rules come from Access `Form_Bankacc` and MVC `BanksController` (backup rules, lines 114–115 and 249); each source is named in the tests.
  - Uniqueness uses a digits-only `number_key`, so Persian and Latin digits compare equal.
  - DB backstops: a UNIQUE constraint on `number_key` and the partial unique index `bank_account_one_backup`, both mapped back to stable codes in the unit of work.
- **`FiscalDate` moved to BuildingBlocks.Domain,** so there is still one Jalali parser. Cashflow now has `using` changes only.
- **Deploy:**
  - compose service `accounts-api`, with gateway route `/api/accounts/`;
  - `ACCOUNTS_DB_PASSWORD` in `.env.example`, CI, and the Windows script. The script adds the missing setting to an existing settings file; I checked this on an old file, and the existing database got the new role and schema;
  - `smoke-test.sh` checks the new endpoint.
- **Tests:**
  - 15 domain + 13 use-case + 5 PostgreSQL adapter tests;
  - a DB schema test proving `accounts_svc` sees no other schema;
  - architecture tests now cover Accounts;
  - all 31 new error codes have fa/en text in `i18n.ts`.
- **Full run:** 201 .NET tests (200 passed, 1 skipped), 14 DB tests, 23 migration tests, 9 UI-kit tests, and the frontend build all pass. The offline build has 0 warnings, and the Accounts shim runs pass. Docker smoke test and Playwright browser check also pass.
- **Not done yet:**
  - no login; the endpoints are open, as with the other services until Phase B login;
  - balances, and the link from cheque books to bank accounts, come later.
- **Follow-up:** the shared application building block is BehinTaghavi/treasury#6.

### Pull request 6

**Share the command plumbing of the three services (no behaviour change)**

Merged, 15 Nov 2026 · branch `claude/shared-application-block` · https://github.com/BehinTaghavi/treasury/pull/6

- **New project `src/BuildingBlocks/Treasury.BuildingBlocks.Application`** (namespace `Treasury.BuildingBlocks.Application`). It references only `Treasury.BuildingBlocks.Domain` and contains:
  - `Result<T>`: the cashflow version with `Warnings`, a superset of the other two;
  - `IUnitOfWork`, `IIdempotencyStore` and `ConcurrencyConflictException`;
  - `CommandRunner`: idempotency key check, one transaction, failed `Result` aborts the transaction (idempotency record included), `DomainException` → result, `ConcurrencyConflictException` → `Concurrency.Conflict`.
- **Removed:** the copies in the Accounts and Reference `Application/Abstractions` folders, and the duplicate types in Cashflow's `Ports.cs`. The service-specific ports stay where they were.
- **Cashflow:** `InstrumentUseCases` replaces its private `RunAsync`/`CommandFailedException` with the shared `CommandRunner`. The logic is the same: it is the code the other two were copied from.
- **Everything else** is `using` changes, plus solution entries in `Treasury.slnx` and `Treasury.Offline.slnf`. Dockerfiles copy all of `src/`, so they are unchanged.
- **Architecture tests (+4):**
  - the shared block references only `BuildingBlocks.Domain` and `System.*`;
  - each of the three services' Application assemblies references it.
- `docs/architecture.md` describes both building blocks and says business rules never go there.
- **Verified:**
  - 205 .NET tests (204 passed, 1 skipped as before), including the PostgreSQL adapter tests against PostgreSQL 17;
  - the offline build has 0 warnings;
  - shim runs of the Cashflow, Reference and Accounts application tests and of the architecture tests.
  - DB, migration and frontend are untouched; CI runs them.
- **Risk:** low. This pull request's own change is 37 files with 81 lines added and 221 removed, all mechanical, and the API contracts are unchanged.

### Pull request 7

**Docs: reviewer's guide in Farsi**

Merged, 15 Nov 2026 · branch `claude/wonderful-mccarthy-e19bxy` · https://github.com/BehinTaghavi/treasury/pull/7

- Documentation only: one new file, one sentence in `README.md` ("Start here") and one row in the done log of `docs/plan.md`. No code, schema or test changes.
- The guide describes the code as of 15 Nov 2026 and says what is not built yet (Keycloak login, the outbox publisher, the four later services, the `DevelopmentPolicies` stand-ins in cashflow-svc).
- Checked: every repository path and every class, method, test and error-code name mentioned in the guide exists in the repository (scripted check). The page was rendered to HTML to confirm the right-to-left layout, the 21 tables, the checklist and the two left-to-right diagrams.
- Not checked: the external links were not opened. They point to the front pages of official documentation sites, with page titles to search for.
- Farsi only, at the owner's request. `docs/decisions.md` says English is the master for documents; this guide has no English copy. If one is wanted, it can follow in a separate pull request.
- The guide is a snapshot: it will need updating when the patterns it describes change (it says so in its first lines).

### Pull request 8

**Work of 11–15 Nov: Basic Information completed, opening balances, receipts, payment requests, vouchers, cheque steps**

Merged, 15 Nov 2026 · branch `claude/nifty-newton-d1cong` · https://github.com/BehinTaghavi/treasury/pull/8

**Why one large pull request.** It is 45 commits, 251 files, about 24,600 added lines, well past the "one task per pull request" rule. The commits build on each other (receipts on the Basic Information changes, payment requests and vouchers on receipts, cheque steps on vouchers), so it is sent as one piece instead of being re-cut. The commits are topical and in order; reviewing commit by commit is the practical way through.

**CI has not run on any of this yet.** The workflow runs on pull requests and on pushes to `main` only, so this pull request is the first time these commits go through it. The tests were not re-run when opening it. Failures, if any, will be fixed on this branch.

**The branch is 4 merge commits behind `main`** (the merge commits of pull requests 1, 2, 5 and 6). Its base is the tip of pull request 6, so the content is the same; a trial merge into `main` is clean.

What to look at:

- **New service** `src/Services/Requests` (`requests-svc`: Domain, Application, Infrastructure, Infrastructure.Postgres, Api) with its three test projects, database role `requests_svc`, and micro-frontend `frontend/remotes/requests`. Wired into Docker Compose, the gateway, CI, the smoke test and the Windows start script. `deploy/.env.example` gains `REQUESTS_DB_PASSWORD` (placeholder value).
- **New micro-frontend** `frontend/remotes/receipts`; large additions to `frontend/remotes/basic-info` and the shell menu.
- **Schema scripts 007 to 032** under `db/postgres` (reference, accounts, cashflow, requests), plus changes to `000_roles.sql` and `db/postgres/tests/test_schema.py`.
- **`InstrumentLifecycle.cs` changed** (the single source of allowed state changes), with the matching change in `tools/migration/treasury_migration/legacy_mapping.py` and in `docs/domain/instrument-lifecycle.md` / `.fa.md`. This is a business decision per the project rules; the owner's answers behind it are Q30 to Q36.
- **Cashflow domain**: `Receipt`, vouchers, cheque steps, cheque formats, with domain, use-case and PostgreSQL tests.
- **`frontend/packages/ui-kit/src/i18n.ts`**: about 570 added lines of Persian and English text for the new error codes and screens.
- **Docs**: `docs/decisions.md` (owner's decisions of 11 to 15 Nov), `docs/plan.md`, `docs/domain/open-questions.md` (Q12 to Q36), `posting-rules.md`, new `cheque-printing.md` and `received-cheque-steps.md`, each with its Farsi copy; an addition to `docs/adr/0002-one-transaction-per-command.md`.
- **New package references**: `Npgsql 10.0.3`, `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0`, `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0` (for the new service); the new remotes use the same frontend packages as the existing ones. None is on the list of excluded commercial licences.

Checked when opening this pull request: the diff adds no `.mdb`, backup, Excel, archive or key files and no binary files, and a pattern search of the added lines found no password or key values.

Not in this pull request: the reviewer's guide in Farsi (pull request 7) and the Farsi README home page (branch `claude/charming-feynman-obr5jn`, which will need a small fix in `docs/decisions.md` and `docs/plan.md` after this merges).

### Pull request 9

**Docs: Farsi home page and the reviewer's build-and-run guide, brought up to today's main**

Merged, 15 Nov 2026 · branch `claude/eloquent-johnson-jflwpx` · https://github.com/BehinTaghavi/treasury/pull/9

- Documentation only: no code, schema or test changes.
- **Source branches.** `claude/charming-feynman-obr5jn` (README) and `claude/magical-lovelace-xt2v4e` (guide) are merged here with their history. Conflicts with pull requests 7 and 8 in `docs/plan.md`, `docs/decisions.md` and `docs/architecture.md` keep both sides.
- **Two reviewer guides, two file names.** The second branch added its guide as `docs/reviewer-guide.fa.md`, the name pull request 7 already uses for the guide to the code and to reviewing. The build-and-run guide is kept as `docs/reviewer-run-guide.fa.md`; the links in `CLAUDE.md`, `docs/getting-started.md`, `docs/plan.md` and both READMEs follow.
- **Brought up to date with `main` after pull request 8:**
  - README (both languages): latest changes (pull requests 7 and 8), current status (four services, three micro-frontends), next steps, repository layout, reviewer's starting points.
  - Run guide: twelve Compose services instead of ten (`requests-api`, `remote-requests`), four APIs in the smoke test, the menu with «موجودی اول دوره», what each menu item shows, `/api/requests/`. Taken from `deploy/docker-compose.yml`, `deploy/gateway.conf`, `deploy/smoke-test.sh` and the shell's menu.
  - `CLAUDE.md`: the "Where things are" line for the services, and the two README/guide rules the branches added.
- **Test counts.** The README's per-project test counts were from six pull requests ago. They are replaced by test kinds per part and one total taken from the CI run of pull request 8: 510 .NET tests, 509 passed, 1 skipped (it runs only locally with a client's real files). Counts for the database, migration and ui-kit tests are no longer stated.
- **Not re-run by hand.** The run guide's commands were run from a fresh clone by the session that wrote it, against the earlier `main`. They were not re-run here after the update; the CI job `compose` runs the same build and smoke test on this pull request.
- **Stated in the README's next steps:** pull request 8 was merged on the owner's instruction without a reviewer's approval on record, so the reviewer's pass over that work is still to come.
- The status column of phases B to D in `docs/plan.md` was not rewritten; the done log is current.

### Pull request 10

**Promissory notes: receipt form with printed receipt, steps after receipt, and authorised signatories**

Open since 16 Nov 2026, CI green · branch `claude/elegant-ramanujan-58j452` · https://github.com/BehinTaghavi/treasury/pull/10

**Size.** This is larger than our usual pull request: the owner asked for the three parts one after another in one session. The commits are separable and can be reviewed in order: note receipt → his answers → authorised signatories → note steps → his answers (the last commit only links this pull request from the home pages).

**Cashflow domain**
- `ReceiptKind.Notes` with `NoteReceiptTerms` (reason, form, maturity, custody, original received, attachments, signers). `Receipt.RecordNotes` checks the notes against the terms, the article 308 checks and the signature rule.
- `ReceivedInstrumentDetails` gained `WrittenOn`, `OnDemand`, `Checks`; `NoteGuarantor` gained `DetailCode` and `Phone`. All are optional, so stored JSON from before still reads. `MaxGuarantors` is now 3 (owner's answer; the manual said two).
- A note on demand has no due date (`Instrument.OnDemandHasDueDate`); a dated note is not due before the date written on it.
- **Lifecycle:** no state or step was added, so the migration tool's copy is unchanged and its comparison test passes. Only the kinds changed: `IsNegotiable` lets an ordinary note be deposited and endorsed (old rule R3 allowed ordinary and bank cheques only). `Endorse` asks for the Sayad tracking number of cheques only.
- **Vouchers:** two new fixed accounts, `GuaranteeNotesForCompany` and `NotesInCollection`. Handing a guarantee note back reverses the off-balance entry. Handing an ordinary note back debits the معین of its receipt's purpose group (looked up in `VoucherUseCases.ComputeStepAsync`). A cheque returned to its payer still has no voucher, as before.

**Reference service**
- New `Signatory` and `SignatureRule` aggregates, `SignatureUseCases`, in-memory and PostgreSQL repositories, endpoints `/api/reference/signatories` and `/api/reference/signature-rules`.
- Cashflow reads the rule over HTTP (`IReceiptReferenceLookup.SignatureRuleAsync`) and checks the choice in its own domain (`SignatureRuleFact.Choose`), keeping the signers' names and positions on the receipt.

**Database:** scripts 033 (kind `Notes`, `note_terms`, fixed account), 034 (`reference.signatory`, `reference.signature_rule`), 035 (fixed account). 033 replaces the kind checks by looking them up by definition, so it is safe to run again; I ran `apply.sh` twice.

**Frontend**
- `NoteReceiptForm.vue`, `NoteReceiptSheet.vue` / `NoteReceiptPrint.vue`, `notes.ts`; `Signatories.vue` in basic-info.
- `ChequeOperations.vue` takes a `notes` prop and swaps the wording through one helper rather than a second copy of the screen. Worth a look: the swap is by text replacement.
- Printing teleports a copy of the sheet under `<body>` and hides everything else with a non-scoped `@media print` rule.

**Tests:** CI is green on all four jobs (`dotnet`, `database-and-migration`, `frontend`, `compose`). Locally: 575 .NET tests (574 passed, 1 skipped as on `main`), 39 schema tests, 23 migration tests, ui-kit tests, offline build. New: `NoteReceiptTests`, `NoteStepTests`, `SignatureTests`, `NoteReceiptUseCaseTests`, `SignatureUseCaseTests`, `SignaturePostgresTests`, and additions to the voucher, receipt and schema tests.

**Checked by hand** in a browser against the in-memory services: saving and printing a note receipt (one A4 page), the signatories page and rule, deposit of two notes on one slip, collection refused before the due date, collection of a note on demand, handing a guarantee note back.

**Not done or not run**
- Docker Compose and `deploy/smoke-test.sh` were not run by hand; only the CI job `compose` ran them. The new screens were not looked at in the Docker build.
- Local PostgreSQL was 16; CI ran the schema and adapter tests on 17.
- Endorsing a note was covered by use-case tests only, not in the browser (it needs requests-svc and an approved request).
- The voucher for handing an ordinary note back was added after the browser check; it is covered by domain and use-case tests only.
- Attachments are ticks only, no file upload (Q43).
- The printed receipt leaves the company's name as a dotted line: company particulars are not kept in Basic Information yet.
- The cheque receipt API still accepts notes as before; only the screens separate them.
- Signature rules of the other five documents are stored but not used yet.

### Pull request 11

**«ثبت صیادی شد»: a cheque goes to the bank only after it is ticked as recorded in Sayad**

Open since 17 Nov 2026, CI green · branch `claude/sleepy-rubin-6znv1v` · https://github.com/BehinTaghavi/treasury/pull/11

**Design**
- The tick is a property of `Instrument` (`SayadRegistration`: moment and user), not a state and not a history step. `InstrumentLifecycle.cs` is unchanged, so the migration tool's copy still matches.
- `DepositForCollection` (both overloads) and `Deliver` refuse with `Instrument.SayadNotRegistered` when a cheque kind has no tick. `Instrument.AllowedActions()` leaves the guarded action out until then, so the screen needs no rule of its own.
- `ConfirmSayadRegistration` / `WithdrawSayadRegistration` are allowed only while the guarded step is still ahead (`CanChangeSayadRegistration`). Each raises `InstrumentSayadRegistrationChanged` to the outbox, which is the trail of who ticked and when.
- New command `SetSayadRegistrationAsync` (idempotency key, one aggregate saved once, clock from `TimeProvider`) and `POST /api/cashflow/instruments/{id}/sayad-registration`. `InstrumentUseCases` now takes a `TimeProvider`.
- Schema `033_cashflow_sayad_registration.sql`: `sayad_registered_at timestamptz`, `sayad_registered_by text`, and a check that both are set or both are null.
- Answer to Q39: script 033 ticks existing received cheques **only when it creates the columns**, so running `apply.sh` again ticks nothing. The migration tool does the same for migrated received cheques. Both write `system:assumed` in place of a user (`SayadRegistration.AssumedBy`); a migration test fails if the marker or the cheque kinds differ between the domain, the script and the tool.
- Frontend: `formatJalaliDateTime` in ui-kit (Jalali date and time in `Asia/Tehran`), four new error codes in `i18n.ts` (Persian and English), and the column, hint and card line in `ChequeOperations.vue`.
- `deploy/smoke-test.sh` deposited a cheque without a tick and would have failed; it now checks the refusal (422), ticks, then deposits.

**Tests**
- Domain: `SayadRegistrationTests` (current and post-dated, removal, back from the bank, notes, issued cheques, assumed tick, endorsement without tick). Existing tests that deposit or deliver now tick first.
- Use cases and PostgreSQL adapter: refusal, tick with moment and user, removal, outbox events, assumed tick read back.
- Schema: the columns and their check; the one-time tick of existing cheques and that a second run ticks nothing.
- Migration: migrated received cheques are ticked, issued cheques and notes are not.
- Local run: 526 .NET tests passed and 1 skipped (it needs the client's real files), 39 schema tests, 24 migration tests, 13 ui-kit tests; `pnpm build` passes. The screen was driven in a browser with synthetic data (receive, tick, remove, deposit, card).

**Not verified here**
- The local database was PostgreSQL 16, not 17.
- Docker is not available in this workspace, so the whole system in Compose and the changed `smoke-test.sh` were not run end to end; its three new calls were tried against the in-memory API. The `compose` job on CI is the real check.
- The text «از قبل ثبتشده فرض شد» was not seen in a browser.

**Left for later**
- The tick box on the payment form, before «تحویل چک به ذینفع» (Phase E).
- Who may tick (permissions) comes with the Keycloak login.

### Pull request 12

**«اطلاعات شرکت»: the company's name and logo at the top of every screen, with its official details**

Open since 17 Nov 2026, CI green · branch `claude/wonderful-mccarthy-tr7zvt` · https://github.com/BehinTaghavi/treasury/pull/12

**Design**
- `reference-svc`: new aggregate `CompanyProfile` (`Domain/Company`) with a `CompanyDetails` record; one profile per client database. Use cases `CompanyUseCases` (save form, attach logo, remove logo, read), in-memory and PostgreSQL adapters, endpoints under `/api/reference/company` (`GET`, `PUT`, `POST /logo`, `POST /logo/remove`, `GET /logo`).
- Version 0 means "not entered yet"; the first save registers the profile, later saves check the version. A concurrent first entry is stopped by the table (`company_single`) and reported as `Concurrency.Conflict`.
- Schema `033_reference_company.sql` (one-row table, logo in its own table so reading the name never loads the picture) and `034_reference_company_details.sql` (`ADD COLUMN IF NOT EXISTS`, each with a shape check). `reference_svc` may not delete the profile.
- The national-ID check reuses `Beneficiary.IsLegalNationalId`.
- Frontend: `CompanyInfo.vue` in the basic-info remote; the shell's `useCompany` composable and top bar. The remote tells the shell about a change with the window event `treasury:company-changed` (constant in `@treasury/ui-kit`, `company.ts`), so neither imports the other. The logo is fetched and shown from a blob URL, not `<img src>`, so the request can carry the token once Keycloak is wired in.
- 13 new error codes `Company.*`, with Persian and English text.

**Tests**
- Domain (`CompanyProfileTests`), use cases (`CompanyUseCaseTests`), PostgreSQL adapter (`CompanyPostgresTests`), schema (`test_one_company_profile_with_a_small_logo`), ui-kit (`logoFileProblem`), and one line in `deploy/smoke-test.sh`.
- Run locally: 524 .NET tests passed and 1 skipped (the existing real-exports test); 38 schema tests; 14 ui-kit tests; offline build and the two offline Reference test projects.
- Browser check against the in-memory API: save name and logo, see them on other pages and after a reload, PDF refused, wrong national ID and website refused with Persian messages, logo removed.

**Not done / to watch**
- Local PostgreSQL was version 16, not 17; CI runs the adapter and schema tests on 17.
- The full Docker Compose run was not done locally; the `compose` job covers it.
- The top bar's requests carry no token yet, like the rest of the shell (Keycloak login is later in Phase B).
- **Numbering overlaps with the two other open pull requests.** #10 adds schema scripts 033–035 and questions Q37–Q52; #11 adds script 033 and questions Q37–Q39; this one adds scripts 033–034 and questions Q37–Q38. Scripts with the same number and different names still apply in order, but the question numbers now mean three different things. Whichever merges later should renumber its questions (and preferably its scripts) and will have text conflicts in `README.md`, `README.en.md`, `docs/decisions.md` and `docs/domain/open-questions.md`. This pull request merges cleanly into `main` today.

### Pull request 13

**Show the running version at the top of the screen and in the «Start Treasury» window**

Open since 17 Nov 2026, CI green · branch `claude/nifty-newton-d1cong` · https://github.com/BehinTaghavi/treasury/pull/13

- `frontend/shell/public/version.txt` contains `$Format:%h %cs$` and is marked `export-subst` in `.gitattributes`. `git archive` and GitHub's ZIP download both fill in the commit and date.
- The shell's layout (`frontend/shell/app/layouts/default.vue`) fetches `/version.txt` with `cache: 'no-store'`. It hides the stamp when the placeholder was not replaced, e.g. in a dev run or a plain `git clone`. A style is added in `shell.css`.
- `deploy/windows/treasury.ps1` prints the stamp and the folder before building.
- **Gateway (`deploy/gateway.conf`, 18 Nov):**
  - The problem: nginx resolved the upstream host names once at start. `docker compose up -d` after a build (what the Windows script does) recreates the changed containers with new IP addresses but leaves the gateway running, so the gateway kept sending traffic to the old addresses.
  - Seen in Docker on 18 Nov: `/` and `/version.txt` returned the receipts remote's `index.html`.
  - The fix: the names now sit in variables and are resolved at request time through Docker's DNS (`resolver 127.0.0.11 valid=10s ipv6=off`). The `/remotes/<x>/` prefix is stripped with a `rewrite … break`, because `proxy_pass` with a variable passes the URI unchanged.
  - Checked: `nginx -t` passes. Every route returns 200 (APIs, the three `remoteEntry.js` files, `/version.txt`). Recreating `shell`, `remote-receipts` and `remote-basic-info` without touching the gateway still served the right pages.
- Docs updated as CLAUDE.md asks:
  - `README.md` / `README.en.md`: a waiting-for-review note under "Latest changes" and the "Deployment" row of "Current status";
  - `docs/test-server-windows.md` and `docs/reviewer-run-guide.fa.md`: where to see the version;
  - `docs/plan.md`.
- The branch's earlier pull request (#8) was merged; `main` was merged into the branch (`5df88c1`) rather than rebased, so history is not rewritten.
- **Verified:**
  - Built the affected images from a `git archive` ZIP and started the stack in Docker; the browser showed the stamp in the top bar.
  - `dotnet build Treasury.slnx` and `pnpm build` both succeed.
  - No test was added: the version stamp is display only and the gateway change is configuration, with no business rule. `deploy/smoke-test.sh` covers the routes.
  - The Windows script is not run here; PowerShell is not available in this workspace.

### Pull request 14

**Legal action on bounced cheques, rule N3 («ثبت صیادی» before the bank), certificate of non-payment, receipt lists, company particulars**

Open since 18 Nov 2026, CI: **`compose` failed** · branch `claude/funny-wozniak-u12rbh` · https://github.com/BehinTaghavi/treasury/pull/14

- **Work of another Claude session (branch `claude/funny-wozniak-u12rbh`).** This pull request was opened at the owner's request; the commit messages describe each change in detail. The branch is based on today's `main`.
- **Lifecycle:** new states and steps `SayadRegistered`, `InLegal`, `ReturnFromLegal`; the deposit starts from `SayadRegistered` (rule N3). Old history is migrated as recorded: `LEGACY_TRANSITIONS` in `tools/migration`.
- **New template accounts:** `ProtestedChequesInLegal` and `GuaranteeChequesInLegal`.
- **Schema:** 033–037 (cheque status, legal-action scans, legal-action voucher, deposit kinds, company profile).
- **Overlaps with other open pull requests, for the reviewer to decide:**
  - **#11 («ثبت صیادی شد»)** builds the same Sayad rule as a tick on the cheque (owner, 15 Nov). This pull request makes it a step and status (rule N3, owner, 18 Nov). Only one should be merged; this one follows the owner's later rule.
  - **#12 («اطلاعات شرکت»)** and the «مشخصات شرکت» page here both keep the company's name and address in `reference-svc`. They need to become one page before both are merged.
  - **#10 (promissory notes)** changes the same lifecycle, receipt and voucher code; a local merge gave 26 conflicted files, several of them real design conflicts. For example, notes are deposited from `InCashDesk` there, but only from `SayadRegistered` here, and both added an optional `ForStep` parameter for different purposes. #10 has to be adapted to this pull request, or the other way round.
- **Tests (local, at `8ab8085` merged with #13 (`0c1705e`), PostgreSQL 17):**
  - .NET 552 total, 551 passed, 1 skipped
  - schema tests 42 passed
  - migration tests 24 passed
  - ui-kit 16 passed
  - `pnpm build` passed
- **Docker:** the stack was built from a `git archive` ZIP and started over a database of the previous test package; scripts 036 and 037 applied without errors. The new tabs and «مشخصات شرکت» open with no page errors.
