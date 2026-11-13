# Plan and progress

Update this file when a task finishes (what is done, what is next). Months count from the start of Phase A. The pace is set by review and business answers, not by coding.

## Phases

| Phase | When | Scope | Done when | Status |
|---|---|---|---|---|
| 0 · First package | done 9 Nov 2026 | Repository skeleton, Cashflow domain (instruments, cheque books), PostgreSQL schema with fiscal-year partitions, migration tool, Nuxt shell + receipts screen | 49 + 12 + 6 .NET tests, 10 DB tests, 21 migration tests, browser smoke test pass | Done (see design document 13.8) |
| A · Finish Phase 0 and set up | weeks 1–3 | First real build with NuGet; PostgreSQL adapters and Keycloak wiring compiled and tested; `docker compose` runs; GitHub CI green; test server; migration dry run on a copy of `Cashnajoo86` | Everything builds and passes on GitHub; test server runs; dry run reconciles | **Next** |
| B · Basic information and login | months 1–2 | **Basic information first** (owner, 10 Nov 2026): currencies, country/province/city, bank names, the company's bank accounts (manual section 5), cheque books, references (عطف), purpose groups and purposes, cash desks; then the receipts form uses them; then Keycloak login (`reference-svc`, `accounts-svc`) | Basic data migrated and reconciled | **Started**: menu order, rule N2; step 1 `reference-svc` lists (currencies, country/province/city, bank names) and the Basic Information screen; step 2 chart of accounts and تفصیل copied from the accounting system (Excel import, allowed تفصیل types); step 3 the company's bank accounts (`accounts-svc`); owner's report no. 1 fixed; step 4 Iran's cities, cheque books, references, purpose groups and purposes, cash desks; step 5 cheque types with scans, petty cash holders, departments, requesters, beneficiaries — **all 14 items of the manual's Basic Information are built, waiting for review**. Next: the receipts form uses them (bank list, branch, purpose group and purpose, references); then Keycloak login |
| C · Payment requests | months 2–4 | `requests-svc`: request, approval workflow, signatures, task inbox (rules R7, R9) | **Gate: early pilot at Iran Nazho Co.** | Planned |
| D · Receipts and the cheque lifecycle | months 4–6 | Receive, deposit for collection, clear, bounce, return, endorse; answers to Q1–Q10 | All transitions tested; parallel run | Planned |
| E · Payments and cheque printing | months 6–8 | Issue and print cheques (bank layouts, amount in words), deliver, cancel, bank transfers, cash payments | Parallel month-end close works | Planned |
| F · Accounting vouchers | months 7–9 | `posting-svc`: posting rules, voucher batches, sending to the general ledger, error handling | **Gate: one parallel month-end, vouchers match the old system to the Rial** | Planned |
| G · Reports and full pilot | months 9–12 | `reporting-svc`: the reports in both manuals, dashboard, cash-flow forecast | **Gate: Iran Nazho Co. signs off after two parallel month-ends**, then wider rollout | Planned |

## Next tasks (Phase A)
1. ~~Install the .NET 10 SDK in the cloud environment (setup script), build `Treasury.slnx` with real packages, run all tests with real xUnit; fix what fails. Give the owner the setup script to save in the environment.~~ Done 10 Nov 2026.
2. ~~Integration tests for the PostgreSQL adapters against a real PostgreSQL 17 (Docker).~~ Done 10 Nov 2026 (14 tests; found and fixed: reading an instrument outside a transaction).
3. ~~Run `deploy/docker-compose.yml` end to end; fix what fails.~~ Done 10 Nov 2026 (`deploy/smoke-test.sh`; fixed: ports lost behind the gateway, host files copied into images).
4. ~~Make `.github/workflows/ci.yml` the required status check: .NET build + tests, DB tests, migration tests, frontend tests.~~ CI green 10 Nov 2026, with a new `compose` job. **Remaining (owner, in GitHub settings):** mark the checks `dotnet`, `database-and-migration`, `frontend`, `compose` as required for `main`.
5. Migration preflight and dry run on an anonymised copy of `Cashnajoo86` (the owner provides it outside GitHub); share the reconciliation report.
6. ~~Test server.~~ Done 10 Nov 2026: the owner's Windows PC with double-click start/stop (`docs/test-server-windows.md`). Still to do: deployment notes for the pilot server (office or ArvanCloud) before Phase C.

