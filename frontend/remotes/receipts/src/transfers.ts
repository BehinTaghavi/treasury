/**
 * Transfers between the company's own bank accounts («نقل و انتقال بین بانکی», owner's report no. 7 of 20 Nov 2026): the shape of a
 * transfer as GET /api/cashflow/transfers answers it, the names of the ways of transfer, and the small rules the screens share.
 * The rules themselves (which way fits which accounts, the limits, the steps of the cheque) are decided by cashflow-svc; this file
 * only names things and prepares the forms.
 */
import { chequeStateName, formatAmount, toLatinDigits, type Locale } from '@treasury/ui-kit'

export type TransferMethod = 'Internal' | 'Pol' | 'Paya' | 'Satna' | 'Cheque'
export type ElectronicMethod = Exclude<TransferMethod, 'Cheque'>
export type TransferChannel = 'InPerson' | 'Remote'
export type TransferState = 'Recorded' | 'ChequeIssued' | 'ChequeReceived' | 'Collected' | 'Deleted' | 'Cancelled'

/** The cheque of a cheque transfer: the same sheet on the payment side (issued) and on the receipts side (received). */
export interface TransferCheque {
  issuedChequeId: string; chequeBookId: string; number: string; sayadId: string; dueDate: string
  receivedChequeId: string | null; receivedOn: string | null; collectedOn: string | null
  /** The latest status of the cheque on the receipts side (an instrument state), once it is there. */
  receivedState: string | null; reachedDueDate: boolean
  /** The day the payer registered the cheque in Sayad («پرداخت ثبت صیاد شد»); null: not yet, and the cheque can still be cancelled (answer to Q79). */
  payerSayadOn?: string | null
  /** A post-dated cheque stays in «اسناد پرداختنی» until its maturity («سررسید») is recorded (answer to Q81). */
  isPostDated?: boolean; maturedOn?: string | null; canMature?: boolean
  cancelledOn?: string | null; cancelReason?: string | null
  /** The code of the rule that closes «ابطال چک», or null while it is open. */
  whyNotCancellable?: string | null
}
export interface Transfer {
  id: string; fiscalYear: number; number: number; date: string; method: TransferMethod; channel: TransferChannel | null
  sourceAccountId: string; sourceAccountNumber: string; sourceBankId: string; sourceBankName: string
  destinationAccountId: string; destinationAccountNumber: string; destinationBankId: string; destinationBankName: string
  amount: number; currency: string; rateToRial: number | null; amountRial: number; trackingNumber: string | null; description: string | null
  state: TransferState; recordedBy: string; deletedBy: string | null; deletedAt: string | null; cheque: TransferCheque | null
  payerVoucherConfirmed: boolean; receiverVoucherConfirmed: boolean
  /** The code of the rule that closes «ویرایش» / «حذف», or null while it is open. */
  whyNotCorrectable: string | null; whyNotDeletable: string | null
  canUndoReceive: boolean; version: number
  /** The bank's fee of an electronic transfer («کارمزد خدمات بانکی», answer to Q77). */
  fee?: number | null
  feeDetailCode?: string | null
  /** The approved payment request the transfer pays (answer to Q82). */
  paymentRequestId?: string | null; paymentRequestNumber?: number | null
  /** The voucher steps the treasurer confirmed: 0 payer / electronic, 1 receiver, 2 maturity, 3 and 4 the two sides of a cancellation. */
  confirmedSteps?: number[] | null
}

/**
 * An approved payment request of the kind «نقل و انتقال بین بانک‌های شرکت» (owner, 21 Nov 2026, answer to Q82: «برای نقل و انتقال از هر
 * نوع لازم است درخواست وجه صادر و مجوز آن اخذ شود»), as GET /api/requests/payment-requests answers it.
 */
export interface TransferRequest {
  id: string; number: number; date: string; method: string; state: string; purpose: string; amount: number; currency: string
  payFromId: string | null; transferToAccountId: string | null; paidAmount: number; remainingAmount: number; beneficiaryName: string
  /** The payments already recorded on the request (owner's answer to Q84: how many, and what is left, are shown with the request). */
  payments?: readonly { key: string; amount: number; date: string }[] | null
}
/** Whether the fee line asks for a تفصیل: the company's fee account and the تفصیل types it takes (owner's answer to Q83). */
export interface FeeAccount { accountCode: string | null; accountName: string | null; detailTypes: string[] }
/** What the source account already sent on a day by one way (owner's answer to Q84). */
export interface DayUsage { totalRialSameChannel: number; count: number }
export const OWN_TRANSFER = 'OwnAccountTransfer'
/** The requests a transfer may pay: approved ones of the kind «نقل و انتقال بین بانک‌های شرکت», lowest number first. */
export const payableRequests = (all: readonly TransferRequest[]) =>
  all.filter(r => r.method === OWN_TRANSFER && r.state === 'Approved' && r.remainingAmount > 0).sort((a, b) => a.number - b.number)
