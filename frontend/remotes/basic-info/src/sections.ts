import type { MenuGroup } from '@treasury/ui-kit'

/**
 * The screens of «اطلاعات پایه», in the order of the new-version manual ("اطلاعات پایه" items 1–10) with the copy of the accounting
 * system's accounts, as the owner arranged them. Owner, 20 Nov 2026 (the design he approved): the list stands in the column beside
 * the main menu under small group titles; the titles only sit between the items and do not change their order.
 */
export const sections: readonly MenuGroup[] = [
  { title: ['شرکت', 'Company'], items: [
    // Owner, 16 Nov 2026: the company's name and logo, shown at the top of every screen; 18 Nov 2026: its address and legal address
    // (the city decides «خارج از مرکز» on received cheques). One page since the owner joined «اطلاعات شرکت» and «مشخصات شرکت» (18 Nov 2026).
    // First, as the system is set up with it.
    { id: 'company', title: ['مشخصات شرکت', 'Company particulars'] },
  ] },
  { title: ['جدول‌های پایه', 'Base lists'], items: [
    { id: 'countries', title: ['کشور', 'Countries'] },
    { id: 'provinces', title: ['استان / ایالت', 'Provinces'] },
    { id: 'cities', title: ['شهر', 'Cities'] },
    { id: 'currencies', title: ['نوع ارز', 'Currencies'] },
    { id: 'banks', title: ['نام بانک‌ها', 'Bank names'] },
  ] },
  { title: ['حساب‌های حسابداری', 'Accounting accounts'], items: [
    { id: 'chart', title: ['کدینگ حساب‌ها', 'Chart of accounts'] },
    { id: 'details', title: ['حساب‌های تفصیل', 'Detail accounts'] },
  ] },
  { title: ['بانک و چک', 'Banks and cheques'], items: [
    { id: 'bank-accounts', title: ['حساب‌های بانکی', 'Bank accounts'] },
    // Owner, 14 Nov 2026: cheque types first; cheque books choose from them.
    { id: 'cheque-formats', title: ['انواع چک و چاپ', 'Cheque types and printing'] },
    { id: 'cheque-books', title: ['دسته چک‌ها', 'Cheque books'] },
  ] },
  { title: ['بابت‌ها', 'Purposes'], items: [
    { id: 'references', title: ['عطف‌ها', 'References'] },
    { id: 'purposes', title: ['گروه بابت‌ها و بابت‌ها', 'Purpose groups and purposes'] },
  ] },
  { title: ['صندوق، تنخواه و کارت', 'Cash, petty cash and cards'], items: [
    { id: 'cash-desks', title: ['صندوق‌ها', 'Cash desks'] },
    { id: 'petty-cash', title: ['تنخواه گردان‌ها', 'Petty cash'] },
    // Owner's answer to Q69 (20 Nov 2026): the company's bank cards that have no bank account, for «واریز به کارت بانک».
    { id: 'company-cards', title: ['کارت‌های بانک شرکت (بدون حساب بانکی)', 'Company bank cards (no bank account)'] },
  ] },
  // Opening balances are an item of the main menu (owner, 14 Nov 2026), exposed as ./OpeningBalances.
  { title: ['سازمان و اشخاص', 'Organisation and persons'], items: [
    { id: 'departments', title: ['واحدهای سازمانی', 'Departments'] },
    { id: 'requesters', title: ['درخواست کنندگان وجوه', 'Requesters of funds'] },
    // Owner, 19 Nov 2026: one form for the three groups.
    { id: 'beneficiaries', title: ['ذینفعان / متعهدین / ضامنین', 'Beneficiaries / obligors / guarantors'] },
    // Owner, 16 Nov 2026: the holders of authorised signatures and the signature rule of each valuable document.
    { id: 'signatories', title: ['دارندگان امضای مجاز', 'Authorised signatories'] },
  ] },
  { title: ['اسناد', 'Vouchers'], items: [
    // Owner's report no. 4, item 8: the fixed accounts of treasury vouchers.
    { id: 'voucher-template', title: ['شابلون اسناد خزانه داری', 'Treasury voucher template'] },
  ] },
]
