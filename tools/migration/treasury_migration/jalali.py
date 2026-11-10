"""Jalali (Shamsi) <-> Gregorian conversion.

Algorithm of jalaali-js (Borkowski's leap-year breaks), valid for Jalali years -61..3177.
tests/test_jalali.py checks it day by day against .NET's PersianCalendar for 1380-1420,
so the migration and the .NET services always agree on dates.
"""
from __future__ import annotations

from datetime import date, timedelta

_BREAKS = [-61, 9, 38, 199, 426, 686, 756, 818, 1111, 1181, 1210, 1635, 2060, 2097, 2192, 2262, 2324, 2394, 2456, 3178]


def _div(a: int, b: int) -> int:
    return int(a / b)


def _mod(a: int, b: int) -> int:
    return a - int(a / b) * b


def _jal_cal(jy: int) -> tuple[int, int, int]:
    """Returns (leap, gregorian year, march day of Farvardin 1)."""
    bl = len(_BREAKS)
    gy = jy + 621
    leap_j = -14
    jp = _BREAKS[0]
    if jy < jp or jy >= _BREAKS[bl - 1]:
        raise ValueError(f"Jalali year {jy} out of range")
    jump = 0
    for i in range(1, bl):
        jm = _BREAKS[i]
        jump = jm - jp
        if jy < jm:
            break
        leap_j = leap_j + _div(jump, 33) * 8 + _div(_mod(jump, 33), 4)
        jp = jm
    n = jy - jp
    leap_j = leap_j + _div(n, 33) * 8 + _div(_mod(n, 33) + 3, 4)
    if _mod(jump, 33) == 4 and jump - n == 4:
        leap_j += 1
    leap_g = _div(gy, 4) - _div((_div(gy, 100) + 1) * 3, 4) - 150
    march = 20 + leap_j - leap_g
    if jump - n < 6:
        n = n - jump + _div(jump + 4, 33) * 33
    leap = _mod(_mod(n + 1, 33) - 1, 4)
    if leap == -1:
        leap = 4
    return leap, gy, march


def is_leap(jy: int) -> bool:
    return _jal_cal(jy)[0] == 0


def month_length(jy: int, jm: int) -> int:
    if jm <= 6:
        return 31
    if jm <= 11:
        return 30
    return 30 if is_leap(jy) else 29


def to_gregorian(jy: int, jm: int, jd: int) -> date:
    if not (1 <= jm <= 12) or not (1 <= jd <= month_length(jy, jm)):
        raise ValueError(f"invalid Jalali date {jy:04d}/{jm:02d}/{jd:02d}")
    _, gy, march = _jal_cal(jy)
    start = date(gy, 3, march)
    day_of_year = (jm - 1) * 31 - _div(jm, 7) * (jm - 7) + jd - 1
    return start + timedelta(days=day_of_year)


def from_gregorian(d: date) -> tuple[int, int, int]:
    jy = d.year - 621
    _, gy, march = _jal_cal(jy)
    start = date(gy, 3, march)
    if d < start:
        jy -= 1
        _, gy, march = _jal_cal(jy)
        start = date(gy, 3, march)
    k = (d - start).days
    if k < 186:
        return jy, 1 + k // 31, 1 + k % 31
    k -= 186
    return jy, 7 + k // 30, 1 + k % 30


def parse_legacy(text: str | None) -> date | None:
    """'14050818' or '1405/08/18' -> date. Returns None for empty, '0000/00/00' or invalid text."""
    if text is None:
        return None
    digits = text.strip().replace("/", "").replace("-", "")
    if len(digits) != 8 or not digits.isdigit():
        return None
    try:
        return to_gregorian(int(digits[:4]), int(digits[4:6]), int(digits[6:]))
    except ValueError:
        return None


def jalali_year(d: date) -> int:
    return from_gregorian(d)[0]


def fiscal_year_bounds(jy: int) -> tuple[date, date]:
    """First and last day of a fiscal year equal to the Jalali calendar year (open question Q5)."""
    return to_gregorian(jy, 1, 1), to_gregorian(jy + 1, 1, 1) - timedelta(days=1)
