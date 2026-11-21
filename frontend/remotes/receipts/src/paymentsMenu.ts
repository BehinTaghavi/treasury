import type { MenuGroup } from '@treasury/ui-kit'

/**
 * The screens of «پرداخت‌ها» built so far (owner's report no. 7, 20 Nov 2026: «آثار پرداخت نقل و انتقال را در بخش پرداخت هم پیاده‌سازی
 * فرمایید»): the payment side of a transfer by Sayad cheque — «نقل و انتقال (انتقال دهنده)» in the cheque payments — and the
 * electronic transfer between the company's own accounts. The other payment screens (cheques to beneficiaries, bank transfers, cash)
 * come with Phase E and take their places in these groups.
 */
export type PaymentScreen = 'transferCheque' | 'electronicTransfer'

export const paymentsMenu: readonly MenuGroup<PaymentScreen>[] = [
  { title: ['چک', 'Cheques'], items: [
    { id: 'transferCheque', title: ['پرداخت چک – نقل و انتقال (انتقال دهنده)', 'Cheque payment – transfer (the payer)'] },
  ] },
  { title: ['نقل و انتقال بانکی', 'Bank transfers'], items: [
    { id: 'electronicTransfer', title: ['انتقال الکترونیکی بین حساب‌های شرکت', 'Electronic transfer between company accounts'] },
  ] },
]
