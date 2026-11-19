/**
 * Lists the payment request screens take from Basic Information (reference-svc and accounts-svc), loaded once per screen.
 */
import { ref } from 'vue'
import type { TreasuryApi } from '@treasury/ui-kit'

export interface Entry { id: string; name: string; code?: string | null; isActive?: boolean }
export interface Requester { id: string; number: number; fullName: string; departmentId: string | null; costCenterCode: string; isActive: boolean }
/** An entry of «ذینفعان / متعهدین / ضامنین»; only the group «ذینفعان» is paid (owner, 19 Nov 2026). */
export interface Beneficiary { id: string; detailCode: string; legalName: string; sheba: string | null; isActive: boolean; isBeneficiary?: boolean }
/** A received cheque that may be transferred: in the cash desk with the status «ثبت صیادی شد» (cashflow-svc). */
export interface TransferableCheque {
  id: string; kind: string; number: string | null; bankName: string | null; amount: number; currency: string; dueDate: string | null; counterparty: string
  allowedActions: string[]; details: { sayadId: string | null } | null
}
export interface PurposeGroup { id: string; number: number; direction: 'Receipt' | 'Payment'; name: string; reference1Id: string | null; reference2Id: string | null; isActive: boolean }
export interface Purpose { id: string; groupId: string; name: string; isActive: boolean }
export interface Department { id: string; number: number; name: string; isActive: boolean }
export interface CashDesk { id: string; name: string; currencyCode: string; isActive: boolean }
export interface BankAccount { id: string; accountNumber: string; bankId: string; branchName: string | null; currencyCode: string; isActive: boolean }

export function useLookups(api: TreasuryApi) {
  const requesters = ref<Requester[]>([])
  const beneficiaries = ref<Beneficiary[]>([])
  const groups = ref<PurposeGroup[]>([])
  const references = ref<Entry[]>([])
  const departments = ref<Department[]>([])
  const currencies = ref<Entry[]>([])
  const banks = ref<Entry[]>([])
  const desks = ref<CashDesk[]>([])
  const bankAccounts = ref<BankAccount[]>([])

  async function load() {
    const [rq, bf, g, rf, d, c, b, k, a] = await Promise.all([
      api.get<Requester[]>('/api/reference/requesters?includeInactive=true'),
      api.get<Beneficiary[]>('/api/reference/beneficiaries?includeInactive=true&role=Beneficiary'),
      api.get<PurposeGroup[]>('/api/reference/purpose-groups'),
      api.get<Entry[]>('/api/reference/references?includeInactive=true'),
      api.get<Department[]>('/api/reference/departments'),
      api.get<Entry[]>('/api/reference/currencies'),
      api.get<Entry[]>('/api/reference/banks?includeInactive=true'),
      api.get<CashDesk[]>('/api/accounts/cash-desks?includeInactive=true'),
      api.get<BankAccount[]>('/api/accounts/bank-accounts?includeInactive=true'),
    ])
    if (rq.ok) requesters.value = rq.value
    if (bf.ok) beneficiaries.value = bf.value
    if (g.ok) groups.value = g.value
    if (rf.ok) references.value = rf.value
    if (d.ok) departments.value = d.value
    if (c.ok) currencies.value = c.value
    if (b.ok) banks.value = b.value
    if (k.ok) desks.value = k.value
    if (a.ok) bankAccounts.value = a.value
  }

  const bankName = (id: string | null | undefined) => banks.value.find(b => b.id === id)?.name ?? ''
  const accountTitle = (a: BankAccount) => `${bankName(a.bankId)} – ${a.accountNumber}${a.branchName ? ` (${a.branchName})` : ''}`

  return { requesters, beneficiaries, groups, references, departments, currencies, banks, desks, bankAccounts, load, bankName, accountTitle }
}

/** Payment methods in the manual's order («نوع پرداخت»). */
export const methods: { value: string; fa: string; en: string }[] = [
  { value: 'CurrentCheque', fa: 'چک روز (نقدی)', en: 'Current cheque' },
  { value: 'PostDatedCheque', fa: 'چک مدت‌دار', en: 'Post-dated cheque' },
  { value: 'GuaranteeCheque', fa: 'چک تضمینی (انتظامی)', en: 'Guarantee cheque' },
  { value: 'Cash', fa: 'پرداخت نقدی', en: 'Cash' },
  { value: 'PromissoryNote', fa: 'سفته', en: 'Promissory note' },
  { value: 'GuaranteeNote', fa: 'سفته تضمینی (انتظامی)', en: 'Guarantee note' },
  { value: 'Transfer', fa: 'نقل و انتقال (بین بانکی)', en: 'Bank transfer' },
  { value: 'CardToCard', fa: 'کارت به کارت', en: 'Card to card' },
  // Owner, 19 Nov 2026: a received cheque registered in Sayad is handed on to the beneficiary.
  { value: 'ChequeTransfer', fa: 'ثبت انتقال (خرج چک)', en: 'Cheque transfer (endorsement)' },
]

export const stateNames: Record<string, [string, string]> = {
  Registered: ['ثبت شده', 'Registered'], Approved: ['تایید شده', 'Approved'], Paid: ['پرداخت شده', 'Paid'], Cancelled: ['ابطال شده', 'Cancelled'],
}
