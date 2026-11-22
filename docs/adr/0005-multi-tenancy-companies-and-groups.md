# ADR 0005 — Multi-tenancy: companies, groups and one database per company

Status: accepted as a business decision (CEO, 19 Nov 2026); the technical points marked "to confirm" are settled in review when the login step is built. Not built yet.

## Context

The design document (8.1, 8.4) already planned "one database per client company, shared services, tenant resolved from the user's token". What is built so far serves one company per installation: each service reads one fixed connection string (`ConnectionStrings:Treasury`). On 19 Nov 2026 the owner confirmed what multi-tenancy must mean:

1. Separate client companies that must never see each other's data.
2. Several companies of one group (holding), where one user may work in more than one company.
3. The company is chosen at login; users with the right may see combined reports across the companies of their group.
4. A central hosted service (for example on ArvanCloud) for many clients, beside clients such as Iran Nazho Co. who keep the system on their own server.
5. Each company has its own name and logo on screens and printed reports, and its own users and permissions managed by its own administrator.

## Words

- **Company**: one legal entity with its own books. It is the tenant.
- **Client**: who we sell to. A client is one company, or a **group** (holding) of several companies.
- **Installation**: one running copy of the services. Either a client's own server (one client) or the central hosted service (many clients). Same code and images in both.

## Decision

1. **One PostgreSQL database per company** stays (decision of 9 Nov 2026). The database is the wall between companies: tables get **no `company_id` column**, and the schema scripts, the fiscal-year partitions and the migration tool stay as they are. A group's companies have one database each, like any other company.
2. **A small catalog per installation** (`platform` database, outside every company's database) lists clients, their companies (code, name, logo, status, where the company's database is) and which companies form a group. It holds no business data and no password; database credentials come from the environment or the secret store (rule 7).
3. **Every request names one company.** After login the user chooses a company among those they may enter (skipped when there is only one); it is shown in the top bar beside the fiscal year and can be changed without logging in again. The user's token lists the companies they may enter and their rights in each. Each API checks the requested company against the token and refuses a request without a company or with one the token does not list (`Company.NotAllowed`). The company is never taken from the browser's word alone.
4. **Inside a service** the current company is a port in `Treasury.BuildingBlocks.Application` (`ICompanyContext`). Infrastructure opens the connection to that company's database; Domain and Application code do not change. In-memory adapters keep one store per company. Background work (outbox, yearly carry-over) runs company by company; every event and message carries its company. Caches are keyed by company.
5. **No command writes to two companies.** Rule 3 (one command = one transaction = one aggregate) holds inside one company. Anything that concerns two companies of a group is two commands, one in each.
6. **Combined reports** are read-only and belong to `reporting-svc` (Phase G): it reads the databases of the group's companies in which the user has the report right and shows them side by side, one column per company and a total; foreign currency per currency and as its Rial equivalent. First ones (QT3): bank and cash balances, received and issued cheques by due date, payment requests waiting for approval or payment, cash-flow forecast. A user without that right sees one company at a time.
7. **Name and logo** of the company and its official details for printed headers are the company's own data, entered on «مشخصات شرکت» (owner, 16 and 18 Nov 2026; pull requests 12 and 14, one page since their merge) and kept in the company's database; the catalog holds only the short name shown in the company list at login. The shell and every printed report show the chosen company's name and logo.
8. **Administration at three levels** (QT4): the provider (us) creates clients and companies and runs the schema scripts for a new company's database; a group's administrator (all companies of the group) or a company's own administrator (that company only) creates its users and gives them rights, per company, on a screen inside the system (not in the Keycloak console); users of one client never appear in another client's lists.
9. **Each company has its own Basic Information** (QT1). A new company of a group can be started as a copy of another company's general lists (purpose groups, references, cheque types); the copy is made once, the lists are not shared afterwards.
10. **A payment between two companies of a group** is recorded separately in each (QT2): the other company is an ordinary تفصیل. Nothing is created automatically in the other company.
11. **A user's rights are set company by company** (QT5). A group has a group administrator over all its companies (QT4).
12. **On a client's own server** nothing differs except that the catalog lists one client. The signed licence file (design document 8.4) lists the companies the installation may serve.

## To confirm when built (review)

- **Keycloak layout:** proposed one realm per client (own users, password policy, administrator and optional LDAP per client), companies of a group as groups inside the realm with rights per company. Alternative: one realm with Keycloak's Organizations feature. Decide with the login step.
- **Where the catalog lives** in the compact (one-server) profile: its own small database in the same PostgreSQL server is proposed.
- **Database roles:** PostgreSQL roles belong to the server, so each service keeps one role (`cashflow_svc`, …) granted in every company database of that server; the connection string differs only in host and database name.

## Consequences

- Cheapest to build together with the Keycloak login (Phase B), before more screens assume one company.
- New tests: a token for company A cannot read or write company B through any API; no adapter opens a connection without a company; in-memory and PostgreSQL adapters behave the same with two companies.
- `deploy/` gains the catalog and a "new company" script; both hand-run guides are updated then. The Windows test server keeps working with one company.
- Business points QT1–QT5 (`docs/domain/open-questions.md`) were answered by the owner on 19 Nov 2026 and are part of the decision above.
