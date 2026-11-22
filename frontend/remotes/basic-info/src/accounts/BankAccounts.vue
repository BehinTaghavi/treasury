<script setup lang="ts">
/**
 * Bank accounts (معرفی حساب‌های بانکی), manual section 5. Lists come from reference-svc (banks, provinces, cities,
 * currencies, chart of accounts, تفصیل); the bank account itself is saved by accounts-svc, which checks every rule again.
 * - The ledger account boxes offer only accounts that take entries (no lower level), as the old program did; opening a box
 *   shows the whole list, typing narrows it by code or name (owner's report of 11 Nov 2026, items 11 and 13).
 * - The تفصیل boxes offer only the types the chosen ledger account allows (owner, 10 Nov 2026).
 * - Required fields have a red frame (item 12); the city is chosen after its province (item 1).
 * - A backup account (پشتیبان) names a main current account of the same bank, defined first; without one it cannot be
 *   saved (item 10).
 * - Collection priority is a number from 1 (highest) to 5, chosen from a small pop-up (item 15).
 * - The list has a search box (item 16); typing a number that already exists opens that account (item 17).
 * - Owner's report no. 8 (21 Nov 2026): each account has its own authorised signatories – «دارندگان امضای مجاز هر حساب بانکی ممکن است
 *   متفاوت از دارندگان امضای مجاز حساب بانک بعدی باشد». The block «دارندگان امضای مجاز» asks for the number of signatures (one digit)
 *   and, row by row, a signatory chosen from Basic Information → «دارندگان امضای مجاز (اسناد بهادار)» who may sign cheques, the kind
 *   of signature – «ثابت»: on every cheque; «متغیر»: chosen among the variable ones to complete the number – and shows the scan of
 *   the signature. The first row is added with «تعریف دارنده امضاء مجاز اول», the next ones with «رکورد جدید».
 */
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { cardNumberHint, createApi, normaliseJalali, translate, type Locale, toLatinDigits } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { fetchSignature } from '../organization/signatureScan'
import { searchKey } from '../text'

interface Entry { id: string; name: string; code: string | null; isActive: boolean; parentId: string | null }
interface LedgerAccount { code: string; name: string; isPostable: boolean; isActive: boolean; detail1Types: string[]; detail2Types: string[] }
interface Detail { code: string; name: string; type: string | null; isActive: boolean }
interface BankAccount {
  id: string; accountNumber: string; bankId: string; branchName: string | null; branchCode: string | null; cityId: string | null
  openingDate: string | null; currencyCode: string; mainAccountId: string | null; ledgerAccountCode: string; detail1Code: string | null
  detail2Code: string | null; collectionAccountCode: string | null; bouncedAccountCode: string | null; collectionPriority: number | null
  isFacility: boolean; isActive: boolean; version: number; isInUse: boolean
  accountType: AccountType; term: string | null; cards: Card[] | null; hasCheques: boolean
  requiredSignatures: number; signatories: AccountSignatory[] | null
}
type SignatureKind = 'Fixed' | 'Variable'
interface AccountSignatory { signatoryId: string; kind: SignatureKind | '' }
/** One of «دارندگان امضای مجاز (اسناد بهادار)» of Basic Information. */
interface Signatory { id: string; fullName: string; position: string; signsCheques: boolean; hasSignature: boolean; isActive: boolean; version: number }
type AccountType = 'CurrentQardHasan' | 'SavingsQardHasan' | 'ShortTermDeposit' | 'LongTermDeposit'
interface Card { number: string; holderName: string }

/**
 * Kinds of account (owner's report no. 4, items 1 and 2): only a current Qard-al-hasan account has cheque books; current and savings
 * Qard-al-hasan and short-term deposits may have company bank cards; all but short-term deposits may be in a foreign currency.
 */
const TYPES: Record<AccountType, { fa: string; en: string; cards: boolean; foreign: boolean }> = {
  CurrentQardHasan: { fa: 'سپرده قرض الحسنه جاری', en: 'Current Qard-al-hasan', cards: true, foreign: true },
  SavingsQardHasan: { fa: 'قرض الحسنه پس انداز', en: 'Savings Qard-al-hasan', cards: true, foreign: true },
  ShortTermDeposit: { fa: 'سپرده سرمایه گذاری کوتاه مدت', en: 'Short-term deposit', cards: true, foreign: false },
  LongTermDeposit: { fa: 'سپرده سرمایه گذاری بلند مدت', en: 'Long-term deposit', cards: false, foreign: true },
}
const TERMS: Record<string, [string, string]> = { Ordinary: ['عادی', 'Ordinary'], ThreeMonths: ['سه ماهه', '3-month'], SixMonths: ['شش ماهه', '6-month'] }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US')
const base = '/api/accounts/bank-accounts'
const PRIORITIES = [1, 2, 3, 4, 5]

const banks = ref<Entry[]>([])
const provinces = ref<Entry[]>([])
const cities = ref<Entry[]>([])
const currencies = ref<Entry[]>([])
const ledger = ref<LedgerAccount[]>([])
const accounts = ref<BankAccount[]>([])
const details1 = ref<Detail[]>([])
const details2 = ref<Detail[]>([])
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const editing = ref<BankAccount | null>(null)
const showForm = ref(false)
const busy = ref(false)
const search = ref('')
const priorityOpen = ref(false)

