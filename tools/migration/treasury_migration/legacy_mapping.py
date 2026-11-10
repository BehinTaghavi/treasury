"""Maps one row of the old dbo.Checks table (Access era, kept in Cashnajoo86) to the new instrument model.

Pure functions, no database access, so every rule here is covered by tests/test_mapping.py.
The mapping follows doc section 12.3; open points are marked "Q<n>" and listed in tools/migration/README.md.
"""
from __future__ import annotations

import uuid
from dataclasses import dataclass, field
from datetime import date
from decimal import ROUND_HALF_UP, Decimal

from . import jalali

# Namespace for deterministic ids: re-running the migration produces the same ids, so loads are idempotent.
_NAMESPACE = uuid.UUID("6f0e6a2c-4c1b-4b8e-9a59-1f4b2f0c7d11")

RECEIVED, ISSUED = "Received", "Issued"

# Old Checktype by side (dbo.Checktype lookup table, Lcash.mdb).
KIND_BY_TYPE = {
    1: {0: "OrdinaryCheque", 1: "GuaranteeCheque", 2: "BankCheque", 3: "Instalment", 4: "PromissoryNote", 5: "Other", 6: "GuaranteeNote"},
    2: {0: "OrdinaryCheque", 1: "GuaranteeCheque", 2: "PostDatedCheque", 3: "Instalment", 4: "PromissoryNote", 5: "Other", 6: "GuaranteeNote"},
}
CASH_PAYMENT_TYPE = 9  # issued side "پرداخت نقدی" — not an instrument

GUARANTEE_KINDS = {"GuaranteeCheque", "GuaranteeNote"}

# Allowed steps — must stay identical to InstrumentLifecycle.All in the .NET domain (checked by test_lifecycle_matches_dotnet).
TRANSITIONS = {
    (RECEIVED, "InCashDesk", "DepositForCollection"): "DepositedForCollection",
    (RECEIVED, "DepositedForCollection", "RecordCleared"): "Cleared",
    (RECEIVED, "DepositedForCollection", "RecordBounced"): "Bounced",
    (RECEIVED, "InCashDesk", "ReturnToIssuer"): "ReturnedToIssuer",
    (RECEIVED, "InCashDesk", "Endorse"): "Endorsed",
    (ISSUED, "BeingIssued", "Issue"): "Issued",
    (ISSUED, "Issued", "Deliver"): "Delivered",
    (ISSUED, "BeingIssued", "Cancel"): "Cancelled",
    (ISSUED, "Issued", "Cancel"): "Cancelled",
    (ISSUED, "Issued", "RecordMatured"): "Issued",
    (ISSUED, "Delivered", "RecordMatured"): "Delivered",
    (ISSUED, "Issued", "RecordPaidByBank"): "PaidByBank",
    (ISSUED, "Delivered", "RecordPaidByBank"): "PaidByBank",
    (ISSUED, "Issued", "RecordBouncedAtBank"): "BouncedAtBank",
    (ISSUED, "Delivered", "RecordBouncedAtBank"): "BouncedAtBank",
}


@dataclass(frozen=True)
class LegacyCheque:
    """The columns of dbo.Checks that the migration reads (names as used by the Access VBA code)."""

    checkid: int
    dp: int
    checktype: int
    stcode: int
    checkno: str | None = None
    bank_name: str | None = None
    amount: Decimal = Decimal(0)
    iscur: bool = False
    curid: int | None = None
    currate: Decimal | None = None
    person: str | None = None
    babat: str | None = None
    maturity: str | None = None
    d1: str | None = None
    d2: str | None = None
    d3: str | None = None
    d4: str | None = None
    vagno: str | None = None
    baccno: str | None = None
    bundleid: int | None = None
    vos: bool = False
    backed: bool = False
    regvos: bool = False
    ctocash: bool = False
    goinlaw: bool = False


@dataclass
class Step:
    seq: int
    action: str
    from_state: str | None
    to_state: str
    event_date: date
    note: str | None = None


