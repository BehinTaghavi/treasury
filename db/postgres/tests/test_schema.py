"""Tests of the PostgreSQL schema (partitioning, closed years, cheque sheets, roles).

Run against an empty test database:
    TREASURY_TEST_DSN="host=localhost dbname=treasury_test user=postgres" pytest db/postgres/tests
The database is reset by applying the scripts with apply.sh first.
"""
import os
import subprocess
import threading
import time
import uuid
from datetime import date
from pathlib import Path

import psycopg
import pytest

DSN = os.environ.get("TREASURY_TEST_DSN", "host=localhost dbname=treasury_test user=postgres")
HERE = Path(__file__).resolve().parent.parent


@pytest.fixture(scope="session")
def db():
    params = psycopg.conninfo.conninfo_to_dict(DSN)
    name = params["dbname"]
    admin = psycopg.conninfo.make_conninfo(DSN, dbname="postgres")
    with psycopg.connect(admin, autocommit=True) as c:
        c.execute(f'DROP DATABASE IF EXISTS "{name}" WITH (FORCE)')
        c.execute(f'CREATE DATABASE "{name}"')
    env = dict(os.environ, PGHOST=params.get("host", ""), PGPORT=str(params.get("port", "5432")), PGUSER=params.get("user", "postgres"))
    subprocess.run([str(HERE / "apply.sh"), name], check=True, env=env, capture_output=True)
    with psycopg.connect(DSN, autocommit=True) as c:
        c.execute("SELECT cashflow.open_fiscal_year(1404, '2025-03-21', '2026-03-20')")
        c.execute("SELECT cashflow.open_fiscal_year(1405, '2026-03-21', '2027-03-20')")
    return DSN


def insert_instrument(conn, year, state="InCashDesk", direction="Received"):
    iid = uuid.uuid4()
    conn.execute(
        """INSERT INTO cashflow.instrument (origin_fiscal_year, id, direction, kind, number, bank_name, amount, amount_rial,
               issue_date, due_date, counterparty_id, counterparty_name, purpose, state)
           VALUES (%s, %s, %s, 'OrdinaryCheque', '123', 'ملت', 1000, 1000, %s, %s, %s, 'الف', 'فروش', %s)""",
        (year, iid, direction, date(2025, 4, 1), date(2025, 5, 1), uuid.uuid4(), state))
    return iid


def insert_event(conn, year, instrument_year, iid, seq=1, action="Receive", to_state="InCashDesk"):
    conn.execute(
        """INSERT INTO cashflow.instrument_event (fiscal_year, instrument_year, instrument_id, seq, action, to_state, event_date, user_id)
           VALUES (%s, %s, %s, %s, %s, %s, %s, 'test')""",
        (year, instrument_year, iid, seq, action, to_state, date(2026, 4, 1)))


def test_open_year_creates_partitions(db):
    with psycopg.connect(db) as c:
        names = {r[0] for r in c.execute("SELECT relname FROM pg_class WHERE relname LIKE 'instrument%fy14%'")}
    assert {"instrument_fy1404", "instrument_fy1405", "instrument_event_fy1404", "instrument_event_fy1405"} <= names


def test_rows_go_to_their_year_and_queries_touch_only_that_partition(db):
    with psycopg.connect(db) as c:
        a = insert_instrument(c, 1404)
        insert_instrument(c, 1405)
        assert c.execute("SELECT count(*) FROM cashflow.instrument_fy1404 WHERE id = %s", (a,)).fetchone()[0] == 1
        plan = "\n".join(r[0] for r in c.execute("EXPLAIN SELECT * FROM cashflow.instrument WHERE origin_fiscal_year = 1405 AND state = 'InCashDesk'"))
        assert "instrument_fy1405" in plan and "instrument_fy1404" not in plan


def test_a_year_that_was_never_opened_is_an_error(db):
    with psycopg.connect(db) as c:
        with pytest.raises(psycopg.errors.CheckViolation):
            insert_instrument(c, 1410)


def test_a_step_belongs_to_the_year_it_happens_in(db):
    with psycopg.connect(db) as c:
        iid = insert_instrument(c, 1404)
        insert_event(c, 1404, 1404, iid, 1)
        insert_event(c, 1405, 1404, iid, 2, "RecordCleared", "Cleared")
        rows = c.execute("SELECT fiscal_year FROM cashflow.instrument_event WHERE instrument_id = %s ORDER BY seq", (iid,)).fetchall()
    assert [r[0] for r in rows] == [1404, 1405]


def test_an_event_for_an_unknown_instrument_is_rejected(db):
    with psycopg.connect(db) as c:
        with pytest.raises(psycopg.errors.ForeignKeyViolation):
            insert_event(c, 1405, 1405, uuid.uuid4())


def test_the_same_sheet_can_never_be_used_twice_even_concurrently(db):
    book = uuid.uuid4()
    with psycopg.connect(db, autocommit=True) as c:
        c.execute("INSERT INTO cashflow.cheque_book (id, bank_account_id, first_sheet, last_sheet) VALUES (%s, %s, 1, 25)", (book, uuid.uuid4()))
    errors = []

    def use_sheet(delay):
        try:
            with psycopg.connect(db) as c:
                c.execute("SELECT 1 FROM cashflow.cheque_book WHERE id = %s FOR UPDATE", (book,))
                c.execute("INSERT INTO cashflow.cheque_sheet (cheque_book_id, sheet, sheet_use) VALUES (%s, 7, 'Issued')", (book,))
                time.sleep(delay)
        except psycopg.Error as e:
            errors.append(type(e).__name__)

    threads = [threading.Thread(target=use_sheet, args=(0.5,)), threading.Thread(target=use_sheet, args=(0,))]
    for t in threads:
        t.start()
        time.sleep(0.1)
    for t in threads:
        t.join()
    assert errors == ["UniqueViolation"]


def test_closing_a_year_freezes_its_steps_but_not_its_open_instruments(db):
    with psycopg.connect(db, autocommit=True) as c:
        c.execute("SELECT cashflow.open_fiscal_year(1403, '2024-03-20', '2025-03-20')")
        iid = insert_instrument(c, 1403)
        insert_event(c, 1403, 1403, iid, 1)
        c.execute("SELECT cashflow.close_fiscal_year(1403)")
        with pytest.raises(psycopg.errors.RaiseException, match="closed"):
            insert_event(c, 1403, 1403, iid, 2, "RecordCleared", "Cleared")
        with pytest.raises(psycopg.errors.RaiseException):
            c.execute("DELETE FROM cashflow.instrument_event WHERE fiscal_year = 1403")
        # The cheque itself can still change state; the new step goes into the open year.
        c.execute("UPDATE cashflow.instrument SET state = 'Cleared', version = version + 1 WHERE id = %s", (iid,))
        insert_event(c, 1405, 1403, iid, 2, "RecordCleared", "Cleared")


def test_fiscal_years_may_not_overlap(db):
    with psycopg.connect(db) as c:
        c.execute("INSERT INTO reference.fiscal_year (id, starts_on, ends_on) VALUES (1405, '2026-03-21', '2027-03-20')")
        with pytest.raises(psycopg.errors.ExclusionViolation):
            c.execute("INSERT INTO reference.fiscal_year (id, starts_on, ends_on) VALUES (1406, '2027-03-01', '2028-03-19')")


def test_the_cashflow_service_role_cannot_read_another_services_schema(db):
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        c.execute("SELECT count(*) FROM cashflow.instrument")
        with pytest.raises(psycopg.errors.InsufficientPrivilege):
            c.execute("SELECT count(*) FROM reference.fiscal_year")


def test_a_rial_amount_has_no_exchange_rate(db):
    with psycopg.connect(db) as c:
        with pytest.raises(psycopg.errors.CheckViolation):
            c.execute(
                """INSERT INTO cashflow.instrument (origin_fiscal_year, id, direction, kind, amount, currency, rate_to_rial, amount_rial,
                       issue_date, counterparty_id, counterparty_name, purpose, state)
                   VALUES (1405, %s, 'Received', 'OrdinaryCheque', 10, 'IRR', 5, 10, '2026-04-01', %s, 'x', 'y', 'InCashDesk')""",
                (uuid.uuid4(), uuid.uuid4()))


def test_the_scripts_can_run_again_after_years_were_opened_by_an_administrator(db):
    # Every restart of deploy/docker-compose.yml re-runs apply.sh; it failed with "permission denied for table instrument_fy1405".
    params = psycopg.conninfo.conninfo_to_dict(db)
    env = dict(os.environ, PGHOST=params.get("host", ""), PGPORT=str(params.get("port", "5432")), PGUSER=params.get("user", "postgres"))
    result = subprocess.run([str(HERE / "apply.sh"), params["dbname"]], env=env, capture_output=True, text=True)
    assert result.returncode == 0, result.stderr
    with psycopg.connect(db) as c:
        owners = {r[0] for r in c.execute(
            "SELECT pg_get_userbyid(relowner) FROM pg_class WHERE relnamespace = 'cashflow'::regnamespace AND relname LIKE 'instrument%fy14%'")}
    assert owners == {"treasury_owner"}


def test_the_scripts_run_again_over_rows_saved_with_the_newest_values(db):
    # 19 Nov 2026: after a cheque was registered in Sayad, the next start stopped in 031 ("instrument_state_known ... is violated"):
    # 031 rewrote the list of states without SayadRegistered, which 033 added. Every list a script rewrites is now the whole list.
    params = psycopg.conninfo.conninfo_to_dict(db)
    env = dict(os.environ, PGHOST=params.get("host", ""), PGPORT=str(params.get("port", "5432")), PGUSER=params.get("user", "postgres"))
    ids = [uuid.uuid4(), uuid.uuid4()]
    with psycopg.connect(db, autocommit=True) as c:
        for i, state in zip(ids, ("SayadRegistered", "InLegal")):
            c.execute("""INSERT INTO cashflow.instrument (origin_fiscal_year, id, direction, kind, number, bank_name, amount, amount_rial,
                         issue_date, counterparty_id, counterparty_name, purpose, state)
                         VALUES (1405, %s, 'Received', 'OrdinaryCheque', 'R-1', 'b', 10, 10, '2026-04-01', %s, 'x', 'y', %s)""",
                      (i, uuid.uuid4(), state))
        # 20 Nov 2026: a receipt of the newest kind, «واریز به کارت بانک» (033 rewrites the lists of kinds, 045 the account kinds).
        card_receipt = uuid.uuid4()
        c.execute("""INSERT INTO cashflow.receipt (fiscal_year, id, kind, number, receipt_date, payer_id, payer_name, payer_detail_code, purpose_group_id,
                     purpose_group_name, purpose, recorded_by, account_kind, account_id, deposit_method, amount, currency, total_rial, state)
                     VALUES (1405, %s, 'CardDeposit', 990077, '2026-11-20', %s, 'x', '200015', %s, 'y', 'y', 'test', 'PettyCashCard', %s, 'CardToCard', 10, 'IRR', 10, 'Recorded')""",
                  (card_receipt, uuid.uuid4(), uuid.uuid4(), uuid.uuid4()))
        # 20 Nov 2026 (046): «وجوه در راه» saved in the template; 031, 033, 035 and 038 rewrite the list of its keys at every start.
        keys = ("ProtestedChequesInLegal", "GuaranteeChequesInLegal", "GuaranteeNotesForCompany", "NotesInCollection", "FundsInTransit")
        for k in keys:
            c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES (%s, '9999') ON CONFLICT DO NOTHING", (k,))
        try:
            result = subprocess.run([str(HERE / "apply.sh"), params["dbname"]], env=env, capture_output=True, text=True)
            assert result.returncode == 0, result.stderr[-2000:]
        finally:
            c.execute("DELETE FROM cashflow.instrument WHERE id = ANY(%s)", (ids,))
            c.execute("DELETE FROM cashflow.voucher_account WHERE ledger_account_code = '9999'")
            c.execute("DELETE FROM cashflow.receipt WHERE id = %s", (card_receipt,))


