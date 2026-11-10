# Plan and progress

Update this file when a task finishes (what is done, what is next). Months count from the start of Phase A. The pace is set by review and business answers, not by coding.

## Phases

| Phase | When | Scope | Done when | Status |
|---|---|---|---|---|
| 0 · First package | done 9 Nov 2026 | Repository skeleton, Cashflow domain (instruments, cheque books), PostgreSQL schema with fiscal-year partitions, migration tool, Nuxt shell + receipts screen | 49 + 12 + 6 .NET tests, 10 DB tests, 21 migration tests, browser smoke test pass | Done (see design document 13.8) |
| A · Finish Phase 0 and set up | weeks 1–3 | First real build with NuGet; PostgreSQL adapters and Keycloak wiring compiled and tested; `docker compose` runs; GitHub CI green; test server; migration dry run on a copy of `Cashnajoo86` | Everything builds and passes on GitHub; test server runs; dry run reconciles | **Next** |
| B · Basic information and login | months 1–2 | **Basic information first** (owner, 10 Nov 2026): currencies, country/province/city, bank names, the company's bank accounts (manual section 5), cheque books, references (عطف), purpose groups and purposes, cash desks; then the receipts form uses them; then Keycloak login (`reference-svc`, `accounts-svc`) | Basic data migrated and reconciled | **Started** (menu order, rule N2) |
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
| 2026-11-10 | Owner's review of the receipts screen: main menu reordered, rule N2 (no clearing before the due date); basic information planned first |
| 2026-11-10 | Test server on the owner's Windows PC (double-click scripts, guide); fixed: the system could not restart after a stop |
