/**
 * A received cheque or promissory note as GET /api/cashflow/instruments/{id} answers it, with its history, and the names the cheque
 * screens share. The cheque card (ChequeCard.vue) is shown by the steps screens and, since the owner's report no. 6 (items 14 and
 * 20), by the receipts lists.
 */
import { chequeActionName, chequeStateName, type Locale } from '@treasury/ui-kit'

export interface Step {
  action: string; from: string | null; to: string; date: string; userId: string; reference: string | null; note: string | null
  details: {
    accountId: string | null; partyName: string | null; documentDate: string | null; purpose: string | null; sayadTracking: string | null; receiptId: string | null
    trackingCode: string | null; partyDetailCode?: string | null; paymentRequestId?: string | null
    /** Cancellation: «اصل چک ممهور به مهر ابطال شد» and the computer's date and time when it was recorded (answer to Q62). */
    originalStamped?: boolean; recordedAt?: string | null
    sheetNumber?: number | null; signers?: { id: string; name: string; position: string | null }[] | null
  } | null
}

export interface Cheque {
  id: string; kind: string; number: string | null; bankName: string | null; amount: number; currency: string; amountInRial: number
  dueDate: string | null; counterparty: string; state: string; collectionAccountId: string | null; destination: string | null; isPostDated: boolean
  reachedDueDate: boolean
  allowedActions: string[]; receiptId: string | null; history: Step[]; counterpartyDetailCode: string | null
  /** «بابت» of the cheque's receipt, shown as the cheque's description (report no. 6, item 4). */
  purpose?: string | null
  details: { branchName: string | null; sayadId: string | null; onDemand?: boolean; accountNumber?: string | null; accountHolder?: string | null
    /**
     * A cheque of a transfer between two bank accounts of the company (owner's report no. 7, «نقل و انتقال (انتقال گیرنده)»): its
     * transfer, and the destination account — the only account it is deposited at. Such a cheque came on no receipt.
     */
    transferId?: string | null; transferAccountId?: string | null } | null
}

/** A cheque the company issued from one of its own accounts for a transfer to another (owner's report no. 7). */
export const isTransferCheque = (c: Pick<Cheque, 'details'> | null | undefined) => !!c?.details?.transferId
export const transferChequeMark = (locale: Locale) => (locale === 'fa' ? 'نقل و انتقال' : 'Transfer')

export const isNoteKind = (kind: string | null | undefined) => !!kind && kind.endsWith('Note')

/** The words of the cheque screens for a promissory note: سفته for چک, واخواست‌نامه for the bounce notice, متعهد for the payer. */
const noteWords: Record<Locale, [string, string][]> = {
  fa: [['چکی ', 'سفته‌ای '], ['چک', 'سفته'], ['اعلامیهٔ برگشت', 'واخواست‌نامه'], ['اعلامیه', 'واخواست‌نامه'], ['علت برگشت', 'علت واخواست'],
    ['پرداخت کننده', 'متعهد'], ['پرداخت‌کننده', 'متعهد']],
  en: [['cheques', 'notes'], ['Cheque', 'Note'], ['cheque', 'note'], ['bounce notice', 'protest'], ['Bounce notice', 'Protest'], ['Payer', 'Obligor'], ['payer', 'obligor']],
}
export const noteWording = (text: string, locale: Locale) => noteWords[locale].reduce((s, [a, b]) => s.replaceAll(a, b), text)

// The owner's names, shared by every screen (ui-kit chequeStatus); for a note a bounce is «واخواست» and a return «استرداد» (#10).
const noteStateNames: Record<string, [string, string]> = {
  Bounced: ['واخواست شده', 'Protested'], ReturnedToIssuer: ['مسترد شده به متعهد', 'Handed back to the obligor'],
}
const noteActionNames: Record<string, [string, string]> = {
  RecordBounced: ['واخواست', 'Protest'], ReturnToIssuer: ['استرداد به متعهد', 'Hand back to the obligor'],
}

/**
 * The name of a status or step of this instrument. `kind` is the instrument's kind, or `true` for "a note" where the kind is not at
 * hand: a note has its own words, and a Sayad guaranteed cheque is «کنترل چک در صیاد» where other cheques are «ثبت صیادی» (owner's
 * report no. 6, item 18).
 */
export function stateNameOf(state: string | null | undefined, kind: string | boolean | null | undefined, locale: Locale): string {
  const note = kind === true || isNoteKind(kind as string)
  if (note && state && noteStateNames[state]) return noteStateNames[state][locale === 'fa' ? 0 : 1]
  const name = chequeStateName(state, locale, note ? true : (kind as string | null) ?? false)
  return note ? noteWording(name, locale) : name
}
export function actionNameOf(action: string | null | undefined, kind: string | boolean | null | undefined, locale: Locale): string {
  const note = kind === true || isNoteKind(kind as string)
  if (note && action && noteActionNames[action]) return noteActionNames[action][locale === 'fa' ? 0 : 1]
  const name = chequeActionName(action, locale, note ? true : (kind as string | null) ?? false)
  return note ? noteWording(name, locale) : name
}
