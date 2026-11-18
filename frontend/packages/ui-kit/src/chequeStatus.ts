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
  Endorsed: ['خرج شده', 'Endorsed'],
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
  Replace: ['تعویض چک', 'Replace'],
  Cancel: ['ابطال', 'Cancel'],
  Endorse: ['خرج چک', 'Endorse'],
  RecordEndorsementReturned: ['برگشت خرج', 'Endorsement returned'],
  ReturnToIssuer: ['عودت از صندوق', 'Return to the payer'],
}

const pick = (m: Record<string, [string, string]>, key: string | null | undefined, locale: Locale) =>
  key && m[key] ? m[key][locale === 'fa' ? 0 : 1] : key ?? ''

export const chequeStateName = (state: string | null | undefined, locale: Locale) => pick(stateNames, state, locale)
export const chequeActionName = (action: string | null | undefined, locale: Locale) => pick(actionNames, action, locale)

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

export interface ChequeStatus { state: string; reachedDueDate: boolean }
export interface ChequeStatusGroup { state: string; name: string; count: number; due: number }

/** The cheques of one receipt grouped by status, in the order they appear: the «وضعیت» column of the receipts list. */
export function groupChequeStatuses(statuses: ChequeStatus[] | null | undefined, locale: Locale): ChequeStatusGroup[] {
  const groups: ChequeStatusGroup[] = []
  for (const s of statuses ?? []) {
    let g = groups.find(x => x.state === s.state)
    if (!g) groups.push(g = { state: s.state, name: chequeStateName(s.state, locale), count: 0, due: 0 })
    g.count++
    if (s.reachedDueDate) g.due++
  }
  return groups
}
