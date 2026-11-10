# Accounting vouchers for receipts and payments

> Farsi version: [posting-rules.fa.md](posting-rules.fa.md). Keep both in step when either changes.

Source: the owner's instruction **182-99 (22/07/1399) «راهنمای سند حسابداری پرداخت‌ها و دریافت‌ها در خزانه‌داری»**, sent 10 Nov 2026.
These rules are for Phase F (`posting-svc`); they are written down now because Basic Information already stores what they need.

## Account structure (owner, 10 Nov 2026)
- **Fixed part**, four levels that depend on each other: گروه → کل → معین → معین در معین. Each level's code starts with its parent's code; the length of each level is a company setting (Iran Nazho Co.: 1 / 2 / 4 / 6 digits).
- **Floating part**: تفصیل, a unique code of its own length (Iran Nazho Co.: 6 digits), with a type (بانکها، اشخاص و شرکت‌ها، مشتریان، کارکنان، مراکز هزینه، سفارشات، قراردادها…).
- Entries go only on an account without a lower level (a معین without معین در معین, or a معین در معین). Old program: «حساب داراي سطح معين در معين است».
- A معین / معین در معین lists the تفصیل types it allows in the first and, optionally, the second position, possibly several per position. A تفصیل of a type not listed is refused; when types are listed, the تفصیل is required. The new-generation ERP defines this on the account; for older ERPs (Iran Nazho Co.) it is set in Treasury → Basic Information → Chart of accounts.

## The rule
The **purpose group (گروه بابت)** chosen by the user is the key: each purpose group points to one معین / معین در معین. With it, the voucher is built automatically.

| | Debit (بدهکار) | Credit (بستانکار) |
|---|---|---|
| **Payment** | معین of the purpose group + **payee's** تفصیل | معین «موجودی بانک» + **the bank account's** تفصیل |
| Payment by post-dated cheque | same | معین «اسناد پرداختنی» + **payee's** تفصیل |
| **Receipt** | معین «موجودی بانک» + **the bank account's** تفصیل | معین of the purpose group + **payer's** تفصیل |
| Receipt of a post-dated cheque | معین «اسناد دریافتنی» or «اسناد نزد صندوق» + **payer's** تفصیل | same |

Examples from the instruction: payment purpose groups «خرید مواد اولیه» and «هزینه حمل» → حساب‌های پرداختنی تجاری; «پیش پرداخت خرید» → پیش پرداخت خرید کالا. Receipt purpose group «وجوه حاصل فروش» → کنترل فروش.

## Setup order (from the instruction)
Payments: 1) bank accounts (their معین already exists in the accounting system), 2) cheque books per bank account, 3) payment purpose groups. Receipts: 1) receipt purpose groups.

## Still to settle with the owner before Phase F
- Which معین is used for each instrument state (in the cash desk, deposited for collection, bounced, cleared): the bank account form already has «معین واگذاری» and «معین برگشتی» (manual section 5).
- Voucher per operation or per day; numbering; how vouchers reach the accounting system.