def test_a_name_is_unique_per_list_and_parent_and_a_currency_code_once(db):
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        country, a, b = uuid.uuid4(), uuid.uuid4(), uuid.uuid4()
        add = "INSERT INTO reference.catalogue_item (id, kind, parent_id, name, name_key, code) VALUES (%s, %s, %s, %s, %s, %s)"
        c.execute(add, (country, "Country", None, "ایران", "ایران", None))
        c.execute(add, (a, "Province", country, "فارس", "فارس", None))
        c.execute(add, (b, "Province", country, "اصفهان", "اصفهان", None))
        c.execute(add, (uuid.uuid4(), "City", a, "نیک‌آباد", "نیکآباد", None))
        c.execute(add, (uuid.uuid4(), "City", b, "نیک‌آباد", "نیکآباد", None))  # same name, other province: allowed
        c.execute(add, (uuid.uuid4(), "Currency", None, "دلار", "دلار", "USD"))
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "Country", None, "ايران", "ایران", None))  # same key, no parent
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "Currency", None, "دلار آمریکا", "دلارآمریکا", "USD"))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "City", None, "کرج", "کرج", None))  # a city needs its province


def test_the_reference_service_role_cannot_read_the_cashflow_schema(db):
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        with pytest.raises(psycopg.errors.InsufficientPrivilege):
            c.execute("SELECT count(*) FROM cashflow.instrument")


def test_the_accounts_service_role_owns_bank_accounts_and_sees_no_other_schema(db):
    with psycopg.connect(db) as c:
        c.execute("SET ROLE accounts_svc")
        main = uuid.uuid4()
        add = """INSERT INTO accounts.bank_account (id, account_number, number_key, bank_id, currency, main_account_id, ledger_account,
                     collection_priority, is_facility, is_active) VALUES (%s, %s, %s, %s, 'IRR', %s, '1103', NULL, false, true)"""
        c.execute(add, (main, "0101-1", "01011", uuid.uuid4(), None))
        c.execute(add, (uuid.uuid4(), "0101-2", "01012", uuid.uuid4(), main))
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "0101 1", "01011", uuid.uuid4(), None))  # same number, other spelling
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "0101-3", "01013", uuid.uuid4(), main))  # second backup of the same main
        with pytest.raises(psycopg.errors.InsufficientPrivilege):
            with c.transaction():
                c.execute("SELECT count(*) FROM reference.catalogue_item")


def test_collection_priority_is_a_number_from_1_to_5(db):
    # Owner's report of 11 Nov 2026, item 15 (008_accounts_collection_priority.sql).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE accounts_svc")
        add = """INSERT INTO accounts.bank_account (id, account_number, number_key, bank_id, currency, ledger_account,
                     collection_priority, is_facility, is_active) VALUES (%s, %s, %s, %s, 'IRR', '1103', %s, false, true)"""
        c.execute(add, (uuid.uuid4(), "777-1", "7771", uuid.uuid4(), 1))
        c.execute(add, (uuid.uuid4(), "777-5", "7775", uuid.uuid4(), 5))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "777-6", "7776", uuid.uuid4(), 6))


def test_an_old_yes_no_priority_becomes_priority_1(db):
    # A database created before 008 (e.g. the owner's test PC) has a boolean column; the script turns "yes" into 1, "no" into none.
    # Runs inside one transaction that is rolled back (DDL is transactional in PostgreSQL).
    script = (HERE / "008_accounts_collection_priority.sql").read_text(encoding="utf-8")
    with psycopg.connect(db) as c:
        c.execute("ALTER TABLE accounts.bank_account DROP CONSTRAINT bank_account_collection_priority_range")
        c.execute("ALTER TABLE accounts.bank_account ALTER COLUMN collection_priority TYPE boolean USING collection_priority IS NOT NULL")
        yes, no = uuid.uuid4(), uuid.uuid4()
        add = """INSERT INTO accounts.bank_account (id, account_number, number_key, bank_id, currency, ledger_account,
                     collection_priority, is_facility, is_active) VALUES (%s, %s, %s, %s, 'IRR', '1103', %s, false, true)"""
        c.execute(add, (yes, "888-1", "8881", uuid.uuid4(), True))
        c.execute(add, (no, "888-2", "8882", uuid.uuid4(), False))
        c.execute(script)
        c.execute(script)  # safe to run again
        got = dict(c.execute("SELECT id, collection_priority FROM accounts.bank_account WHERE id IN (%s, %s)", (yes, no)).fetchall())
        assert got == {yes: 1, no: None}
        c.rollback()


def test_the_reference_service_may_delete_an_unused_entry_but_not_one_with_entries_under_it(db):
    # Owner's report of 11 Nov 2026, items 6 and 9 (007_reference_catalogue_corrections.sql).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        add = "INSERT INTO reference.catalogue_item (id, kind, parent_id, name, name_key, code) VALUES (%s, %s, %s, %s, %s, %s)"
        country, province, bank = uuid.uuid4(), uuid.uuid4(), uuid.uuid4()
        c.execute(add, (country, "Country", None, "کشور آزمایشی", "کشورآزمایشی", None))
        c.execute(add, (province, "Province", country, "استان آزمایشی", "استانآزمایشی", None))
        c.execute(add, (bank, "Bank", None, "بانک آزمایشی", "بانکآزمایشی", None))
        assert c.execute("SELECT in_use FROM reference.catalogue_item WHERE id = %s", (bank,)).fetchone()[0] is False
        c.execute("DELETE FROM reference.catalogue_item WHERE id = %s", (bank,))
        with pytest.raises(psycopg.errors.ForeignKeyViolation):
            with c.transaction():
                c.execute("DELETE FROM reference.catalogue_item WHERE id = %s", (country,))


def test_entries_named_by_existing_bank_accounts_are_marked_in_use_by_the_upgrade(db):
    # 007 catches up databases where bank accounts were saved before in_use existed. Rolled back afterwards.
    script = (HERE / "007_reference_catalogue_corrections.sql").read_text(encoding="utf-8")
    with psycopg.connect(db) as c:
        add = "INSERT INTO reference.catalogue_item (id, kind, parent_id, name, name_key, code) VALUES (%s, %s, %s, %s, %s, %s)"
        bank, other, eur = uuid.uuid4(), uuid.uuid4(), uuid.uuid4()
        c.execute(add, (bank, "Bank", None, "بانک قدیمی", "بانکقدیمی", None))
        c.execute(add, (other, "Bank", None, "بانک بی‌حساب", "بانکبیحساب", None))
        c.execute("DELETE FROM reference.catalogue_item WHERE code = 'EUR'")
        c.execute(add, (eur, "Currency", None, "یورو", "یورو", "EUR"))
        c.execute("""INSERT INTO accounts.bank_account (id, account_number, number_key, bank_id, currency, ledger_account,
                     collection_priority, is_facility, is_active) VALUES (%s, '999-1', '9991', %s, 'EUR', '1103', NULL, false, true)""",
                  (uuid.uuid4(), bank))
        c.execute(script)
        got = dict(c.execute("SELECT id, in_use FROM reference.catalogue_item WHERE id IN (%s, %s, %s)", (bank, other, eur)).fetchall())
        assert got == {bank: True, other: False, eur: True}
        c.rollback()


def test_a_purpose_group_keeps_its_reference_and_its_purposes(db):
    # 009_reference_purposes.sql: a reference named by a group and a group with purposes cannot be deleted; same description once per direction.
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        ref, group, purpose = uuid.uuid4(), uuid.uuid4(), uuid.uuid4()
        c.execute("INSERT INTO reference.catalogue_item (id, kind, name, name_key) VALUES (%s, 'Reference', 'شماره آزمایشی', 'شمارهآزمایشی')", (ref,))
        add = """INSERT INTO reference.purpose_group (id, number, direction, name, name_key, ledger_account, reference1_id)
                 VALUES (%s, %s, %s, %s, %s, '2101', %s)"""
        c.execute(add, (group, 900001, "Receipt", "گروه آزمایشی", "گروهآزمایشی", ref))
        c.execute(add, (uuid.uuid4(), 900002, "Payment", "گروه آزمایشی", "گروهآزمایشی", None))  # other direction: allowed
        c.execute("INSERT INTO reference.purpose (id, group_id, name, name_key) VALUES (%s, %s, 'بابت آزمایشی', 'بابتآزمایشی')", (purpose, group))
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), 900003, "Receipt", "گروه  آزمایشی", "گروهآزمایشی", None))
        with pytest.raises(psycopg.errors.ForeignKeyViolation):
            with c.transaction():
                c.execute("DELETE FROM reference.catalogue_item WHERE id = %s", (ref,))
        with pytest.raises(psycopg.errors.ForeignKeyViolation):
            with c.transaction():
                c.execute("DELETE FROM reference.purpose_group WHERE id = %s", (group,))
        c.execute("DELETE FROM reference.purpose WHERE id = %s", (purpose,))
        c.execute("DELETE FROM reference.purpose_group WHERE id = %s", (group,))