@dataclass
class MigratedInstrument:
    id: uuid.UUID
    legacy_source: str
    direction: str
    kind: str
    number: str | None
    bank_name: str | None
    bank_account_ref: str | None
    cheque_book_ref: int | None
    amount: Decimal
    currency: str
    rate_to_rial: Decimal | None
    amount_rial: Decimal
    issue_date: date
    due_date: date | None
    counterparty_id: uuid.UUID
    counterparty_name: str
    purpose: str
    destination: str | None
    state: str
    origin_fiscal_year: int
    steps: list[Step] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)


@dataclass(frozen=True)
class Rejected:
    legacy_source: str
    reason: str


class _Builder:
    def __init__(self, direction: str, first_date: date, warnings: list[str]):
        self.direction = direction
        self.state: str | None = None
        self.steps: list[Step] = []
        self.last_date = first_date
        self.warnings = warnings

    def start(self, action: str, to_state: str, on: date, note: str | None = None) -> None:
        self.state = to_state
        self.steps.append(Step(1, action, None, to_state, on, note))

    def apply(self, action: str, on: date | None, what: str, note: str | None = None) -> None:
        key = (self.direction, self.state, action)
        if key not in TRANSITIONS:
            raise ValueError(f"step {action} not allowed from {self.state}")
        if on is None:
            self.warnings.append(f"{what} date missing; previous step date used")
            on = self.last_date
        if on < self.last_date:
            self.warnings.append(f"{what} date {on} is before the previous step ({self.last_date}); previous step date used")
            on = self.last_date
        to_state = TRANSITIONS[key]
        self.steps.append(Step(len(self.steps) + 1, action, self.state, to_state, on, note))
        self.state = to_state
        self.last_date = on


def counterparty_id_for(name: str) -> uuid.UUID:
    """Deterministic placeholder id until the Counterparties context is migrated (old Person/Tafno)."""
    return uuid.uuid5(_NAMESPACE, "counterparty:" + name.strip())


