"""End-to-end: CSV export of old cheques -> PostgreSQL schema -> reconciliation report.

Needs a PostgreSQL server: TREASURY_TEST_DSN="host=... user=postgres dbname=treasury_migration_test".
Skipped when TREASURY_TEST_DSN is not set.
"""
import csv
import os
import subprocess
from pathlib import Path

import psycopg
import pytest

from treasury_migration.__main__ import main

DSN = os.environ.get("TREASURY_TEST_DSN")
ROOT = Path(__file__).resolve().parents[3]
pytestmark = pytest.mark.skipif(not DSN, reason="TREASURY_TEST_DSN not set")

HEADER = ["Checkid", "Dp", "Checktype", "Stcode", "Checkno", "BankName", "Amount", "Iscur", "Curid", "Currate", "Person", "Babat",
          "Maturity", "D1", "D2", "D3", "D4", "Vagno", "Baccno", "Bundleid", "Vos", "Backed", "RegVos", "Ctocash", "GoInlaw"]
ROWS = [
    # received: in cash desk, collected (crossing into 1387), bounced, returned, endorsed
    [1, 1, 0, 11, "100001", "ملت", "5000000", 0, "", "", "مشتری الف", "فروش", "13861201", "13861110", "", "", "", "", "", "", 0, 0, 0, 0, 0],
    [2, 1, 0, 13, "100002", "ملی", "7000000", 0, "", "", "مشتری ب", "فروش", "13870115", "13861215", "", "13861220", "13870116", "ملت جاری", "", "", 1, 0, 1, 0, 0],
    [3, 1, 2, 14, "100003", "تجارت", "2500000", 0, "", "", "مشتری ج", "فروش", "13861020", "13861001", "", "13861005", "13861021", "ملت جاری", "", "", 0, 1, 1, 0, 1],
    [4, 1, 4, 15, "S-17", "", "1200000", 0, "", "", "مشتری د", "ضمانت", "13861130", "13861101", "", "13861115", "", "", "", "", 0, 0, 0, 0, 0],
    [5, 1, 0, 16, "100005", "صادرات", "3300000", 0, "", "", "مشتری ه", "فروش", "13861210", "13861105", "", "13861106", "", "پیمانکار ب", "", "", 0, 0, 0, 0, 0],
    # issued: delivered, post-dated matured and paid, cancelled, cash payment (rejected), unknown status (rejected)
    [6, 2, 0, 22, "900101", "", "4000000", 0, "", "", "تامین کننده الف", "خرید", "13861105", "13861105", "13861106", "", "", "", "0101", 7, 0, 0, 0, 0, 0],
    [7, 2, 2, 22, "900102", "", "9000000", 0, "", "", "تامین کننده ب", "خرید", "13870110", "13861205", "13861206", "13870110", "13870112", "", "0101", 7, 1, 0, 1, 1, 0],
    [8, 2, 0, 26, "900103", "", "1000000", 0, "", "", "تامین کننده ج", "خرید", "13861107", "13861107", "13861107", "", "", "", "0101", 7, 0, 0, 0, 0, 0],
    [9, 2, 9, 21, "", "", "600000", 0, "", "", "کارمند", "تنخواه", "", "13861108", "", "", "", "", "0101", "", 0, 0, 0, 0, 0],
    [10, 1, 0, 42, "100010", "ملت", "800000", 0, "", "", "مشتری و", "فروش", "13861201", "13861110", "", "", "", "", "", "", 0, 0, 0, 0, 0],
]


@pytest.fixture()
def database():
    params = psycopg.conninfo.conninfo_to_dict(DSN)
    admin = psycopg.conninfo.make_conninfo(DSN, dbname="postgres")
    with psycopg.connect(admin, autocommit=True) as c:
        c.execute(f'DROP DATABASE IF EXISTS "{params["dbname"]}" WITH (FORCE)')
        c.execute(f'CREATE DATABASE "{params["dbname"]}"')
    env = dict(os.environ, PGHOST=params.get("host", ""), PGPORT=str(params.get("port", "5432")), PGUSER=params.get("user", "postgres"))
    subprocess.run([str(ROOT / "db/postgres/apply.sh"), params["dbname"]], check=True, env=env, capture_output=True)
    return DSN


def write_csv(path: Path) -> Path:
    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f)
        w.writerow(HEADER)
        w.writerows(ROWS)
    return path


def test_migrates_reconciles_and_is_idempotent(database, tmp_path):
    export = write_csv(tmp_path / "Cashnajoo86.csv")
    args = ["cheques", "--csv", str(export), "--source-name", "Cashnajoo86", "--target", database, "--report", str(tmp_path)]
    assert main(args) == 0
    report = (tmp_path / "Cashnajoo86_reconciliation.md").read_text(encoding="utf-8")
    assert "MATCH" in report and "Rejected: **2**" in report

    with psycopg.connect(database) as c:
        assert c.execute("SELECT count(*) FROM cashflow.instrument").fetchone()[0] == 8
        states = dict(c.execute("SELECT legacy_source, state FROM cashflow.instrument").fetchall())
        assert states["Cashnajoo86.Checks:7"] == "PaidByBank"
        assert states["Cashnajoo86.Checks:3"] == "Bounced"
        # The cheque received in 1386 and collected in 1387: instrument in 1386, collection step in 1387.
        rows = c.execute("""SELECT e.fiscal_year, e.action FROM cashflow.instrument_event e JOIN cashflow.instrument i ON i.id = e.instrument_id
                            WHERE i.legacy_source = 'Cashnajoo86.Checks:2' ORDER BY e.seq""").fetchall()
        assert rows == [(1386, "Receive"), (1386, "DepositForCollection"), (1387, "RecordCleared")]
        assert c.execute("SELECT origin_fiscal_year FROM cashflow.instrument WHERE legacy_source = 'Cashnajoo86.Checks:2'").fetchone()[0] == 1386

    # Second run inserts nothing new and still reconciles.
    assert main(args) == 0
    with psycopg.connect(database) as c:
        assert c.execute("SELECT count(*) FROM cashflow.instrument").fetchone()[0] == 8
        assert c.execute("SELECT count(*) FROM cashflow.instrument_event").fetchone()[0] == 22  # 1+3+3+2+2 received, 3+5+3 issued

    rejected = (tmp_path / "Cashnajoo86_rejected.csv").read_text(encoding="utf-8-sig")
    assert "cash payment" in rejected and "Stcode 42" in rejected


def test_dry_run_writes_reports_but_not_the_database(database, tmp_path):
    export = write_csv(tmp_path / "x.csv")
    assert main(["cheques", "--csv", str(export), "--source-name", "Cashnajoo86", "--target", database, "--report", str(tmp_path), "--dry-run"]) == 0
    with psycopg.connect(database) as c:
        assert c.execute("SELECT count(*) FROM cashflow.instrument").fetchone()[0] == 0
