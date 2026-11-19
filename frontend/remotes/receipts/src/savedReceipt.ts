/**
 * A saved receipt as GET /api/cashflow/receipts/{id} answers it, for «ویرایش» (owner's report no. 6, items 2 and 6): the receipt forms
 * open it, and save it back with its version. `notCorrectable` is the code of the rule that closes the correction (voucher confirmed,
 * cancelled, a cheque that left «نزد صندوق»), or null while the receipt can be corrected.
 */
import type { NoteChecks, NoteTerms } from './notes'

export interface SavedInstrument {
  id: string; kind: string; number: string | null; bankName: string | null; amount: number; currency: string; rateToRial: number | null; dueDate: string | null
  noteIssueDate?: string | null
  details: {
    bankId?: string | null; branchName: string | null; branchCode?: string | null; accountNumber: string | null; accountHolder?: string | null; city?: string | null
    outOfTown?: boolean; sayadId: string | null; onDemand?: boolean; checks?: NoteChecks | null
    guarantors: { name: string; nationalCode: string | null; detailCode?: string | null; phone?: string | null }[] | null
  } | null
}

export interface SavedReceipt {
  id: string; kind: 'Cash' | 'BankDeposit' | 'Instruments' | 'Notes'; fiscalYear: number; number: number; date: string; payerDetailCode: string; payerName: string
  purposeGroupId: string; purposeGroupName: string; purpose: string; reference1: string | null; reference2: string | null; departmentId: string | null
  salesProceeds: boolean; note: string | null; accountId: string | null; method: string | null; trackingNumber: string | null; amount: number | null
  currency: string | null; rateToRial: number | null; payerSheba: string | null; state: string; version: number; instruments: SavedInstrument[] | null
  replacedInstrumentId: string | null; noteTerms: NoteTerms | null; notCorrectable: string | null
}

/** An amount as the amount boxes hold it: plain digits, without a float's tail ("12500000", "100.5"). */
export const amountText = (n: number | null | undefined) => (n === null || n === undefined ? '' : String(Number(n.toFixed(4))))