def map_cheque(row: LegacyCheque, source_db: str, currency_by_curid: dict[int, str] | None = None) -> MigratedInstrument | Rejected:
    source = f"{source_db}.Checks:{row.checkid}"
    if row.dp not in (1, 2):
        return Rejected(source, f"unknown Dp {row.dp}")
    if row.dp == 2 and row.checktype == CASH_PAYMENT_TYPE:
        return Rejected(source, "cash payment (Checktype 9) - migrated as a payment, not an instrument")
    kind = KIND_BY_TYPE[row.dp].get(row.checktype)
    if kind is None:
        return Rejected(source, f"unknown Checktype {row.checktype} for Dp {row.dp}")

    first = jalali.parse_legacy(row.d1)
    if first is None:
        return Rejected(source, f"invalid or missing D1 (date received/issued): {row.d1!r}")

    warnings: list[str] = []
    amount = Decimal(row.amount or 0)
    if amount <= 0:
        return Rejected(source, f"amount must be positive: {row.amount}")

    if row.iscur:
        code = (currency_by_curid or {}).get(row.curid or -1)
        if code is None:
            return Rejected(source, f"foreign currency Curid {row.curid} has no ISO code mapping")
        rate = Decimal(row.currate or 0)
        if rate <= 0:
            return Rejected(source, "foreign currency without a positive Currate")
        currency, amount_rial = code, (amount * rate).quantize(Decimal(1), ROUND_HALF_UP)
    else:
        currency, rate = "IRR", None
        if amount != amount.to_integral_value():
            warnings.append(f"Rial amount {amount} had decimals; rounded")
            amount = amount.quantize(Decimal(1), ROUND_HALF_UP)
        amount_rial = amount

    due = jalali.parse_legacy(row.maturity)
    if due is None and kind not in GUARANTEE_KINDS:
        warnings.append(f"due date missing or invalid: {row.maturity!r}")

    person = (row.person or "").strip()
    if not person:
        warnings.append("drawer/payee name missing")
        person = "(نامشخص)"
    purpose = (row.babat or "").strip()
    if not purpose:
        warnings.append("purpose (بابت) missing")
        purpose = "(نامشخص)"

    direction = RECEIVED if row.dp == 1 else ISSUED
    b = _Builder(direction, first, warnings)
    destination: str | None = None
    try:
        if direction == RECEIVED:
            b.start("Receive", "InCashDesk", first)
            d3, d4 = jalali.parse_legacy(row.d3), jalali.parse_legacy(row.d4)
            if row.stcode == 11:
                pass
            elif row.stcode in (12, 13, 14):
                destination = (row.vagno or "").strip() or None
                b.apply("DepositForCollection", d3, "D3 (sent for collection)")
                if row.stcode == 13:
                    b.apply("RecordCleared", d4, "D4 (collected)")
                    # Rule N2 (CEO, 10 Nov 2026): new clearings may not be dated before the due date. Old history is kept as
                    # recorded, but listed so the reconciliation shows how often the old system allowed it.
                    if due is not None and b.last_date < due:
                        warnings.append(f"cleared on {b.last_date}, before the due date {due} (rule N2); kept as recorded")
                elif row.stcode == 14:
                    b.apply("RecordBounced", d4, "D4 (bounced)", "legal action (GoInlaw)" if row.goinlaw else None)
                    if due is not None and b.last_date < due:  # rule N2 / Q11, as for clearings above
                        warnings.append(f"bounced on {b.last_date}, before the due date {due} (rule N2); kept as recorded")
            elif row.stcode == 15:
                b.apply("ReturnToIssuer", d3, "D3 (returned)")
            elif row.stcode == 16:
                destination = (row.vagno or "").strip() or None
                b.apply("Endorse", d3, "D3 (endorsed)")
            else:
                return Rejected(source, f"unknown received Stcode {row.stcode}")
        else:
            b.start("PrepareIssue", "BeingIssued", first)
            d2, d3, d4 = (jalali.parse_legacy(x) for x in (row.d2, row.d3, row.d4))
            if row.stcode == 20:
                pass
            elif row.stcode in (21, 22):
                b.apply("Issue", first, "D1 (issued)")
                later: list[tuple[date | None, int, str, str]] = []
                if row.stcode == 22:
                    later.append((d2, 0, "Deliver", "D2 (delivered)"))
                if row.ctocash and kind != "OrdinaryCheque":
                    later.append((d3, 1, "RecordMatured", "D3 (matured)"))
                for on, _, action, what in sorted(later, key=lambda x: (x[0] or b.last_date, x[1])):
                    b.apply(action, on, what)
                if row.regvos and row.vos:
                    b.apply("RecordPaidByBank", d4, "D4 (paid by bank)")
                elif row.regvos and row.backed:
                    b.apply("RecordBouncedAtBank", d4, "D4 (returned by bank)")
            elif row.stcode == 26:
                if (row.checkno or "").strip() and row.bundleid:
                    b.apply("Issue", first, "D1 (issued)")
                b.apply("Cancel", d2, "D2 (cancelled)", "cancelled in the old system")
            else:
                return Rejected(source, f"unknown issued Stcode {row.stcode}")
    except ValueError as e:
        return Rejected(source, str(e))

    return MigratedInstrument(
        id=uuid.uuid5(_NAMESPACE, source),
        legacy_source=source,
        direction=direction,
        kind=kind,
        number=(row.checkno or "").strip() or None,
        bank_name=(row.bank_name or "").strip() or None,
        bank_account_ref=(row.baccno or "").strip() or None,
        cheque_book_ref=row.bundleid,
        amount=amount,
        currency=currency,
        rate_to_rial=rate,
        amount_rial=amount_rial,
        issue_date=first,
        due_date=due,
        counterparty_id=counterparty_id_for(person),
        counterparty_name=person,
        purpose=purpose,
        destination=destination,
        state=b.state or "",
        origin_fiscal_year=jalali.jalali_year(first),
        steps=b.steps,
        warnings=warnings,
    )
