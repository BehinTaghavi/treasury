"""Loads migrated instruments into PostgreSQL (cashflow schema) and reconciles source against target."""
from __future__ import annotations

from collections import defaultdict
from dataclasses import dataclass, field
from decimal import Decimal

import psycopg

from . import jalali
from .legacy_mapping import MigratedInstrument, Rejected

MIGRATION_USER = "migration"


def ensure_fiscal_years(conn: psycopg.Connection, years: set[int]) -> None:
    for y in sorted(years):
        starts, ends = jalali.fiscal_year_bounds(y)
        conn.execute("SELECT cashflow.open_fiscal_year(%s, %s, %s)", (y, starts, ends))


def load(conn: psycopg.Connection, items: list[MigratedInstrument]) -> int:
    """Inserts instruments and their steps in one transaction. Already-migrated rows (same id) are skipped. Returns rows inserted."""
    years = {i.origin_fiscal_year for i in items} | {jalali.jalali_year(s.event_date) for i in items for s in i.steps}
    inserted = 0
    with conn.transaction():
        ensure_fiscal_years(conn, years)
        for i in items:
            cur = conn.execute(
                """INSERT INTO cashflow.instrument (origin_fiscal_year, id, direction, kind, number, bank_name, amount, currency, rate_to_rial,
                       amount_rial, issue_date, due_date, counterparty_id, counterparty_name, purpose, destination, state, legacy_source, version)
                   VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, 1)
                   ON CONFLICT (origin_fiscal_year, id) DO NOTHING""",
                (i.origin_fiscal_year, i.id, i.direction, i.kind, i.number, i.bank_name, i.amount, i.currency, i.rate_to_rial,
                 i.amount_rial, i.issue_date, i.due_date, i.counterparty_id, i.counterparty_name, i.purpose, i.destination, i.state,
                 i.legacy_source))
            if cur.rowcount == 0:
                continue
            inserted += 1
            for s in i.steps:
                conn.execute(
                    """INSERT INTO cashflow.instrument_event (fiscal_year, instrument_year, instrument_id, seq, action, from_state, to_state,
                           event_date, user_id, reference, note)
                       VALUES (cashflow.fiscal_year_of(%s), %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)""",
                    (s.event_date, i.origin_fiscal_year, i.id, s.seq, s.action, s.from_state, s.to_state, s.event_date,
                     MIGRATION_USER, i.legacy_source, s.note))
    return inserted


@dataclass
class Reconciliation:
    source_rows: int
    rejected: list[Rejected]
    expected: dict[tuple[str, str, str], tuple[int, Decimal]]
    loaded: dict[tuple[str, str, str], tuple[int, Decimal]]
    warnings: dict[str, int] = field(default_factory=dict)

    @property
    def differences(self) -> list[tuple[tuple[str, str, str], tuple[int, Decimal], tuple[int, Decimal]]]:
        keys = sorted(set(self.expected) | set(self.loaded))
        zero = (0, Decimal(0))
        return [(k, self.expected.get(k, zero), self.loaded.get(k, zero)) for k in keys if self.expected.get(k, zero) != self.loaded.get(k, zero)]

    @property
    def ok(self) -> bool:
        return not self.differences

    def to_markdown(self, title: str) -> str:
        lines = [f"# Reconciliation — {title}", "",
                 f"- Source rows read: **{self.source_rows}**",
                 f"- Migrated: **{sum(c for c, _ in self.expected.values())}**",
                 f"- Rejected: **{len(self.rejected)}** (listed below; to be decided with the domain expert)",
                 f"- Result: **{'MATCH' if self.ok else 'DIFFERENCES FOUND'}**", "",
                 "| Direction | State | Kind | Expected count | Loaded count | Expected Rial | Loaded Rial |",
                 "|---|---|---|---:|---:|---:|---:|"]
        zero = (0, Decimal(0))
        for k in sorted(set(self.expected) | set(self.loaded)):
            e, l = self.expected.get(k, zero), self.loaded.get(k, zero)
            mark = "" if e == l else " ⚠"
            lines.append(f"| {k[0]} | {k[1]} | {k[2]}{mark} | {e[0]} | {l[0]} | {e[1]:,} | {l[1]:,} |")
        if self.warnings:
            lines += ["", "## Data warnings (migrated, but worth checking)", "", "| Warning | Rows |", "|---|---:|"]
            lines += [f"| {w} | {n} |" for w, n in sorted(self.warnings.items(), key=lambda x: -x[1])]
        if self.rejected:
            reasons: dict[str, int] = defaultdict(int)
            for r in self.rejected:
                reasons[r.reason.split(":")[0]] += 1
            lines += ["", "## Rejected rows by reason", "", "| Reason | Rows |", "|---|---:|"]
            lines += [f"| {k} | {v} |" for k, v in sorted(reasons.items(), key=lambda x: -x[1])]
        return "\n".join(lines) + "\n"


def reconcile(conn: psycopg.Connection, source_db: str, source_rows: int, results: list[MigratedInstrument | Rejected]) -> Reconciliation:
    expected: dict[tuple[str, str, str], list] = defaultdict(lambda: [0, Decimal(0)])
    warnings: dict[str, int] = defaultdict(int)
    rejected = []
    for r in results:
        if isinstance(r, Rejected):
            rejected.append(r)
            continue
        e = expected[(r.direction, r.state, r.kind)]
        e[0] += 1
        e[1] += r.amount_rial
        for w in r.warnings:
            warnings[w.split(":")[0].split(" date ")[0]] += 1
    loaded = {}
    for d, s, k, n, total in conn.execute(
            """SELECT direction, state, kind, count(*), sum(amount_rial) FROM cashflow.instrument
               WHERE legacy_source LIKE %s GROUP BY 1, 2, 3""", (source_db + ".Checks:%",)):
        loaded[(d, s, k)] = (n, Decimal(total))
    return Reconciliation(source_rows, rejected, {k: (v[0], v[1]) for k, v in expected.items()}, loaded, dict(warnings))
