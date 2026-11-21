/**
 * Lists the receipt screens take from Basic Information (reference-svc and accounts-svc), loaded once per screen.
 * Decision of 10 Nov 2026: banks, bank accounts, purpose groups, purposes and references come from Basic Information.
 */
import { ref } from 'vue'
import type { TreasuryApi } from '@treasury/ui-kit'

export interface Entry { id: string; name: string; code?: string | null; parentId?: string | null; isActive?: boolean }
export interface PurposeGroup {
  id: string; number: number; direction: 'Receipt' | 'Payment'; name: string; ledgerAccountCode?: string | null; reference1Id: string | null; reference2Id: string | null; isActive: boolean
}
export interface Purpose { id: string; groupId: string; name: string; isActive: boolean }
export interface Department { id: string; number: number; name: string; isActive: boolean }
export interface CashDesk { id: string; name: string; currencyCode: string; isActive: boolean }
export interface BankAccount { id: string; accountNumber: string; bankId: string; branchName: string | null; currencyCode: string; isActive: boolean
  /** The kind of account has cheque books (current Qard-al-hasan; owner's report no. 4, item 1). */
  hasCheques?: boolean
  /** The company's bank cards on the account; card-to-card deposits need one. */
  cards?: { number: string; holderName?: string | null }[] | null }
export interface Detail { code: string; name: string; type: string | null; isActive: boolean }
/**
 * A bank card money is paid onto on «واریز به کارت بانک» (owner's report no. 6, item 12): the card of a petty-cash holder (`kind`
 * PettyCashCard; `id` is the petty cash) or a company bank card without a bank account (CompanyCard). `value` names it in a list box.
 */
export interface DepositCard { value: string; kind: 'PettyCashCard' | 'CompanyCard'; id: string; holderName: string; number: string; isActive: boolean }
interface PettyCashFund { id: string; holderName: string | null; isActive: boolean; keeping: { card: boolean; cardHolderName: string | null; cardNumber: string | null } | null }
interface CompanyCard { id: string; cardNumber: string; holderName: string; isActive: boolean }
export const cardValue = (kind: string | null | undefined, id: string | null | undefined) => (kind && id ? `${kind}:${id}` : '')
export interface LedgerAccount { code: string; name: string }
/** A type of تفصیل account as the accounting system names it, with the role of its accounts (Customer, PersonOrCompany…). */
export interface DetailType { type: string; activeCount: number; role: string | null }

