from datetime import date
from pathlib import Path

from treasury_migration import jalali

REFERENCE = Path(__file__).with_name("persian_calendar_dotnet_1380_1420.txt")


def test_matches_dotnet_persian_calendar_every_day_1380_to_1420():
    """The .NET services use System.Globalization.PersianCalendar; the migration must agree to the day."""
    mismatches = []
    for line in REFERENCE.read_text().splitlines():
        iso, jal = line.split()
        g = date.fromisoformat(iso)
        if jalali.parse_legacy(jal) != g or "%04d%02d%02d" % jalali.from_gregorian(g) != jal:
            mismatches.append(line)
    assert mismatches == []


def test_invalid_legacy_dates_become_none():
    for text in [None, "", "0000/00/00", "13861301", "1386/07/31", "abc"]:
        assert jalali.parse_legacy(text) is None


def test_fiscal_year_bounds():
    assert jalali.fiscal_year_bounds(1405) == (date(2026, 3, 21), date(2027, 3, 20))
