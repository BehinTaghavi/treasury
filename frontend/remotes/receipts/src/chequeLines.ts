import { normaliseJalali, parseAmount } from '@treasury/ui-kit'

/**
 * What the cheque receipt form shows about its lines besides the boxes (owner, 20 Nov 2026, the design he approved): a cheque that
 * was entered folds into one line, a folded line that still lacks something is marked «ناقص», and the summary beside the form counts
 * the current and the post-dated cheques. Nothing here decides what is saved: the server checks every receipt again.
 */
export interface ChequeLineFacts { kind: string; number: string; dueDate: string; currency: string; amount: string; rate: string; bankId: string; sayadId: string }

export const isCheque = (kind: string) => kind.endsWith('Cheque')
export const isGuarantee = (kind: string) => kind.startsWith('Guarantee')

/** The Rial value of an amount as typed: the amount itself in Rial, amount × rate in a foreign currency; null while either is unreadable. */
export function rialOf(amount: string, currency: string, rate: string): number | null {
  const a = parseAmount(amount)
  if (a === null) return null
  if (currency === 'IRR') return a
  const r = parseAmount(rate)
  return r === null ? null : Math.round(a * r)
}

/** Current (روز) when due on or before the receipt date; post-dated (مدت‌دار) after it. Manual: «چک روز … به صورت اتوماتیک تیک می خورد». */
export function lineTiming(dueDate: string, receiptDate: string): 'current' | 'later' | null {
  const due = normaliseJalali(dueDate)
  const date = normaliseJalali(receiptDate)
  if (!due || !date) return null
  return due > date ? 'later' : 'current'
}

/**
 * A line that still lacks one of the fields the form marks as required: number, amount, due date (a guarantee cheque may have none,
 * but a date that is typed must be a date), the rate of a foreign-currency cheque, and for a cheque its bank and Sayad id.
 */
export function lineIncomplete(l: ChequeLineFacts): boolean {
  if (!l.number.trim() || parseAmount(l.amount) === null) return true
  if (l.dueDate.trim() ? !normaliseJalali(l.dueDate) : !isGuarantee(l.kind)) return true
  if (l.currency !== 'IRR' && parseAmount(l.rate) === null) return true
  return isCheque(l.kind) && (!l.bankId || !l.sayadId.trim())
}

export interface LinesSummary { current: { count: number; rial: number }; later: { count: number; rial: number }; undated: { count: number; rial: number } }

/** The cheques of the receipt by timing, with the Rial total of each; a line without a readable due date is counted apart. */
export function summariseLines(lines: readonly ChequeLineFacts[], receiptDate: string): LinesSummary {
  const s: LinesSummary = { current: { count: 0, rial: 0 }, later: { count: 0, rial: 0 }, undated: { count: 0, rial: 0 } }
  for (const l of lines) {
    const part = s[lineTiming(l.dueDate, receiptDate) ?? 'undated']
    part.count++
    part.rial += rialOf(l.amount, l.currency, l.rate) ?? 0
  }
  return s
}