/** «مانده … از مبلغ … – از حساب … به حساب …»: a request as it is offered in the list box; the figures come first because a narrow box cuts the end. */
export function requestLabel(r: Pick<TransferRequest, 'amount' | 'remainingAmount' | 'purpose'>, from: string, to: string, locale: Locale): string {
  const n = (x: number) => formatAmount(x, locale)
  return locale === 'fa'
    ? `مانده ${n(r.remainingAmount)} از مبلغ ${n(r.amount)} – از حساب ${from} به حساب ${to}${r.purpose ? ` – ${r.purpose}` : ''}`
    : `${n(r.remainingAmount)} left of ${n(r.amount)} – from ${from} to ${to}${r.purpose ? ` – ${r.purpose}` : ''}`
}

/** The vouchers of a transfer by their step, in the owner's words. */
const voucherTitles: Record<number, [string, string]> = {
  0: ['سند انتقال دهنده', "The payer's voucher"],
  1: ['سند انتقال گیرنده', "The receiver's voucher"],
  2: ['سند سررسید چک پرداختنی', 'Maturity of the payable cheque'],
  3: ['سند ابطال – انتقال دهنده', "Cancellation – the payer's side"],
  4: ['سند ابطال – انتقال گیرنده', "Cancellation – the receiver's side"],
}
export function voucherTitle(step: number, cheque: boolean, locale: Locale): string {
  if (!cheque && step === 0) return locale === 'fa' ? 'سند نقل و انتقال' : 'Voucher of the transfer'
  const x = voucherTitles[step]
  return x ? x[locale === 'fa' ? 0 : 1] : String(step)
}
/** The steps the payment side shows (everything but the receiver's own vouchers), and the ones the receipts side shows. */
export const payerSteps: readonly number[] = [0, 2, 3]
export const receiverSteps: readonly number[] = [1, 4]
/** Every voucher of these steps that exists is confirmed. `existing` is the number of vouchers the transfer has among them. */
export const isConfirmed = (t: Pick<Transfer, 'confirmedSteps'>, step: number) => (t.confirmedSteps ?? []).includes(step)
/** A cheque transfer whose cheque can still take a step on the payment side: neither collected, cancelled nor deleted. */
export const isOpenCheque = (t: Pick<Transfer, 'state' | 'cheque'>) => !!t.cheque && (t.state === 'ChequeIssued' || t.state === 'ChequeReceived')
/** One row of «سقف‌های انتقال الکترونیکی»; null is "no limit"; `bankId` null is the general row of the way. */
export interface TransferLimit {
  method: ElectronicMethod; bankId: string | null; minPerOrder: number | null; maxPerOrderInPerson: number | null; maxPerOrderRemote: number | null
  maxPerDayInPerson: number | null; maxPerDayRemote: number | null; maxCountPerDay: number | null
}
/** What the screens need of a cheque book (GET /api/cashflow/cheque-books). */
export interface TransferChequeBook {
  id: string; bankAccountId: string; serial: string; firstSheet: number; lastSheet: number; nextSheet: number | null; remainingSheets: number
  isActive: boolean; sayadMiddle: string | null; sayadFirstSerial: number | null; serialDigits: number
}

