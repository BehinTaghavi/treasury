/**
 * What the payment sheet of a cheque transfer shows («برگ پرداخت – رسید واگذاری چک», owner, 19 Nov 2026, item 6 of «ثبت انتقال (خرج
 * چک)»), and its two fixed texts in the owner's words.
 */
export interface TransferSheet {
  company: { name: string; logoUrl: string | null }
  /** Serial number of the payment sheet, from 1 each fiscal year (owner's answer to Q57); null on a transfer recorded before 19 Nov 2026. */
  sheetNumber: number | null
  /** Who signs besides the two parties (owner's answer to Q57), chosen among the permission holders of the transfer (report no. 8). */
  signers: { id: string; name: string; position: string | null }[]
  /** Number of the approved payment request the transfer pays; null when it could not be read. */
  requestNumber: number | null
  /** Date of the transfer in Sayad. */
  date: string
  /** Tracking code of the transfer in the Sayad system. */
  sayadTracking: string
  receiverName: string
  receiverDetailCode: string | null
  purposeGroupName: string | null
  purpose: string | null
  cheque: {
    sayadId: string | null; number: string; bankName: string | null; branchName: string | null; accountNumber: string | null
    dueDate: string | null; amount: number; currency: string
    /** The holder of the account written on the cheque, when it is not the payer («اگر چک شخص دیگری است»). */
    accountHolder: string | null
    /** Who gave the cheque to the company. */
    payerName: string
  }
  recordedBy: string
  note: string | null
}

/** The owner's words (19 Nov 2026), printed as given. */
export const deliverySentence: [string, string] = ['چک جهت وصول بابت بدهی تحویل می‌شود.', 'The cheque is handed over for collection, on account of a debt.']
export const footText: [string, string] = [
  'چک با مشخصات بالا پس از ثبت و تایید انتقال در سامانه صیاد بابت بدهی شرکت واگذار شد. تسویه بدهی به میزان وجه وصول شده از چک خواهد بود و صرف تحویل و انتقال چک به معنای برائت ذمه از شرکت نیست.',
  'The cheque described above was handed over on account of the company\'s debt after its transfer was registered and confirmed in the Sayad system. The debt is settled to the extent of the money collected from the cheque; handing over and transferring the cheque does not by itself discharge the company.',
]
