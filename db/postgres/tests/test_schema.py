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
            for t in ("cashflow.instrument_event_fy1404", "cashflow.receipt_fy1404", "cashflow.voucher_fy1404", "cashflow.instrument_file_fy1404",
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
            for t in ("cashflow.instrument_event_fy1404", "cashflow.receipt_fy1404", "cashflow.voucher_fy1404", "cashflow.instrument_file_fy1404",
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
            for t in ("cashflow.instrument_event_fy1404", "cashflow.receipt_fy1404", "cashflow.voucher_fy1404", "cashflow.instrument_file_fy1404",
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
            for t in ("cashflow.instrument_event_fy1404", "cashflow.receipt_fy1404", "cashflow.voucher_fy1404", "cashflow.instrument_file_fy1404",
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


def test_the_company_profile_is_one_row(db):
    # 037_reference_company_profile.sql (owner, 18 Nov 2026: «مشخصات شرکت»; the city and postal code are the legal address's).
    with psycopg.connect(db) as c:
        c.execute("SET ROLE reference_svc")
        c.execute("INSERT INTO reference.company_profile (name, legal_address, city_id, postal_code) VALUES ('sample', 'legal', %s, '1234567890')", (uuid.uuid4(),))
        with pytest.raises(psycopg.errors.UniqueViolation):   # one profile per client database
            with c.transaction():
                c.execute("INSERT INTO reference.company_profile (name, city_id) VALUES ('second', %s)", (uuid.uuid4(),))
        for name, postal in ((" ", None), ("sample", "123")):
            with pytest.raises(psycopg.errors.CheckViolation):
                with c.transaction():
                    c.execute("UPDATE reference.company_profile SET name = %s, postal_code = %s", (name, postal))
        c.execute("RESET ROLE")
        c.execute("SET ROLE cashflow_svc")
        with pytest.raises(psycopg.errors.InsufficientPrivilege):  # another service asks reference-svc, it does not read the table
            with c.transaction():
                c.execute("SELECT * FROM reference.company_profile")
        c.rollback()


def test_a_payment_request_keeps_its_payments(db):
    # 032_requests_payments.sql (owner's answer to Q36: an endorsed cheque pays an approved payment request).
    with psycopg.connect(db) as c:
        row = c.execute("""SELECT column_default, is_nullable FROM information_schema.columns
                           WHERE table_schema = 'requests' AND table_name = 'payment_request' AND column_name = 'payments'""").fetchone()
        assert row is not None and row[1] == "NO" and "[]" in row[0]
