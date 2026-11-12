-- Numbers of a cheque book (owner, 12 Nov 2026): the series (شماره سری) is 4 digits, the serials (شماره سریال) 6 to 8 digits
-- (checked by the application; old books keep what they had), and the Sayad ID (شماره صیادی, 16 digits per sheet): 4 digits typed
-- when the cheque is issued, 8 fixed middle digits of the book, and a 4-digit serial running from sayad_first, one per sheet.
-- Old books from before Sayad cheques have no Sayad numbers.
ALTER TABLE cashflow.cheque_book ADD COLUMN IF NOT EXISTS sayad_middle char(8) CONSTRAINT cheque_book_sayad_middle CHECK (sayad_middle ~ '^[0-9]{8}$');
ALTER TABLE cashflow.cheque_book ADD COLUMN IF NOT EXISTS sayad_first smallint CONSTRAINT cheque_book_sayad_first CHECK (sayad_first BETWEEN 0 AND 9999);
ALTER TABLE cashflow.cheque_book DROP CONSTRAINT IF EXISTS cheque_book_sayad_pair;
ALTER TABLE cashflow.cheque_book ADD CONSTRAINT cheque_book_sayad_pair CHECK ((sayad_middle IS NULL) = (sayad_first IS NULL));
