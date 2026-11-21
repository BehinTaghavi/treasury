import type { Locale } from './format'

/**
 * The latest status («آخرین وضعیت») of a received cheque, in the owner's words of 18 Nov 2026 (docs/domain/received-cheque-steps.md,
 * section 5; «اقدام حقوقی» is the name he asked for the same day). One list for every screen, so a cheque is called the same everywhere. Which status a cheque has, and whether it has
 * reached its due date, is decided by cashflow-svc; this file only names them.
 */
const stateNames: Record<string, [string, string]> = {
  InCashDesk: ['نزد صندوق', 'In the cash desk'],
  SayadRegistered: ['ثبت صیادی شد', 'Registered in Sayad'],
  DepositedForCollection: ['در جریان وصول', 'At the bank for collection'],
  Cleared: ['وصول چک', 'Collected'],
  ReturnedToIssuer: ['عودت از صندوق', 'Returned to the payer'],
  ReturnedFromBank: ['عودت چک از بانک', 'Returned by the bank'],
  Bounced: ['برگشت چک', 'Bounced'],
  InLegal: ['اقدام حقوقی', 'Legal action'],
  Replaced: ['تعویض چک', 'Replaced'],
  // Owner, 19 Nov 2026: the transfer is registered in Sayad with its tracking code; the status is «ثبت انتقال شد (خرج چک شد)».
  Endorsed: ['ثبت انتقال شد (خرج چک شد)', 'Transferred (endorsed)'],
  Cancelled: ['ابطال شده', 'Cancelled'],
}

/** The steps that lead to those statuses, as they are named on buttons and in a cheque's history. */
const actionNames: Record<string, [string, string]> = {
  Receive: ['دریافت', 'Received'],
  RegisterSayad: ['ثبت صیادی', 'Register in Sayad'],
  DepositForCollection: ['واگذاری به بانک', 'Deposit at the bank'],
  RecordCleared: ['وصول چک', 'Collected'],
  ReturnFromBank: ['عودت چک از بانک', 'Returned by the bank'],
  RecordBounced: ['برگشت چک', 'Bounced'],
  SendToLegal: ['اقدام حقوقی', 'Legal action'],
  ReturnFromLegal: ['تحویل به صندوق (قابل وصول)', 'Back to the cash desk'],
  Replace: ['تعویض چک', 'Replace'],
  Cancel: ['ابطال', 'Cancel'],
  Endorse: ['ثبت انتقال (خرج چک)', 'Transfer (endorse)'],
  RecordEndorsementReturned: ['برگشت خرج', 'Endorsement returned'],
  ReturnToIssuer: ['عودت از صندوق', 'Return to the payer'],
}

/** A promissory note has no Sayad transfer: it is endorsed on its back («خرج سفته», owner, 16 Nov 2026). */
const noteStateNames: Record<string, [string, string]> = { Endorsed: ['خرج شده', 'Endorsed'] }
const noteActionNames: Record<string, [string, string]> = { Endorse: ['خرج سفته', 'Endorse'] }

/**
 * «چک تضمین شده صیاد» (owner's report no. 6, item 18; the kind BankCheque, called «چک رمزدار بانکی» before): the drawer's bank issues
 * it and registers it in Sayad itself, so the company does not register it — it checks the cheque in Sayad («کنترل چک در صیاد»)
 * and then deposits it. The step and the steps after it are those of every cheque; only the names of this one step differ.
 */
export const GUARANTEED_SAYAD_CHEQUE = 'BankCheque'
const guaranteedStateNames: Record<string, [string, string]> = { SayadRegistered: ['کنترل چک در صیاد انجام شد', 'Checked in Sayad'] }
const guaranteedActionNames: Record<string, [string, string]> = { RegisterSayad: ['کنترل چک در صیاد', 'Check in Sayad'] }