/** The owner's names of the ways (report no. 7). */
const methodNames: Record<TransferMethod, [string, string]> = {
  Internal: ['انتقال داخلی (حساب به حساب)', 'Internal (account to account)'],
  Pol: ['پل', 'Pol'],
  Paya: ['پایا', 'Paya'],
  Satna: ['ساتنا', 'Satna'],
  Cheque: ['چک صیادی', 'Sayad cheque'],
}
export const electronicMethods: readonly ElectronicMethod[] = ['Internal', 'Pol', 'Paya', 'Satna']
export const methodName = (m: string | null | undefined, locale: Locale) => (m && m in methodNames ? methodNames[m as TransferMethod][locale === 'fa' ? 0 : 1] : m ?? '')
/** The way as it is named in a list, where the form's full name would not fit. */
export const methodShortName = (m: string | null | undefined, locale: Locale) => (m === 'Internal' ? (locale === 'fa' ? 'انتقال داخلی' : 'Internal') : methodName(m, locale))
/** What each way is for, as the owner wrote it. */
const methodUses: Record<ElectronicMethod, [string, string]> = {
  Internal: ['دو حساب شرکت در یک بانک (همان شعبه یا شعبهٔ دیگر)', 'two accounts of the company in the same bank'],
  Pol: ['دو بانک متفاوت – مبالغ کم و فوری', 'two different banks – small, urgent amounts'],
  Paya: ['دو بانک متفاوت – مبالغ متوسط', 'two different banks – middle amounts'],
  Satna: ['دو بانک متفاوت – مبالغ بالا', 'two different banks – large amounts'],
}
export const methodUse = (m: ElectronicMethod, locale: Locale) => methodUses[m][locale === 'fa' ? 0 : 1]

const channelNames: Record<TransferChannel, [string, string]> = {
  InPerson: ['حضوری (در شعبه)', 'At the branch'],
  Remote: ['غیرحضوری (اینترنت‌بانک / همراه‌بانک)', 'Remote (internet / mobile bank)'],
}
export const channels: readonly TransferChannel[] = ['InPerson', 'Remote']
export const channelShortName = (c: string | null | undefined, locale: Locale) =>
  (c === 'InPerson' ? (locale === 'fa' ? 'حضوری' : 'branch') : c === 'Remote' ? (locale === 'fa' ? 'غیرحضوری' : 'remote') : '')
export const channelName = (c: string | null | undefined, locale: Locale) => (c && c in channelNames ? channelNames[c as TransferChannel][locale === 'fa' ? 0 : 1] : c ?? '')

/**
 * The ways that fit two accounts (owner's report no. 7): within one bank the internal transfer; between two banks Pol, Paya and
 * Satna. Until both accounts are chosen every way is offered.
 */
export function waysFor(sourceBankId: string | null | undefined, destinationBankId: string | null | undefined): ElectronicMethod[] {
  if (!sourceBankId || !destinationBankId) return [...electronicMethods]
  return sourceBankId === destinationBankId ? ['Internal'] : ['Pol', 'Paya', 'Satna']
}

/** The limits that apply to a transfer out of an account of `bankId`: the bank's own row, else the general row of the way. */
export function limitFor(limits: readonly TransferLimit[], method: string, bankId: string | null | undefined): TransferLimit | undefined {
  return limits.find(l => l.method === method && l.bankId !== null && l.bankId === bankId) ?? limits.find(l => l.method === method && l.bankId === null)
}

/** «۳ میلیارد ریال», «۵۰۰ میلیون ریال», otherwise the figure with separators. */
export function shortRial(amount: number, locale: Locale): string {
  const fa = locale === 'fa'
  const n = (x: number) => formatAmount(x, locale)
  if (amount >= 1e9 && amount % 1e8 === 0) return fa ? `${n(amount / 1e9)} میلیارد ریال` : `${n(amount / 1e9)} milliard Rial`
  if (amount >= 1e6 && amount % 1e5 === 0) return fa ? `${n(amount / 1e6)} میلیون ریال` : `${n(amount / 1e6)} million Rial`
  return fa ? `${n(amount)} ریال` : `${n(amount)} Rial`
}

/**
 * The limits of a way in plain words, for the line under the way on the form. With a channel only that channel's limits are
 * named; without one, both. No limit at all gives an empty list.
 */
export function limitLines(limit: TransferLimit | undefined, channel: TransferChannel | '' | null, locale: Locale): string[] {
  if (!limit) return []
  const fa = locale === 'fa'
  const lines: string[] = []
  if (limit.minPerOrder !== null) lines.push(fa ? `حداقل هر دستور پرداخت: ${shortRial(limit.minPerOrder, locale)}` : `Least amount of one order: ${shortRial(limit.minPerOrder, locale)}`)
  const both: [TransferChannel, number | null, number | null][] = [
    ['InPerson', limit.maxPerOrderInPerson, limit.maxPerDayInPerson], ['Remote', limit.maxPerOrderRemote, limit.maxPerDayRemote]]
  for (const [c, order, day] of both) {
    if (channel && channel !== c) continue
    const name = c === 'InPerson' ? (fa ? 'حضوری' : 'at the branch') : (fa ? 'غیرحضوری' : 'remote')
    if (order !== null) lines.push(fa ? `سقف هر دستور ${name}: ${shortRial(order, locale)}` : `Most of one order ${name}: ${shortRial(order, locale)}`)
    if (day !== null) lines.push(fa ? `سقف روزانهٔ ${name} (جمع): ${shortRial(day, locale)}` : `Most of a day ${name} (together): ${shortRial(day, locale)}`)
  }
  if (limit.maxCountPerDay !== null) lines.push(fa ? `حداکثر ${formatAmount(limit.maxCountPerDay, locale)} دستور در روز` : `At most ${limit.maxCountPerDay} orders a day`)
  return lines
}

