-- A cheque serial (شماره سریال) may start with 0 (owner, 12 Nov 2026). Sheets stay numbers (first_sheet, last_sheet, cheque_sheet.sheet)
-- so ranges and overlaps are plain arithmetic; serial_digits keeps the printed width so that 056780 is shown as 056780, not 56780.
-- Empty for books saved before this column: their width is the digits of first_sheet.
ALTER TABLE cashflow.cheque_book ADD COLUMN IF NOT EXISTS serial_digits smallint CONSTRAINT cheque_book_serial_digits CHECK (serial_digits BETWEEN 1 AND 18);
