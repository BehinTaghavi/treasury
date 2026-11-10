# ADR 0001 — Build services as independent modules, run them in few processes at first

Status: accepted (9 Nov 2026)

Context: the team is minimal and AI-assisted (doc section 13); pilot installations run on one client server.
Decision: every service keeps its own projects, schema, contract and outbox from day one, but for the pilot several may be hosted together. No module may read another module's tables or call its classes directly.
Consequence: moving a module to its own container later needs configuration only. The architecture tests and the per-schema database roles keep the boundaries honest.