/** «بانک ملت – 123456»: an account of a transfer as it is named in lists. */
export const accountText = (bankName: string, accountNumber: string) => [bankName, accountNumber].filter(Boolean).join(' – ')

/** The serial of a sheet as printed, with its leading zeros (owner, 12 Nov 2026): 56780 with 6 digits is 056780. */
export const sheetText = (sheet: number, digits: number) => String(sheet).padStart(digits, '0')

/** The sheet number typed in the box (Persian digits allowed), or null when it is not a number. */
export function sheetNumber(text: string): number | null {
  const digits = toLatinDigits(text ?? '').trim()
  return /^\d+$/.test(digits) ? Number(digits) : null
}

/**
 * The three fixed parts of a sheet's 16-digit Sayad ID (owner, 12 Nov 2026): the book's 8 middle digits in two boxes and the sheet's
 * 4-digit Sayad serial (the first Sayad serial of the book plus the sheet's place in it). The first 4 digits are typed when the cheque is issued.
 * Null for a book without Sayad numbers or a sheet outside the book.
 */
export function sayadParts(book: Pick<TransferChequeBook, 'firstSheet' | 'lastSheet' | 'sayadMiddle' | 'sayadFirstSerial'> | null | undefined, sheet: number | null): [string, string, string] | null {
  if (!book || sheet === null || book.sayadMiddle === null || book.sayadFirstSerial === null || book.sayadMiddle.length !== 8) return null
  if (sheet < book.firstSheet || sheet > book.lastSheet) return null
  return [book.sayadMiddle.slice(0, 4), book.sayadMiddle.slice(4), String(book.sayadFirstSerial + (sheet - book.firstSheet)).padStart(4, '0')]
}

/** A Sayad ID in four groups of four, as it is printed on the cheque. */
export const groupSayad = (id: string | null | undefined) => (id ?? '').replace(/(\d{4})(?=\d)/g, '$1 ')

/**
 * The status of a cheque transfer in the owner's words: «پرداخت ثبت صیاد شد» while the cheque is only on the payment side, then the
 * latest status of the cheque on the receipts side, «وصول شد» at the end.
 */
export function chequeTransferStatus(t: Pick<Transfer, 'state' | 'cheque'>, locale: Locale): string {
  const fa = locale === 'fa'
  // Owner, 21 Nov 2026 (answer to Q79): the payer's Sayad registration is a step of its own; until it is recorded the cheque is only
  // «صادر شد» and can be cancelled.
  const registered = !!t.cheque?.payerSayadOn
  switch (t.state) {
    case 'Deleted': return fa ? 'حذف شده' : 'Deleted'
    case 'Cancelled': return fa ? 'ابطال شده' : 'Cancelled'
    case 'ChequeIssued': return registered
      ? (fa ? 'پرداخت ثبت صیاد شد – در انتظار ثبت در دریافت' : 'Issued and registered in Sayad – waiting for the receipts side')
      : (fa ? 'صادر شد – ثبت صیادی پرداخت نشده' : 'Issued – not yet registered in Sayad by the payer')
    case 'Collected': return fa ? 'وصول شد' : 'Collected'
    case 'ChequeReceived': {
      const state = chequeStateName(t.cheque?.receivedState ?? 'InCashDesk', locale)
      if (!registered) return fa ? `دریافت شد – ${state} – ثبت صیادی پرداخت نشده` : `Received – ${state} – not yet registered in Sayad by the payer`
      return fa ? `دریافت شد – ${state}` : `Received – ${state}`
    }
    default: return fa ? 'ثبت شده' : 'Recorded'
  }
}

/** The total of the rows that count: deleted transfers are left out, as deleted receipts are, and so are cancelled cheques. */
export const totalRial = (rows: readonly Pick<Transfer, 'state' | 'amountRial'>[]) =>
  rows.filter(r => r.state !== 'Deleted' && r.state !== 'Cancelled').reduce((s, r) => s + r.amountRial, 0)
