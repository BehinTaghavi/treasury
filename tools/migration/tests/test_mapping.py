import re
from datetime import date
from decimal import Decimal
from pathlib import Path

from treasury_migration.legacy_mapping import TRANSITIONS, LegacyCheque, MigratedInstrument, Rejected, map_cheque

SRC = "Cashnajoo86"


def received(stcode, **kw):
    base = dict(checkid=1, dp=1, checktype=0, stcode=stcode, checkno="123456", bank_name="ملت", amount=Decimal(5_000_000),
                person="مشتری الف", babat="فروش", maturity="13860801", d1="13860715")
    base.update(kw)
    return LegacyCheque(**base)


def issued(stcode, **kw):
    base = dict(checkid=2, dp=2, checktype=0, stcode=stcode, checkno="900100", amount=Decimal(3_000_000),
                person="تامین کننده", babat="خرید", maturity="13860720", d1="13860720", baccno="0101", bundleid=7)
    base.update(kw)
    return LegacyCheque(**base)


def ok(result) -> MigratedInstrument:
    assert isinstance(result, MigratedInstrument), result
    return result


def test_cheque_in_cash_desk():
    m = ok(map_cheque(received(11), SRC))
    assert (m.direction, m.kind, m.state) == ("Received", "OrdinaryCheque", "InCashDesk")
    assert m.issue_date == date(2007, 10, 7) and m.origin_fiscal_year == 1386
    assert [s.action for s in m.steps] == ["Receive"]
    assert m.legacy_source == "Cashnajoo86.Checks:1"


def test_collected_cheque_rebuilds_its_history_from_d3_and_d4():
    m = ok(map_cheque(received(13, d3="13860720", d4="13860801", vagno="ملت جاری"), SRC))
    assert [(s.action, s.to_state, s.event_date) for s in m.steps] == [
        ("Receive", "InCashDesk", date(2007, 10, 7)),
        ("DepositForCollection", "DepositedForCollection", date(2007, 10, 12)),
        ("RecordCleared", "Cleared", date(2007, 10, 23)),
    ]
    assert m.destination == "ملت جاری" and m.warnings == []


def test_bounced_cheque_in_legal_action_keeps_a_note():
    m = ok(map_cheque(received(14, d3="13860720", d4="13860801", goinlaw=True), SRC))
    assert m.state == "Bounced" and m.steps[-1].note == "legal action (GoInlaw)"


def test_returned_and_endorsed():
    assert ok(map_cheque(received(15, d3="13860716"), SRC)).state == "ReturnedToIssuer"
    e = ok(map_cheque(received(16, d3="13860716", vagno="پیمانکار ب"), SRC))
    assert e.state == "Endorsed" and e.destination == "پیمانکار ب"


def test_missing_step_date_uses_previous_date_with_warning():
    m = ok(map_cheque(received(12, d3=None), SRC))
    assert m.steps[1].event_date == m.steps[0].event_date
    assert any("D3" in w for w in m.warnings)


def test_step_dated_before_receipt_is_clamped_with_warning():
    m = ok(map_cheque(received(12, d3="13860701"), SRC))
    assert m.steps[1].event_date == m.issue_date
    assert any("before the previous step" in w for w in m.warnings)


def test_issued_and_delivered_cheque():
    m = ok(map_cheque(issued(22, d2="13860722"), SRC))
    assert [s.action for s in m.steps] == ["PrepareIssue", "Issue", "Deliver"]
    assert m.state == "Delivered" and m.bank_account_ref == "0101" and m.cheque_book_ref == 7


def test_post_dated_cheque_matured_and_paid_by_bank_in_date_order():
    m = ok(map_cheque(issued(22, checktype=2, maturity="13860901", d2="13860725", d3="13860901", d4="13860903",
                             ctocash=True, regvos=True, vos=True), SRC))
    assert (m.kind, m.state) == ("PostDatedCheque", "PaidByBank")
    assert [s.action for s in m.steps] == ["PrepareIssue", "Issue", "Deliver", "RecordMatured", "RecordPaidByBank"]


def test_maturity_before_delivery_is_kept_in_date_order():
    m = ok(map_cheque(issued(22, checktype=2, maturity="13860721", d2="13860725", d3="13860721", ctocash=True), SRC))
    assert [s.action for s in m.steps] == ["PrepareIssue", "Issue", "RecordMatured", "Deliver"]


def test_issued_cheque_returned_by_our_bank():
    assert ok(map_cheque(issued(21, d4="13860801", regvos=True, backed=True), SRC)).state == "BouncedAtBank"


def test_cancelled_with_and_without_a_sheet():
    with_sheet = ok(map_cheque(issued(26, d2="13860721"), SRC))
    assert [s.action for s in with_sheet.steps] == ["PrepareIssue", "Issue", "Cancel"]
    without = ok(map_cheque(issued(26, d2="13860721", checkno=None, bundleid=None), SRC))
    assert [s.action for s in without.steps] == ["PrepareIssue", "Cancel"]


def test_cash_payments_and_unknown_codes_are_rejected_with_a_reason():
    assert "cash payment" in map_cheque(issued(21, checktype=9), SRC).reason
    assert "Stcode 99" in map_cheque(received(99), SRC).reason
    assert "Checktype 7" in map_cheque(received(11, checktype=7), SRC).reason
    assert isinstance(map_cheque(received(11, d1="0000/00/00"), SRC), Rejected)
    assert isinstance(map_cheque(received(11, amount=Decimal(0)), SRC), Rejected)


def test_foreign_currency_needs_a_currency_mapping():
    row = received(11, iscur=True, curid=2, currate=Decimal(9300), amount=Decimal("100.5"))
    assert "Curid 2" in map_cheque(row, SRC).reason
    m = ok(map_cheque(row, SRC, {2: "USD"}))
    assert (m.currency, m.amount_rial) == ("USD", Decimal(934650))


def test_guarantee_items_need_no_due_date_but_others_warn():
    assert ok(map_cheque(received(11, checktype=1, maturity=None), SRC)).warnings == []
    assert any("due date" in w for w in ok(map_cheque(received(11, maturity=None), SRC)).warnings)


def test_ids_are_deterministic_so_reruns_do_not_duplicate():
    assert ok(map_cheque(received(11), SRC)).id == ok(map_cheque(received(11), SRC)).id
    assert ok(map_cheque(received(11), SRC)).id != ok(map_cheque(received(11), "Cashnajoo05")).id


def test_lifecycle_matches_dotnet():
    """The Python copy of the lifecycle must equal InstrumentLifecycle.All in the .NET domain."""
    cs = Path(__file__).resolve().parents[3] / "src/Services/Cashflow/Treasury.Cashflow.Domain/Instruments/InstrumentLifecycle.cs"
    pattern = r"new\(InstrumentDirection\.(\w+), InstrumentState\.(\w+), InstrumentAction\.(\w+), InstrumentState\.(\w+),"
    dotnet = {(d, f, a): t for d, f, a, t in re.findall(pattern, cs.read_text())}
    assert dotnet == TRANSITIONS
