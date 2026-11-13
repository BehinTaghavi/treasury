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
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, normaliseJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '../components/SearchSelect.vue'
import { searchKey } from '../text'

interface Entry { id: string; name: string; code: string | null; isActive: boolean; parentId: string | null }
interface LedgerAccount { code: string; name: string; isPostable: boolean; isActive: boolean; detail1Types: string[]; detail2Types: string[] }
interface Detail { code: string; name: string; type: string | null; isActive: boolean }
interface BankAccount {
  id: string; accountNumber: string; bankId: string; branchName: string | null; branchCode: string | null; cityId: string | null
  openingDate: string | null; currencyCode: string; mainAccountId: string | null; ledgerAccountCode: string; detail1Code: string | null
  detail2Code: string | null; collectionAccountCode: string | null; bouncedAccountCode: string | null; collectionPriority: number | null
  isFacility: boolean; isActive: boolean; version: number
}

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
})
const form = reactive(empty())

const name = (list: { id: string; name: string }[], id: string | null) => list.find(x => x.id === id)?.name ?? ''
const ledgerName = (code: string | null) => ledger.value.find(a => a.code === code)?.name ?? ''
const chosenLedger = computed(() => ledger.value.find(a => a.code === form.ledgerAccountCode))
const foreignCurrencies = computed(() => currencies.value.filter(c => c.isActive && c.code !== 'IRR'))
// MVC BanksController: a current (main) account, not a backup itself and without a backup yet. Owner's report no. 2, item 3:
// «پشتیبان» is ticked first, at the top of the form; the main account is chosen next and the backup takes its bank, branch and
// city. With no such current account the owner's message shows and the rest of the form stays locked, so nothing is saved.
const mainChoices = computed(() => accounts.value.filter(a =>
  a.isActive && !a.mainAccountId && !accounts.value.some(b => b.mainAccountId === a.id)))
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
  const [b, p, c, cu, l] = await Promise.all([
    api.get<Entry[]>('/api/reference/banks'),
    api.get<Entry[]>('/api/reference/provinces'),
    api.get<Entry[]>('/api/reference/cities'),
    api.get<Entry[]>('/api/reference/currencies'),
    api.get<LedgerAccount[]>('/api/reference/ledger/accounts?postableOnly=true'),
  ])
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
  }
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

onMounted(async () => {
  await loadLists()
  await loadAccounts()
})
</script>

<template>
  <div>
    <h3>{{ t('حساب‌های بانکی', 'Bank accounts') }}</h3>
    <p v-if="!ledger.length" class="muted">{{ t('ابتدا در «کدینگ حساب‌ها» حساب‌های سیستم حسابداری را وارد کنید؛ حساب معین بانک از آنجا انتخاب می‌شود.', 'Import the chart of accounts first; the bank\'s ledger account is chosen from it.') }}</p>
    <button v-if="!showForm" type="button" @click="startNew">{{ t('حساب بانکی جدید', 'New bank account') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <p class="muted small"><span class="star">*</span> {{ t('فیلدهای با کادر قرمز اجباری هستند.', 'Fields with a red frame are required.') }}</p>
      <fieldset>
        <legend>{{ t('مشخصات حساب', 'Account') }}</legend>
        <div class="tr-kind">
          <label class="tr-check"><input v-model="form.isForeign" type="checkbox" :disabled="restLocked && !form.isBackup" /> {{ t('ارزی', 'Foreign currency') }}</label>
          <label v-if="form.isForeign" class="req"><span>{{ t('نوع ارز', 'Currency') }} <span class="star">*</span></span>
            <select v-model="form.currencyCode"><option value="" /><option v-for="c in foreignCurrencies" :key="c.id" :value="c.code">{{ c.name }} ({{ c.code }})</option></select>
          </label>
          <label class="tr-check"><input :checked="form.isBackup" type="checkbox" :disabled="!!editing" @change="onBackupChange(($event.target as HTMLInputElement).checked)" /> {{ t('پشتیبان', 'Backup account') }}</label>
          <label v-if="form.isBackup" class="req main"><span>{{ t('شماره حساب جاری اصلی', 'Main current account') }} <span class="star">*</span></span>
            <select v-model="form.mainAccountId" :disabled="!!editing || backupBlocked">
              <option value="" />
              <option v-if="editing?.mainAccountId" :value="editing.mainAccountId">{{ accounts.find(a => a.id === editing!.mainAccountId)?.accountNumber }}</option>
              <option v-for="a in mainChoices" :key="a.id" :value="a.id">{{ mainLabel(a) }}</option>
            </select>
            <small v-if="!backupBlocked && !editing" class="muted">{{ t('بانک، شعبه و شهر از حساب جاری اصلی گرفته می‌شود.', 'Bank, branch and city come from the main account.') }}</small>
          </label>
          <p v-if="backupBlocked" class="tr-msg error wide" role="alert">{{ translate('BankAccount.MainAccountRequired', locale) }}</p>
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
              <th>{{ t('شماره حساب', 'Number') }}</th><th>{{ t('بانک', 'Bank') }}</th><th>{{ t('شعبه', 'Branch') }}</th><th>{{ t('شهر', 'City') }}</th>
              <th>{{ t('ارز', 'Currency') }}</th><th>{{ t('معین', 'Ledger') }}</th><th>{{ t('تفصیل', 'Detail') }}</th><th>{{ t('اولویت', 'Priority') }}</th>
              <th>{{ t('پشتیبانِ', 'Backup of') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th />
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in shown" :key="a.id" :class="{ inactive: !a.isActive }">
              <td dir="ltr">{{ a.accountNumber }}</td>
              <td>{{ name(banks, a.bankId) }}</td><td>{{ a.branchName }}</td><td>{{ name(cities, a.cityId) }}</td><td>{{ a.currencyCode }}</td>
              <td dir="ltr" :title="ledgerName(a.ledgerAccountCode)">{{ a.ledgerAccountCode }}</td><td dir="ltr">{{ a.detail1Code }}</td>
              <td>{{ a.collectionPriority ? num(a.collectionPriority) : '' }}</td>
              <td dir="ltr">{{ accounts.find(m => m.id === a.mainAccountId)?.accountNumber }}</td>
              <td>{{ a.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
              <td class="tr-actions">
                <button type="button" class="tr-link" @click="startEdit(a)">{{ t('ویرایش', 'Edit') }}</button>
                <button type="button" class="tr-link" @click="toggle(a)">{{ a.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
              </td>
            </tr>
            <tr v-if="!shown.length"><td colspan="11" class="muted">{{ t('با این جستجو حسابی پیدا نشد.', 'No account matches this search.') }}</td></tr>
          </tbody>
        </table>
      </div>
    </template>
    <p v-else-if="!showForm" class="muted">{{ t('هنوز حساب بانکی تعریف نشده است.', 'No bank account yet.') }}</p>
  </div>
</template>

<style scoped>
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
.req > input, .req > select { border-color: #d64545; box-shadow: inset 0 0 0 1px #f5c2c2; }
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
.muted { color: #7b8794; }
</style>
