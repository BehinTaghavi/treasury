import type { MenuGroup } from '@treasury/ui-kit'

/**
 * The screens of «دریافت‌ها», in the order the owner gave (report no. 6, item 12: the most used and most important first) — cheque
 * receipt, cheque steps, cash, «واریز به بانک», «واریز به کارت بانک», note receipt, note steps, legal action — with the list,
 * «فهرست جامع دریافت‌ها», last in a coloured box. Owner, 20 Nov 2026 (the design he approved): they stand in a column beside the main
 * menu instead of a row of tabs, with a small title above each group; the titles do not change the order.
 */
export type ReceiptScreen = 'Instruments' | 'collection' | 'Cash' | 'BankDeposit' | 'CardDeposit' | 'Notes' | 'noteSteps' | 'legal' | 'list'

export const receiptsMenu: readonly MenuGroup<ReceiptScreen>[] = [
  { title: ['چک', 'Cheques'], items: [
    { id: 'Instruments', title: ['دریافت چک', 'Cheque receipt'] },
    // Owner's report no. 4, item 9, and item 14: every step of a received cheque after receipt.
    { id: 'collection', title: ['عملیات چک‌های دریافتی', 'Received cheque steps'] },
  ] },
  { title: ['نقد و بانک', 'Cash and bank'], items: [
    { id: 'Cash', title: ['دریافت نقدی', 'Cash receipt'] },
    // «واریز به بانک» replaces «واریز به حساب / کارتخوان» (report no. 6, item 12).
    { id: 'BankDeposit', title: ['واریز به بانک', 'Bank deposit'] },
    // Money paid onto the bank card of a petty-cash holder, or onto a company card without a bank account (report no. 6, item 12).
    { id: 'CardDeposit', title: ['واریز به کارت بانک', 'Deposit onto a bank card'] },
  ] },
  { title: ['سفته', 'Promissory notes'], items: [
    // Owner's report no. 4, item 13, and his form «فرم دریافت سفته» (16 Nov 2026): promissory notes have a form of their own.
    { id: 'Notes', title: ['دریافت سفته', 'Note receipt'] },
    // Owner, 16 Nov 2026: the steps of a note after receipt (to the bank, collected, handed back, endorsed…).
    { id: 'noteSteps', title: ['عملیات سفته‌های دریافتی', 'Received note steps'] },
  ] },
  { title: ['پیگیری', 'Follow-up'], items: [
    // Owner, 18 Nov 2026: the legal action on a bounced cheque has its own screen.
    { id: 'legal', title: ['اقدام حقوقی', 'Legal action'] },
  ] },
  // «جامع» was added and the item is drawn in a coloured box (report no. 6, item 12).
  { items: [{ id: 'list', title: ['فهرست جامع دریافت‌ها', 'All receipts'], boxed: true }] },
]
