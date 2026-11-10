# ADR 0003 — Fiscal-year LIST partitions; instrument by origin year, steps by event year

Status: accepted (9 Nov 2026)

Decision: `cashflow.instrument` is partitioned by the year the instrument was created in and stays there; `cashflow.instrument_event` by the year each step happens in. Closing a year puts a trigger on that year's event partition so no step can be added, changed or removed. There is no DEFAULT partition: data for a year that was never opened is an error.
Consequence: a cheque received in 1404 and cleared in 1405 keeps one record, and its clearing (and voucher) belongs to 1405. Old years can be moved to cheaper storage with `cashflow.archive_fiscal_year`.
