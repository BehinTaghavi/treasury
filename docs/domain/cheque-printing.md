# Cheque types and cheque printing — proposal

> Farsi version: [cheque-printing.fa.md](cheque-printing.fa.md). Keep both in step when either changes.
> Status: **proposal, waiting for the owner's answers** (questions Q12–Q18 below). Built in Phase E, after opening balances.

The owner asked (14 Nov 2026): if one bank can have different types of cheques, what has to change; and how the scan of each
type is used to place the variable fields (date, date in words, purpose, amount, amount in words, fixed texts such as
«غیرقابل انتقال») so that, once a payment is finally approved, the raw cheque is printed with every value in its place.

## 1. Where we stand

- A **cheque type** (نوع چک) has a print number, one of seven print types (استاندارد and the manual's six), a scan of the cheque
  and a scope: **one bank account**, or **standard for all accounts** (owner's report no. 3, item 3).
- A **cheque book** names the type it is printed with; the type can be changed at any time, even after sheets were used.
- Issuing a cheque (`IssueChequeAsync`) already requires an **approved payment** and reserves the next free sheet (rule R5).

So one account can already have several types at once (e.g. old large and new standard books), and each book says which.

## 2. A bank with several cheque types — what changes

| Need | Today | Proposed change |
|---|---|---|
| A design used by **all accounts of one bank** (e.g. Mellat's own large cheque) | Must be defined per account, or as standard for all banks | Add a third scope, **bank**: types are found in the order *account → bank → standard*; the cheque book list shows all three, the most specific first |
| Different **sizes** (large, small, standard) | Only the print type name says it | Each type stores the real **width and height in millimetres** |
| Different **field positions** per design | Not stored | Each type has its own **layout** (section 3) |
| Same design, different **printer** | — | Printer calibration is separate from the layout (section 4), so one layout serves every printer |
| Copying a layout to a similar type | — | «کپی از نوع دیگر» on the designer |

Nothing changes for cheque books or sheets: a book keeps naming one type; issuing takes the book's type and its layout.

## 3. The layout of a cheque type (field positions)

The scan, shown at its **real size** behind the designer, is used to place a box for each field. Positions are stored in
**millimetres from the top-right corner** of the cheque (Persian reading direction), never in screen pixels: the MVC version
stored pixels (`ChequeFormatTypeDetail.m_Left/m_TOP`) and the print moved with every screen and printer.

**Fields** (from the MVC label list `BasicChequeLables` and the owner's request; Q13 confirms which appear on Iran Nazho's cheques):

| Field | Example | Notes |
|---|---|---|
| Date (numbers) | ۱۴۰۵/۰۸/۲۲ | Due date of the cheque |
| Date in words | بیست و دوم آبان یک هزار و چهارصد و پنج | |
| Day / month / year separately (numbers or words) | ۲۲ / آبان / ۱۴۰۵ | For cheques with separate boxes |
| Amount (numbers) | ***۱۲٬۵۰۰٬۰۰۰*** | Guard characters so nothing can be added (MVC: «***/…/***») |
| Amount in words | دوازده میلیون و پانصد هزار ریال تمام | Persian; ending «تمام» as in the MVC version (Q15) |
| Payee (در وجه) | شرکت نمونه بازرگانی | Beneficiary's registered legal title |
| Payee's national code / ID | 10101010103 | From the beneficiary |
| Purpose (بابت) | بابت خرید مواد اولیه | Editable at issue (owner, 13 Nov) |
| Payee and purpose together | | MVC `beneficiaryandbehalf` |
| Pay into account / Sheba | IR46 0120 … | For «واریز به حساب» cheques |
| Gregorian date, English amount | 2026/11/13, Twelve million… | Only for foreign-currency cheques (Q18) |
| Fixed texts | «غیرقابل انتقال»، «حامل»، «قابل پرداخت به ذینفع اول»، crossing lines | Printed when the option is chosen for the cheque (Q14) |
| Free text | any | Typed once on the layout |

**Per field:** position (x, y), width and height (mm), font (e.g. Vazirmatn, B Nazanin), size, bold, alignment, number of lines,
shrink-to-fit, shown or hidden.

**Designer screen** (on «انواع چک و چاپ»): drag and resize the boxes over the scan; arrow keys move 0.5 mm; sample values
show how a long payee name or amount fits; **test print** on plain A4 with the cheque outline, to lay over a real cheque.

## 4. Printing

1. **When:** only after the payment is finally approved; issuing reserves the sheet, then «چاپ چک» is offered.
2. **How:** the system builds the cheque page at its exact size in millimetres with the fonts embedded; the browser prints it at
   **100 % (actual size)**. No separate Windows print service is needed (the MVC version had one).
3. **Printer calibration:** once per printer, a small shift (x/y in mm) and how the cheque is fed (centre or edge of the tray),
   set with a test print; stored per printer, not per layout (Q16).
4. **Options per cheque** (at issue): non-transferable, bearer, crossed, pay into account/Sheba — they switch the fixed texts on
   or off (Q14).
5. **Log and control:** every print is recorded (who, when, which printer). A **reprint** needs a reason (Q17). A spoiled sheet is
   voided with its reason (rule R5 already allows it) and the next sheet is used.
6. **Amount and date in words** are business rules with tests: a Persian number-to-words service (up to hundreds of
   billions, with «و» between groups) and Jalali date-to-words.

## 5. Changes in the system (for the reviewer)

- **Domain (cashflow-svc):** `ChequeFormat` gets `Scope` (Standard / Bank / Account) with `BankId`, `WidthMm`, `HeightMm` and a
  `ChequeLayout` (list of `LayoutField`: kind, x, y, width, height, font, size, bold, alignment, lines, text); new
  `PersianWords` (amount and date in words) with tests; `ChequePrint` record (instrument, sheet, user, time, printer, reason).
- **Application:** save layout; choose the type for a book from account → bank → standard; print a cheque (approved payment,
  issued instrument) returning the page; reprint with reason.
- **Schema:** layout as a JSON column on `cashflow.cheque_format`, `bank_id`, size columns; table `cashflow.cheque_print`;
  printer calibration table.
- **Screens:** layout designer (scan background, drag boxes, test print); «چاپ چک» on the payment / cheque issue screen;
  printer calibration.
- **Libraries:** none new; the page is built with CSS millimetre units and printed by the browser (no paid PDF library).

## 6. Questions for the owner

| # | Question | Proposed answer |
|---|---|---|
| Q12 | Do we need types shared by **all accounts of one bank** (scope "bank"), or are standard + per-account types enough? | Add the bank scope; it costs little and saves defining the same design for each account |
| Q13 | Which fields are printed on Iran Nazho's cheques today? A scan of a **filled-in** cheque would settle it. | The list in section 3 |
| Q14 | Which fixed texts and options do you use (غیرقابل انتقال، حامل، بدون خط، قابل پرداخت به ذینفع اول، واریز به حساب)? Chosen per cheque, or default per purpose group? | Chosen per cheque, with a default per purpose group |
| Q15 | Amount in words in **ریال** or **تومان**, ending with «تمام»? | ریال … تمام |
| Q16 | Which printer, and how is the cheque fed (single sheet, envelope feeder, centre or edge)? | Calibrated once per printer |
| Q17 | Who may reprint a cheque, and must a reason be given? | Treasury users with print permission; reason required; every print logged |
| Q18 | Are cheques ever printed in English (foreign-currency accounts, Gregorian date)? | Only for foreign-currency accounts |