const empty = () => ({
  accountNumber: '', bankId: '', branchName: '', branchCode: '', provinceId: '', cityId: '', openingDate: '', isForeign: false,
  currencyCode: '', isBackup: false, mainAccountId: '', ledgerAccountCode: '', detail1Code: '', detail2Code: '',
  collectionAccountCode: '', bouncedAccountCode: '', collectionPriority: null as number | null, isFacility: false,
  accountType: 'CurrentQardHasan' as AccountType, term: '', hasCards: false, cards: [] as Card[],
  requiredSignatures: '', signatories: [] as AccountSignatory[],
})
const form = reactive(empty())

const name = (list: { id: string; name: string }[], id: string | null) => list.find(x => x.id === id)?.name ?? ''
const typeChoices = computed(() => (Object.keys(TYPES) as AccountType[]).map(k => ({ value: k, label: fa.value ? TYPES[k].fa : TYPES[k].en })))
const termChoices = computed(() => Object.entries(TERMS).map(([k, [f, e]]) => ({ value: k, label: fa.value ? f : e })))
const typeName = (a: BankAccount) => {
  const ty = TYPES[a.accountType ?? 'CurrentQardHasan']
  const term = a.term ? ` (${fa.value ? TERMS[a.term]?.[0] : TERMS[a.term]?.[1]})` : ''
  return (fa.value ? ty.fa : ty.en) + term
}
function addCard() { form.cards.push({ number: '', holderName: '' }) }

// ---- authorised signatories of the account (owner's report no. 8) ----
const signatories = ref<Signatory[]>([])
const signatoryOf = (id: string) => signatories.value.find(s => s.id === id)
/** Offered on a row: the active signatories who may sign cheques and are not on another row; the row's own choice stays. */
const signatoryChoices = (row: AccountSignatory): Choice[] => signatories.value
  .filter(s => s.id === row.signatoryId || (s.isActive && s.signsCheques && !form.signatories.some(r => r.signatoryId === s.id)))
  .map(s => ({ value: s.id, label: s.fullName, hint: s.position, muted: !s.isActive }))
const fixedCount = computed(() => form.signatories.filter(r => r.kind === 'Fixed').length)
const variableCount = computed(() => form.signatories.filter(r => r.kind === 'Variable').length)
function addSignatory() { form.signatories.push({ signatoryId: '', kind: '' }) }
/** The scans of the signatures, fetched once per signatory and shown from memory. */
const scans = reactive<Record<string, string>>({})
async function showScan(id: string) {
  const s = signatoryOf(id)
  if (!s?.hasSignature || scans[id] !== undefined) return
  scans[id] = ''
  scans[id] = await fetchSignature({ apiBase: props.apiBase, token: props.token }, `/api/reference/signatories/${id}/signature?v=${s.version}`)
}
watch(() => form.signatories.map(r => r.signatoryId), ids => { for (const id of ids) if (id) void showScan(id) })
onBeforeUnmount(() => { for (const url of Object.values(scans)) if (url) URL.revokeObjectURL(url) })
const signatureSummary = (a: BankAccount) => (a.requiredSignatures
  ? t(`${num(a.requiredSignatures)} امضا از ${num(a.signatories?.length ?? 0)} نفر`, `${a.requiredSignatures} of ${a.signatories?.length ?? 0}`)
  : '')
// A kind that cannot be in a foreign currency clears the currency; one without cards clears them.
watch(() => form.accountType, ty => {
  if (!TYPES[ty].foreign) Object.assign(form, { isForeign: false, currencyCode: '' })
  if (!TYPES[ty].cards) form.hasCards = false
})
const ledgerName = (code: string | null) => ledger.value.find(a => a.code === code)?.name ?? ''
const chosenLedger = computed(() => ledger.value.find(a => a.code === form.ledgerAccountCode))
const foreignCurrencies = computed(() => currencies.value.filter(c => c.isActive && c.code !== 'IRR'))
// MVC BanksController: a current (main) account, not a backup itself and without a backup yet. Owner's report no. 2, item 3:
// «پشتیبان» is ticked first, at the top of the form; the main account is chosen next and the backup takes its bank, branch and
// city. With no such current account the owner's message shows and the rest of the form stays locked, so nothing is saved.
// Owner, 13 Nov 2026: the backup has the currency of its main account (Rial with Rial, the same foreign currency).
const wantedCurrency = (a: BankAccount) => !form.isForeign ? a.currencyCode === 'IRR' : form.currencyCode ? a.currencyCode === form.currencyCode : a.currencyCode !== 'IRR'
const mainChoices = computed(() => accounts.value.filter(a =>
  a.isActive && !a.mainAccountId && !accounts.value.some(b => b.mainAccountId === a.id) && wantedCurrency(a)))
const backupBlocked = computed(() => form.isBackup && !editing.value && !mainChoices.value.length)
const mainLabel = (a: BankAccount) => `${a.accountNumber} – ${name(banks.value, a.bankId)}${a.branchName ? ' – ' + a.branchName : ''}${a.currencyCode !== 'IRR' ? ' (' + a.currencyCode + ')' : ''}`
/** The rest of the form waits until the main account of a backup is chosen. */
const restLocked = computed(() => backupBlocked.value || (form.isBackup && !editing.value && !form.mainAccountId))