## Done log
| Date | What |
|---|---|
| 2026-11-09 | Repository loaded: Phase 0 code, design document (EN/FA), decisions log, plan, masked legacy source |
| 2026-11-09 | Phase 0 first package delivered |
| 2026-11-10 | Phase A tasks 1–4: first build with NuGet (0 warnings), 81 .NET tests incl. 14 PostgreSQL adapter tests, Docker Compose runs end to end, CI green with a `compose` job; environment setup script |
| 2026-11-10 | Basic information step 3: the company's bank accounts (`accounts-svc`, manual section 5) with the bank's معین and allowed تفصیل, collection/bounced accounts, backup accounts; checked in the browser with Iran Nazho Co.'s chart of accounts (not in the repository). Next: references (عطف), purpose groups and purposes, cash desks |
| 2026-11-10 | Basic information step 2: code lengths, chart of accounts and تفصیل list imported from the accounting system's Excel exports (checked with Iran Nazho Co.'s files: 634 accounts, 9,628 تفصیل, no rejected row), allowed تفصیل types per account; voucher rules written down. Next: bank accounts |
| 2026-11-10 | Basic information step 1: `reference-svc` (currencies, country/province/city, bank names with default lists), Basic Information micro-frontend; next: bank accounts (waits for the owner's ledger-account Excel) |
| 2026-11-10 | Owner's review of the receipts screen: main menu reordered, rule N2 (no clearing before the due date); basic information planned first |
| 2026-11-10 | Test server on the owner's Windows PC (double-click scripts, guide); fixed: the system could not restart after a stop |
| 2026-11-11 | Test server on a slow internet connection: the first build broke off after 21 minutes (528 of 530 frontend packages). Downloads are now kept between tries, the three screen images download once, and Start Treasury tries the build 3 times |
| 2026-11-12 | Cheque book numbers as the owner described them: 4-digit series, 6–8-digit serial from first serial and number of sheets (at most 100), 16-digit Sayad ID (8 fixed middle digits and a 4-digit serial per book; first 4 digits at issue) |
| 2026-11-12 | Cheque book sizes: a new book has 25, 50 or 100 sheets; a book partly used before go-live is entered with its unused sheets only (any number up to 100) and marked "partly used" in the list |
| 2026-11-12 | Cheque serials may start with 0: each book keeps the number of digits typed, so 056780 stays 056780 on the screen and on issued cheques |
| 2026-11-13 | Owner's report no. 2 on Basic Information: Iran's provinces and cities found even beside a second «ایران»; تفصیل list page by page; typed codes kept when leaving a list box; backup account chosen first; petty cash limits, creation date, how money is kept, top-up by card; cash desk creation date; beneficiaries' national code/ID and postal details; purposes deletable after use |
| 2026-11-13 | تفصیل roles from their type (staff, cost centres, organisational units): holder, requester and cost-centre lists; requesters from staff تفصیل, department optional; backup account in the currency of its main account |
| 2026-11-14 | City screen: each country shows its number of provinces, a country without provinces explains what to do, and the province box asks «— استان را انتخاب کنید —» instead of «همه (…)» |
| 2026-11-14 | Owner's report no. 3: delete unused bank accounts; «جدید» always visible; standard cheque type for all accounts; all list boxes searchable |
| 2026-11-14 | Print type «استاندارد»; cheque types before cheque books in the menu. To plan: opening balances of bank accounts at go-live (owner: they count as a transaction of the account) |
| 2026-11-12 | Basic information step 5 (owner: finish the whole section before review): cheque types per bank account with the scan of the cheque (`cashflow-svc`, linked to cheque books), petty cash holders with amount, settlement limit, ceiling, allowed payment purpose groups and top-up method (`accounts-svc`), departments with their cost centres, requesters of funds and beneficiaries with Sheba check (`reference-svc`). Cheque books: no shared sheet number within a bank account, whatever the series |
| 2026-11-12 | Basic information step 4: Iran's 1,354 cities by province (Statistical Centre list, MIT-licensed dataset), added on start also to existing databases; references (عطف) with the manual's four defaults; purpose groups (receipt/payment, number, account, references 1–2) and purposes; cash desks with the holder's تفصیل (`accounts-svc`); cheque books screen (`cashflow-svc`: bank account checked with `accounts-svc`, overlapping sheets refused, correct/delete while unused, switch on/off). Checked end to end in Docker on an existing database |
| 2026-11-11 | Owner's report no. 1 (19 items on Basic Information): lists can be searched without hiding new entries, corrected (province of a city, country of a province, unused currency code) and cleaned up (delete while unused, `in_use` recorded by reference-svc); bank names compared without «بانک»; searchable drop-downs; bank accounts with required fields, priority 1–5, backup rule, search; darker main menu. Receipts-form changes still follow the remaining Basic Information sections |