export function useLookups(api: TreasuryApi) {
  const banks = ref<Entry[]>([])
  const currencies = ref<Entry[]>([])
  const cities = ref<Entry[]>([])
  const references = ref<Entry[]>([])
  const groups = ref<PurposeGroup[]>([])
  const departments = ref<Department[]>([])
  const desks = ref<CashDesk[]>([])
  const bankAccounts = ref<BankAccount[]>([])
  const ledger = ref<LedgerAccount[]>([])
  const detailTypes = ref<DetailType[]>([])
  /** City of the company's legal address (Basic Information → «مشخصات شرکت»); empty until it is entered. */
  const companyCity = ref('')
  const cards = ref<DepositCard[]>([])

  async function load() {
    const [b, c, ci, r, g, d, k, a, l, ty, co, pc, cc] = await Promise.all([
      api.get<Entry[]>('/api/reference/banks'),
      api.get<Entry[]>('/api/reference/currencies'),
      api.get<Entry[]>('/api/reference/cities'),
      api.get<Entry[]>('/api/reference/references?includeInactive=true'),
      api.get<PurposeGroup[]>('/api/reference/purpose-groups'),
      api.get<Department[]>('/api/reference/departments'),
      api.get<CashDesk[]>('/api/accounts/cash-desks?includeInactive=true'),
      api.get<BankAccount[]>('/api/accounts/bank-accounts?includeInactive=true'),
      // Names of the accounts on vouchers (owner's report no. 4, item 8).
      api.get<LedgerAccount[]>('/api/reference/ledger/accounts?includeInactive=true'),
      // Owner, 18 Nov 2026: the types of تفصیل for the list before a تفصیل field, and the company's city for «خارج از مرکز».
      api.get<DetailType[]>('/api/reference/ledger/detail-types'),
      api.get<{ cityName: string | null }>('/api/reference/company'),
      // «واریز به کارت بانک»: the petty-cash holders' cards and the company's cards without a bank account.
      api.get<PettyCashFund[]>('/api/accounts/petty-cash?includeInactive=true'),
      api.get<CompanyCard[]>('/api/accounts/company-cards?includeInactive=true'),
    ])
    if (b.ok) banks.value = b.value
    if (c.ok) currencies.value = c.value
    if (ci.ok) cities.value = ci.value
    if (r.ok) references.value = r.value
    if (g.ok) groups.value = g.value
    if (d.ok) departments.value = d.value
    if (k.ok) desks.value = k.value
    if (a.ok) bankAccounts.value = a.value
    if (l.ok) ledger.value = l.value
    if (ty.ok) detailTypes.value = ty.value
    if (co.ok) companyCity.value = co.value.cityName ?? ''
    cards.value = [
      ...(pc.ok ? pc.value : []).filter(f => f.keeping?.card && f.keeping.cardNumber).map(f => ({
        value: cardValue('PettyCashCard', f.id), kind: 'PettyCashCard' as const, id: f.id, holderName: f.keeping!.cardHolderName ?? f.holderName ?? '', number: f.keeping!.cardNumber!, isActive: f.isActive })),
      ...(cc.ok ? cc.value : []).map(x => ({ value: cardValue('CompanyCard', x.id), kind: 'CompanyCard' as const, id: x.id, holderName: x.holderName, number: x.cardNumber, isActive: x.isActive })),
    ]
  }

  const accountName = (code: string) => ledger.value.find(x => x.code === code)?.name
  const bankName = (id: string | null | undefined) => banks.value.find(b => b.id === id)?.name ?? ''
  const accountTitle = (a: BankAccount) => `${bankName(a.bankId)} – ${a.accountNumber}${a.branchName ? ` (${a.branchName})` : ''}`
  /**
   * A card as it is named in lists and on printed receipts: whose it is, the name on it and its number in groups of four. The number is
   * kept left to right inside the Persian text (U+2066 … U+2069), or its groups would be shown in the reverse order.
   */
  const cardTitle = (c: DepositCard, fa = true) =>
    `${c.kind === 'PettyCashCard' ? (fa ? 'تنخواه‌گردان' : 'Petty cash') : (fa ? 'کارت شرکت' : 'Company card')} – ${c.holderName} – \u2066${c.number.replace(/(\d{4})(?=\d)/g, '$1-')}\u2069`
  /** Where the money of a cash, bank or card receipt went: the cash desk, the bank account or the bank card. */
  function receiptAccountTitle(r: { accountKind?: string | null; accountId?: string | null }, fa = true): string {
    if (r.accountKind === 'CashDesk') return desks.value.find(d => d.id === r.accountId)?.name ?? ''
    if (r.accountKind === 'PettyCashCard' || r.accountKind === 'CompanyCard') {
      const c = cards.value.find(x => x.value === cardValue(r.accountKind, r.accountId))
      return c ? cardTitle(c, fa) : ''
    }
    const a = bankAccounts.value.find(x => x.id === r.accountId)
    return a ? accountTitle(a) : ''
  }

  /** تفصیل found by name or code, of one type when `type` is given; the chosen one is always kept in the list. */
  async function searchDetails(text: string, keep?: string, roles?: string, type?: string): Promise<Detail[]> {
    // With a type chosen the list is for picking directly, so more of it is shown.
    const q = new URLSearchParams({ take: type ? '200' : '50' })
    if (type) q.set('type', type)
    if (text.trim()) q.set('search', text.trim())
    if (roles) q.set('roles', roles)
    const r = await api.get<Detail[]>(`/api/reference/ledger/details?${q}`)
    const found = r.ok ? r.value : []
    if (keep && !found.some(d => d.code === keep)) {
      const one = await api.get<Detail>(`/api/reference/ledger/details/${encodeURIComponent(keep)}`)
      if (one.ok) found.unshift(one.value)
    }
    return found
  }

  return { banks, currencies, cities, references, groups, departments, desks, bankAccounts, ledger, detailTypes, companyCity, cards, load, accountName, bankName, accountTitle,
    cardTitle, receiptAccountTitle, searchDetails }
}