const toChoices = (list: Entry[]): Choice[] => list.filter(e => e.isActive).map(e => ({ value: e.id, label: e.name }))
const bankChoices = computed(() => toChoices(banks.value))
const provinceChoices = computed(() => toChoices(provinces.value))
const cityChoices = computed(() => toChoices(cities.value.filter(c => !form.provinceId || c.parentId === form.provinceId)))
const ledgerChoices = computed<Choice[]>(() =>
  ledger.value.filter(a => a.isPostable && a.isActive).map(a => ({ value: a.code, label: a.name, hint: a.code })))
const detailChoices = (list: Detail[]): Choice[] => list.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive }))

const shown = computed(() => {
  const q = searchKey(search.value)
  if (!q) return accounts.value
  return accounts.value.filter(a => [a.accountNumber, name(banks.value, a.bankId), a.branchName ?? '', a.branchCode ?? '', a.ledgerAccountCode,
    a.detail1Code ?? '', name(cities.value, a.cityId)].some(f => searchKey(f).includes(q)))
})

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const numberKey = (s: string) => searchKey(s).replace(/\D/g, '')

async function loadLists() {
  const [b, p, c, cu, l, sg] = await Promise.all([
    api.get<Entry[]>('/api/reference/banks'),
    api.get<Entry[]>('/api/reference/provinces'),
    api.get<Entry[]>('/api/reference/cities'),
    api.get<Entry[]>('/api/reference/currencies'),
    api.get<LedgerAccount[]>('/api/reference/ledger/accounts?postableOnly=true'),
    api.get<Signatory[]>('/api/reference/signatories?includeInactive=true'),
  ])
  if (sg.ok) signatories.value = sg.value
  if (b.ok) banks.value = b.value
  if (p.ok) provinces.value = p.value
  if (c.ok) cities.value = c.value
  if (cu.ok) currencies.value = cu.value
  if (l.ok) ledger.value = l.value
}

async function loadAccounts() {
  const r = await api.get<BankAccount[]>(`${base}?includeInactive=true`)
  if (r.ok) accounts.value = r.value
  else fail(r.code)
}

/**
 * تفصیل offered for one position (the server keeps thousands): of the allowed types when the ledger account lists some,
 * otherwise any; the chosen one is always kept in the list so its name shows.
 */
async function searchDetails(position: 1 | 2, text: string) {
  const types = position === 1 ? chosenLedger.value?.detail1Types ?? [] : chosenLedger.value?.detail2Types ?? []
  const queries = (types.length ? types : [null]).map(type => {
    const q = new URLSearchParams({ take: '50' })
    if (text.trim()) q.set('search', text.trim())
    if (type) q.set('type', type)
    return api.get<Detail[]>(`/api/reference/ledger/details?${q}`)
  })
  const found = (await Promise.all(queries)).flatMap(r => (r.ok ? r.value : []))
  const current = position === 1 ? form.detail1Code : form.detail2Code
  if (current && !found.some(d => d.code === current)) {
    const r = await api.get<Detail>(`/api/reference/ledger/details/${encodeURIComponent(current)}`)
    if (r.ok) found.unshift(r.value)
  }
  ;(position === 1 ? details1 : details2).value = found
}

watch(() => form.ledgerAccountCode, () => { void searchDetails(1, ''); void searchDetails(2, '') })
let timer: ReturnType<typeof setTimeout> | undefined
function onDetailSearch(position: 1 | 2, text: string) {
  clearTimeout(timer)
  timer = setTimeout(() => searchDetails(position, text), 250)
}

/** A backup account is at the bank and branch of its main account (owner's report no. 1, item 10). */
watch(() => form.mainAccountId, id => {
  const main = accounts.value.find(a => a.id === id)
  if (!main || editing.value) return
  Object.assign(form, {
    bankId: main.bankId, branchName: main.branchName ?? '', branchCode: main.branchCode ?? '', cityId: main.cityId ?? '',
    isForeign: main.currencyCode !== 'IRR', currencyCode: main.currencyCode === 'IRR' ? '' : main.currencyCode,
    provinceId: cities.value.find(c => c.id === main.cityId)?.parentId ?? '',
  })
})

/** Ticking «پشتیبان» starts from an empty form; unticking leaves what was typed. */
function onBackupChange(checked: boolean) {
  if (checked) Object.assign(form, empty(), { isBackup: true, isForeign: form.isForeign, currencyCode: form.currencyCode })
  else Object.assign(form, { isBackup: false, mainAccountId: '' })
}

function onProvinceChange(id: string) {
  form.provinceId = id
  if (form.cityId && name(cities.value.filter(c => c.parentId === id), form.cityId) === '') form.cityId = ''
}

function startNew() {
  Object.assign(form, empty())
  editing.value = null
  showForm.value = true
  message.value = null
}