/** The names of the kinds of received cheques and notes, the same on every screen and printed form. */
const kindNames: Record<string, [string, string]> = {
  OrdinaryCheque: ['چک', 'Cheque'], BankCheque: ['چک تضمین شده صیاد', 'Sayad guaranteed cheque'], GuaranteeCheque: ['چک تضمینی', 'Guarantee cheque'],
  PromissoryNote: ['سفته', 'Note'], GuaranteeNote: ['سفته تضمینی', 'Guarantee note'], Instalment: ['اقساط', 'Instalment'], Other: ['سایر', 'Other'],
}
export const instrumentKindName = (kind: string | null | undefined, locale: Locale) => pick(kindNames, kind, locale)

const pick = (m: Record<string, [string, string]>, key: string | null | undefined, locale: Locale) =>
  key && m[key] ? m[key][locale === 'fa' ? 0 : 1] : key ?? ''

/** The names that differ for this instrument: a note's (given as `true` or by its kind), or a Sayad guaranteed cheque's. */
const special = (kind: boolean | string | null | undefined, notes: Record<string, [string, string]>, guaranteed: Record<string, [string, string]>) =>
  (kind === true || (typeof kind === 'string' && kind.endsWith('Note')) ? notes : kind === GUARANTEED_SAYAD_CHEQUE ? guaranteed : {})

/**
 * @param kind `true` for a promissory note, whose endorsement is not a Sayad transfer; or the instrument's kind ('BankCheque',
 *   'PromissoryNote' …), so that a Sayad guaranteed cheque is named «کنترل چک در صیاد» where other cheques are «ثبت صیادی».
 */
export function chequeStateName(state: string | null | undefined, locale: Locale, kind: boolean | string | null = false) {
  const own = special(kind, noteStateNames, guaranteedStateNames)
  return pick(state && own[state] ? own : stateNames, state, locale)
}
export function chequeActionName(action: string | null | undefined, locale: Locale, kind: boolean | string | null = false) {
  const own = special(kind, noteActionNames, guaranteedActionNames)
  return pick(action && own[action] ? own : actionNames, action, locale)
}

/** «سررسید چک»: the mark of a post-dated cheque that reached its due date while in the cash desk or at the bank. */
export const dueMark = (locale: Locale) => (locale === 'fa' ? 'سررسید چک' : 'Due date reached')

/** What a cheque that reached its due date waits for (owner, 18 Nov 2026). */
export function dueWaitsFor(state: string, locale: Locale): string {
  if (state === 'DepositedForCollection')
    return locale === 'fa' ? 'در انتظار وصول چک، عودت چک از بانک یا برگشت چک' : 'Waiting for: collected, returned by the bank or bounced'
  if (state === 'InCashDesk' || state === 'SayadRegistered')
    return locale === 'fa' ? 'در انتظار واگذاری به بانک یا تعویض چک' : 'Waiting for: deposit at the bank or replacement'
  return ''
}

/** The latest status of one cheque of a receipt; since report no. 6 (items 14 and 20) with the cheque's id, number and kind. */
export interface ChequeStatus { state: string; reachedDueDate: boolean; id?: string; number?: string | null; kind?: string | null }
export interface ChequeStatusGroup { state: string; name: string; count: number; due: number }

/** The cheques of one receipt grouped by status, in the order they appear: the «وضعیت» column of the receipts list. */
export function groupChequeStatuses(statuses: ChequeStatus[] | null | undefined, locale: Locale, note = false): ChequeStatusGroup[] {
  const groups: ChequeStatusGroup[] = []
  for (const s of statuses ?? []) {
    // Grouped by the name shown: a Sayad guaranteed cheque is «کنترل چک در صیاد انجام شد» where another is «ثبت صیادی شد».
    const name = chequeStateName(s.state, locale, note || (s.kind ?? false))
    let g = groups.find(x => x.state === s.state && x.name === name)
    if (!g) groups.push(g = { state: s.state, name, count: 0, due: 0 })
    g.count++
    if (s.reachedDueDate) g.due++
  }
  return groups
}
