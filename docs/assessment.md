# Treasury (AR/AP) Modernization — Assessment & Target Architecture

Nov 8, 2026 · @Behin

Persian version / نسخه فارسی: [assessment.fa.md](assessment.fa.md)

## 1. Executive summary

**Verdict:** the current MVC version should not be repaired in place. Its problems are structural, not cosmetic: business logic is spread across \~28,000 lines of controllers and \~98 stored procedures, there are no database transactions around multi-step financial operations, cheque numbers are allocated with a race condition, credentials are stored in plain text, and card CVV2 codes are stored in the database. Fixing these one by one inside the same design would cost almost as much as rebuilding — and leave you with the same design.

**Recommendation:** build the new Treasury as a set of DDD-based microservices with a micro-frontend web client, on an open-source stack (.NET 8/10 + PostgreSQL 17 (SQL Server 2025 as an option) + RabbitMQ + Keycloak, in containers on the client's own server or on ArvanCloud, with Nuxt 4 micro-frontends). Migrate with the *Strangler Fig* pattern, one bounded context at a time, so your clients keep working while the new system replaces the old one piece by piece.

**The most valuable asset you have is not the code — it is the domain knowledge:** the `PlaningType` / `PlaningTypeCirculation` tables (the cheque/document lifecycle and its accounting rules), the voucher-generation procedures, and 20 years of user workflows documented in the old Access manual. The new system must preserve that knowledge exactly, and make it explicit and tested.

**Realistic size:** a core team of 6–8 people (architect, 3–4 backend, 2 frontend, 1 QA/DevOps, plus a domain expert from your side) for roughly 12–15 months to reach full functional parity, with the first production module (Basic Info + Request for Payment) at month 5–6.

## 2. What was analyzed

| Material | Status | What it showed |
| --- | --- | --- |
| New MVC source (5-part RAR, \~525 MB) | Extracted and reviewed | ASP.NET MVC 5 / .NET 4.5, EF6, 11 projects, 116 C# files + 330 Razor views in the UI, 61 WCF service references, Stimulsoft reports |
| Client database backup (`Treasury_NewDBYaran`, SQL Server .bak) | Schema read from EF model + SQL text extracted from backup | 95 tables/views, 98 stored procedures/functions, 95 foreign keys |
| New system user manual (.docx) | Read | Basic info, request for payment, payments, receipts, cheque/promissory-note lifecycle |
| Old system user manual (.doc, 54 pages) | Read | Full functional scope of the Access version (sections 1–11) |
| `14050806_Treasury_New.rar` | **Password-protected — not opened** | Contains `TreasuryFileYaran` (likely the attachments DB) and `Service_Simlaki.rar` |
| Old Access source program | **Not found among the uploads** | — |

### 2.1 Second batch (received 9 Nov 2026)

| Material | Status | What it contained |
| --- | --- | --- |
| `14050806_Treasury_New.rar` (password received) | Opened | `TreasuryFileYaran` and `TreasuryFileSimLaki` (SQL Server backups of two clients' attachment databases), `Service_Simlaki.rar` (compiled WCF service and configs of one client), `TreasuryPrintService.rar` (source code of the cheque print service) |
| `Treasury_Old_source_part_1.rar` | Partly extracted (it is the first volume of a split archive) | Three SQL Server backups of the **old** system: `Cashnajoo05` (Treasury DB, fiscal year 1405), `AccIranNajo05` (accounting DB, 1405) and `Cashnajoo86` (Treasury DB, 1386). The last one is cut off because it continues in part 2 |
| `Treasury_Old_source_part2.rar` | **Wrong file** | Byte-for-byte identical to `14050806_Treasury_New.rar`; it is not the second volume |

**Third batch (9 Nov 2026):** `14050813.part02_7.rar` is the correct second volume. Together with part 1 it extracted completely: `Lcash.mdb` (the whole old Access program, 127 MB) and the full `Cashnajoo86` backup. The old program is analyzed in **section 12**. Nothing further is needed for the analysis.

## 3. Findings — why the current MVC version doesn't work properly

### 3.1 Data integrity (the most likely cause of the problems your users see)

- **No transactions around financial operations.** For example, `ReceiptController.Save` calls `SaveChanges()` several times (header, then details, then plan states). If a later step fails, earlier rows stay in the database and the code tries to undo them by hand (`DelTavizReceipt_Detail_PlanStates`). Any failure between steps leaves half-saved receipts.
- **Cheque number race condition.** `ChequePaymentController` reads `LastUsedPaperNo + 1` to show the next cheque number, and increments it later without a lock. Two users issuing cheques from the same cheque book at the same time can get the same number.
- **Dates stored as text** (`char(8)` Shamsi strings such as `14050806`) in almost every table, with date logic re-implemented in SQL functions (`fn_ShamsiDateStrDiffDay`, `MiladiTOShamsi`). This makes range queries, validation and partitioning fragile.
- **Wrong audit data.** `HostName` is filled with `Dns.GetHostName()` — the *server's* name, not the user's machine (72 places). The audit trail does not show who did what from where.
- **Hard-coded state IDs.** 57 places in the UI use literal values like `PlaningTypeID = 21`. A change to the `PlaningType` table silently breaks the workflows.

### 3.2 Security

- Connection strings use the **`sa` account with plain-text passwords** in `Web.config` (also committed in several backup copies of the config).
- Passwords are hashed with **unsalted SHA-1**; the helper named `EnryptString` is only Base64 encoding.
- The `BankCards` table stores **CVV2 and expiry date** — this must never be stored.
- **SQL built by string concatenation** (`Utility.GetPostVoucherType`, `CanDeleteReceiptDetail`, `SelectBy_DynamicWhereClause("BankID =" + BankID ...)`), and dynamic SQL inside stored procedures (`exec(@str)`).
- **CSRF:** 132 `[HttpPost]` actions but only 17 `[ValidateAntiForgeryToken]`.
- `customErrors="Off"` and `debug="true"`; raw database exception text is returned to users.
- WCF services are hosted at `Design_Time_Addresses` (a development-only address) on fixed IPs with no transport security.

### 3.3 Architecture

- **Two data-access paths for the same data.** The UI opens the database directly with `new TreasuryEntities()` 283 times, while a separate layer of 61 WCF service references (one per table, \~475,000 lines of generated proxy code) is used only twice. There is no single place where a rule is enforced.
- **Business logic in three places at once:** controllers (the largest is 3,400 lines), the `Utility` class (2,000 lines), and 98 stored procedures — including voucher creation, which writes into a *different accounting database* per fiscal year chosen at runtime (`GetAccDBName` → dynamic SQL).
- A WCF endpoint is mis-wired: `Voucher_Row_Reciept` points to the `cls_City` address.
- No automated tests of any kind.

### 3.4 Technology age

.NET Framework 4.5, EF 6.1, jQuery 1.10 *and* 2.0 *and* AngularJS 1.x mixed in the same views, Bootstrap 3, Newtonsoft 6. Most of these are out of support and have known vulnerabilities. Windows/IIS-only hosting, and Stimulsoft (commercial) for reporting.

### 3.5 What the old system's databases show

- **The old system is an Access front-end over SQL Server.** The tables still have the `upsize_ts` columns that Access's Upsizing Wizard adds, and the database holds no business logic of its own: all rules are in the Access file.
- **One Treasury database and one accounting database per fiscal year.** Names found in the backups: `Cashnajoo86`, `Cashnajoo87`, `Cashnajoo05`, `AccIranNajo94`, `AccIranNajo05`. For this client that means roughly 20 yearly Treasury databases from 1386 to 1405, plus the matching accounting databases. This confirms the design in section 7: merging them into one partitioned database is a core migration task (see 7.6).
- **Old core tables** (read from the constraint names in the backup): `Checks` (every cheque and note in one table, with status flags such as `Backed` (bounced), `Signed`, `Vos`/`RegVos` (collected / sent for collection), `Ctocash`, `Hazfhavale`, `GoInlaw` (legal action), `Backtohi`, `EbtorKharj`, `Payit`/`Paied`), `Bundle` (cheque books: first sheet, last sheet, last used), `Bankacc`, `Bank`, `Babs` (بابت), `Balance`, `Mainrep`/`Dailyrep`, `Voucher`/`Voucher_row`/`Povouch`, `Globals` (system settings), `Emps`, `Rights`/`Rights_m` (menu permissions).
- **What this means:** in the old system a cheque's status is a *combination of flags*; in the MVC system it is a *row in `Receipt_Detail_PlanState`*. Migration must map both onto the single new state machine (section 4.2). The mapping table will be drafted by us and confirmed by your domain expert.

### 3.6 Cheque print service — critical security finding

`TreasuryPrintService` checks a password before printing a cheque, but **the check is reversed**: it prints only when the password does *not* match the configured hash. A comment in the code says the condition was flipped because the password was not available. In practice, anyone who can reach the service on the network can print a cheque by sending any wrong password. In addition, the password hash is MD5, the WCF binding has no encryption, and the service shows a `MessageBox` on the server.

- **Now, in production:** wherever this service is installed, allow its port only from the treasury users' machines (firewall), and fix the condition with a real password.
- **In the new system:** `cashflow-svc` produces the cheque as a PDF (QuestPDF) only after the payment is authorized; the user prints it from the browser; each print is logged and tied to the reserved cheque number; a reprint needs a separate permission.

## 4. Domain model (Domain-Driven Design)

The current system models the database (one WCF service per table). DDD turns this around: we model the **business capabilities** described in the two manuals, and each gets its own language, rules and data.

### 4.1 Bounded contexts

| # | Bounded context | Covers (from the manuals and schema) | Main aggregates |
| --- | --- | --- | --- |
| 1 | **Organization & Reference Data** | Countries, provinces, cities, company branches, organizational units/locations, currencies, exchange rates and standard rates, reference types (عطف‌ها) | `Branch`, `OrgUnit`, `Currency`, `ExchangeRateTable` |
| 2 | **Fiscal Periods** | Fiscal years, open/closed status, posting date boundaries, year-end carry-forward | `FiscalYear` |
| 3 | **Treasury Accounts** | Bank accounts, backup accounts, cash desks, cashiers, cheque books, cheque print layouts, authorized signatories, opening balances | `BankAccount`, `CashDesk`, `ChequeBook`, `ChequeLayout`, `SignatoryRule` |
| 4 | **Counterparties** | Beneficiaries/payees, payers, requesters, their bank accounts/IBAN (Sheba), phones, national ID / economic code | `Counterparty` |
| 5 | **Payment Requests** (درخواست وجه) | Requests from departments, priorities, aggregation/splitting, approval workflow, signatures, status history | `PaymentRequest` |
| 6 | **Disbursements** (پرداخت) | Cheque issue, cash payment, bank transfer, card-to-card, promissory notes, direct debit, inter-bank transfers, instalments and commitments | `Payment`, `InterBankTransfer` |
| 7 | **Receipts** (دریافت) | Cash, cash deposit, POS/ATM, cheques, promissory notes, guarantee cheques/notes, external receipt notes | `Receipt` |
| 8 | **Negotiable Instruments** | The full life of every received or issued cheque/note: deposit for collection, cleared, bounced, replaced (تعویض), endorsed/spent (خرج), returned (عودت), cancelled (ابطال), due-date change, partial decrease | `Instrument` (state machine) |
| 9 | **Petty Cash** (تنخواه گردان) | Funds, holders, allowed purpose groups, spending entries, settlement, recharge, rejected items (واخواهی) | `PettyCashFund`, `PettyCashSettlement` |
| 10 | **Accounting Integration** | Posting rules (debit/credit accounts per operation), voucher generation, posting to the ERP general ledger, error reports, re-send | `PostingRule`, `VoucherBatch` |
| 11 | **Liquidity & Reporting** | Bank/cash balances, liquidity forecast, maturity reports, account turnover, daily summary, sensitive receivables/payables | Read models only (CQRS) |
| 12 | **Documents** | Scans, attachments, signature images | `Document` |

Identity & access is not a business context: it is handled by Keycloak (section 8).

### 4.2 The heart of the system: the instrument state machine

Today this logic lives in the `PlaningType` table (operations with debit/credit accounts) and `PlaningTypeCirculation` (which operation may follow which), and is enforced partly in SQL, partly in controllers with hard-coded IDs. In the new system it becomes an explicit, tested domain model:

- `Instrument` aggregate with states such as *InCashDesk → DepositedForCollection → Cleared / Bounced → Replaced / Returned / Endorsed / Cancelled*.
- Every allowed transition is declared once (data-driven from the migrated `PlaningTypeCirculation`, but validated by the domain), and each transition raises a domain event, e.g. `ChequeDepositedForCollection`, `ChequeBounced`.
- The accounting effect of each transition is **not** coded in the instrument; the Accounting Integration context subscribes to the events and applies the posting rule. This separates *what happened* from *how it is booked*.
- Each transition is covered by automated tests generated from the current `PlaningType` data, so we can prove the new engine books exactly what the old one did.

### 4.3 Main business flow across contexts

`PaymentRequest` submitted → approved (signatory rules, amount limits) → `Payment` executed (cheque number reserved from `ChequeBook`) → `Instrument` issued → events → `VoucherBatch` posted to the GL → liquidity read models updated. This long-running flow is coordinated by a **saga** with compensating steps, instead of today's manual clean-up code.

## 5. Target backend architecture

### 5.1 Services

Twelve contexts do not mean twelve services on day one. Too many small services is the most common way microservice projects fail. Start with **seven deployable services**, and split later only if load or team structure requires it:

| Service | Contexts inside | Notes |
| --- | --- | --- |
| `reference-svc` | Organization & Reference Data, Fiscal Periods | Mostly read; heavily cached |
| `accounts-svc` | Treasury Accounts, Counterparties | Owns cheque books and number reservation |
| `requests-svc` | Payment Requests | Approval workflow, signatures |
| `cashflow-svc` | Disbursements, Receipts, Negotiable Instruments, Petty Cash | The transactional core; one service so that a receipt and its instruments are saved in **one** local transaction |
| `posting-svc` | Accounting Integration | Anti-corruption layer to the ERP general ledger |
| `reporting-svc` | Liquidity & Reporting | Read models built from events; report rendering |
| `documents-svc` | Documents | Object storage for scans and signatures |

### 5.2 Clean Architecture inside each service

```
Treasury.Cashflow.Domain          entities, value objects, domain events, rules — no framework references
Treasury.Cashflow.Application     use cases (commands/queries), ports (interfaces), validation
Treasury.Cashflow.Infrastructure  EF Core/PostgreSQL, messaging, outbox, external adapters
Treasury.Cashflow.Api             REST/gRPC endpoints, auth, OpenAPI
Treasury.Cashflow.Tests           domain unit tests, application tests, integration tests (Testcontainers)
```

Dependencies point inward only: the domain knows nothing about the database, the UI or the message broker. Architecture tests (NetArchTest/ArchUnitNET) fail the build if someone breaks this rule.

### 5.3 Key design rules that fix the current problems

- **One transaction per command.** Each use case loads one aggregate, changes it, and saves once. No more multiple `SaveChanges` with manual undo.
- **Transactional outbox.** Domain events are written in the same transaction as the data and published afterwards, so an event is never lost and never sent for data that was rolled back.
- **Sagas for cross-service flows** (request → payment → voucher) with explicit compensation.
- **Idempotency keys** on every write command, so a double-click or network retry can't create a duplicate payment.
- **Optimistic concurrency** (row version) on all aggregates; **atomic cheque number reservation** in `ChequeBook` (a single `UPDATE … RETURNING` under a row lock).
- **Value objects** for `Money` (amount + currency + rate), `Iban`, `NationalId`, `AccountCode`, `FiscalDate`. Dates stored as real `date`/`timestamptz`; Jalali (Shamsi) is a display and input format only.
- **No hard-coded IDs.** Operation types become named enums/codes; database IDs never appear in code.
- **No business logic in stored procedures.** The 98 procedures are re-implemented in the domain and covered by tests; the database only stores data.
- **Posting rules are configuration, not code**: the debit/credit account mapping per operation (today in `PlaningType` / `Receipt_Pay_TypeDetail`) is maintained by users with versioning and an effective date.

## 6. Micro-frontend architecture and user-friendliness

### 6.1 Structure

- **Shell application** — login (Keycloak), main menu, fiscal-year and branch selector, notifications, language/RTL switch, error handling. It loads the other parts at runtime.
- **One micro-frontend per business area**, owned by the same team as its backend service: `mf-basic-info`, `mf-requests`, `mf-payments`, `mf-receipts`, `mf-instruments`, `mf-petty-cash`, `mf-posting`, `mf-reports`.
- **Composition:** each micro-frontend is built and deployed independently; how they are joined depends on the framework (see 6.3).
- **Shared design-system package**: components, Persian/English texts, Jalali date picker, number and amount formatting (thousand separators, amount-in-words for cheques), RTL layout. This is what keeps the whole system looking like one product.
- **Backend-for-Frontend (BFF)** behind the gateway, so screens call one endpoint that combines data from several services instead of making many calls from the browser.

Framework: **Nuxt 4** (Vue 3 + TypeScript) — **decided 9 Nov 2026**. The design is described in 6.3.

### 6.2 Making it user-friendly

Your users have used the Access system for many years and are fast with it. The new UI should respect that:

- **Keyboard-first data entry** for receipts and payments: Enter moves to the next field, shortcut keys for save/new/search, as in the Access forms.
- **Task inbox** for approvers and cashiers: “requests waiting for my signature”, “cheques due this week”, “vouchers with errors”.
- **Instrument timeline**: one screen that shows every step a cheque went through, with the voucher created at each step.
- **Only valid actions are offered**: the action buttons on a cheque come from the state machine, so users cannot pick an operation that will later fail.
- **Clear, translated error messages** instead of database exception text.
- **One search pattern everywhere**, with saved filters and export to Excel/PDF.
- **Visual cheque-layout designer** (drag fields over a scanned cheque image) replacing the current coordinate tables.
- **Dashboard** with bank and cash balances and liquidity forecast.
- Usability testing with 5–6 real users from your clients at the end of each phase.

### 6.3 Framework decision: Next.js 16 or Nuxt 4

|  | Next.js 16.4.x (React) | Nuxt 4.6.x (Vue) |
| --- | --- | --- |
| How micro-frontends are joined | **Multi-Zones**, Next.js's own approach: each business area is a separate Next.js app under its own path (`/payments`, `/receipts`, …), built and deployed independently; the gateway routes by path | **Module Federation 2.0** through `@module-federation/vite` (the plugin Vite recommends; Nuxt 4 host/remote examples exist), or path-based zones as with Next.js; **Nuxt Layers** to share the shell layout |
| Several teams' components on the same page | Limited: the Module Federation plugin for Next.js (`nextjs-mf`) is deprecated and does not support the App Router | Supported, through Module Federation |
| RTL / Persian component libraries | Ant Design, MUI | PrimeVue, Vuetify, Element Plus |
| Ecosystem and hiring | Larger | Smaller, but strong |

**Decision (9 Nov 2026): Nuxt 4.** The micro-frontend design on Nuxt:

- **Host (shell):** a Nuxt 4 app with login (Keycloak, OpenID Connect), main menu, fiscal-year and branch selector, notifications and fa/en language switch with RTL.
- **Remotes:** one Nuxt/Vue app per business area (`mf-basic-info`, `mf-requests`, `mf-payments`, `mf-receipts`, `mf-instruments`, `mf-petty-cash`, `mf-posting`, `mf-reports`), each built with **Module Federation 2.0** (`@module-federation/vite`) and deployed on its own. The host reads each remote's `mf-manifest.json` at runtime and registers its routes, so a new version of one area goes live without rebuilding the others.
- **Client-side rendering (`ssr: false`)** for host and remotes. The system is an internal application behind a login, so server-side rendering brings no SEO benefit, while it would make Module Federation and on-premises hosting more complex. The built apps are static files served by Nginx — light enough for a client's single server.
- **Shared singletons** across all remotes: `vue`, `vue-router`, `pinia`, the UI library and the design-system package, so the browser loads them once and versions stay in step.
- **Nuxt Layers** for what every app needs at build time: Keycloak authentication, `@nuxtjs/i18n` with Persian/English and RTL, the typed API client (generated from the services' OpenAPI specs), Jalali date and amount formatting, error handling.
- **UI library:** PrimeVue (MIT licence, RTL support, a rich data table for financial grids), with Vuetify as the fallback. Both are checked in Phase 0 with a prototype of the received-cheque entry form, in RTL and keyboard-only use, before the final choice.
- **Fallback for composition:** if runtime federation causes problems, the same remotes can be published as separate apps under their own paths (zones), without code changes inside the areas.

**Version policy:** start on Nuxt 4.6.4 (a security release of 2 November 2026) or the latest 4.6.x patch at project start, on Node.js 24 LTS (Nuxt CLI v4 needs Node 22.21+ or 24.11+). Apply security patches promptly, and plan the move to Nuxt 5 as a normal maintenance task once it is released and stable.

## 7. One database for all fiscal years, partitioned by fiscal year

### 7.1 What happens today

The Treasury database keeps a `FiscalYear` column on some tables (`Receipt_Detail`, `Request_Payment`) but not on others, and the `Config` table maps each fiscal year to a **separate accounting database** (`AccDatabaseName`). Stored procedures pick that database at runtime and run dynamic SQL against it. This is slow, hard to test, and a security risk.

### 7.2 Reconciling “one database” with microservices

Microservices normally require each service to own its data. We can keep your requirement of a single database and still respect that rule:

- **One PostgreSQL database per client company**, with **one schema per service** (`reference`, `accounts`, `requests`, `cashflow`, `posting`, `reporting`, `documents`).
- Each service connects with **its own database role** that can only access its own schema. No cross-schema joins or foreign keys; services exchange data through APIs and events.
- Backup, restore and operations stay as simple as “one database”; if one service later needs its own server, its schema can be moved without code changes.

### 7.3 Partitioning design

- A `fiscal_year` table in the `reference` schema is the single source of truth (start/end date, status: *Open, Closing, Closed*). Every transactional row carries `fiscal_year_id`, set by the domain from the operation date, never typed by the user.
- **PostgreSQL declarative LIST partitioning** on `fiscal_year_id` for all transactional tables: receipts, payments, requests, instrument events, petty cash entries, vouchers, audit log. Reference/master data is not partitioned.
- The partition key is part of every primary key, e.g. `(fiscal_year_id, receipt_id)` — PostgreSQL requires this for unique constraints on partitioned tables.
- A new year's partitions are created automatically when the fiscal year is opened (pg\_partman or an application job).
- Closed years: partitions become read-only (permissions + application check), can be moved to cheaper storage (tablespace), and can be detached for archiving without touching current data.
- Queries always include the fiscal year, so PostgreSQL scans only that year's partition (partition pruning). Multi-year reports work naturally across partitions.

### 7.4 Things that cross a year boundary

A cheque received in year 1404 with a due date in 1405 is the classic case. The rule:

- The **instrument** belongs to the year it was created in.
- Each **state change** (deposit, clearance, bounce…) belongs to the year in which it happens, and its voucher is posted to that year.
- At year-end, the `FiscalYear` close process produces **carry-forward balances** (bank, cash desk, petty cash, open instruments) as opening entries of the new year, replacing today's manual “opening balance” entry.
- A closed year cannot receive new postings; corrections are made as adjusting entries in the open year.

### 7.5 Database engine: PostgreSQL 17 or SQL Server 2025

Recommendation: **PostgreSQL 17 as the standard engine**, with **SQL Server 2025 as a supported option** for clients who require it.

- **Why PostgreSQL 17 by default:** no licence cost for each client installation (this adds up quickly with many on-premises servers), mature declarative partitioning, the same behaviour on a client's server and in the cloud, and it matches your open-source preference.
- **Why keep SQL Server 2025 possible:** many of your clients already run SQL Server for the ERP (the accounting databases such as `AccIranNajo05` are SQL Server), and their IT staff know how to operate and back it up.
- **How to support both without doubling the work:** all data access through EF Core (Npgsql and SQL Server providers); no business logic and no engine-specific SQL in application code; only the partitioning and data-migration scripts exist in two versions; the CI pipeline runs the full integration test suite against **both** engines (Testcontainers) on every build.
- **Partitioning on SQL Server 2025:** a partition function on `fiscal_year_id` plus a partition scheme. The same rule applies (partition key inside the clustered primary key), and closed years can be moved out with `SWITCH PARTITION` to archive tables.
- **Review point:** if, after the pilot, no client actually needs SQL Server, drop it and save the double testing.

### 7.6 Merging the old yearly databases

The old system creates a new Treasury database and a new accounting database every fiscal year (section 3.5). The migration pipeline merges them:

1. **Reference data first**, merged and de-duplicated across years: banks, bank accounts, cash desks, بابت groups, counterparties. The same bank account appears again in every yearly database and must become one record.
2. **Transactions year by year, oldest first**, each year loaded into its own fiscal-year partition.
3. **Instruments that cross a year end** (a cheque carried forward appears in two yearly databases) are matched by bank, cheque number, amount and due date, and kept as **one** instrument with its full history.
4. **Reconciliation per year:** the closing balance of year N must equal the opening balance of year N+1 for every bank account, cash desk and petty cash fund.
5. Each client chooses how many old years to migrate in full; older years can be loaded read-only, for reports only, to shorten the migration.

For Iran Nazho Co. the old program's `Coyearinfo` table lists **24 fiscal years (1382–1405)**, each with its own Treasury, accounting, inventory and sales databases. Cheques, cheque books and payment-order vouchers were already kept in one shared database (`Cashnajoo86`) across years (see 12.2), which makes step 3 easier: those instruments are already single records.

## 8. Distributed deployment, security and observability

### 8.1 Deployment for a distributed environment

- **Containers everywhere** (Docker images, built once and promoted from test to production).
- **Kubernetes** for central / multi-client hosting; **k3s** or **Docker Compose** for small clients that run on a single server on-premises. Same images in both cases.
- **API gateway** (Apache APISIX or YARP) as the single entry point: TLS, routing, rate limiting, token validation.
- **Multi-tenancy:** one database per client company (strong isolation of financial data), shared application services, tenant resolved from the user's token.
- **Branches and remote users** connect over HTTPS only; no direct database or service access from user machines. Optional read replicas for branches with slow links.
- **Resilience:** retries with back-off and circuit breakers (Polly), health checks, horizontal scaling of busy services, message broker buffers peaks.

### 8.2 Security

- **Keycloak** (OpenID Connect) for login, single sign-on with the rest of your ERP, password policy, MFA (OTP) for approvers and administrators, optional LDAP/Active Directory.
- **Authorization by role *and* attributes:** branch, cash desk, bank account, amount limits; **four-eyes principle** (the person who creates a payment cannot approve it).
- **Secrets** in OpenBao (open-source Vault) or Kubernetes secrets — never in config files. Least-privilege DB roles; no `sa`.
- Passwords handled by Keycloak (PBKDF2/Argon2), TLS for all traffic including between services.
- **No CVV2 storage**; card numbers masked; IBAN and national IDs validated.
- **Immutable audit log** of every financial action: user, real client IP (from the gateway's `X-Forwarded-For`), time, before/after values.
- Security checks in the build pipeline: dependency scanning, static analysis, container image scanning (Trivy), OWASP ZAP against the test environment.

### 8.3 Observability

OpenTelemetry in every service → **Prometheus** (metrics), **Loki** (logs), **Tempo** or **Jaeger** (traces), shown in **Grafana** with alerts. One correlation ID follows a request from the browser through all services, so “the payment didn't save” can be traced in minutes.

### 8.4 Hosting: the client's own server, and ArvanCloud

I have read “Cloud Aran” as **ArvanCloud**; please correct me if you mean a different provider. Both models use exactly the same container images; only the deployment profile differs.

|  | On the client's server (preferred) | ArvanCloud |
| --- | --- | --- |
| Platform | One Linux server (two or three for high availability) running **k3s** or Docker Compose | ArvanCloud's Kubernetes-compatible container platform, or cloud servers running k3s |
| Database | PostgreSQL 17 (or SQL Server 2025) on the same or a separate server; daily backups plus continuous WAL archiving | A dedicated cloud server or managed database; backups to ArvanCloud object storage |
| Files and scans | MinIO on the client's server | ArvanCloud object storage (S3-compatible) |
| Tenancy | One client company | Several client companies, one database each |
| Updates | Offline-capable update package (signed images + database migration scripts + installer), or pull from your private registry | Automatic rolling updates through CI/CD (Argo CD) |

- **Compact profile for on-premises:** one replica per service, single-node RabbitMQ, lighter monitoring. Rough size for a pilot client: 8 vCPU, 16–32 GB RAM, 200 GB+ SSD, Ubuntu LTS (to be confirmed by a load test in Phase 0).
- **Licence file per installation:** a signed file listing the client, licensed modules, number of users and expiry date, checked at the gateway — useful for selling on-premises installations.
- **Data location:** with on-premises hosting the financial data never leaves the client's network; for ArvanCloud, choose the data-centre region that fits your clients' legal and audit requirements.

## 9. Open-source technology stack

| Layer | Recommended | Licence | Replaces |
| --- | --- | --- | --- |
| Backend runtime | .NET 8 LTS / .NET 10 LTS, ASP.NET Core (Linux containers) | MIT | .NET Framework 4.5, IIS |
| Data access | EF Core + Npgsql (Dapper for heavy reports) | MIT / PostgreSQL | EF6, `sa` connections |
| Database | PostgreSQL 17 with pg\_partman (SQL Server 2025 as an option, see 7.5) | PostgreSQL | SQL Server (commercial) |
| Messaging | RabbitMQ (or Apache Kafka for very high volume) | MPL 2.0 / Apache 2.0 | WCF |
| Messaging library | Wolverine, Rebus, or MassTransit v8 | MIT / Apache 2.0 | — |
| In-process CQRS | Wolverine or a small in-house dispatcher | MIT | (avoid MediatR 13+, now commercial) |
| Object mapping | Mapperly or manual mapping | Apache 2.0 | (avoid AutoMapper 15+, now commercial) |
| Validation | FluentValidation | Apache 2.0 | scattered controller checks |
| Identity | Keycloak | Apache 2.0 | custom Membership provider |
| API gateway | Apache APISIX or YARP | Apache 2.0 / MIT | fixed IP endpoints |
| Caching | Valkey (open-source Redis fork) | BSD | — |
| File storage | MinIO or SeaweedFS (S3-compatible) | AGPL / Apache 2.0 | `TreasuryFile` database |
| Reporting | FastReport Open Source, or JasperReports; QuestPDF for cheque printing | MIT / LGPL | Stimulsoft (commercial) |
| Workflow (approvals) | Elsa Workflows 3 (optional) or saga in code | MIT | hard-coded statuses |
| Frontend | Nuxt 4 (Vue 3, TypeScript), Module Federation 2.0 via @module-federation/vite, Nuxt Layers, Pinia, PrimeVue (see 6.3) | MIT | jQuery + AngularJS + Bootstrap 3 |
| Jalali dates | dayjs + jalaliday (front), PersianCalendar (back) | MIT | SQL string functions |
| Testing | xUnit, NSubstitute, Shouldly, Testcontainers, Playwright | Apache 2.0 / BSD / MIT | none |
| CI/CD | GitLab CE or Gitea + Woodpecker/Jenkins, Argo CD | MIT / Apache 2.0 | manual publish |
| Observability | OpenTelemetry, Prometheus, Grafana, Loki, Tempo | Apache 2.0 / AGPL | none |
| Containers | Docker, Kubernetes / k3s, Helm | Apache 2.0 | — |

Note on licences: in 2025 several popular .NET libraries changed to commercial licences for business use (MediatR and AutoMapper; MassTransit from v9; FluentAssertions from v8; Duende IdentityServer). The stack above avoids them or pins their last open-source versions. Licences should be re-checked when the project starts.

**SQL Server 2025** is covered in section 7.5: everything in this document works on it as well, using SQL Server partition functions instead of PostgreSQL partitioning and the EF Core SQL Server provider.

## 10. Migration strategy and phased roadmap

### 10.1 Approach: Strangler Fig, not big-bang

The new services are placed behind the gateway next to the current system. One business area at a time is moved to the new system; the old screens for that area are switched off only after the new ones pass parallel-run checks. Clients are never without a working system.

- **Data migration** is a repeatable, automated pipeline (not a one-time script): extract from SQL Server → clean and convert (Shamsi text dates → real dates, `char` codes trimmed, fiscal year assigned) → load into PostgreSQL partitions → **reconcile** (row counts, balances per bank account / cash desk / behalf group / fiscal year must match to the last Rial).
- **Behaviour baseline:** before rewriting each stored procedure, we capture its inputs and outputs on the client database you provided and turn them into automated *characterization tests*. The new code must produce the same vouchers and balances.
- **Parallel run:** for each phase, the client runs old and new side by side for at least one month-end close.

### 10.2 Phases

| Phase | Duration | Scope | Exit criteria |
| --- | --- | --- | --- |
| 0. Foundation | Month 1–2 | Domain workshops with your experts, event storming, ubiquitous-language glossary (fa/en), CI/CD, Kubernetes, Keycloak, gateway, shell micro-frontend, design system, observability, migration pipeline skeleton | Shell login working end-to-end; glossary signed off |
| 1. Basic info | Month 2–4 | `reference-svc`, `accounts-svc`; fiscal years, branches, currencies, bank accounts, cash desks, cheque books & layouts, counterparties | All master data migrated and reconciled |
| 2. Payment requests | Month 4–6 | `requests-svc`, approval workflow, signatures, task inbox | **First production go-live** at a pilot client |
| 3. Receipts & instruments | Month 6‐9 | `cashflow-svc` (receipts, instrument state machine, collection, bounce, replacement, endorsement) | All `PlaningType` transitions covered by tests; parallel run passed |
| 4. Payments & petty cash | Month 9‑11 | Cheque issue & printing, cash/transfer payments, inter-bank transfer, petty cash | Parallel month-end close passed |
| 5. Accounting integration | Month 8‑12 (overlaps) | `posting-svc`, posting rules editor, voucher batches, GL adapter, error handling and re-send | Vouchers identical to old system for a full month |
| 6. Reporting & cut-over | Month 11‑14 | `reporting-svc`, all reports from both manuals, dashboard, liquidity forecast; roll-out to all clients; old system read-only | All clients live; old system retired |

### 10.3 How I can help, concretely

Within this workspace I can do real engineering work on this project, step by step with you:

1. **Domain catalogue:** extract every business rule from the 98 stored procedures and the controllers into a written, reviewable rule list (fa/en), mapped to bounded contexts.
2. **Solution skeleton:** generate the repository structure — the services with Clean Architecture layers, architecture tests, Docker/Helm files, CI pipeline, and the micro-frontend shell — ready for your team.
3. **Domain models and tests:** write the `Instrument` state machine, `ChequeBook` number reservation, `Money`/`FiscalDate` value objects, and the posting-rule engine, with unit tests.
4. **Database design:** the PostgreSQL schemas, partitioning scripts and the SQL Server → PostgreSQL migration and reconciliation scripts.
5. **API contracts:** OpenAPI specifications and event schemas for each service.
6. **UI:** screen designs for the main workflows, then the React micro-frontends.
7. **Reviews:** review your team's pull requests against this architecture.

Your team (or contractor) still needs to run the system on your infrastructure, test against real client data, and own the code long-term.

### 10.4 Pilot with a real customer before wider rollout

**Pilot client: Iran Nazho Co.** (decided 9 Nov 2026). The old-system backups already received (`Cashnajoo05`, `Cashnajoo86`, `AccIranNajo05`) appear to be this client's databases, so the migration pipeline can be built and rehearsed on their real data from Phase 0 onwards.

- **Choosing the pilot client:** medium transaction volume, heavy use of cheques, a cooperative finance team, a server available on-premises, and both old (Access-era) and current data to migrate.
- **Two pilot steps:**
  1. *Early pilot* after Phase 2 (month 5–6): basic info and payment requests live in production. Low risk, because requests do not post vouchers.
  2. *Full pilot* of the final software after Phase 5 (around month 12): the complete system at the same client, run in parallel with the current system over **two month-end closes**.
- **Success criteria, agreed before the pilot starts:** vouchers and balances identical to the current system for the parallel months; no lost or duplicated cheque numbers; data entry of a received cheque at least as fast as in the old system for a trained user; no critical defect open for two weeks; a user satisfaction survey.
- **Support during the pilot:** daily check-in for the first two weeks, a shared issue tracker, and a weekly release of fixes.
- **Fallback:** the current system stays fully available and up to date (parallel entry by the pilot client's treasury team) until the client signs off.
- **After sign-off:** rollout in waves of two or three clients at a time, with on-premises clients and ArvanCloud clients in separate waves.

## 11. Risks, decisions needed, next steps

### 11.1 Main risks

| Risk | Mitigation |
| --- | --- |
| Business rules hidden in stored procedures are missed | Characterization tests on real data before rewriting; domain expert sign-off per context |
| Accounting differences after migration | Automated reconciliation to the Rial; parallel run over a month-end |
| Microservices complexity for a small team | Start with 7 services, not 60+; shared platform templates; one framework for all frontends |
| Integration with the ERP general ledger (other modules still on the old stack) | Anti-corruption layer in `posting-svc`; adapter can write to the existing GL database until the GL itself is modernized |
| User resistance to a new UI | Keyboard-first design based on the Access forms; usability tests each phase; training material from the manuals |
| Licence changes in open-source libraries | Prefer foundation-backed projects; pin versions; review licences each release |

### 11.2 Decisions needed from you

1. **Database engine — confirmed:** **PostgreSQL 17** as the standard (SQL Server 2025 stays an option only if a client requires it, 7.5).
2. **Hosting — confirmed:** preferably on the client's own server, with **ArvanCloud** as the cloud option (8.4).
3. **Frontend — decided:** **Nuxt 4** micro-frontends (6.3).
4. **Pilot — decided:** **Iran Nazho Co.** (10.4).
5. **Team — decided:** no full team can be hired now; development will rely on AI (Claude Code) with a minimal set of human roles — see **section 13**. *Still to name: the domain expert and the technical reviewer.*

### 11.3 Immediate next steps

1. ~~Re-send part 2 of the old source~~ — received and analyzed (section 12).
2. **Security now, in production:** restrict and fix the cheque print service (3.6); change the `sa` passwords on the servers named in the configuration files (`CSDSERVER2003`, `192.168.0.10`) **and the one hard-coded in the old Access program** (12.5); delete CVV2 data from the `BankCards` table.
3. ~~Confirm PostgreSQL and ArvanCloud~~ — confirmed 9 Nov 2026.
4. Set up what section 13.6 lists: a Git repository, a test server, and the names of the domain expert and the technical reviewer.
5. Your domain expert reviews the rule catalogue (12.4) and the state mapping (12.3), and adds the MVC `PlaningType` codes.
6. Phase 0 starts with the work in 13.7.

## 12. The old Access program (`Lcash.mdb`) — what it does and which rules to keep

### 12.1 Inventory

| Object | Count | Notes |
| --- | --- | --- |
| Forms | 116 | Data entry, bulk operations, searches |
| Reports | 146 | 48 of them are bank-specific cheque print layouts (Mellat, Melli, Tejarat, Saderat, Sepah, Refah, Parsian, Pasargad, Saman, Sina, Maskan, Keshavarzi, Karafarin, Novin, Tose'e, Ma'dan… in 10/25/50/100-sheet formats) |
| VBA modules | 271 (form and report code + 24 standard modules) | About 23,700 lines |
| Saved queries | 40 named, plus about 400 inside combo boxes and forms |  |
| Macros | 6 |  |
| Tables | 27 linked to SQL Server through ODBC (DSN `Sqlcsd`), 26 local |  |

The same file also holds 10 forms and reports made for another company (`Hepco*`). Customer-specific differences should become configuration and report templates in the new system, not copies of code.

### 12.2 How it works

- **Fiscal-year switch:** at login the user picks a year; the program reads `Coyearinfo` and relinks every ODBC table to that year's databases (24 years, 1382–1405, for Iran Nazho Co.).
- **Cheques live in one shared database:** `Checks`, `Bundle` (cheque books) and `Povouch` are always linked to `Cashnajoo86` (the “not-changed” database), while cash and bank transactions (`Subs`), vouchers and settings come from the yearly database. The old system already treated cheques as cross-year objects — the same idea as the `Instrument` aggregate in 4.2.
- **Vouchers are rebuilt, not posted per transaction:** “ایجاد اسناد” deletes the Treasury's own vouchers from a chosen date and regenerates them from the current state of cheques and transactions; “ارسال اسناد” validates them and copies them into the accounting database (`t_voucher`, `Source_Type = 3`), replacing only vouchers the accountants have not yet updated (`Is_Updated = 0`).

### 12.3 Cheque and note lifecycle in the old system

| Side | Code | State | Allowed from | Done in | New state (4.2) |
| --- | --- | --- | --- | --- | --- |
| Received | 11 | نزد صندوق — in the cash desk | (new receipt) | `D_Check` | InCashDesk |
| Received | 12 | واگذار جهت وصول — sent for collection | 11, only types 0 and 2 (ordinary and bank cheques) | `D_Check_Vag_m` | DepositedForCollection |
| Received | 13 | وصول شده — collected | 12 | `D_Check_vos_m` | Cleared |
| Received | 14 | برگشت شده — bounced | 12 | `D_Check_vos_m`, `Checkbacked` | Bounced |
| Received | 15 | عودت شده — returned to the issuer | 11 | `D_Check_Od_m` | Returned |
| Received | 16 | واگذار به سایرین — endorsed to a third party | 11 | `D_Check_Kh_m` | Endorsed |
| Issued | 20 | در جریان صدور — being issued | (payment order) | `Porder` | Approved |
| Issued | 21 | چک صادره — issued | 20 | `Check_Print_single` | Issued |
| Issued | 22 | تحویل شده — delivered to the payee | 21 | `Check_deliv_m` | Delivered |
| Issued | 26 | ابطال شده — cancelled | 20 / 21 | `Check_Print_single` | Cancelled |

Instrument types (`Checktype`): ordinary cheque, guarantee cheque (انتظامی), bank cheque, instalments, promissory note, other receivables/payables, guarantee note, and cash payment (type 9, issued side only). Guarantee items and items in legal action (`GoInlaw`) are left out of some vouchers. The MVC version models the same lifecycle with `PlaningType` rows; in Phase 0 the old codes, the MVC `PlaningType` codes and the new states are merged into one mapping table, confirmed by your domain expert.

Issued cheques also carry flags outside the status code: maturity of post-dated items (`Ctocash`) and payment or return by our own bank (`Vos` / `Backed` with `RegVos`). The new model turns them into steps — *RecordMatured*, *PaidByBank*, *BouncedAtBank* (found while building the migration, 13.8).

### 12.4 Business rules to carry over (first cut)

| # | Area | Rule in the old program | Where | New home |
| --- | --- | --- | --- | --- |
| R1 | Receipt | Type, bank, issuer, amount, due date, receipt date, بابت and payer's detail account required. The duplicate-number check (bank + number) exists in the code but is commented out (open question Q1) | `D_Check` | Receipts, Instrument |
| R2 | Dates | Valid Shamsi date (8 digits, month 1–12, valid day) inside the allowed year range from settings — one range for normal entries, a wider one for post-dated cheques | `Controldate` | Fiscal Periods posting window, `FiscalDate` |
| R3 | Collection | Only ordinary and bank cheques can be sent for collection, and a collection account is required | `D_Check_Vag_m` | Instrument transition guard |
| R4 | Collection result | Collected or bounced only from “sent for collection”; any other order is refused (“ترتیب عملیات نادرست است”) | `D_Check_vos_m`, `Checkbacked` | Instrument |
| R5 | Cheque book | Number must lie within the book's first and last sheet and not be used already; next number suggested; the book is deactivated after its last sheet | `Check_Print_single` | `ChequeBook` (atomic reservation) |
| R6 | Issue | Due date cannot be earlier than the issue date (ordinary and post-dated cheques only) | `Check_Print_single` | Payment |
| R7 | Issue | A payment must be approved before its cheque is printed | `Check_Print_single` | Payment Requests approval |
| R8 | Issue | An ordinary cheque that would make the bank balance negative is blocked (a warn-only variant exists but is commented out) | `Check_Print_single` | Payment policy per account |
| R9 | Payment order | Beneficiary, amount, currency, بابت and account required; cannot be deleted once paid | `Porder` | Payment Requests |
| R10 | Currency | Voucher amount = amount × rate; the collection account's currency is checked | `Voucher_create`, `D_Check_vos_m` | `Money`, posting rules |
| R11 | Posting | Control accounts come from settings: cheques and notes in the cash desk, collection in progress, bounced cheques, payables, guarantee accounts | `Voucher_create` | `PostingRule` configuration |
| R12 | Posting | Voucher line text = type + number + “در وجه” + person + “بابت” + purpose | `Voucher_create` | `PostingRule` template |
| R13 | Posting | Checks before sending: duplicate line, invalid date, unbalanced voucher, missing amount, missing or unknown subsidiary account, wrong account level, missing or unknown detail account, missing value/date fields | `Voucher_send` | Accounting Integration pre-posting validation |
| R14 | Posting | Re-sending replaces only vouchers the accountants have not updated | `Voucher_send` | `VoucherBatch`: Draft → Sent → Locked |
| R15 | Posting | Vouchers can be regenerated from a date for periods that are not yet locked | `Voucher_create` | Keep: “regenerate unposted vouchers” |
| R16 | Alerts | Due-date alarms for received and issued post-dated cheques | `Check_d_longsalarm`, `Check_p_longsalarm` | Task inbox, notifications |
| R17 | Security | Menu-level permissions per employee, with up to 11 action flags per menu | `Rights`, `Rights_m` | Keycloak roles + permission matrix |
| R18 | Printing | Bank-specific cheque layouts and amount in Persian words | `CH_*` reports, `func_nomtoword_l` | Layout designer + PDF |

### 12.5 Weaknesses not to carry over

- **Login by password only, with no user name.** The password is stored in plain text and pasted into the SQL text, so a crafted password can log in as another employee.
- **The `sa` password is hard-coded** in the VBA code (`Form_Sal`, `Form_Sal1`).
- **No transactions:** 126 recordset opens, 40 `RunSQL` calls and 31 record edits, none inside `BeginTrans`/`CommitTrans`.
- **Shared selection flag:** bulk operations mark cheques with a `Sel` column in the shared `Checks` table, so two users selecting at the same time change each other's selection.
- **Voucher numbers** in the accounting database are taken with `DMax(...)` and incremented, so two people sending at once can collide.
- **Fixed columns per stage** (`D1–D5`, `G1–G5`, `V1–V5`) instead of a history: a cheque keeps only the latest action of each stage.
- **Dates as 8-character text**, as in the MVC version.

### 12.6 Extracted source for your team

The 271 VBA modules, the SQL of all 438 queries, the lookup tables and the object inventory have been extracted into `Lcash_extracted.zip` (the hard-coded password is masked). Together with the MVC stored procedures, this is the input for the characterization tests of Phase 0.

## 13. Delivery model: building it with AI (Claude Code) and a minimal team

### 13.1 Is it realistic?

**Yes, with conditions.** This project suits AI-assisted development well: the business rules are now written down (old Access code, MVC code, two manuals, the rule catalogue in 12.4), the target architecture is fixed, and the work splits into many small, testable pieces. AI can write most of the code, the automated tests, the database and migration scripts and the documentation, quickly and to a consistent standard.

What AI cannot be is the *whole* team. A treasury system moves money and produces accounting vouchers; somebody has to decide the business questions, check what was built, run it on real servers, and take responsibility for it. So the realistic model is **a minimal human team plus AI**, not AI alone.

### 13.2 Minimum human roles

| Role | Who | Time needed | Why AI cannot replace it |
| --- | --- | --- | --- |
| **Product owner / domain expert** | From your side (you or your senior treasury expert) | About one day a week | Decides the rules, approves the status mapping and each delivered feature |
| **Technical reviewer / operator** | One experienced .NET + Linux developer; can be a part-time contractor | Two to three days a week | Reviews and approves every change, runs the system on test and production servers, holds passwords and production access, signs off security |
| **Pilot users at Iran Nazho Co.** | A treasury clerk and an accountant | During the pilots | Real-world use and the parallel run |
| **External security review** | Specialist firm | Once, before go-live | Independent penetration test of a financial system |

### 13.3 How the work runs

- **One Git repository** (GitHub preferred) is the single source of truth. It holds the code, this architecture, the rule catalogue and the coding conventions (in a `CLAUDE.md` file), so every AI session starts from the same knowledge.
- **Small, verifiable tasks**, for example “Instrument state machine with tests”, “Received-cheque API”, “Received-cheque screen”. One task per AI session: read the repository docs, write the code and its tests, run them, open a pull request.
- **Every pull request is built and tested automatically**, then reviewed and approved by the technical reviewer. Nothing reaches a server without human approval.
- **The safety net is the tests, not trust:** characterization tests taken from the old and MVC behaviour, and reconciliation on Iran Nazho Co.'s data — the new code must produce the same vouchers and balances.
- **A weekly demo** to the domain expert, who accepts or rejects what was built.

### 13.4 Adjustments to keep it manageable

All the requirements stay (microservice boundaries, Clean Architecture, DDD, micro-frontends, PostgreSQL 17 partitioned by fiscal year), but the start is made lighter so that a very small team can operate it:

- **Build each service as an independent module from day one** — its own project, database schema, API contract and events through the outbox — but **run them in as few processes as practical for the pilot**. Splitting a module into its own container later is a deployment change, not a rewrite. This lowers the number of things the reviewer must run and monitor on a client's single server.
- **One service template** for every module, so the AI and the reviewer find the same structure everywhere.
- **Micro-frontends in steps:** start with the Nuxt host plus three remotes (basic info; receipts, payments and instruments; reports) and split further when the areas grow.

### 13.5 Revised timeline (rough)

With AI writing most of the code, the pace is set by review, testing and pilot feedback rather than by typing. A rough estimate with the roles in 13.2: Phase 0 in one to two months; the early pilot (basic info and payment requests) at Iran Nazho Co. around month 4; the full pilot around months 9–11; wider rollout after sign-off. The biggest factor is how quickly reviews and business decisions happen each week.

### 13.6 What is needed to start

1. **A Git repository** — a GitHub repository can be connected to this workspace, so that I can push code and open pull requests there directly.
2. **A test server** (a Linux VM on your premises or on ArvanCloud) for demos.
3. **A decision on client data:** whether Iran Nazho Co.'s real databases may be used in AI sessions, or whether an anonymized copy should be made first (recommended for names, national IDs and account numbers).
4. **The names** of the domain expert and the technical reviewer.

### 13.7 What can start immediately

These Phase 0 pieces need no infrastructure and can be delivered first, into the repository (or as a downloadable package until it exists):

- The repository skeleton: solution structure, the service template with Clean Architecture layers and architecture tests, the Nuxt 4 host with one remote, and a Docker Compose file to run everything locally.
- The `Instrument` state machine and the `ChequeBook` number reservation, with tests generated from 12.3 and 12.4.
- The PostgreSQL 17 schema with fiscal-year partitioning for the core tables.
- The first migration script: reading `Lcash`/`Cashnajoo` cheques into the new instrument model, with a reconciliation report.

### 13.8 Progress: Phase 0, first package (9 Nov 2026)

Delivered as a code repository (`treasury-phase0.zip`) with `CLAUDE.md` (rules and commands for every AI session and developer), architecture decision records and the open questions.

| Part | State | How it was checked |
| --- | --- | --- |
| Cheque/note lifecycle and cheque book (domain) | Built | 49 automated tests, each tied to a behaviour of the old program |
| Use cases: receive, state changes, issue a cheque with sheet reservation | Built | 12 tests, including 40 simultaneous issues from one book with no duplicate number |
| Clean Architecture rules | Enforced | 6 architecture tests |
| Cashflow API | Built | Run and called over HTTP |
| PostgreSQL schema: fiscal-year partitions, closed-year freeze, per-service roles | Built | 10 tests on PostgreSQL 16 (17 was not installable in the build workspace; same features) |
| PostgreSQL adapters, Keycloak token check | Written | Type-checked only — first real build and run is for the technical reviewer |
| Migration of old cheques with reconciliation report | Built | 21 tests, including an end-to-end load; Jalali dates checked day by day against .NET for 1380–1420 |
| Nuxt 4 shell + receipts micro-frontend | Built | Browser test: receive a cheque, send it for collection, duplicate warning, invalid date |
| Docker Compose for a client's server | Written | Not yet run |

The build workspace could not reach the .NET package registry (NuGet), so the parts that need packages are marked as not yet compiled. Next: run the migration preflight and a dry run on Iran Nazho Co.'s `Cashnajoo86`, answer Q1–Q10, then payment requests and the Keycloak login.
