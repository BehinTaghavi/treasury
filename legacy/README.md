# Legacy source (reference only)

This folder holds the two systems the new Treasury replaces. Use it to find out **how the old program behaved** when the design documents do not say. Do not build, run or copy code from here; the new system follows `CLAUDE.md`, not these patterns.

All passwords, connection-string secrets and the print-service hash are replaced with `***MASKED***` (or `****` in the Access code). No client data, database backups or `.mdb` files are kept here.

| Folder | What it is | Most useful for |
|---|---|---|
| `access/` | The old MS Access program `Lcash.mdb` (20+ years in production), extracted: 271 VBA modules (`vba/`), the SQL of 438 queries (`queries/`), lookup tables, the object inventory and the tables it links from SQL Server (`linked_tables.csv`). See `access/README.txt`. | Business rules R1–R18 (design document 12.4): `Form_D_Check*` (receipts), `Form_Check_Print_single` (issuing cheques), `Form_Porder` (payment orders), `Voucher_create` / `Voucher_send` modules (accounting vouchers) |
| `mvc/` | The unfinished ASP.NET MVC 5 version: controllers, views, services, EF data model (`CSD.Treasury.DataModel/*.edmx`), Stimulsoft report definitions (`*.mrt`). Vendor libraries, binaries and generated WCF proxies are left out. | Payment requests and approval flow, `PlaningType` codes, screens and reports the users already know |
| `db/mvc-schema.txt` | Tables, views and columns of the MVC version's database (95 tables/views). | Field names and meanings |
| `db/mvc-procedures-recovered.sql` | 88 of its 98 stored procedures, functions and views, **recovered as text from a backup**. Short lines (`AS`, `BEGIN`, `END`, brackets) may be missing; it does not compile. | Logic that lives only in SQL (payment status, voucher sending, balances) |
| `print-service/` | Source of `TreasuryPrintService`, the WCF cheque-printing service. Note: its password check is reversed (design document 3.6); do not repeat that. | Cheque print layout fields |
| `manuals/` | User manuals of the old and new versions (Persian text). | How users actually work; terminology for screens and reports |

The Access program keeps cheques in the shared database `Cashnajoo86` and everything else in one database per fiscal year; see design document 12.2 and `docs/domain/instrument-lifecycle.md` for how that maps to the new model.