def test_departments_requesters_and_beneficiaries_keep_their_rules(db):
    # 012_reference_organization.sql: numbers once, a department with requesters stays, Sheba shape, account number needs a bank.
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        dep = uuid.uuid4()
        c.execute("INSERT INTO reference.department (id, number, name, name_key, cost_centers) VALUES (%s, 990001, 'بخش آزمایشی', 'بخشآزمایشی', '{770100}')", (dep,))
        c.execute("INSERT INTO reference.requester (id, number, full_name, department_id, cost_center) VALUES (%s, 990001, 'شخص', %s, '770100')", (uuid.uuid4(), dep))
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute("INSERT INTO reference.department (id, number, name, name_key) VALUES (%s, 990001, 'دیگر', 'دیگر')", (uuid.uuid4(),))
        with pytest.raises(psycopg.errors.ForeignKeyViolation):
            with c.transaction():
                c.execute("DELETE FROM reference.department WHERE id = %s", (dep,))
        add = "INSERT INTO reference.beneficiary (id, detail_code, legal_name, account_number, sheba) VALUES (%s, %s, 'ذینفع', %s, %s)"
        c.execute(add, (uuid.uuid4(), "990001", None, "IR460120000000003123456789"))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "990002", None, "IR4601"))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "990003", "0101-55", None))  # account number without its bank
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "990001", None, None))  # one beneficiary per تفصیل


def test_petty_cash_amounts_fit_together_and_one_per_holder(db):
    # 013_accounts_petty_cash.sql
    with psycopg.connect(db) as c:
        c.execute("SET ROLE accounts_svc")
        add = """INSERT INTO accounts.petty_cash (id, holder_detail, ledger_account, amount, settle_limit, pay_ceiling, recovery)
                 VALUES (%s, %s, '1102', %s, %s, %s, 'Cash')"""
        c.execute(add, (uuid.uuid4(), "880001", 1000, 800, 100))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "880002", 1000, 1001, 100))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "880003", 1000, 800, 1001))
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "880001", 1000, 800, 100))


def test_a_cheque_type_once_per_account_and_kept_while_a_book_uses_it(db):
    # 014_cashflow_cheque_formats.sql
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        account, fmt = uuid.uuid4(), uuid.uuid4()
        add = "INSERT INTO cashflow.cheque_format (id, number, bank_account_id, print_type) VALUES (%s, %s, %s, %s)"
        c.execute(add, (fmt, 970001, account, "NewLarge"))
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), 970002, account, "NewLarge"))
        c.execute("INSERT INTO cashflow.cheque_format_image (format_id, data) VALUES (%s, %s)", (fmt, b"\x89PNG"))
        c.execute("INSERT INTO cashflow.cheque_book (id, bank_account_id, first_sheet, last_sheet, format_id) VALUES (%s, %s, 1, 10, %s)", (uuid.uuid4(), account, fmt))
        with pytest.raises(psycopg.errors.ForeignKeyViolation):
            with c.transaction():
                c.execute("DELETE FROM cashflow.cheque_format WHERE id = %s", (fmt,))


def test_sayad_numbers_of_a_cheque_book_come_together_or_not_at_all(db):
    # 015_cashflow_cheque_book_sayad.sql: 8 middle digits and a 4-digit first serial; old books may have neither.
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        add = "INSERT INTO cashflow.cheque_book (id, bank_account_id, serial, first_sheet, last_sheet, sayad_middle, sayad_first) VALUES (%s, %s, '1234', 567801, 567850, %s, %s)"
        c.execute(add, (uuid.uuid4(), uuid.uuid4(), "12345678", 1))
        c.execute(add, (uuid.uuid4(), uuid.uuid4(), None, None))
        for middle, first in [("12345678", None), ("1234567", 1), ("12345678", 10000)]:
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute(add, (uuid.uuid4(), uuid.uuid4(), middle, first))


def test_books_registered_before_the_size_rule_with_another_size_become_partly_used(db):
    # 016_cashflow_cheque_book_partly_used.sql: a new book has 25, 50 or 100 sheets (owner, 12 Nov 2026); an older 30-sheet book is
    # marked partly used so that it can still be corrected. Rolled back at the end.
    script = (HERE / "016_cashflow_cheque_book_partly_used.sql").read_text(encoding="utf-8")
    with psycopg.connect(db) as c:
        add = "INSERT INTO cashflow.cheque_book (id, bank_account_id, serial, first_sheet, last_sheet) VALUES (%s, %s, '1234', 567801, %s)"
        full, odd = uuid.uuid4(), uuid.uuid4()
        c.execute(add, (full, uuid.uuid4(), 567850))
        c.execute(add, (odd, uuid.uuid4(), 567830))
        c.execute(script)
        c.execute(script)  # safe to run again
        got = dict(c.execute("SELECT id, partly_used FROM cashflow.cheque_book WHERE id IN (%s, %s)", (full, odd)).fetchall())
        assert got == {full: False, odd: True}
        c.rollback()


def test_a_cheque_book_keeps_the_digits_of_its_serial(db):
    # 017_cashflow_cheque_book_serial_digits.sql: 056780 is stored as 56780 with 6 digits (owner, 12 Nov 2026); empty for older books.
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        add = "INSERT INTO cashflow.cheque_book (id, bank_account_id, serial, first_sheet, last_sheet, serial_digits) VALUES (%s, %s, '1234', 56701, 56725, %s)"
        c.execute(add, (uuid.uuid4(), uuid.uuid4(), 6))
        c.execute(add, (uuid.uuid4(), uuid.uuid4(), None))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), uuid.uuid4(), 0))


def test_petty_cash_ceiling_at_most_limit_below_amount_and_top_up_by_card(db):
    # 018_accounts_petty_cash_and_cash_desk_details.sql (owner's report no. 2, items 4, 11, 12, 13).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE accounts_svc")
        add = """INSERT INTO accounts.petty_cash (id, holder_detail, ledger_account, amount, settle_limit, pay_ceiling, recovery, created_on, keeping)
                 VALUES (%s, %s, '1102', %s, %s, %s, %s, '2026-11-13', '{"Cash": true}')"""
        c.execute(add, (uuid.uuid4(), "881001", 1000, 800, 800, "BankCard"))
        for holder, amount, limit, ceiling in [("881002", 1000, 1000, 100), ("881003", 1000, 500, 600)]:
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute(add, (uuid.uuid4(), holder, amount, limit, ceiling, "Cash"))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "881004", 1000, 800, 100, "Transfer"))
        c.rollback()