function startEdit(a: BankAccount) {
  Object.assign(form, {
    accountNumber: a.accountNumber, bankId: a.bankId, branchName: a.branchName ?? '', branchCode: a.branchCode ?? '',
    provinceId: cities.value.find(c => c.id === a.cityId)?.parentId ?? '', cityId: a.cityId ?? '',
    openingDate: a.openingDate ?? '', isForeign: a.currencyCode !== 'IRR', currencyCode: a.currencyCode === 'IRR' ? '' : a.currencyCode,
    isBackup: !!a.mainAccountId, mainAccountId: a.mainAccountId ?? '', ledgerAccountCode: a.ledgerAccountCode, detail1Code: a.detail1Code ?? '',
    detail2Code: a.detail2Code ?? '', collectionAccountCode: a.collectionAccountCode ?? '', bouncedAccountCode: a.bouncedAccountCode ?? '',
    collectionPriority: a.collectionPriority, isFacility: a.isFacility,
    accountType: a.accountType ?? 'CurrentQardHasan', term: a.term ?? '', hasCards: !!a.cards?.length, cards: (a.cards ?? []).map(c => ({ ...c })),
    requiredSignatures: a.requiredSignatures ? String(a.requiredSignatures) : '', signatories: (a.signatories ?? []).map(x => ({ ...x })),
  })
  editing.value = a
  showForm.value = true
  message.value = null
  void searchDetails(1, '')
  void searchDetails(2, '')
}

/** Item 17: a number that is already defined opens that account instead of starting a second one. */
function checkNumber() {
  if (editing.value || !numberKey(form.accountNumber)) return
  const existing = accounts.value.find(a => numberKey(a.accountNumber) === numberKey(form.accountNumber))
  if (!existing) return
  startEdit(existing)
  message.value = { kind: 'error', text: t('این حساب قبلاً تعریف شده است؛ مشخصات آن نمایش داده شد.', 'This account is already defined; its details are shown.') }
}

function choosePriority(p: number | null) {
  form.collectionPriority = p
  priorityOpen.value = false
}

async function save() {
  if (busy.value) return
  if (backupBlocked.value) return fail('BankAccount.MainAccountRequired')
  const payload = {
    accountNumber: form.accountNumber, bankId: form.bankId || '00000000-0000-0000-0000-000000000000', branchName: form.branchName || null,
    branchCode: form.branchCode || null, cityId: form.cityId || null, openingDate: form.openingDate ? normaliseJalali(form.openingDate) ?? form.openingDate : null,
    isForeignCurrency: form.isForeign, currencyCode: form.isForeign ? form.currencyCode : null, ledgerAccountCode: form.ledgerAccountCode,
    detail1Code: form.detail1Code || null, detail2Code: form.detail2Code || null,
    collectionAccountCode: form.collectionAccountCode || null, bouncedAccountCode: form.bouncedAccountCode || null,
    collectionPriority: form.collectionPriority, isFacility: form.isFacility,
    accountType: form.accountType, term: form.accountType === 'ShortTermDeposit' ? form.term || null : null,
    cards: form.hasCards && TYPES[form.accountType].cards
      ? form.cards.filter(c => c.number.trim() || c.holderName.trim()).map(c => ({ number: toLatinDigits(c.number).replace(/\D/g, ''), holderName: c.holderName.trim() }))
      : [],
    // Rows left empty are not sent; a row with a signatory and no kind is refused by the service with its message.
    requiredSignatures: Number(toLatinDigits(form.requiredSignatures).replace(/\D/g, '')) || 0,
    signatories: form.signatories.filter(r => r.signatoryId).map(r => ({ signatoryId: r.signatoryId, kind: r.kind || 'Unset' })),
  }
  if (payload.signatories.some(r => r.kind === 'Unset')) return fail('BankAccount.SignatureKindRequired')
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { form: payload, version: editing.value.version })
    : await api.post<string>(base, { form: payload, mainAccountId: form.isBackup && form.mainAccountId ? form.mainAccountId : null, isBackup: form.isBackup })
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t('حساب بانکی ثبت شد.', 'Bank account saved.') }
  showForm.value = false
  await loadAccounts()
}

async function toggle(a: BankAccount) {
  const r = await api.post<string>(`${base}/${a.id}/${a.isActive ? 'deactivate' : 'activate'}`, { version: a.version })
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: a.isActive ? t('غیرفعال شد.', 'Switched off.') : t('فعال شد.', 'Switched on.') }
  await loadAccounts()
}

/** Owner's report no. 3, item 1: an account nothing uses (no cheque book, cheque type or cheque) and without a backup. */
const hasBackup = (a: BankAccount) => accounts.value.some(b => b.mainAccountId === a.id)
async function remove(a: BankAccount) {
  if (!window.confirm(t(`حساب «${a.accountNumber}» حذف شود؟`, `Delete account "${a.accountNumber}"?`))) return
  const r = await api.post<string>(`${base}/${a.id}/delete`, { version: a.version })
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t('حساب بانکی حذف شد.', 'Bank account deleted.') }
  if (editing.value?.id === a.id) showForm.value = false
  await loadAccounts()
}

onMounted(async () => {
  await loadLists()
  await loadAccounts()
})
</script>

