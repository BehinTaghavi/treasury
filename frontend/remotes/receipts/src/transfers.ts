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
export type TransferState = 'Recorded' | 'ChequeIssued' | 'ChequeReceived' | 'Collected' | 'Deleted'

/** The cheque of a cheque transfer: the same sheet on the payment side (issued) and on the receipts side (received). */
export interface TransferCheque {
  issuedChequeId: string; chequeBookId: string; number: string; sayadId: string; dueDate: string
  receivedChequeId: string | null; receivedOn: string | null; collectedOn: string | null
  /** The latest status of the cheque on the receipts side (an instrument state), once it is there. */
  receivedState: string | null; reachedDueDate: boolean
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
}
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
  switch (t.state) {
    case 'Deleted': return fa ? 'حذف شده' : 'Deleted'
    case 'ChequeIssued': return fa ? 'پرداخت ثبت صیاد شد – در انتظار ثبت در دریافت' : 'Issued and registered in Sayad – waiting for the receipts side'
    case 'Collected': return fa ? 'وصول شد' : 'Collected'
    case 'ChequeReceived': {
      const state = chequeStateName(t.cheque?.receivedState ?? 'InCashDesk', locale)
      return fa ? `دریافت شد – ${state}` : `Received – ${state}`
    }
    default: return fa ? 'ثبت شده' : 'Recorded'
  }
}

/** The total of the rows that count: deleted transfers are left out, as deleted receipts are. */
export const totalRial = (rows: readonly Pick<Transfer, 'state' | 'amountRial'>[]) => rows.filter(r => r.state !== 'Deleted').reduce((s, r) => s + r.amountRial, 0)
