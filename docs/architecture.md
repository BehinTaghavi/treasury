# Architecture (short version)

Full design document: [docs/assessment.md](assessment.md) (Farsi: [assessment.fa.md](assessment.fa.md)), sections 4–13. Online copy: https://claude.ai/artifact/RhHq7wisV2FZVNKjAdyALP

- **Bounded contexts → services.** Seven services are planned (reference, accounts, requests, cashflow, posting, reporting, documents). Each is built as an independent module (own projects, own database schema, own API contract, events through an outbox). For the pilot several modules may run in one process; splitting later is a deployment change (ADR 0001).
- **Layers per service:** Domain (rules, no dependencies) → Application (use cases, ports) → Infrastructure (PostgreSQL, messaging) → Api (HTTP). Enforced by `tests/Treasury.ArchitectureTests`.
- **Shared building blocks** (`src/BuildingBlocks`), the only code services share: `Treasury.BuildingBlocks.Domain` (aggregate root, domain exceptions with stable codes, `FiscalDate`, `PersianText`) and `Treasury.BuildingBlocks.Application` (`Result<T>`, `IUnitOfWork`, `IIdempotencyStore`, `ConcurrencyConflictException`, and `CommandRunner`: idempotency key + one transaction + rule violations turned into results). They know no service and no framework package; business rules never go there.
- **Data:** PostgreSQL 17, one database per client, one schema per service, transactional tables LIST-partitioned by fiscal year; closed years are frozen by trigger (ADR 0003).
- **Frontend:** Nuxt 4 shell + Vue micro-frontends via Module Federation 2.0, loaded at run time from manifests listed in `shell-config.json` (ADR 0004).
- **Identity:** Keycloak (OpenID Connect). The API validates tokens when `Keycloak:Authority` is set; it refuses to start outside Development without it.

Built so far: the cashflow service's instrument and cheque-book model, its use cases and HTTP API, the PostgreSQL schema, the old-cheque migration tool, and the shell with the receipts micro-frontend (Phase 0); `reference-svc` with the simple Basic Information lists (currencies, country/province/city, bank names) and the Basic Information micro-frontend (Phase B, step 1).
