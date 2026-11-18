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

By 15 Nov 2026 eight pull requests were merged into `main`. The CI run of the latest one, #8, was green before it was merged.

**18 Nov 2026, waiting for review — latest status of a received cheque:** as the product owner described it, the «وضعیت» column of the receipts list shows the latest status of each receipt's cheques; two new statuses, «ثبت صیادی شد» (registered in Sayad, for a cheque in the cash desk; required before the deposit at the bank) and «اقدام حقوقی» (a bounced cheque handed to the company's lawyer, which can still be replaced) with its own screen: the bounced cheque chosen from a list, its particulars shown, the date and description of the action, the number, date and tracking code of the certificate of non-payment («گواهی عدم پرداخت»), the scans of its papers and its voucher (Dr «اسناد واخواست شده حقوقی شده», a new template account; for guarantee items «اسناد تضمینی حقوقی شده به نفع شرکت»), and the hand-back to the cash desk for a cheque that becomes collectable; the automatic mark «سررسید چک» on post-dated cheques in the cash desk or at the bank, with what the cheque waits for; a voucher for the return to the payer; and one list of status names (the owner's words) for every screen. Details in `docs/domain/received-cheque-steps.md` (sections 5 and 6); Q37–Q42 are all answered. Also: a «فهرست دریافت‌ها» button with search and print under every receipt screen, and six kinds of a direct bank deposit (Paya and Satna with the Sheba, internal transfer, cash at a branch, card to card, payment gateway); Q43–Q45 are answered too. Also: Basic Information has a new page «مشخصات شرکت» (the company's name, address, and legal address with its own city and postal code); on a cheque receipt «خارج از مرکز» (out of town) is ticked automatically when the city of the cheque's bank is not the company's city; and a list «نوع حساب تفصیل» (type of detail account) before the payer shows the detail accounts of the chosen type, to pick directly or search.
**In review, [#12](https://github.com/BehinTaghavi/treasury/pull/12) (16 Nov 2026) — company information:** a new Basic Information page «اطلاعات شرکت» keeps the company's name and its scanned logo, and the shell shows both at the top of every screen; the page also keeps the company's official details (شناسه ملی, کد اقتصادی, شماره ثبت, address, postal code, telephone, website, e-mail) for the top of printed forms (`reference-svc`, schemas 033 and 034). Since 18 Nov 2026 it is one page with «مشخصات شرکت» (the legal address and its city), named «مشخصات شرکت» (owner's decision of 18 Nov 2026).

| Pull request | Subject | What it added |
|---|---|---|
| [#10](https://github.com/BehinTaghavi/treasury/pull/10) (open, in review) | Promissory notes and authorised signatories | **New tab «دریافت سفته»** on the Receipts screen, built from the owner's sheet and the mock-up he approved (16 Nov 2026): the notes of one obligor on one receipt with its own numbers; reason (debt, guarantee, trust, other), paper or electronic, due date or on demand; guarantors with their تفصیل; supplementary information and the article 308 checks on each note; total in figures and words; voucher shown under the form; **printed A4 receipt** for the three signatures. New fixed account «سفته انتظامی به نفع شرکت». With the owner's answers of the same day: up to three guarantors, and a receipt is saved only when every article 308 item of each note is ticked. **Authorised signatories:** new Basic Information page «دارندگان امضای مجاز» (name, position, تفصیل, national code, mobile, landline) with a signature rule for six kinds of document (number of signatures, compulsory and optional signatories); the note receipt asks who signs by its rule and prints them. **Steps of a note after receipt:** new tab «عملیات سفته‌های دریافتی» — deposit at the bank, collected, returned by the bank, protested, handed back to the obligor, endorsed to pay an approved request, cancelled — with their vouchers and the new fixed account «سفته های در جریان وصول». Open questions Q42 and Q43. |
| [#8](https://github.com/BehinTaghavi/treasury/pull/8) | Work of 11–15 Nov | **Basic Information completed** (all 14 items of the manual, and the owner's reports no. 1 to 4): Iran's cities, references (عطف), purpose groups and purposes, cash desks, petty cash, organisational units, requesters of funds, beneficiaries, bank account kinds and bank cards, cheque types with the scan of the cheque, and cheque books with series, serial and Sayad ID. **Opening balances** of bank accounts, cash desks and petty cash. **Receipts:** cash, money paid into a bank account (slip, transfer, POS, card-to-card), and cheques and notes. **Payment requests** in the new `requests-svc`: request, approval, cancellation, the list approved for payment, and scanned attachments. **Treasury vouchers** from the owner's template. **Steps of a received cheque after receipt** (deposit at the bank, collected, bounced, returned, replacement, cancellation, endorsement), each with its voucher; an endorsement pays an approved payment request. |
| [#7](https://github.com/BehinTaghavi/treasury/pull/7) | Reviewer's guide in Farsi | `docs/reviewer-guide.fa.md`: the code and its patterns explained, a pull-request review checklist, and a learning path for the technologies used. |
| [#6](https://github.com/BehinTaghavi/treasury/pull/6) | Shared command plumbing | The three services each had their own copy of the same code: idempotency key, one transaction per command, rule violations turned into error codes. It now lives once in `src/BuildingBlocks/Treasury.BuildingBlocks.Application`. No behaviour change. |
| [#5](https://github.com/BehinTaghavi/treasury/pull/5) | Basic information, step 3: the company's bank accounts | New `accounts-svc` and its screen: account number, bank, branch, currency, the bank's معین with تفصیل اول/دوم, collection and bounced-cheque accounts, and a backup account. A duplicate account number is refused (Persian and Latin digits count as the same). A main account has at most one backup, from the same bank. Accounts are switched off, not deleted. |
| [#4](https://github.com/BehinTaghavi/treasury/pull/4) | Basic information, step 2: chart of accounts and تفصیل | Account structure with four fixed levels (گروه، کل، معین، معین در معین) and a floating تفصیل; code lengths are set per company. Accounts and تفصیل are imported from the accounting system's Excel exports, with a preview before saving. Re-importing never deletes. Each account lists its allowed تفصیل types. Checked with Iran Nazho Co.'s real exports: 634 accounts and 9,628 تفصیل, no rejected row (the files are not in the repository). Voucher rules written down: `docs/domain/posting-rules.md`. |
| [#3](https://github.com/BehinTaghavi/treasury/pull/3) | Basic information, step 1: simple lists | New `reference-svc`: currencies, country/province/city and bank names, with default lists. New "Basic Information" micro-frontend. |
| [#2](https://github.com/BehinTaghavi/treasury/pull/2) | Owner's review of the receipts screen | Main menu order: Dashboard, Basic information, Payment requests, Payments, Receipts, Reports. Rule N2: a received cheque cannot be cleared or bounced before its due date; depositing it for collection earlier stays allowed. |
| [#1](https://github.com/BehinTaghavi/treasury/pull/1) | Phase A: first real build | Full build with NuGet packages and no warnings, PostgreSQL adapter tests on PostgreSQL 17, the whole system run with Docker Compose and `deploy/smoke-test.sh`, and CI with four jobs: `dotnet`, `database-and-migration`, `frontend`, `compose`. Test server on the owner's Windows PC, started with a double-click: `docs/test-server-windows.md`. |

Before these, the first package (Phase 0) was loaded on 9 Nov 2026: repository skeleton, the cheque and cheque-book domain model, the database schema, the migration tool, the Nuxt shell with the receipts screen, the design document, and the old programs' source for reference.

**Waiting for review (not yet in `main`), [pull request #13](https://github.com/BehinTaghavi/treasury/pull/13):** the version running (commit and date) is shown in the top bar of every screen, next to the «English» button, and in the «Start Treasury» window together with the folder the system is built from, so the owner can see he runs the newest version (17 Nov 2026).

## Current status

Phase A is almost finished. Basic Information (Phase B) is built and waits for review. Payment requests (Phase C) and receipts with the steps of a received cheque (Phase D) were started early at the owner's request. Login with Keycloak is not built yet.

| Part | What is built | Tests |
|---|---|---|
| `cashflow-svc` | Lifecycle of received and issued cheques and notes with rule N2; cheque types with their scans and cheque books (series, serial, Sayad ID); receipts (cash, bank deposit, cheques, and promissory notes on a form of their own with a printed receipt); the steps of a received cheque and of a received note after receipt and their latest status (including «ثبت صیادی شد», «اقدام حقوقی» and the mark «سررسید چک»); treasury vouchers | Domain, use-case and PostgreSQL tests |
| `reference-svc` | Company particulars, one page since 18 Nov 2026 (name and logo shown at the top of every screen, official details for printed forms, address, legal address with its city and postal code); currencies, country/province/city (Iran's cities pre-filled), bank names; code lengths, chart of accounts and تفصیل with Excel import, allowed تفصیل types; references (عطف), purpose groups and purposes; organisational units, requesters of funds, beneficiaries; authorised signatories and signature rules | Domain, use-case, Excel-reader and PostgreSQL tests |
| `accounts-svc` | The company's bank accounts with their kinds and bank cards, cash desks, petty cash, opening balances | Domain, use-case and PostgreSQL tests |
| `requests-svc` | Payment requests: request, approval, cancellation, the list approved for payment, scanned attachments, payment by an endorsed cheque | Domain, use-case and PostgreSQL tests |
| Architecture rules | Layers depend inward only; no service references another service's code | Architecture tests |
| Database | One schema per service (scripts 000 to 039), fiscal-year partitions, closed-year freeze, and one role per service that cannot see another service's schema | Schema tests on PostgreSQL 17 |
| Migration tool | Old cheques moved to the new schema with a reconciliation report; Jalali dates checked day by day against .NET for 1380–1420 | Migration tests |
| User interface | Nuxt 4 shell (the company's name and logo at the top of every screen) and three micro-frontends: Receipts, Basic Information and Payment requests | ui-kit tests and browser checks |
| Deployment | Docker Compose for one client's server, and the Windows test server; the version running is shown at the top of the screen | `deploy/smoke-test.sh` and the CI job `compose` on every pull request |
| Login with Keycloak | Token validation in the API is compiled | **Not yet run**; comes later in Phase B |

The CI run of pull request #8 had 510 .NET tests: 509 passed, and one is skipped on CI because it runs only locally, with a client's real files.

## Next steps

The full plan and phases A–G are in `docs/plan.md`.

- **Review of pull request #8:** it was merged on the owner's instruction so that `main` matches his test server; the reviewer's pass over the work of 11–15 Nov is still to come.
- **Left in Phase A:** migration dry run on an anonymised copy of Iran Nazho Co.'s data (`Cashnajoo86`), and deployment notes for the pilot server.
- **Rest of Phase B:** login with Keycloak, and with it the permissions for approvals.
- **Payment requests (Phase C):** the owner's answers to Q25–Q29, then paying approved requests (Phase E).
- **Payments and cheque printing (Phase E):** the proposal in `docs/domain/cheque-printing.md` waits for the owner's answers to Q12–Q18.
- **Later phases:** accounting vouchers sent to the general ledger (F), reports and the full pilot (G).
- **Authorised signatories:** the signature rules of the receipt of cheques, issued cheques and notes and the payment sheets are used when those forms and printed sheets are built.
- **Promissory notes:** the owner's answers to Q42 and Q43 of the section «Receipt of promissory notes» (the reason asked once; scans of the attachments).
- **Open business questions:** Q1–Q10, Q25–Q29, Q42 and Q43 (notes section), Q46 and Q47 (company particulars) in `docs/domain/open-questions.md`, and Q12–Q18 in `docs/domain/cheque-printing.md`.

## Repository layout

| Path | Contents |
|---|---|
| `src/Services/Cashflow` | Receipts, cheques and notes with their steps, cheque types and cheque books, treasury vouchers (later: payments) |
| `src/Services/Reference` | Basic Information lists (also purposes, organisational units, requesters, beneficiaries) and the copy of the chart of accounts |
| `src/Services/Accounts` | The company's bank accounts and bank cards, cash desks, petty cash, opening balances |
| `src/Services/Requests` | Payment requests |
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

1. `docs/reviewer-guide.fa.md` (Farsi): the code and its patterns, and how to review a pull request. `docs/reviewer-run-guide.fa.md` (Farsi): build and run any version with only Git and Docker.
2. `CLAUDE.md`: the rules for every change, and the commands.
3. `docs/plan.md`: phases, what is done, what is next.
4. `docs/decisions.md`: confirmed decisions with date and reason.
5. `docs/architecture.md` and `docs/adr/`: architecture summary and architecture decisions.
6. `docs/domain/`: instrument lifecycle, posting rules and open questions (each with a Farsi copy).
7. `docs/assessment.md`: the full design document (Farsi: `docs/assessment.fa.md`).