<template>
  <div>
    <h3>{{ t('حساب‌های بانکی', 'Bank accounts') }}</h3>
    <p v-if="!ledger.length" class="muted">{{ t('ابتدا در «کدینگ حساب‌ها» حساب‌های سیستم حسابداری را وارد کنید؛ حساب معین بانک از آنجا انتخاب می‌شود.', 'Import the chart of accounts first; the bank\'s ledger account is chosen from it.') }}</p>
    <button type="button" class="tr-new" @click="startNew">{{ t('حساب بانکی جدید', 'New bank account') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <p class="muted small"><span class="star">*</span> {{ t('فیلدهای با کادر قرمز اجباری هستند.', 'Fields with a red frame are required.') }}</p>
      <fieldset>
        <legend>{{ t('مشخصات حساب', 'Account') }}</legend>
        <div class="tr-kind">
          <label class="req type"><span>{{ t('نوع حساب', 'Kind of account') }}</span>
            <SearchSelect v-model="form.accountType" :options="typeChoices" required :aria-label="t('نوع حساب', 'Kind of account')" />
          </label>
          <label v-if="form.accountType === 'ShortTermDeposit'" class="req"><span>{{ t('مدت سپرده', 'Term') }}</span>
            <SearchSelect v-model="form.term" :options="termChoices" required :aria-label="t('مدت سپرده', 'Term')" />
          </label>
          <label v-if="TYPES[form.accountType].foreign" class="tr-check"><input v-model="form.isForeign" type="checkbox" :disabled="restLocked && !form.isBackup" /> {{ t('ارزی', 'Foreign currency') }}</label>
          <label v-if="form.isForeign" class="req"><span>{{ t('نوع ارز', 'Currency') }} <span class="star">*</span></span>
            <SearchSelect v-model="form.currencyCode" :options="foreignCurrencies.map(c => ({ value: c.code, label: c.name, hint: c.code }))" required :aria-label="t('نوع ارز', 'Currency')" />
          </label>
          <label class="tr-check"><input :checked="form.isBackup" type="checkbox" :disabled="!!editing" @change="onBackupChange(($event.target as HTMLInputElement).checked)" /> {{ t('پشتیبان', 'Backup account') }}</label>
          <label v-if="form.isBackup" class="req main"><span>{{ t('شماره حساب جاری اصلی', 'Main current account') }} <span class="star">*</span></span>
            <SearchSelect v-model="form.mainAccountId" :disabled="!!editing || backupBlocked" required :aria-label="t('شماره حساب جاری اصلی', 'Main current account')"
                          :options="editing?.mainAccountId
                            ? accounts.filter(a => a.id === editing!.mainAccountId).map(a => ({ value: a.id, label: mainLabel(a) }))
                            : mainChoices.map(a => ({ value: a.id, label: mainLabel(a) }))" />
            <small v-if="!backupBlocked && !editing" class="muted">{{ t('بانک، شعبه و شهر از حساب جاری اصلی گرفته می‌شود.', 'Bank, branch and city come from the main account.') }}</small>
          </label>
          <p v-if="backupBlocked" class="tr-msg error wide" role="alert">{{ translate('BankAccount.MainAccountRequired', locale) }}</p>
          <label v-if="TYPES[form.accountType].cards" class="tr-check"><input v-model="form.hasCards" type="checkbox" @change="form.hasCards && !form.cards.length && addCard()" /> {{ t('کارت بانک دارد', 'Has bank cards') }}</label>
          <p v-if="!TYPES[form.accountType].cards || form.accountType !== 'CurrentQardHasan'" class="muted small wide">
            {{ form.accountType === 'CurrentQardHasan' ? '' : t('این نوع حساب دسته چک ندارد.', 'This kind of account has no cheque books.') }}
          </p>
        </div>
        <div v-if="form.hasCards && TYPES[form.accountType].cards" class="tr-cards">
          <div v-for="(c, i) in form.cards" :key="i" class="tr-card">
            <label class="req"><span>{{ t('شماره ۱۶ رقمی کارت بانک', '16-digit card number') }}</span>
              <input v-model="c.number" dir="ltr" maxlength="19" inputmode="numeric" autocomplete="off" />
              <!-- Owner's report no. 6, item 17: the digits are counted, and a wrong check digit is named as such. -->
              <small v-if="cardNumberHint(c.number, locale)" :class="cardNumberHint(c.number, locale)!.bad ? 'tr-card-bad' : 'muted'">{{ cardNumberHint(c.number, locale)!.text }}</small>
            </label>
            <label class="req"><span>{{ t('عنوان دارندهٔ کارت', 'Name on the card') }}</span>
              <input v-model="c.holderName" :placeholder="t('نام شرکت یا دارندهٔ تنخواه', 'Company or petty-cash holder')" />
            </label>
            <button type="button" class="tr-link danger" @click="form.cards.splice(i, 1)">{{ t('حذف کارت', 'Remove') }}</button>
          </div>
          <button type="button" class="tr-link" @click="addCard">{{ t('+ کارت دیگر', '+ Another card') }}</button>
        </div>
      </fieldset>
      <fieldset :disabled="restLocked">
        <legend class="sr-only">{{ t('مشخصات حساب', 'Account') }}</legend>
        <label class="req"><span>{{ t('شماره حساب', 'Account number') }} <span class="star">*</span></span>
          <input v-model="form.accountNumber" dir="ltr" autocomplete="off" @blur="checkNumber" />
        </label>
        <label class="req"><span>{{ t('نام بانک', 'Bank') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.bankId" :options="bankChoices" required :disabled="!!editing?.mainAccountId || form.isBackup" :aria-label="t('نام بانک', 'Bank')" />
        </label>
        <label class="req"><span>{{ t('نام شعبه', 'Branch') }} <span class="star">*</span></span><input v-model="form.branchName" /></label>
        <label class="req"><span>{{ t('کد شعبه', 'Branch code') }} <span class="star">*</span></span><input v-model="form.branchCode" dir="ltr" /></label>
        <label>{{ t('استان', 'Province') }}
          <SearchSelect :model-value="form.provinceId" :options="provinceChoices" :empty-label="t('همه', 'All')" :aria-label="t('استان', 'Province')" @update:model-value="onProvinceChange" />
        </label>
        <label class="req"><span>{{ t('شهر', 'City') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.cityId" :options="cityChoices" required :aria-label="t('شهر', 'City')" :none-text="t('برای این استان شهری تعریف نشده است', 'No city for this province')" />
        </label>
        <label>{{ t('تاریخ افتتاح', 'Opening date') }}<input v-model="form.openingDate" placeholder="1400/01/15" dir="ltr" /></label>
      </fieldset>

      <fieldset :disabled="restLocked">
        <legend>{{ t('حساب‌های سیستم حسابداری', 'Accounting') }}</legend>
        <label class="req"><span>{{ t('معین / معین در معین', 'Ledger account') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.ledgerAccountCode" :options="ledgerChoices" required :aria-label="t('معین / معین در معین', 'Ledger account')" :none-text="t('حسابی پیدا نشد', 'No account found')" />
        </label>
        <label class="req"><span>{{ t('تفصیل', 'Detail') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.detail1Code" :options="detailChoices(details1)" remote required :aria-label="t('تفصیل', 'Detail')"
                        :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @search="s => onDetailSearch(1, s)" />
          <small v-if="chosenLedger?.detail1Types.length">{{ t('نوع مجاز:', 'Allowed:') }} {{ chosenLedger.detail1Types.join('، ') }}</small>
        </label>
        <label>{{ t('تفصیل ۲', 'Detail 2') }}
          <SearchSelect v-model="form.detail2Code" :options="detailChoices(details2)" remote :empty-label="t('ندارد', 'None')" :aria-label="t('تفصیل ۲', 'Detail 2')"
                        :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @search="s => onDetailSearch(2, s)" />
        </label>
        <label class="req"><span>{{ t('معین واگذاری', 'Collection account') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.collectionAccountCode" :options="ledgerChoices" required :aria-label="t('معین واگذاری', 'Collection account')" :none-text="t('حسابی پیدا نشد', 'No account found')" />
          <small class="muted">{{ t('می‌تواند همان حساب معین بانک باشد.', 'May be the bank\'s own ledger account.') }}</small>
        </label>
        <label class="req"><span>{{ t('معین برگشتی', 'Bounced-cheque account') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.bouncedAccountCode" :options="ledgerChoices" required :aria-label="t('معین برگشتی', 'Bounced-cheque account')" :none-text="t('حسابی پیدا نشد', 'No account found')" />
        </label>
        <div class="tr-priority">
          <span class="lbl">{{ t('اولویت واگذاری', 'Collection priority') }}</span>
          <button type="button" class="tr-secondary" :aria-expanded="priorityOpen" @click="priorityOpen = !priorityOpen">
            {{ form.collectionPriority ? num(form.collectionPriority) : t('ندارد', 'None') }} ▾
          </button>
          <div v-if="priorityOpen" class="tr-pop" role="dialog" :aria-label="t('اولویت واگذاری', 'Collection priority')">
            <p class="small">{{ t('۱ بالاترین اولویت است. حساب‌های با اولویت بالاتر در فهرست انتخاب بانک برای واگذاری چک‌های دریافتی اول می‌آیند.', '1 is the highest. Accounts with a higher priority come first when choosing the bank to deposit received cheques.') }}</p>
            <div class="tr-pop-buttons">
              <button v-for="p in PRIORITIES" :key="p" type="button" :class="{ chosen: form.collectionPriority === p }" @click="choosePriority(p)">{{ num(p) }}</button>
              <button type="button" class="tr-link" @click="choosePriority(null)">{{ t('بدون اولویت', 'No priority') }}</button>
            </div>
          </div>
        </div>
        <label class="tr-check"><input v-model="form.isFacility" type="checkbox" /> {{ t('تسهیلات', 'Used for facilities') }}</label>
      </fieldset>

      <!-- Owner's report no. 8: the authorised signatories of this account. -->
      <fieldset :disabled="restLocked" class="tr-signers">
        <legend>{{ t('دارندگان امضای مجاز', 'Authorised signatories') }}</legend>
        <p class="muted small tr-full">{{ t('امضاکنندگان چک‌های همین حساب. از میان «دارندگان امضای مجاز (اسناد بهادار)» که تیک «چک» دارند انتخاب می‌شوند. «ثابت»: امضای او روی هر چک الزامی است. «متغیر»: برای تکمیل تعداد امضای لازم، از میان امضاهای متغیر انتخاب می‌شود.',
          'Who signs the cheques of this account, chosen from the authorised signatories ticked for cheques. Fixed: the signature is on every cheque. Variable: chosen among the variable ones to complete the number of signatures.') }}</p>
        <label><span>{{ t('تعداد امضا', 'Number of signatures') }}</span>
          <input v-model="form.requiredSignatures" dir="ltr" inputmode="numeric" maxlength="1" class="tr-count" :aria-label="t('تعداد امضا', 'Number of signatures')" />
        </label>
        <p v-if="form.signatories.length" class="muted small tr-summary">{{ t(`ثابت: ${num(fixedCount)} نفر · متغیر: ${num(variableCount)} نفر. تعداد امضا نباید از امضاهای ثابت کمتر و از کل دارندگان امضا بیشتر باشد.`,
          `Fixed: ${fixedCount} · variable: ${variableCount}. The number of signatures is not less than the fixed ones and not more than all the signatories.`) }}</p>
        <div class="tr-full tr-signer-rows">
          <div v-for="(r, i) in form.signatories" :key="i" class="tr-signer">
            <label class="req"><span>{{ t('نام دارنده امضای مجاز', 'Authorised signatory') }}</span>
              <SearchSelect v-model="r.signatoryId" :options="signatoryChoices(r)" required :aria-label="t(`دارنده امضای مجاز ردیف ${num(i + 1)}`, `Signatory of row ${i + 1}`)"
                            :none-text="t('صاحب امضای دیگری با مجوز چک تعریف نشده است', 'No other signatory is authorised for cheques')" />
            </label>
            <label class="req"><span>{{ t('نوع امضا', 'Kind of signature') }}</span>
              <select v-model="r.kind" :aria-label="t(`نوع امضای ردیف ${num(i + 1)}`, `Kind of signature of row ${i + 1}`)">
                <option value="" disabled>{{ t('— انتخاب کنید —', '— choose —') }}</option>
                <option value="Fixed">{{ t('امضای ثابت', 'Fixed signature') }}</option>
                <option value="Variable">{{ t('امضای متغیر', 'Variable signature') }}</option>
              </select>
            </label>
            <div class="tr-signer-scan" :title="t('اسکن امضا', 'Scan of the signature')">
              <img v-if="scans[r.signatoryId]" :src="scans[r.signatoryId]" :alt="t('نمونه امضا', 'Specimen signature')" />
              <span v-else class="muted small">{{ r.signatoryId ? t('اسکن امضا ثبت نشده', 'No scan stored') : t('اسکن امضا', 'Scan') }}</span>
            </div>
            <button type="button" class="tr-link danger" @click="form.signatories.splice(i, 1)">{{ t('حذف', 'Remove') }}</button>
          </div>
          <p v-if="!signatories.some(s => s.isActive && s.signsCheques)" class="muted small">{{ t('ابتدا در «دارندگان امضای مجاز (اسناد بهادار)» صاحبان امضا را با تیک «چک» تعریف کنید.',
            'First add the signatories ticked for cheques on the page Authorised signatories (valuable documents).') }}</p>
          <button v-else type="button" class="tr-secondary tr-add" @click="addSignatory">{{ form.signatories.length ? t('رکورد جدید', 'New record') : t('تعریف دارنده امضاء مجاز اول', 'Add the first signatory') }}</button>
        </div>
      </fieldset>

      <div class="tr-buttons">
        <button type="submit" :disabled="busy || restLocked">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <template v-if="accounts.length">
      <div class="tr-search">
        <label>{{ t('جستجو (شماره حساب، بانک، شعبه، شهر، معین)', 'Search (number, bank, branch, city, ledger)') }}<input v-model="search" type="search" autocomplete="off" /></label>
        <button type="button" class="tr-secondary" :disabled="!search" @click="search = ''">{{ t('نمایش همه', 'Show all') }}</button>
        <span class="muted small">{{ search ? t(`${num(shown.length)} از ${num(accounts.length)} حساب`, `${shown.length} of ${accounts.length}`) : t(`${num(accounts.length)} حساب`, `${accounts.length} accounts`) }}</span>
      </div>
      <div class="tr-scroll">
        <table class="tr-table">
          <thead>
            <tr>
              <th>{{ t('شماره حساب', 'Number') }}</th><th>{{ t('نوع حساب', 'Kind') }}</th><th>{{ t('بانک', 'Bank') }}</th><th>{{ t('شعبه', 'Branch') }}</th><th>{{ t('شهر', 'City') }}</th>
              <th>{{ t('ارز', 'Currency') }}</th><th>{{ t('معین', 'Ledger') }}</th><th>{{ t('تفصیل', 'Detail') }}</th><th>{{ t('اولویت', 'Priority') }}</th>
              <th>{{ t('پشتیبانِ', 'Backup of') }}</th><th>{{ t('امضای مجاز', 'Signatures') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th />
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in shown" :key="a.id" :class="{ inactive: !a.isActive }">
              <td dir="ltr">{{ a.accountNumber }}</td>
              <td>{{ typeName(a) }}<span v-if="a.cards?.length" class="tr-tag">{{ t(`${num(a.cards.length)} کارت`, `${a.cards.length} cards`) }}</span></td>
              <td>{{ name(banks, a.bankId) }}</td><td>{{ a.branchName }}</td><td>{{ name(cities, a.cityId) }}</td><td>{{ a.currencyCode }}</td>
              <td dir="ltr" :title="ledgerName(a.ledgerAccountCode)">{{ a.ledgerAccountCode }}</td><td dir="ltr">{{ a.detail1Code }}</td>
              <td>{{ a.collectionPriority ? num(a.collectionPriority) : '' }}</td>
              <td dir="ltr">{{ accounts.find(m => m.id === a.mainAccountId)?.accountNumber }}</td>
              <td>{{ signatureSummary(a) }}</td>
              <td>{{ a.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }} <span v-if="a.isInUse" class="tr-tag">{{ t('در حال استفاده', 'in use') }}</span></td>
              <td class="tr-actions">
                <button type="button" class="tr-link" @click="startEdit(a)">{{ t('ویرایش', 'Edit') }}</button>
                <button type="button" class="tr-link" @click="toggle(a)">{{ a.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
                <button v-if="!a.isInUse && !hasBackup(a)" type="button" class="tr-link danger" @click="remove(a)">{{ t('حذف', 'Delete') }}</button>
              </td>
            </tr>
            <tr v-if="!shown.length"><td colspan="12" class="muted">{{ t('با این جستجو حسابی پیدا نشد.', 'No account matches this search.') }}</td></tr>
          </tbody>
        </table>
      </div>
    </template>
    <p v-else-if="!showForm" class="muted">{{ t('هنوز حساب بانکی تعریف نشده است.', 'No bank account yet.') }}</p>
  </div>
</template>

<style scoped>
.tr-card-bad { color: #b42318; font-size: .78rem; }
h3 { margin-top: 0; }
.tr-form { display: grid; gap: 1rem; margin: 1rem 0; }
fieldset { border: 1px solid #e4e7eb; border-radius: 8px; display: grid; grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr)); gap: .75rem; align-items: start; padding: .75rem; }
legend { font-weight: 600; padding: 0 .25rem; }
label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
.tr-check { flex-direction: row; align-items: center; align-self: center; }
/* Owner's report no. 2, item 3: «ارزی» and «پشتیبان» first, under the form title, on the left. */
.tr-kind { grid-column: 1 / -1; display: flex; flex-wrap: wrap; gap: .75rem 1.25rem; align-items: end; justify-content: flex-end; }
.tr-kind .main { min-width: 18rem; }
.tr-kind .wide { flex-basis: 100%; margin: 0; }
fieldset:disabled { opacity: .55; }
.sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
input, select { padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; background: #fff; }
/* Owner, item 12: required fields have a red frame. */
.star { color: #d64545; font-weight: 700; }
small { color: #52606d; min-height: 1em; }
.small { font-size: .8rem; margin: 0; }
.tr-warn { color: #b42318; font-weight: 600; }
button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .5; cursor: default; }
.tr-secondary { background: #fff; color: #1d4ed8; }
.tr-link { background: transparent; color: #1d4ed8; border-color: transparent; padding: .2rem .4rem; }
.tr-buttons { display: flex; gap: .5rem; }
.tr-priority { position: relative; display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
.tr-pop { position: absolute; z-index: 20; top: 100%; inset-inline-start: 0; width: 17rem; margin-top: .25rem; padding: .6rem; background: #fff; border: 1px solid #cbd2d9; border-radius: 8px; box-shadow: 0 6px 18px rgba(31, 41, 51, .15); }
.tr-pop-buttons { display: flex; flex-wrap: wrap; gap: .35rem; margin-top: .5rem; }
.tr-pop-buttons button:not(.tr-link) { min-width: 2.4rem; background: #fff; color: #1d4ed8; }
.tr-pop-buttons button.chosen { background: #2563eb; color: #fff; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-search { display: flex; flex-wrap: wrap; gap: .75rem; align-items: end; margin-top: 1rem; }
.tr-search label { width: 22rem; max-width: 100%; }
.tr-scroll { overflow-x: auto; }
.tr-table { width: 100%; border-collapse: collapse; margin-top: .75rem; }
.tr-table th, .tr-table td { padding: .4rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; }
.tr-table tr.inactive td { color: #9aa5b1; }
.tr-actions { white-space: nowrap; }
.tr-link.danger { color: #b42318; }
.tr-tag { background: #eef2f6; color: #52606d; border-radius: 4px; padding: 0 .3rem; font-size: .75rem; }
.muted { color: #7b8794; }
.tr-cards { display: grid; gap: .5rem; margin-top: .75rem; }
/* Lined up at the top: the hint under the card number makes that field taller than the others. */
.tr-card { display: grid; grid-template-columns: minmax(12rem, 16rem) minmax(12rem, 1fr) auto; gap: .75rem; align-items: start; }
.tr-card > button { margin-top: 1.5rem; }
.type { min-width: 16rem; }
/* Owner's report no. 8: the signatories of the account, one row each. */
.tr-full { grid-column: 1 / -1; margin: 0; }
.tr-summary { grid-column: span 2; align-self: end; margin: 0; }
.tr-count { width: 5rem; }
.tr-signer-rows { display: grid; gap: .6rem; }
.tr-signer { display: grid; grid-template-columns: minmax(12rem, 1.4fr) minmax(9rem, 12rem) 9rem auto; gap: .75rem; align-items: end; }
.tr-signer-scan { height: 3.4rem; display: flex; align-items: center; justify-content: center; background: #f5f7fa; border: 1px dashed #cbd2d9; border-radius: 6px; overflow: hidden; padding: 0 .25rem; text-align: center; }
.tr-signer-scan img { max-width: 100%; max-height: 100%; object-fit: contain; }
.tr-add { justify-self: start; }
@media (max-width: 720px) {
  .tr-summary { grid-column: 1 / -1; }
  .tr-signer { grid-template-columns: 1fr; border: 1px solid #e4e7eb; border-radius: 8px; padding: .5rem; align-items: stretch; }
  .tr-signer > button { justify-self: start; }
  .tr-card { grid-template-columns: 1fr; }
  .tr-card > button { margin-top: 0; justify-self: start; }
  .tr-kind { justify-content: flex-start; }
  .tr-kind .main, .type { min-width: 0; width: 100%; }
}
</style>
