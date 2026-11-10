"""Reads old cheques from SQL Server (dbo.Checks of the shared Cashnajoo86 database) or from a CSV export of it."""
from __future__ import annotations

import csv
from collections.abc import Iterator
from decimal import Decimal, InvalidOperation
from pathlib import Path

from .legacy_mapping import LegacyCheque

# Columns read from dbo.Checks (names as used by the Access VBA code). preflight() reports any that are missing.
REQUIRED_COLUMNS = [
    "Checkid", "Dp", "Checktype", "Stcode", "Checkno", "Bankid", "Amount", "Iscur", "Curid", "Currate", "Person", "Babat",
    "Maturity", "D1", "D2", "D3", "D4", "Vagno", "Baccno", "Bundleid", "Vos", "Backed", "RegVos", "Ctocash", "GoInlaw",
]

EXTRACT_SQL = """
SELECT c.Checkid, c.Dp, c.Checktype, c.Stcode, c.Checkno, b.Bankname AS BankName, c.Amount, c.Iscur, c.Curid, c.Currate,
       c.Person, c.Babat, c.Maturity, c.D1, c.D2, c.D3, c.D4, c.Vagno, c.Baccno, c.Bundleid,
       c.Vos, c.Backed, c.RegVos, c.Ctocash, c.GoInlaw
FROM dbo.Checks AS c
LEFT JOIN dbo.Bank AS b ON b.Bankid = c.Bankid
ORDER BY c.Checkid
"""


def _bool(v) -> bool:
    if isinstance(v, bool):
        return v
    if v is None:
        return False
    return str(v).strip().lower() in ("1", "true", "-1", "yes")


def _int(v) -> int | None:
    if v is None or str(v).strip() == "":
        return None
    return int(Decimal(str(v)))


def _dec(v) -> Decimal | None:
    if v is None or str(v).strip() == "":
        return None
    try:
        return Decimal(str(v).replace(",", ""))
    except InvalidOperation:
        return None


def _text(v) -> str | None:
    return None if v is None else str(v)


def row_to_cheque(r: dict) -> LegacyCheque:
    g = {k.lower(): v for k, v in r.items()}
    return LegacyCheque(
        checkid=_int(g["checkid"]) or 0,
        dp=_int(g.get("dp")) or 0,
        checktype=_int(g.get("checktype")) if _int(g.get("checktype")) is not None else -1,
        stcode=_int(g.get("stcode")) or 0,
        checkno=_text(g.get("checkno")),
        bank_name=_text(g.get("bankname")),
        amount=_dec(g.get("amount")) or Decimal(0),
        iscur=_bool(g.get("iscur")),
        curid=_int(g.get("curid")),
        currate=_dec(g.get("currate")),
        person=_text(g.get("person")),
        babat=_text(g.get("babat")),
        maturity=_text(g.get("maturity")),
        d1=_text(g.get("d1")),
        d2=_text(g.get("d2")),
        d3=_text(g.get("d3")),
        d4=_text(g.get("d4")),
        vagno=_text(g.get("vagno")),
        baccno=_text(g.get("baccno")),
        bundleid=_int(g.get("bundleid")),
        vos=_bool(g.get("vos")),
        backed=_bool(g.get("backed")),
        regvos=_bool(g.get("regvos")),
        ctocash=_bool(g.get("ctocash")),
        goinlaw=_bool(g.get("goinlaw")),
    )


def read_csv(path: Path) -> Iterator[LegacyCheque]:
    """CSV exported from SQL Server with EXTRACT_SQL (UTF-8, header row)."""
    with path.open(encoding="utf-8-sig", newline="") as f:
        for r in csv.DictReader(f):
            yield row_to_cheque(r)


def _connect(server: str, user: str, password: str, database: str):
    import pymssql  # imported here so CSV-only runs need no SQL Server driver

    return pymssql.connect(server=server, user=user, password=password, database=database, charset="UTF-8")


def preflight(server: str, user: str, password: str, database: str) -> list[str]:
    """Returns the required columns that dbo.Checks does not have (empty list = ready)."""
    with _connect(server, user, password, database) as conn, conn.cursor() as cur:
        cur.execute("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Checks'")
        present = {r[0].lower() for r in cur.fetchall()}
    return [c for c in REQUIRED_COLUMNS if c.lower() not in present]


def read_sqlserver(server: str, user: str, password: str, database: str) -> Iterator[LegacyCheque]:
    with _connect(server, user, password, database) as conn, conn.cursor(as_dict=True) as cur:
        cur.execute(EXTRACT_SQL)
        for r in cur:
            yield row_to_cheque(r)
