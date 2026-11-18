/**
 * Kinds of a direct deposit into a company bank account (owner, 18 Nov 2026), named once for the form, the receipts list and the
 * list panel. «حواله / انتقال» (Transfer) is the combined kind of receipts recorded before; it is shown but no longer chosen.
 */
export interface DepositMethod { value: string; fa: string; en: string; retired?: boolean }

export const depositMethods: DepositMethod[] = [
  { value: 'Paya', fa: 'حواله پایا با شماره شبا', en: 'Paya transfer (with Sheba)' },
  { value: 'Satna', fa: 'حواله ساتنا با شماره شبا', en: 'Satna transfer (with Sheba)' },
  { value: 'InternalTransfer', fa: 'انتقال داخلی حساب به حساب', en: 'Internal transfer, account to account' },
  { value: 'DepositSlip', fa: 'واریز نقدی در شعبه', en: 'Cash paid in at a branch' },
  { value: 'CardToCard', fa: 'کارت به کارت', en: 'Card to card' },
  { value: 'PaymentGateway', fa: 'واریز به درگاه پرداخت متصل به حساب شرکت', en: "Payment gateway of the company's account" },
  { value: 'Pos', fa: 'کارتخوان (POS)', en: 'Card reader (POS)' },
  { value: 'Transfer', fa: 'حواله / انتقال (ثبت‌های قبلی)', en: 'Transfer (earlier receipts)', retired: true },
]

export const depositMethodName = (value: string | null | undefined, fa: boolean) => {
  const m = depositMethods.find(x => x.value === value)
  return m ? (fa ? m.fa : m.en) : value ?? ''
}

/** Paya and Satna transfers are recorded with the Sheba they came from. */
export const needsSheba = (method: string) => method === 'Paya' || method === 'Satna'

/** A foreign-currency account takes cash deposits and bank transfers only (owner's report no. 4, item 7). */
export const forForeignCurrency = (method: string) => ['DepositSlip', 'Paya', 'Satna', 'InternalTransfer'].includes(method)
