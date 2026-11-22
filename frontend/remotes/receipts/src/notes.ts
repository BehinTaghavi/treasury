/**
 * What the note receipt screens share (owner's form «فرم دریافت سفته», 16 Nov 2026): the shape of a note receipt as the API
 * returns it and the Persian and English names of its choices.
 */
export interface NoteChecks { signature: boolean; date: boolean; amountInWords: boolean; payee: boolean; paymentDate: boolean }
export interface NoteAttachments { noteImage: boolean; contract: boolean; representation: boolean; other: boolean }
export interface NoteGuarantor { name: string; nationalCode: string | null; detailCode?: string | null; phone?: string | null }
export interface NoteTerms {
  reason: string; form: string; maturity: string; issuerNationalCode: string | null; issuerPhone: string | null; commitmentSubject: string | null
  returnTerms: string | null; custodyPlace: string | null; originalReceived: boolean; originalReceivedOn: string | null; discrepancy: string | null
  attachments: NoteAttachments; deliveredBy: string | null; signers: NoteSigner[]
}
/** A confirmer chosen among the permission holders of note receipts («دارندگان مجوزها», Basic Information); position: roles and job title. */
export interface NoteSigner { id: string; name: string; position: string | null }
export interface NoteLine {
  id: string; kind: string; number: string | null; amount: number; amountInRial: number; dueDate: string | null; noteIssueDate?: string | null; state: string
  details: { guarantors: NoteGuarantor[] | null; onDemand?: boolean; checks?: NoteChecks | null } | null
}
export interface NoteReceipt {
  id: string; kind: string; fiscalYear: number; number: number; date: string; payerDetailCode: string; payerName: string
  purposeGroupId: string; purposeGroupName: string; purpose: string; reference1: string | null; reference2: string | null; departmentId: string | null
  note: string | null; totalRial: number; recordedBy: string; instruments: NoteLine[] | null; noteTerms: NoteTerms | null
}

type Names = Record<string, [string, string]>
export const reasonNames: Names = { Debt: ['بابت بدهی', 'For a debt'], Guarantee: ['تضمین', 'Guarantee'], Trust: ['امانی', 'In trust'], Other: ['سایر', 'Other'] }
export const formNames: Names = { Paper: ['کاغذی', 'Paper'], Electronic: ['الکترونیکی', 'Electronic'] }
export const maturityNames: Names = { Fixed: ['معین', 'On a date'], OnDemand: ['عندالمطالبه', 'On demand'] }
/** The items article 308 of the Commercial Code asks of a promissory note («کنترل مشخصات روی هر سفته»). */
export const checkNames: [keyof NoteChecks, string, string][] = [
  ['signature', 'امضا یا مهر', 'Signature or seal'], ['date', 'تاریخ', 'Date'], ['amountInWords', 'مبلغ به حروف', 'Amount in words'],
  ['payee', 'گیرندهٔ وجه', 'Payee'], ['paymentDate', 'تاریخ پرداخت', 'Payment date'],
]
export const attachmentNames: [keyof NoteAttachments, string, string][] = [
  ['noteImage', 'تصویر سفته', 'Image of the note'], ['contract', 'قرارداد', 'Contract'], ['representation', 'مدرک نمایندگی', 'Proof of representation'], ['other', 'سایر', 'Other'],
]
/** «دریافت سفته‌های فوق …»: the text of the owner's form, printed on the receipt and shown on the screen. */
export const confirmationText: [string, string] = [
  'دریافت سفته‌های فوق با مشخصات و بابت درج‌شده تأیید می‌شود. این رسید صرفاً بیانگر تحویل سفته است و به‌تنهایی تأیید دریافت وجه یا تسویهٔ بدهی نیست.',
  'Receipt of the notes above, with the particulars and purpose stated, is confirmed. This receipt only records that the notes were handed over; by itself it does not confirm receipt of money or settlement of a debt.',
]
