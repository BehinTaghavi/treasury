/**
 * Lists the payment request screens take from Basic Information (reference-svc and accounts-svc), loaded once per screen.
 */
import { ref } from 'vue'
import type { TreasuryApi } from '@treasury/ui-kit'

export interface Entry { id: string; name: string; code?: string | null; isActive?: boolean }
export interface Requester { id: string; number: number; fullName: string; departmentId: string | null; costCenterCode: string; isActive: boolean }
/** `detailType`: type of the beneficiary's تفصیل in the accounting system (owner's report no. 9, item 6: «نوع تفصیل»). */
export interface Beneficiary { id: string; detailCode: string; legalName: string; sheba: string | null; isActive: boolean; detailType: string | null }
export interface PurposeGroup { id: string; number: number; direction: 'Receipt' | 'Payment'; name: string; reference1Id: string | null; reference2Id: string | null; isActive: boolean }
export interface Purpose { id: string; groupId: string; name: string; isActive: boolean }
export interface Department { id: string; number: number; name: string; costCenterCodes: string[]; isActive: boolean }
/** A cost centre of the accounting system's تفصیل list (owner's report no. 9, item 2). */
export interface CostCenter { code: string; name: string; isActive: boolean }
export interface CashDesk { id: string; name: string; currencyCode: string; isActive: boolean }
export interface BankAccount { id: string; accountNumber: string; bankId: string; branchName: string | null; currencyCode: string; isActive: boolean }

export function useLookups(api: TreasuryApi) {
  const requesters = ref<Requester[]>([])
  const beneficiaries = ref<Beneficiary[]>([])
  const groups = ref<PurposeGroup[]>([])
  const references = ref<Entry[]>([])
  const departments = ref<Department[]>([])
  const costCenters = ref<CostCenter[]>([])
  const currencies = ref<Entry[]>([])
  const banks = ref<Entry[]>([])
  const desks = ref<CashDesk[]>([])
  const bankAccounts = ref<BankAccount[]>([])

  async function load() {
    const [rq, bf, g, rf, d, c, b, k, a, cc] = await Promise.all([
      api.get<Requester[]>('/api/reference/requesters?includeInactive=true'),
      api.get<Beneficiary[]>('/api/reference/beneficiaries?includeInactive=true'),
      api.get<PurposeGroup[]>('/api/reference/purpose-groups'),
      api.get<Entry[]>('/api/reference/references?includeInactive=true'),
      api.get<Department[]>('/api/reference/departments?includeInactive=true'),
      api.get<Entry[]>('/api/reference/currencies'),
      api.get<Entry[]>('/api/reference/banks?includeInactive=true'),
      api.get<CashDesk[]>('/api/accounts/cash-desks?includeInactive=true'),
      api.get<BankAccount[]>('/api/accounts/bank-accounts?includeInactive=true'),
      api.get<CostCenter[]>('/api/reference/ledger/details?costCentersOnly=true&includeInactive=true&take=1000'),
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
    if (cc.ok) costCenters.value = cc.value
  }

  const bankName = (id: string | null | undefined) => banks.value.find(b => b.id === id)?.name ?? ''
  const accountTitle = (a: BankAccount) => `${bankName(a.bankId)} – ${a.accountNumber}${a.branchName ? ` (${a.branchName})` : ''}`

  return { requesters, beneficiaries, groups, references, departments, costCenters, currencies, banks, desks, bankAccounts, load, bankName, accountTitle }
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
]

export const stateNames: Record<string, [string, string]> = {
  Registered: ['ثبت شده', 'Registered'], Approved: ['تایید شده', 'Approved'], Paid: ['پرداخت شده', 'Paid'], Cancelled: ['ابطال شده', 'Cancelled'],
}