def test_a_beneficiary_keeps_a_ten_or_eleven_digit_national_id_and_a_ten_digit_postal_code(db):
    # 019_reference_beneficiary_identity.sql (owner's report no. 2, item 7).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        add = """INSERT INTO reference.beneficiary (id, detail_code, legal_name, person_kind, national_id, postal_code)
                 VALUES (%s, %s, 'ذینفع', %s, %s, %s)"""
        c.execute(add, (uuid.uuid4(), "882001", "Natural", "0084575948", "1234567890"))
        c.execute(add, (uuid.uuid4(), "882002", "Legal", "10101010103", None))
        for detail, kind, nid, postal in [("882003", "Other", "0084575948", None), ("882004", "Natural", "12345", None), ("882005", "Natural", "0084575948", "123")]:
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute(add, (uuid.uuid4(), detail, kind, nid, postal))
        c.rollback()


def test_a_requester_may_have_no_department_and_keeps_its_staff_detail(db):
    # 020_reference_requester_person.sql (owner, 13 Nov 2026: Iran Nazho Co. has no departments).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        c.execute("INSERT INTO reference.requester (id, number, full_name, department_id, cost_center, person_detail) VALUES (%s, 883001, 'علی', NULL, '770100', '600046')",
                  (uuid.uuid4(),))
        c.rollback()


def test_a_bank_account_knows_whether_it_is_in_use_and_unused_ones_can_be_deleted(db):
    # 021_accounts_bank_account_in_use.sql (owner's report no. 3, item 1).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE accounts_svc")
        acc = uuid.uuid4()
        c.execute("""INSERT INTO accounts.bank_account (id, account_number, number_key, bank_id, currency, ledger_account, is_facility, is_active)
                     VALUES (%s, '884-1', '8841', %s, 'IRR', '1103', false, true)""", (acc, uuid.uuid4()))
        assert c.execute("SELECT in_use FROM accounts.bank_account WHERE id = %s", (acc,)).fetchone()[0] is False
        c.execute("DELETE FROM accounts.bank_account WHERE id = %s", (acc,))
        c.rollback()


def test_a_standard_cheque_type_has_no_account_and_each_print_type_once(db):
    # 022_cashflow_standard_cheque_format.sql (owner's report no. 3, item 3).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        add = "INSERT INTO cashflow.cheque_format (id, number, bank_account_id, print_type) VALUES (%s, %s, %s, %s)"
        c.execute(add, (uuid.uuid4(), 980001, None, "NewestSmall"))
        c.execute(add, (uuid.uuid4(), 980004, None, "Standard"))  # 023: the Standard print type
        c.execute(add, (uuid.uuid4(), 980002, uuid.uuid4(), "NewestSmall"))
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), 980003, None, "NewestSmall"))
        c.rollback()


def test_an_opening_balance_once_per_account_and_year_and_cash_never_below_zero(db):
    # 024_accounts_opening_balances.sql (owner, 14 Nov 2026).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE accounts_svc")
        add = """INSERT INTO accounts.opening_balance (id, account_kind, account_id, fiscal_year, balance_date, amount, rial_equivalent)
                 VALUES (%s, %s, %s, 1405, '2026-11-15', %s, %s)"""
        bank = uuid.uuid4()
        c.execute(add, (uuid.uuid4(), "BankAccount", bank, -100, None))  # a bank account may be overdrawn
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "BankAccount", bank, 5, None))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "CashDesk", uuid.uuid4(), -1, None))
        c.rollback()


def test_receipts_are_partitioned_numbered_once_per_kind_and_read_only_in_a_closed_year(db):
    # 025_cashflow_receipts.sql (owner, 14 Nov 2026: receipts of cash, bank deposits, cheques and notes).
    add = """INSERT INTO cashflow.receipt (fiscal_year, id, kind, number, receipt_date, payer_id, payer_name, payer_detail_code,
                 purpose_group_id, purpose_group_name, purpose, recorded_by, account_kind, account_id, deposit_method, amount, currency,
                 instrument_ids, total_rial, state)
             VALUES (%s, %s, %s, %s, %s, %s, 'الف', '200015', %s, 'فروش', 'فروش', 'test', %s, %s, %s, %s, %s, %s, %s, 'Recorded')"""
    with psycopg.connect(db) as c:
        names = {r[0] for r in c.execute("SELECT relname FROM pg_class WHERE relname LIKE 'receipt_fy14%'")}
        assert {"receipt_fy1404", "receipt_fy1405"} <= names
        c.execute("SET ROLE cashflow_svc")

        def cash(year, number, day, account_kind="CashDesk", method=None):
            c.execute(add, (year, uuid.uuid4(), "Cash", number, day, uuid.uuid4(), uuid.uuid4(), account_kind, uuid.uuid4(), method, 1000, "IRR", [], 1000))

        cash(1405, 1, date(2026, 11, 14))
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                cash(1405, 1, date(2026, 11, 15))
        with pytest.raises(psycopg.errors.CheckViolation):  # cash goes into a cash desk
            with c.transaction():
                cash(1405, 2, date(2026, 11, 15), "BankAccount", "Pos")
        with pytest.raises(psycopg.errors.CheckViolation):  # a receipt of cheques names them
            with c.transaction():
                c.execute(add, (1405, uuid.uuid4(), "Instruments", 1, date(2026, 11, 15), uuid.uuid4(), uuid.uuid4(), None, None, None, None, None, [], 1000))
        c.execute(add, (1405, uuid.uuid4(), "Instruments", 1, date(2026, 11, 15), uuid.uuid4(), uuid.uuid4(), None, None, None, None, None, [uuid.uuid4()], 1000))
        c.execute("""INSERT INTO cashflow.receipt_number (fiscal_year, kind, last_number) VALUES (1405, 'Cash', 1)
                     ON CONFLICT (fiscal_year, kind) DO UPDATE SET last_number = cashflow.receipt_number.last_number + 1""")
        cash(1404, 1, date(2025, 11, 14))
        c.commit()

    with psycopg.connect(db, autocommit=True) as c:
        c.execute("SELECT cashflow.close_fiscal_year(1404)")
    try:
        with psycopg.connect(db) as c:
            with pytest.raises(psycopg.errors.RaiseException):
                c.execute("UPDATE cashflow.receipt SET state = 'Cancelled', cancelled_on = receipt_date, cancel_reason = 'x' WHERE fiscal_year = 1404")
    finally:
        with psycopg.connect(db, autocommit=True) as c:
            c.execute("UPDATE cashflow.fiscal_period SET status = 'Open' WHERE fiscal_year = 1404")
            for t in ("cashflow.instrument_event_fy1404", "cashflow.receipt_fy1404", "cashflow.voucher_fy1404", "cashflow.instrument_file_fy1404", "cashflow.bank_transfer_fy1404",
                      "requests.payment_request_fy1404", "requests.request_file_fy1404"):
                c.execute(f"DROP TRIGGER IF EXISTS closed_year_guard ON {t}")


def test_payment_requests_have_their_own_schema_numbers_and_closed_years(db):
    # 026_requests.sql (owner, 15 Nov 2026: payment requests).
    add = """INSERT INTO requests.payment_request (fiscal_year, id, number, request_date, requester_id, requester_name, beneficiary_id,
                 beneficiary_detail_code, beneficiary_name, purpose_group_id, purpose_group_name, purpose, amount, currency, method, priority,
                 transfer_account, card_number, state, recorded_by)
             VALUES (%s, %s, %s, %s, %s, 'الف', %s, '300200', 'ب', %s, 'خرید', 'خرید', %s, 'IRR', %s, 1, %s, %s, 'Registered', 'test')"""
    with psycopg.connect(db) as c:
        names = {r[0] for r in c.execute("SELECT relname FROM pg_class WHERE relname LIKE 'payment_request_fy14%'")}
        assert {"payment_request_fy1404", "payment_request_fy1405"} <= names
        c.execute("SET ROLE requests_svc")

        def request(number, method="Cash", transfer=None, card=None, amount=1000, year=1405, day=date(2026, 11, 15)):
            c.execute(add, (year, uuid.uuid4(), number, day, uuid.uuid4(), uuid.uuid4(), uuid.uuid4(), amount, method, transfer, card))

        request(1)
        for bad, error in (
            (lambda: request(1), psycopg.errors.UniqueViolation),             # one number per year
            (lambda: request(2, "Transfer"), psycopg.errors.CheckViolation),  # a transfer names the receiving account
            (lambda: request(3, card="6037991234567893"), psycopg.errors.CheckViolation),  # a card only for card-to-card
            (lambda: request(4, amount=10.5), psycopg.errors.CheckViolation),  # Rial amounts are whole
        ):
            with pytest.raises(error):
                with c.transaction():
                    bad()
        with pytest.raises(psycopg.errors.InsufficientPrivilege):  # the service cannot read another service's schema
            with c.transaction():
                c.execute("SELECT 1 FROM cashflow.receipt")
        request(1, year=1404, day=date(2025, 11, 15))
        c.commit()

    with psycopg.connect(db, autocommit=True) as c:
        c.execute("SELECT cashflow.close_fiscal_year(1404)")
    try:
        with psycopg.connect(db) as c:
            with pytest.raises(psycopg.errors.RaiseException):
                c.execute("UPDATE requests.payment_request SET priority = 2 WHERE fiscal_year = 1404")
    finally:
        with psycopg.connect(db, autocommit=True) as c:
            c.execute("UPDATE cashflow.fiscal_period SET status = 'Open' WHERE fiscal_year = 1404")
            for t in ("cashflow.instrument_event_fy1404", "cashflow.receipt_fy1404", "cashflow.voucher_fy1404", "cashflow.instrument_file_fy1404", "cashflow.bank_transfer_fy1404",
                      "requests.payment_request_fy1404", "requests.request_file_fy1404"):
                c.execute(f"DROP TRIGGER IF EXISTS closed_year_guard ON {t}")


def test_bank_account_types_and_one_account_per_bank_card(db):
    # 027_accounts_bank_account_types.sql (owner's report no. 4, items 1, 2 and 4).
    add = """INSERT INTO accounts.bank_account (id, account_number, number_key, bank_id, currency, ledger_account, collection_priority, is_facility,
                 is_active, account_type, term) VALUES (%s, %s, %s, %s, %s, '1103', NULL, false, true, %s, %s)"""
    with psycopg.connect(db) as c:
        c.execute("SET ROLE accounts_svc")
        a = uuid.uuid4()
        c.execute(add, (a, "T-1", "9901", uuid.uuid4(), "IRR", "ShortTermDeposit", "SixMonths"))
        assert c.execute("SELECT account_type FROM accounts.bank_account WHERE number_key = '9901'").fetchone()[0] == "ShortTermDeposit"
        for row, error in (
            (("T-2", "9902", "IRR", "ShortTermDeposit", None), psycopg.errors.CheckViolation),       # a short-term deposit has its term
            (("T-3", "9903", "USD", "ShortTermDeposit", "Ordinary"), psycopg.errors.CheckViolation),  # and is in Rial
            (("T-4", "9904", "IRR", "SavingsQardHasan", "Ordinary"), psycopg.errors.CheckViolation),  # only short-term deposits have a term
        ):
            with pytest.raises(error):
                with c.transaction():
                    c.execute(add, (uuid.uuid4(), row[0], row[1], uuid.uuid4(), row[2], row[3], row[4]))
        card = "INSERT INTO accounts.bank_card (card_number, bank_account_id, holder_name, position) VALUES (%s, %s, 'x', 0)"
        c.execute(card, ("6037991234567893", a))
        b = uuid.uuid4()
        c.execute(add, (b, "T-5", "9905", uuid.uuid4(), "IRR", "CurrentQardHasan", None))
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(card, ("6037991234567893", b))
        c.execute("DELETE FROM accounts.bank_account WHERE id = %s", (a,))  # cards go with their account
        assert c.execute("SELECT count(*) FROM accounts.bank_card WHERE card_number = '6037991234567893'").fetchone()[0] == 0
        c.rollback()


def test_a_voucher_is_confirmed_once_per_receipt_or_cheque_step_and_read_only_in_a_closed_year(db):
    # 030_cashflow_vouchers.sql (owner's report no. 4, item 8: "one voucher for each receipt or operation, Treasurer confirms").
    add = """INSERT INTO cashflow.voucher (fiscal_year, id, source, source_id, step, operation, voucher_date, lines, total_rial, confirmed_by, confirmed_at)
             VALUES (%s, %s, %s, %s, %s, 'دریافت نقدی', %s, '[]', 1000, 'treasurer', now())"""
    with psycopg.connect(db) as c:
        names = {r[0] for r in c.execute("SELECT relname FROM pg_class WHERE relname LIKE 'voucher_fy14%'")}
        assert {"voucher_fy1404", "voucher_fy1405"} <= names
        c.execute("SET ROLE cashflow_svc")
        receipt, cheque = uuid.uuid4(), uuid.uuid4()
        c.execute(add, (1405, uuid.uuid4(), "Receipt", receipt, 0, date(2026, 11, 15)))
        c.execute(add, (1405, uuid.uuid4(), "ChequeStep", cheque, 2, date(2026, 11, 15)))
        for row, error in (
            (("Receipt", receipt, 0), psycopg.errors.UniqueViolation),    # one voucher per receipt
            (("ChequeStep", cheque, 2), psycopg.errors.UniqueViolation),  # and per cheque step
            (("Receipt", uuid.uuid4(), 1), psycopg.errors.CheckViolation),  # a receipt's voucher is step 0
            (("ChequeStep", uuid.uuid4(), 0), psycopg.errors.CheckViolation),
        ):
            with pytest.raises(error):
                with c.transaction():
                    c.execute(add, (1405, uuid.uuid4(), row[0], row[1], row[2], date(2026, 11, 15)))
        with pytest.raises(psycopg.errors.InsufficientPrivilege):  # a confirmed voucher is never changed by the service
            with c.transaction():
                c.execute("UPDATE cashflow.voucher SET total_rial = 1")
        c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('ChequesInCollection', '1202')")
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('Other', '1')")
        c.execute(add, (1404, uuid.uuid4(), "Receipt", uuid.uuid4(), 0, date(2025, 11, 15)))
        c.commit()

    with psycopg.connect(db, autocommit=True) as c:
        c.execute("SELECT cashflow.close_fiscal_year(1404)")
    try:
        with psycopg.connect(db) as c:
            with pytest.raises(psycopg.errors.RaiseException):
                c.execute(add, (1404, uuid.uuid4(), "Receipt", uuid.uuid4(), 0, date(2025, 11, 16)))
    finally:
        with psycopg.connect(db, autocommit=True) as c:
            c.execute("UPDATE cashflow.fiscal_period SET status = 'Open' WHERE fiscal_year = 1404")
            for t in ("cashflow.instrument_event_fy1404", "cashflow.receipt_fy1404", "cashflow.voucher_fy1404", "cashflow.instrument_file_fy1404", "cashflow.bank_transfer_fy1404",
                      "requests.payment_request_fy1404", "requests.request_file_fy1404"):
                c.execute(f"DROP TRIGGER IF EXISTS closed_year_guard ON {t}")


def test_received_cheques_can_come_back_from_the_bank_be_replaced_or_cancelled_and_steps_keep_details(db):
    # 031_cashflow_cheque_steps.sql (owner's item 14, answers to Q30-Q35 of 15 Nov 2026).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        iid = insert_instrument(c, 1405)
        for state in ("ReturnedFromBank", "Replaced", "Cancelled"):
            c.execute("UPDATE cashflow.instrument SET state = %s WHERE id = %s", (state, iid))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("UPDATE cashflow.instrument SET state = 'Lost' WHERE id = %s", (iid,))
        c.execute("""INSERT INTO cashflow.instrument_event (fiscal_year, instrument_year, instrument_id, seq, action, from_state, to_state, event_date, user_id,
                         reference, note, details)
                     VALUES (1405, 1405, %s, 99, 'Cancel', 'InCashDesk', 'Cancelled', '2026-11-15', 'treasurer', 'M-12', 'bad debt',
                             '{"documentDate": "2026-11-14"}')""", (iid,))
        assert c.execute("SELECT details->>'documentDate' FROM cashflow.instrument_event WHERE instrument_id = %s AND seq = 99", (iid,)).fetchone()[0] == "2026-11-14"
        c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('BadDebts', '8101') ON CONFLICT DO NOTHING")
        assert c.execute("SELECT column_name FROM information_schema.columns WHERE table_schema = 'cashflow' AND table_name = 'receipt' "
                         "AND column_name = 'replaced_instrument_id'").fetchone() is not None
        c.rollback()


def test_received_cheques_can_be_registered_in_sayad_and_handed_to_the_lawyer(db):
    # 033_cashflow_cheque_status.sql (owner, 18 Nov 2026: the latest status of a received cheque).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        iid = insert_instrument(c, 1405)
        for state in ("SayadRegistered", "DepositedForCollection", "Bounced", "InLegal", "Replaced"):
            c.execute("UPDATE cashflow.instrument SET state = %s WHERE id = %s", (state, iid))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("UPDATE cashflow.instrument SET state = 'WithLawyer' WHERE id = %s", (iid,))
        index = c.execute("SELECT indexdef FROM pg_indexes WHERE schemaname = 'cashflow' AND indexname = 'instrument_due'").fetchone()
        assert index is not None and "SayadRegistered" in index[0]
        c.rollback()


def test_scans_of_a_legal_action_are_kept_with_the_cheque(db):
    # 034_cashflow_legal_action_files.sql (owner, 18 Nov 2026: the screen «اقدام حقوقی» keeps the scans of the action's papers).
    add = """INSERT INTO cashflow.instrument_file (fiscal_year, file_id, instrument_id, step, file_name, content_type, size, uploaded_by, uploaded_at, content)
             VALUES (%s, %s, %s, 4, 'a.pdf', %s, %s, 'treasurer', now(), %s)"""
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        iid, fid = uuid.uuid4(), uuid.uuid4()
        c.execute(add, (1405, fid, iid, "application/pdf", 4, b"%PDF"))
        assert c.execute("SELECT tableoid::regclass::text FROM cashflow.instrument_file WHERE file_id = %s", (fid,)).fetchone()[0] == "cashflow.instrument_file_fy1405"
        for row in ((1405, uuid.uuid4(), iid, "text/html", 4, b"%PDF"),       # only PDF, JPEG or PNG
                    (1405, uuid.uuid4(), iid, "application/pdf", 5, b"%PDF"),  # the size is the content's size
                    (1405, uuid.uuid4(), iid, "application/pdf", 0, b"")):
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute(add, row)
        with pytest.raises(psycopg.errors.InsufficientPrivilege):  # a scan is added or removed, never changed
            with c.transaction():
                c.execute("UPDATE cashflow.instrument_file SET file_name = 'b.pdf'")
        c.execute("DELETE FROM cashflow.instrument_file WHERE file_id = %s", (fid,))
        c.execute(add, (1404, uuid.uuid4(), iid, "application/pdf", 4, b"%PDF"))
        c.commit()

    with psycopg.connect(db, autocommit=True) as c:
        c.execute("SELECT cashflow.close_fiscal_year(1404)")
    try:
        with psycopg.connect(db) as c:
            c.execute("SET ROLE cashflow_svc")
            with pytest.raises(psycopg.errors.RaiseException):  # a closed year takes no new scan
                c.execute(add, (1404, uuid.uuid4(), uuid.uuid4(), "application/pdf", 4, b"%PDF"))
    finally:
        with psycopg.connect(db, autocommit=True) as c:
            c.execute("UPDATE cashflow.fiscal_period SET status = 'Open' WHERE fiscal_year = 1404")
            for t in ("cashflow.instrument_event_fy1404", "cashflow.receipt_fy1404", "cashflow.voucher_fy1404", "cashflow.instrument_file_fy1404", "cashflow.bank_transfer_fy1404",
                      "requests.payment_request_fy1404", "requests.request_file_fy1404"):
                c.execute(f"DROP TRIGGER IF EXISTS closed_year_guard ON {t}")
            c.execute("DELETE FROM cashflow.instrument_file WHERE fiscal_year = 1404")


def test_the_voucher_template_has_the_account_of_cheques_in_legal_action(db):
    # 035_cashflow_legal_action_voucher.sql (owner's answer to Q38, 18 Nov 2026: «اسناد واخواست شده حقوقی شده»).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('ProtestedChequesInLegal', '1204') ON CONFLICT DO NOTHING")
        # Guarantee cheques and notes in legal action (answer to Q42): «اسناد تضمینی حقوقی شده به نفع شرکت».
        c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('GuaranteeChequesInLegal', '9102') ON CONFLICT DO NOTHING")
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('LegalCheques', '1')")
        c.rollback()


def test_the_voucher_template_accepts_every_fixed_account_of_the_domain(db):
    # 038_cashflow_voucher_accounts_notes_and_legal.sql: the notes (#10) and the legal action (#14) each added fixed accounts.
    import re
    from pathlib import Path
    cs = (Path(__file__).resolve().parents[3] / "src/Services/Cashflow/Treasury.Cashflow.Domain/Vouchers/Vouchers.cs").read_text(encoding="utf-8")
    body = re.search(r"public enum FixedAccount\s*\{(.*?)\}", cs, re.S).group(1)
    names = re.findall(r"^\s*(\w+),?\s*$", re.sub(r"///.*", "", body), re.M)
    assert "NotesInCollection" in names and "ProtestedChequesInLegal" in names
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        for name in names:
            with c.transaction():
                c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES (%s, '9999') ON CONFLICT DO NOTHING", (name,))
        c.rollback()


def test_direct_deposits_have_their_kind_and_a_sheba_for_paya_and_satna(db):
    # 036_cashflow_deposit_kinds.sql (owner, 18 Nov 2026: the kinds of a direct deposit into a bank account).
    add = """INSERT INTO cashflow.receipt (fiscal_year, id, kind, number, receipt_date, payer_id, payer_name, payer_detail_code, purpose_group_id, purpose_group_name,
                 purpose, recorded_by, account_kind, account_id, deposit_method, amount, currency, total_rial, state, payer_sheba)
             VALUES (1405, %s, 'BankDeposit', %s, '2026-11-18', %s, 'payer', '200015', %s, 'sales', 'sales', 'clerk', 'BankAccount', %s, %s, 1000, 'IRR', 1000, 'Recorded', %s)"""
    row = lambda n, method, sheba: (uuid.uuid4(), n, uuid.uuid4(), uuid.uuid4(), uuid.uuid4(), method, sheba)
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        for n, (method, sheba) in enumerate([("Paya", "IR062960000000100324200001"), ("Satna", "IR062960000000100324200001"), ("InternalTransfer", None),
                                             ("PaymentGateway", None), ("CardToCard", None), ("DepositSlip", None), ("Transfer", None), ("Pos", None)]):
            c.execute(add, row(9000 + n, method, sheba))
        for method, sheba in (("Paya", None),                                  # a Paya transfer needs its Sheba
                              ("DepositSlip", "IR062960000000100324200001"),   # other kinds carry none
                              ("Satna", "IR06-2960"),                          # IR and 24 digits
                              ("Cheque", None)):                               # an unknown kind
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute(add, row(9100, method, sheba))
        c.rollback()


def test_the_company_particulars_are_columns_of_the_one_company_row(db):
    # 039_reference_company_joined.sql (owner, 18 Nov 2026: «مشخصات شرکت» and «اطلاعات شرکت» are one page): the legal address and its city.
    with psycopg.connect(db) as c:
        assert c.execute("SELECT to_regclass('reference.company_profile')").fetchone()[0] is None
        c.execute("DELETE FROM reference.company_logo")
        c.execute("DELETE FROM reference.company")
        c.execute("SET ROLE reference_svc")
        city = uuid.uuid4()
        c.execute("INSERT INTO reference.company (id, name, legal_address, city_id, postal_code) VALUES (%s, 'sample', 'legal', %s, '1234567890')",
                  (uuid.uuid4(), city))
        assert c.execute("SELECT legal_address, city_id FROM reference.company").fetchone() == ("legal", city)
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("UPDATE reference.company SET legal_address = ''")
        c.rollback()


def test_a_profile_entered_before_the_join_moves_into_the_company_row(db):
    # 039 moves a row of the first 037 (reference.company_profile) into reference.company and drops the old table.
    import pathlib
    script = pathlib.Path(__file__).resolve().parents[1] / "039_reference_company_joined.sql"
    city = uuid.uuid4()
    with psycopg.connect(db, autocommit=True) as c:
        c.execute("DELETE FROM reference.company_logo")
        c.execute("DELETE FROM reference.company")
        c.execute("CREATE TABLE reference.company_profile (only_one boolean PRIMARY KEY DEFAULT true, name text NOT NULL, address text, "
                  "legal_address text, city_id uuid NOT NULL, postal_code text, version bigint NOT NULL DEFAULT 1)")
        c.execute("INSERT INTO reference.company_profile (name, address, legal_address, city_id, postal_code) VALUES ('Old', 'a', 'l', %s, '1234567890')", (city,))
        c.execute(script.read_text(encoding="utf-8"))
        assert c.execute("SELECT name, address, legal_address, city_id, postal_code FROM reference.company").fetchone() == ("Old", "a", "l", city, "1234567890")
        assert c.execute("SELECT to_regclass('reference.company_profile')").fetchone()[0] is None
        c.execute("DELETE FROM reference.company")

def test_a_payment_request_keeps_its_payments(db):
    # 032_requests_payments.sql (owner's answer to Q36: an endorsed cheque pays an approved payment request).
    with psycopg.connect(db) as c:
        row = c.execute("""SELECT column_default, is_nullable FROM information_schema.columns
                           WHERE table_schema = 'requests' AND table_name = 'payment_request' AND column_name = 'payments'""").fetchone()
        assert row is not None and row[1] == "NO" and "[]" in row[0]


def test_a_note_receipt_has_its_own_numbers_names_its_notes_and_keeps_its_terms(db):
    # 033_cashflow_note_receipts.sql (owner's report no. 4, item 13, and his form «فرم دریافت سفته» of 16 Nov 2026).
    add = """INSERT INTO cashflow.receipt (fiscal_year, id, kind, number, receipt_date, payer_id, payer_name, payer_detail_code,
                 purpose_group_id, purpose_group_name, purpose, recorded_by, instrument_ids, total_rial, state, note_terms)
             VALUES (1405, %s, %s, %s, '2026-11-16', %s, 'الف', '200015', %s, 'تضمین', 'تضمین قرارداد', 'test', %s, 1000, 'Recorded', %s)"""
    terms = '{"reason": "Guarantee", "form": "Paper", "maturity": "OnDemand"}'
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        rid = uuid.uuid4()
        c.execute(add, (rid, "Notes", 1, uuid.uuid4(), uuid.uuid4(), [uuid.uuid4()], terms))
        assert c.execute("SELECT note_terms->>'maturity' FROM cashflow.receipt WHERE id = %s", (rid,)).fetchone()[0] == "OnDemand"
        for kind, number, notes, note_terms in (
                ("Notes", 2, [], terms),                        # a note receipt names its notes
                ("Notes", 2, [uuid.uuid4()], None),             # and always has its terms
                ("Instruments", 90, [uuid.uuid4()], terms),     # which no other kind has
                ("Bills", 1, [uuid.uuid4()], None)):
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute(add, (uuid.uuid4(), kind, number, uuid.uuid4(), uuid.uuid4(), notes, note_terms))
        c.execute("""INSERT INTO cashflow.receipt_number (fiscal_year, kind, last_number) VALUES (1405, 'Notes', 1)
                     ON CONFLICT (fiscal_year, kind) DO UPDATE SET last_number = cashflow.receipt_number.last_number + 1""")
        c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('GuaranteeNotesForCompany', '9102') ON CONFLICT DO NOTHING")
        # 035_cashflow_note_steps.sql (owner, 16 Nov 2026: steps of a note after receipt): «سفته های در جریان وصول».
        c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('NotesInCollection', '1204') ON CONFLICT DO NOTHING")
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('NotesProtested', '1205')")
        c.rollback()


def test_signatories_are_unique_and_a_signature_rule_keeps_its_numbers_together(db):
    # 034_reference_signatories.sql (owner, 16 Nov 2026: authorised signatories in Basic Information and a rule per kind of document).
    add = """INSERT INTO reference.signatory (id, full_name, name_key, position, national_code, mobile) VALUES (%s, %s, %s, 'مدیر عامل', %s, %s)"""
    rule = "INSERT INTO reference.signature_rule (document, id, required_count, compulsory, optional) VALUES (%s, %s, %s, %s, %s)"
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        ceo, cfo = uuid.uuid4(), uuid.uuid4()
        c.execute(add, (ceo, "علی رضایی", "علیرضایی", "0123456789", "09121234567"))
        c.execute(add, (cfo, "بهرام کریمی", "بهرامکریمی", None, None))
        for row, error in (
                ((uuid.uuid4(), "علی رضایی", "علیرضایی", None, None), psycopg.errors.UniqueViolation),       # the same name
                ((uuid.uuid4(), "دیگری", "دیگری", "0123456789", None), psycopg.errors.UniqueViolation),       # the same national code
                ((uuid.uuid4(), "دیگری", "دیگری", "12345", None), psycopg.errors.CheckViolation),
                ((uuid.uuid4(), "دیگری", "دیگری", None, "02188990011"), psycopg.errors.CheckViolation)):
            with pytest.raises(error):
                with c.transaction():
                    c.execute(add, row)
        c.execute(rule, ("NoteReceipt", uuid.uuid4(), 2, [ceo], [cfo]))
        for row, error in (
                (("NoteReceipt", uuid.uuid4(), 1, [ceo], []), psycopg.errors.UniqueViolation),                # one rule per kind of document
                (("IssuedCheque", uuid.uuid4(), 1, [ceo, cfo], []), psycopg.errors.CheckViolation),           # fewer than the compulsory ones
                (("IssuedCheque", uuid.uuid4(), 3, [ceo], [cfo]), psycopg.errors.CheckViolation),             # more than those named
                (("IssuedCheque", uuid.uuid4(), 1, [ceo], [ceo]), psycopg.errors.CheckViolation),             # compulsory and optional
                (("Invoice", uuid.uuid4(), 0, [], []), psycopg.errors.CheckViolation)):
            with pytest.raises(error):
                with c.transaction():
                    c.execute(rule, row)
        c.execute("SET ROLE cashflow_svc")
        with pytest.raises(psycopg.errors.InsufficientPrivilege):  # another service does not read reference-svc's tables
            with c.transaction():
                c.execute("SELECT 1 FROM reference.signatory")
        c.rollback()
def test_one_company_profile_with_a_small_logo(db):
    # 033_reference_company.sql (owner, 16 Nov 2026): one row per database; the logo is a PNG/JPEG of at most 1 MB.
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        company = uuid.uuid4()
        c.execute("DELETE FROM reference.company_logo")
        if c.execute("SELECT count(*) FROM reference.company").fetchone()[0] == 0:
            c.execute("INSERT INTO reference.company (id, name) VALUES (%s, 'شرکت آزمایشی')", (company,))
        company = c.execute("SELECT id FROM reference.company").fetchone()[0]
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute("INSERT INTO reference.company (id, name) VALUES (%s, 'شرکت دوم')", (uuid.uuid4(),))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("UPDATE reference.company SET name = ''")
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("UPDATE reference.company SET logo_type = 'application/pdf'")
        c.execute("UPDATE reference.company SET logo_type = 'image/png'")
        c.execute("INSERT INTO reference.company_logo (company_id, data) VALUES (%s, %s)", (company, b"\x89PNG"))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("UPDATE reference.company_logo SET data = %s", (b"x" * (1024 * 1024 + 1),))
        # 034_reference_company_details.sql (owner's answer to Q38): optional official details, each with its shape.
        c.execute(
            "UPDATE reference.company SET national_id = '10101010103', economic_code = '411111111111', registration_number = '12345', "
            "address = 'تهران', postal_code = '1234567890', phone = '+98-21-88880000', website = 'www.example.ir', email = 'info@example.ir'")
        for column, bad in (("national_id", "1234567890"), ("economic_code", "4111"), ("registration_number", "12/3"), ("address", "x" * 301),
                            ("postal_code", "12345"), ("phone", "abc"), ("website", ""), ("email", "info")):
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute(f"UPDATE reference.company SET {column} = %s", (bad,))
        c.execute(
            "UPDATE reference.company SET national_id = NULL, economic_code = NULL, registration_number = NULL, address = NULL, "
            "postal_code = NULL, phone = NULL, website = NULL, email = NULL")
        with pytest.raises(psycopg.errors.InsufficientPrivilege):
            with c.transaction():
                c.execute("DELETE FROM reference.company")  # the profile is corrected, never deleted
        c.execute("DELETE FROM reference.company_logo")
        c.execute("UPDATE reference.company SET logo_type = NULL")
        c.execute("RESET ROLE")
        c.execute("SET ROLE cashflow_svc")
        with pytest.raises(psycopg.errors.InsufficientPrivilege):
            c.execute("SELECT name FROM reference.company")



def test_beneficiaries_obligors_and_guarantors_share_a_table_with_groups_mobile_and_area_code(db):
    # 040_reference_parties.sql (owner, 19 Nov 2026: «ذینفعان / متعهدین / ضامنین» on one form, mobile number, area code in its own box).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        old = uuid.uuid4()
        c.execute("INSERT INTO reference.beneficiary (id, detail_code, legal_name) VALUES (%s, '883001', 'ذینفع قدیمی')", (old,))
        # An entry saved without the new columns is a beneficiary.
        assert c.execute("SELECT is_beneficiary, is_obligor, is_guarantor, mobile, phone_area_code FROM reference.beneficiary WHERE id = %s",
                         (old,)).fetchone() == (True, False, False, None, None)
        add = """INSERT INTO reference.beneficiary (id, detail_code, legal_name, is_beneficiary, is_obligor, is_guarantor, mobile, phone_area_code)
                 VALUES (%s, %s, 'ضامن', %s, %s, %s, %s, %s)"""
        c.execute(add, (uuid.uuid4(), "883002", False, True, True, "09121234567", "021"))
        for detail, groups, mobile, area in [
            ("883003", (False, False, False), None, None),          # in no group
            ("883004", (True, False, False), "9121234567", None),   # a mobile number is 09 and nine digits
            ("883005", (True, False, False), None, "21"),           # an area code starts with 0
            ("883006", (True, False, False), None, "02155"),        # and has at most 4 digits
        ]:
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute(add, (uuid.uuid4(), detail, *groups, mobile, area))
        c.rollback()


def test_a_cheque_transfer_request_names_its_cheque_and_the_scripts_run_again_over_it(db):
    # 041_requests_cheque_transfer.sql (owner, 19 Nov 2026: payment method «ثبت انتقال (خرج چک)»).
    add = """INSERT INTO requests.payment_request (fiscal_year, id, number, request_date, requester_id, requester_name, beneficiary_id,
                 beneficiary_detail_code, beneficiary_name, purpose_group_id, purpose_group_name, purpose, amount, currency, method, priority,
                 state, recorded_by, cheque_id, cheque_number, cheque_due_date, cheque_payer, pay_from_kind, pay_from_id)
             VALUES (1405, %s, %s, '2026-11-19', %s, 'الف', %s, '300200', 'ب', %s, 'خرید', 'بابت بدهی', 1000, 'IRR', %s, 1, 'Registered', 'test',
                     %s, %s, %s, %s, %s, %s)"""
    ids = [uuid.uuid4() for _ in range(5)]
    cheque = uuid.uuid4()

    def request(c, i, number, method, cheque_id=None, number_on_cheque=None, payer=None, pay_from=None):
        c.execute(add, (ids[i], number, uuid.uuid4(), uuid.uuid4(), uuid.uuid4(), method, cheque_id, number_on_cheque,
                        date(2026, 12, 20) if cheque_id else None, payer, "BankAccount" if pay_from else None, pay_from))

    with psycopg.connect(db) as c:
        c.execute("SET ROLE requests_svc")
        request(c, 0, 9001, "ChequeTransfer", cheque, "445566", "فروشگاه آفتاب")
        for bad in (
            lambda: request(c, 1, 9002, "ChequeTransfer"),                                               # the method names its cheque
            lambda: request(c, 2, 9003, "Cash", cheque, "445566", "فروشگاه آفتاب"),                       # a cheque only with that method
            lambda: request(c, 3, 9004, "ChequeTransfer", cheque, None, "فروشگاه آفتاب"),                 # with its number
            lambda: request(c, 4, 9005, "ChequeTransfer", cheque, "445566", "فروشگاه آفتاب", uuid.uuid4()),  # and no account paid from
        ):
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    bad()
        c.commit()

    # Every start re-runs every script: 041 rewrites the list of methods and must keep the row saved with the newest one.
    params = psycopg.conninfo.conninfo_to_dict(db)
    env = dict(os.environ, PGHOST=params.get("host", ""), PGPORT=str(params.get("port", "5432")), PGUSER=params.get("user", "postgres"))
    try:
        result = subprocess.run([str(HERE / "apply.sh"), params["dbname"]], env=env, capture_output=True, text=True)
        assert result.returncode == 0, result.stderr[-2000:]
    finally:
        with psycopg.connect(db, autocommit=True) as c:
            c.execute("DELETE FROM requests.payment_request WHERE id = ANY(%s)", (ids,))


def test_payment_sheets_are_numbered_from_one_each_fiscal_year(db):
    # 042_cashflow_payment_sheets.sql (owner's answer to Q57, 19 Nov 2026: «برگ پرداخت» has a serial number of its own).
    take = """INSERT INTO cashflow.payment_sheet_number (fiscal_year, last_number) VALUES (%s, 1)
              ON CONFLICT (fiscal_year) DO UPDATE SET last_number = cashflow.payment_sheet_number.last_number + 1 RETURNING last_number"""
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        assert [c.execute(take, (y,)).fetchone()[0] for y in (1405, 1405, 1406, 1405)] == [1, 2, 1, 3]
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("UPDATE cashflow.payment_sheet_number SET last_number = -1 WHERE fiscal_year = 1405")
        c.execute("RESET ROLE")
        c.execute("SET ROLE requests_svc")
        with pytest.raises(psycopg.errors.InsufficientPrivilege):  # another service does not number the sheets
            with c.transaction():
                c.execute("SELECT last_number FROM cashflow.payment_sheet_number")
        c.rollback()


def test_the_cashflow_service_may_delete_a_cheque_taken_off_a_corrected_receipt(db):
    # 043_cashflow_receipt_corrections.sql: report no. 6, items 2 and 6. The step first (it points at the cheque), then the cheque.
    with psycopg.connect(db) as c:
        iid = insert_instrument(c, 1405)
        insert_event(c, 1405, 1405, iid)
        c.execute("SET ROLE cashflow_svc")
        with pytest.raises(psycopg.errors.ForeignKeyViolation):
            with c.transaction():
                c.execute("DELETE FROM cashflow.instrument WHERE id = %s", (iid,))
        c.execute("DELETE FROM cashflow.instrument_event WHERE instrument_id = %s", (iid,))
        c.execute("DELETE FROM cashflow.instrument WHERE id = %s", (iid,))
        assert c.execute("SELECT count(*) FROM cashflow.instrument WHERE id = %s", (iid,)).fetchone()[0] == 0


def test_a_deleted_receipt_keeps_its_row_with_who_deleted_it_and_when(db):
    # 043_cashflow_receipt_corrections.sql: the owner's answers to Q59 and Q67 (19 and 20 Nov 2026). The row stays, in the state 'Deleted'.
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        rid = uuid.uuid4()
        c.execute(
            """INSERT INTO cashflow.receipt (fiscal_year, id, kind, number, receipt_date, payer_id, payer_name, payer_detail_code, purpose_group_id,
                   purpose_group_name, purpose, recorded_by, account_kind, account_id, amount, currency, total_rial, state)
               VALUES (1405, %s, 'Cash', 990001, '2026-11-14', %s, 'الف', '200015', %s, 'فروش', 'فروش', 'test', 'CashDesk', %s, 1000, 'IRR', 1000, 'Recorded')""",
            (rid, uuid.uuid4(), uuid.uuid4(), uuid.uuid4()))
        with pytest.raises(psycopg.errors.CheckViolation):   # deleted without who and when
            with c.transaction():
                c.execute("UPDATE cashflow.receipt SET state = 'Deleted' WHERE id = %s", (rid,))
        with pytest.raises(psycopg.errors.CheckViolation):   # a mark of deletion on a receipt that is not deleted
            with c.transaction():
                c.execute("UPDATE cashflow.receipt SET deleted_at = now(), deleted_by = 'x' WHERE id = %s", (rid,))
        c.execute("UPDATE cashflow.receipt SET state = 'Deleted', deleted_by = 'clerk-1', deleted_at = now() WHERE id = %s", (rid,))
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("UPDATE cashflow.receipt SET state = 'Gone' WHERE id = %s", (rid,))
        with pytest.raises(psycopg.errors.InsufficientPrivilege):   # the service never deletes a receipt row
            with c.transaction():
                c.execute("DELETE FROM cashflow.receipt WHERE id = %s", (rid,))
        # A cancelled receipt still needs its date and reason.
        with pytest.raises(psycopg.errors.CheckViolation):
            with c.transaction():
                c.execute("UPDATE cashflow.receipt SET state = 'Cancelled', deleted_by = NULL, deleted_at = NULL WHERE id = %s", (rid,))


def test_a_company_card_without_a_bank_account_is_entered_once(db):
    # 044_accounts_company_cards.sql: the owner's report no. 6, item 12, and his answer to Q69 (20 Nov 2026).
    add = """INSERT INTO accounts.company_card (id, card_number, holder_name, bank_id, ledger_account, detail1)
             VALUES (%s, %s, 'شرکت نمونه', %s, '1105', %s)"""
    with psycopg.connect(db) as c:
        c.execute("SET ROLE accounts_svc")
        c.execute(add, (uuid.uuid4(), "6037991234567893", uuid.uuid4(), "600009"))
        c.execute(add, (uuid.uuid4(), "6219861034529007", uuid.uuid4(), None))   # an account that takes no تفصیل
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "6037991234567893", uuid.uuid4(), None))
        with pytest.raises(psycopg.errors.CheckViolation):   # 16 digits
            with c.transaction():
                c.execute(add, (uuid.uuid4(), "6037-9912-3456", uuid.uuid4(), None))
        with pytest.raises(psycopg.errors.InsufficientPrivilege):   # a card is switched off, never deleted
            with c.transaction():
                c.execute("DELETE FROM accounts.company_card")
        c.rollback()
        c.execute("SET ROLE cashflow_svc")
        with pytest.raises(psycopg.errors.InsufficientPrivilege):   # another service asks accounts-svc, it does not read its table
            c.execute("SELECT count(*) FROM accounts.company_card")


def test_a_deposit_onto_a_bank_card_has_its_own_numbers_its_card_and_its_kinds_of_deposit(db):
    # 045_cashflow_card_deposits.sql and the lists of 033: «واریز به کارت بانک» (owner's report no. 6, item 12; answers to Q68–Q70).
    add = """INSERT INTO cashflow.receipt (fiscal_year, id, kind, number, receipt_date, payer_id, payer_name, payer_detail_code, purpose_group_id, purpose_group_name,
                 purpose, recorded_by, account_kind, account_id, deposit_method, amount, currency, total_rial, state)
             VALUES (1405, %s, %s, %s, '2026-11-20', %s, 'payer', '200015', %s, 'sales', 'sales', 'clerk', %s, %s, %s, 1000, 'IRR', 1000, 'Recorded')"""
    row = lambda kind, n, account, method: (uuid.uuid4(), kind, n, uuid.uuid4(), uuid.uuid4(), account, uuid.uuid4(), method)
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        # A petty-cash holder's card or a company card; card to card, a deposit slip or the company's gateway (answer to Q70).
        for n, (account, method) in enumerate([("PettyCashCard", "CardToCard"), ("CompanyCard", "DepositSlip"), ("CompanyCard", "PaymentGateway")]):
            c.execute(add, row("CardDeposit", 9500 + n, account, method))
        # Its own series: a bank deposit may have the same number in the same year (answer to Q19 for every kind).
        c.execute(add, row("BankDeposit", 9500, "BankAccount", "CardToCard"))
        with pytest.raises(psycopg.errors.UniqueViolation):
            with c.transaction():
                c.execute(add, row("CardDeposit", 9500, "CompanyCard", "CardToCard"))
        for account, method in (("BankAccount", "CardToCard"),      # a card deposit goes onto a card
                                ("CashDesk", "CardToCard"),
                                ("PettyCashCard", "Paya"),          # not a kind of deposit onto a card
                                ("CompanyCard", None),
                                ("Wallet", "CardToCard")):          # an unknown kind of account
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute(add, row("CardDeposit", 9600, account, method))
        with pytest.raises(psycopg.errors.CheckViolation):          # a bank deposit still goes into a bank account
            with c.transaction():
                c.execute(add, row("BankDeposit", 9601, "CompanyCard", "CardToCard"))
        c.execute("INSERT INTO cashflow.receipt_number (fiscal_year, kind, last_number) VALUES (1405, 'CardDeposit', 3) ON CONFLICT DO NOTHING")
        c.rollback()


def test_transfers_between_the_companys_accounts_have_their_numbers_ways_and_limits(db):
    # 046_cashflow_bank_transfers.sql (owner's report no. 7, 20 Nov 2026: «نقل و انتقال بین بانکی»).
    add = """INSERT INTO cashflow.bank_transfer (fiscal_year, id, number, transfer_date, method, channel, source_account_id, source_account_number, source_bank_id,
                 source_bank_name, destination_account_id, destination_account_number, destination_bank_id, destination_bank_name, amount, currency, amount_rial,
                 cheque, state, recorded_by)
             VALUES (%(year)s, %(id)s, %(number)s, %(day)s, %(method)s, %(channel)s, %(source)s, '0101-1', %(source_bank)s, 'ملت', %(destination)s, '0202-1',
                     %(destination_bank)s, 'ملی', 1000, %(currency)s, 1000, %(cheque)s, %(state)s, 'clerk')"""
    mellat, melli, source = uuid.uuid4(), uuid.uuid4(), uuid.uuid4()
    cheque = '{"issuedChequeId": "%s", "sheet": 100500, "number": "100500", "sayadId": "4821123456780007", "dueDate": "2027-01-05"}' % uuid.uuid4()

    def row(number, method="Paya", channel="Remote", state="Recorded", same_bank=False, cheque=None, year=1405, day=date(2026, 11, 20), currency="IRR", destination=None):
        return dict(year=year, id=uuid.uuid4(), number=number, day=day, method=method, channel=channel, source=source, source_bank=mellat,
                    destination=destination or uuid.uuid4(), destination_bank=mellat if same_bank else melli, currency=currency, cheque=cheque, state=state)

    with psycopg.connect(db) as c:
        names = {r[0] for r in c.execute("SELECT relname FROM pg_class WHERE relname LIKE 'bank_transfer_fy14%'")}
        assert {"bank_transfer_fy1404", "bank_transfer_fy1405"} <= names
        c.execute("SET ROLE cashflow_svc")
        c.execute(add, row(9001))
        c.execute(add, row(9002, "Internal", "InPerson", same_bank=True))
        c.execute(add, row(9003, "Cheque", None, "ChequeIssued", cheque=cheque))
        c.execute(add, row(9004, "Cheque", None, "ChequeIssued", same_bank=True, cheque=cheque))   # a cheque may stay within one bank
        c.execute(add, row(9001, year=1404, day=date(2025, 11, 20)))                               # numbers are per fiscal year
        for bad, error in (
            (row(9001), psycopg.errors.UniqueViolation),                                       # a number once a year
            (row(9010, "Internal", "Remote"), psycopg.errors.CheckViolation),                  # internal: the same bank
            (row(9010, "Satna", "Remote", same_bank=True), psycopg.errors.CheckViolation),     # Pol, Paya, Satna: two banks
            (row(9010, "Paya", "Remote", currency="USD"), psycopg.errors.CheckViolation),      # and Rial
            (row(9010, "Paya", None), psycopg.errors.CheckViolation),                          # an electronic order names its channel
            (row(9010, "Cheque", "Remote", "ChequeIssued", cheque=cheque), psycopg.errors.CheckViolation),
            (row(9010, "Cheque", None, "ChequeIssued"), psycopg.errors.CheckViolation),        # a cheque transfer keeps its cheque
            (row(9010, "Cheque", None, "Recorded", cheque=cheque), psycopg.errors.CheckViolation),
            (row(9010, "Paya", "Remote", "Collected"), psycopg.errors.CheckViolation),
            (row(9010, "Card", "Remote"), psycopg.errors.CheckViolation),                      # an unknown way
            (row(9010, destination=source), psycopg.errors.CheckViolation),                    # two different accounts
            (row(9010, state="Deleted"), psycopg.errors.CheckViolation),                       # a deleted transfer says who and when
        ):
            with pytest.raises(error):
                with c.transaction():
                    c.execute(add, bad)
        # Deleted by mistake: the row stays, marked; the service never removes it.
        c.execute("UPDATE cashflow.bank_transfer SET state = 'Deleted', deleted_by = 'clerk', deleted_at = now() WHERE fiscal_year = 1405 AND number = 9001")
        with pytest.raises(psycopg.errors.InsufficientPrivilege):
            with c.transaction():
                c.execute("DELETE FROM cashflow.bank_transfer WHERE number = 9001")

        take = """INSERT INTO cashflow.bank_transfer_number (fiscal_year, last_number) VALUES (%s, 1)
                  ON CONFLICT (fiscal_year) DO UPDATE SET last_number = cashflow.bank_transfer_number.last_number + 1 RETURNING last_number"""
        assert [c.execute(take, (y,)).fetchone()[0] for y in (1405, 1405, 1406, 1405)] == [1, 2, 1, 3]

        # The limits are the company's data: they start with the owner's figures and are changed, emptied and given per bank.
        limits = {r[0]: r[1:] for r in c.execute(
            """SELECT method, min_per_order, max_per_order_in_person, max_per_order_remote, max_per_day_in_person, max_per_day_remote, max_count_per_day
               FROM cashflow.transfer_limit WHERE bank_key = ''""")}
        assert limits == {"Internal": (None, None, None, None, None, None), "Pol": (None, None, None, 3_000_000_000, 2_000_000_000, 5),
                          "Paya": (None, 5_000_000_000, 3_000_000_000, None, None, 5), "Satna": (2_000_000_000, None, None, None, None, None)}
        c.execute("INSERT INTO cashflow.transfer_limit (method, bank_key, max_per_order_remote) VALUES ('Internal', %s, 500000000)", (str(mellat),))
        c.execute("DELETE FROM cashflow.transfer_limit WHERE bank_key = %s", (str(mellat),))
        for bad in ("INSERT INTO cashflow.transfer_limit (method, bank_key) VALUES ('Cheque', '')",        # no limits for cheques
                    "INSERT INTO cashflow.transfer_limit (method, bank_key) VALUES ('Pol', 'ملت')",         # a bank is named by its id
                    "UPDATE cashflow.transfer_limit SET max_count_per_day = 0 WHERE method = 'Pol'",
                    "UPDATE cashflow.transfer_limit SET min_per_order = -1 WHERE method = 'Satna'"):
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute(bad)

        # A sheet of a deleted cheque transfer is freed by the service (the record never happened).
        c.execute("RESET ROLE")
        book = uuid.uuid4()
        c.execute("INSERT INTO cashflow.cheque_book (id, bank_account_id, serial, first_sheet, last_sheet) VALUES (%s, %s, '1234', 100500, 100524)", (book, source))
        c.execute("SET ROLE cashflow_svc")
        c.execute("INSERT INTO cashflow.cheque_sheet (cheque_book_id, sheet, sheet_use) VALUES (%s, 100500, 'Issued')", (book,))
        assert c.execute("DELETE FROM cashflow.cheque_sheet WHERE cheque_book_id = %s AND sheet = 100500 AND sheet_use = 'Issued'", (book,)).rowcount == 1

        c.execute("RESET ROLE")
        c.execute("SET ROLE requests_svc")
        with pytest.raises(psycopg.errors.InsufficientPrivilege):  # another service does not read the transfers
            with c.transaction():
                c.execute("SELECT count(*) FROM cashflow.bank_transfer")
        c.rollback()


def test_a_transfer_has_its_vouchers_and_the_company_keeps_changed_limits_when_the_scripts_run_again(db):
    # 046: confirmed vouchers of a transfer (source 'Transfer': step 0 the payer's or the electronic one, step 1 the receiver's), the
    # closed-year guard on the transfers, and the seed of the limits, which must not bring back a figure the company changed.
    add = """INSERT INTO cashflow.voucher (fiscal_year, id, source, source_id, step, operation, voucher_date, lines, total_rial, confirmed_by, confirmed_at)
             VALUES (1405, %s, 'Transfer', %s, %s, 'نقل و انتقال بانکی', '2026-11-20', '[]', 1000, 'treasurer', now())"""
    transfer = uuid.uuid4()
    with psycopg.connect(db) as c:
        c.execute("SET ROLE cashflow_svc")
        c.execute(add, (uuid.uuid4(), transfer, 0))
        c.execute(add, (uuid.uuid4(), transfer, 1))
        for step, error in ((1, psycopg.errors.UniqueViolation), (2, psycopg.errors.CheckViolation)):
            with pytest.raises(error):
                with c.transaction():
                    c.execute(add, (uuid.uuid4(), transfer, step))
        c.execute("INSERT INTO cashflow.voucher_account (account_key, ledger_account_code) VALUES ('FundsInTransit', '1109') ON CONFLICT DO NOTHING")
        c.rollback()

    params = psycopg.conninfo.conninfo_to_dict(db)
    env = dict(os.environ, PGHOST=params.get("host", ""), PGPORT=str(params.get("port", "5432")), PGUSER=params.get("user", "postgres"))
    old = uuid.uuid4()
    with psycopg.connect(db, autocommit=True) as c:
        c.execute("UPDATE cashflow.transfer_limit SET max_per_order_remote = 7000000000, max_count_per_day = NULL WHERE method = 'Paya' AND bank_key = ''")
        c.execute("""INSERT INTO cashflow.bank_transfer (fiscal_year, id, number, transfer_date, method, channel, source_account_id, source_account_number, source_bank_id,
                         source_bank_name, destination_account_id, destination_account_number, destination_bank_id, destination_bank_name, amount, amount_rial, state, recorded_by)
                     VALUES (1404, %s, 9900, '2025-11-20', 'Paya', 'Remote', %s, 'a', %s, 'x', %s, 'b', %s, 'y', 10, 10, 'Recorded', 'clerk')""",
                  (old, uuid.uuid4(), uuid.uuid4(), uuid.uuid4(), uuid.uuid4()))
        c.execute("SELECT cashflow.close_fiscal_year(1404)")
        try:
            result = subprocess.run([str(HERE / "apply.sh"), params["dbname"]], env=env, capture_output=True, text=True)
            assert result.returncode == 0, result.stderr[-2000:]
            assert c.execute("SELECT max_per_order_remote, max_count_per_day FROM cashflow.transfer_limit WHERE method = 'Paya' AND bank_key = ''").fetchone() == (7_000_000_000, None)
            with pytest.raises(psycopg.errors.RaiseException):  # a transfer of a closed year is frozen, also after the scripts ran again
                c.execute("UPDATE cashflow.bank_transfer SET description = 'x' WHERE id = %s", (old,))
        finally:
            c.execute("UPDATE cashflow.fiscal_period SET status = 'Open' WHERE fiscal_year = 1404")
            for t in ("cashflow.instrument_event_fy1404", "cashflow.receipt_fy1404", "cashflow.voucher_fy1404", "cashflow.instrument_file_fy1404", "cashflow.bank_transfer_fy1404",
                      "requests.payment_request_fy1404", "requests.request_file_fy1404"):
                c.execute(f"DROP TRIGGER IF EXISTS closed_year_guard ON {t}")
            c.execute("DELETE FROM cashflow.bank_transfer WHERE id = %s", (old,))
            c.execute("UPDATE cashflow.transfer_limit SET max_per_order_remote = 3000000000, max_count_per_day = 5 WHERE method = 'Paya' AND bank_key = ''")

