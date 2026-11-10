<script setup lang="ts">
/**
 * Bank accounts (معرفی حساب‌های بانکی), manual section 5. Lists come from reference-svc (banks, cities, currencies,
 * chart of accounts, تفصیل); the bank account itself is saved by accounts-svc, which checks every rule again.
 * - The ledger account boxes offer only accounts that take entries (no lower level), as the old program did.
 * - The تفصیل boxes offer only the types the chosen ledger account allows (owner, 10 Nov 2026).
 * - A backup account (پشتیبان) names a main current account of the same bank, defined first.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, normaliseJalali, translate, type Locale } from '@treasury/ui-kit'

interface Entry { id: string; name: string; code: string | null; isActive: boolean; parentId: string | null }
interface LedgerAccount { code: string; name: string; isPostable: boolean; isActive: boolean; detail1Types: string[]; detail2Types: string[] }
interface Detail { code: string; name: string; type: string | null; isActive: boolean }
interface BankAccount {
  id: string; accountNumber: string; bankId: string; branchName: string | null; branchCode: string | null; cityId: string | null
  openingDate: string | null; currencyCode: string; mainAccountId: string | null; ledgerAccountCode: string; detail1Code: string | null
  detail2Code: string | null; collectionAccountCode: string | null; bouncedAccountCode: string | null; hasCollectionPriority: boolean
  isFacility: boolean; isActive: boolean; version: number
}

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const base = '/api/accounts/bank-accounts'

const banks = ref<Entry[]>([])
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

const empty = () => ({
  accountNumber: '', bankId: '', branchName: '', branchCode: '', cityId: '', openingDate: '', isForeign: false, currencyCode: '',
  isBackup: false, mainAccountId: '', ledgerAccountCode: '', detail1Code: '', detail2Code: '', collectionAccountCode: '',
  bouncedAccountCode: '', hasCollectionPriority: false, isFacility: false,
})
const form = reactive(empty())

const name = (list: { id: string; name: string }[], id: string | null) => list.find(x => x.id === id)?.name ?? ''
const ledgerName = (code: string | null) => ledger.value.find(a => a.code === code)?.name ?? ''
const postable = computed(() => ledger.value.filter(a => a.isPostable && a.isActive))
const chosenLedger = computed(() => ledger.value.find(a => a.code === form.ledgerAccountCode.trim()))
const foreignCurrencies = computed(() => currencies.value.filter(c => c.isActive && c.code !== 'IRR'))
// MVC BanksController: main account of the same bank, not a backup, and without a backup yet.
const mainChoices = computed(() => accounts.value.filter(a =>
  a.isActive && !a.mainAccountId && a.bankId === form.bankId && !accounts.value.some(b => b.mainAccountId === a.id)))

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }

async function loadLists() {
  const [b, c, cu, l] = await Promise.all([
    api.get<Entry[]>('/api/reference/banks'),
    api.get<Entry[]>('/api/reference/cities'),
    api.get<Entry[]>('/api/reference/currencies'),
    api.get<LedgerAccount[]>('/api/reference/ledger/accounts?postableOnly=true'),
  ])
  if (b.ok) banks.value = b.value
  if (c.ok) cities.value = c.value
  if (cu.ok) currencies.value = cu.value
  if (l.ok) ledger.value = l.value
}

async function loadAccounts() {
  const r = await api.get<BankAccount[]>(`${base}?includeInactive=true`)
  if (r.ok) accounts.value = r.value
  else fail(r.code)
}

/** تفصیل offered for one position: of the allowed types when the ledger account lists some, otherwise any active تفصیل. */
async function searchDetails(position: 1 | 2, text: string) {
  const types = position === 1 ? chosenLedger.value?.detail1Types ?? [] : chosenLedger.value?.detail2Types ?? []
  const queries = (types.length ? types : [null]).map(type => {
    const q = new URLSearchParams({ take: '50' })
    if (text.trim()) q.set('search', text.trim())
    if (type) q.set('type', type)
    return api.get<Detail[]>(`/api/reference/ledger/details?${q}`)
  })
  const found = (await Promise.all(queries)).flatMap(r => (r.ok ? r.value : []))
  ;(position === 1 ? details1 : details2).value = found
}

watch(() => form.ledgerAccountCode, () => { void searchDetails(1, ''); void searchDetails(2, '') })
let timer1: ReturnType<typeof setTimeout> | undefined
let timer2: ReturnType<typeof setTimeout> | undefined
watch(() => form.detail1Code, v => { clearTimeout(timer1); timer1 = setTimeout(() => searchDetails(1, v), 250) })
watch(() => form.detail2Code, v => { clearTimeout(timer2); timer2 = setTimeout(() => searchDetails(2, v), 250) })

function startNew() {
  Object.assign(form, empty())
  editing.value = null
  showForm.value = true
  message.value = null
}

function startEdit(a: BankAccount) {
  Object.assign(form, {
    accountNumber: a.accountNumber, bankId: a.bankId, branchName: a.branchName ?? '', branchCode: a.branchCode ?? '', cityId: a.cityId ?? '',
    openingDate: a.openingDate ?? '', isForeign: a.currencyCode !== 'IRR', currencyCode: a.currencyCode === 'IRR' ? '' : a.currencyCode,
    isBackup: !!a.mainAccountId, mainAccountId: a.mainAccountId ?? '', ledgerAccountCode: a.ledgerAccountCode, detail1Code: a.detail1Code ?? '',
    detail2Code: a.detail2Code ?? '', collectionAccountCode: a.collectionAccountCode ?? '', bouncedAccountCode: a.bouncedAccountCode ?? '',
    hasCollectionPriority: a.hasCollectionPriority, isFacility: a.isFacility,
  })
  editing.value = a
  showForm.value = true
  message.value = null
}

async function save() {
  if (busy.value) return
  const payload = {
    accountNumber: form.accountNumber, bankId: form.bankId || '00000000-0000-0000-0000-000000000000', branchName: form.branchName || null,
    branchCode: form.branchCode || null, cityId: form.cityId || null, openingDate: form.openingDate ? normaliseJalali(form.openingDate) ?? form.openingDate : null,
    isForeignCurrency: form.isForeign, currencyCode: form.isForeign ? form.currencyCode : null, ledgerAccountCode: form.ledgerAccountCode.trim(),
    detail1Code: form.detail1Code.trim() || null, detail2Code: form.detail2Code.trim() || null,
    collectionAccountCode: form.collectionAccountCode.trim() || null, bouncedAccountCode: form.bouncedAccountCode.trim() || null,
    hasCollectionPriority: form.hasCollectionPriority, isFacility: form.isFacility,
  }
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { form: payload, version: editing.value.version })
    : await api.post<string>(base, { form: payload, mainAccountId: form.isBackup && form.mainAccountId ? form.mainAccountId : null })
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
      <fieldset>
        <legend>{{ t('مشخصات حساب', 'Account') }}</legend>
        <label>{{ t('شماره حساب', 'Account number') }}<input v-model="form.accountNumber" dir="ltr" autocomplete="off" /></label>
        <label>{{ t('نام بانک', 'Bank') }}
          <select v-model="form.bankId" :disabled="!!editing?.mainAccountId"><option value="" /><option v-for="b in banks" :key="b.id" :value="b.id">{{ b.name }}</option></select>
        </label>
        <label>{{ t('نام شعبه', 'Branch') }}<input v-model="form.branchName" /></label>
        <label>{{ t('کد شعبه', 'Branch code') }}<input v-model="form.branchCode" dir="ltr" /></label>
        <label>{{ t('شهر', 'City') }}
          <select v-model="form.cityId"><option value="" /><option v-for="c in cities" :key="c.id" :value="c.id">{{ c.name }}</option></select>
        </label>
        <label>{{ t('تاریخ افتتاح', 'Opening date') }}<input v-model="form.openingDate" placeholder="1400/01/15" dir="ltr" /></label>
        <label class="tr-check"><input v-model="form.isForeign" type="checkbox" /> {{ t('ارزی', 'Foreign currency') }}</label>
        <label v-if="form.isForeign">{{ t('نوع ارز', 'Currency') }}
          <select v-model="form.currencyCode"><option value="" /><option v-for="c in foreignCurrencies" :key="c.id" :value="c.code">{{ c.name }} ({{ c.code }})</option></select>
        </label>
        <label class="tr-check"><input v-model="form.isBackup" type="checkbox" :disabled="!!editing" /> {{ t('پشتیبان', 'Backup account') }}</label>
        <label v-if="form.isBackup">{{ t('شماره حساب جاری اصلی', 'Main current account') }}
          <select v-model="form.mainAccountId" :disabled="!!editing">
            <option value="" />
            <option v-if="editing?.mainAccountId" :value="editing.mainAccountId">{{ accounts.find(a => a.id === editing!.mainAccountId)?.accountNumber }}</option>
            <option v-for="a in mainChoices" :key="a.id" :value="a.id">{{ a.accountNumber }}</option>
          </select>
          <small class="muted">{{ t('ابتدا حساب اصلی تعریف شود؛ فقط حساب‌های همین بانک.', 'Define the main account first; same bank only.') }}</small>
        </label>
      </fieldset>

      <fieldset>
        <legend>{{ t('حساب‌های سیستم حسابداری', 'Accounting') }}</legend>
        <datalist id="tr-postable"><option v-for="a in postable" :key="a.code" :value="a.code">{{ a.code }} – {{ a.name }}</option></datalist>
        <label>{{ t('معین / معین در معین', 'Ledger account') }}
          <input v-model="form.ledgerAccountCode" list="tr-postable" dir="ltr" autocomplete="off" />
          <small>{{ ledgerName(form.ledgerAccountCode.trim()) }}</small>
        </label>
        <label>{{ t('تفصیل ۱', 'Detail 1') }}
          <input v-model="form.detail1Code" list="tr-d1" dir="ltr" autocomplete="off" />
          <datalist id="tr-d1"><option v-for="d in details1" :key="d.code" :value="d.code">{{ d.code }} – {{ d.name }} ({{ d.type }})</option></datalist>
          <small v-if="chosenLedger?.detail1Types.length">{{ t('نوع مجاز:', 'Allowed:') }} {{ chosenLedger.detail1Types.join('، ') }}</small>
        </label>
        <label>{{ t('تفصیل ۲', 'Detail 2') }}
          <input v-model="form.detail2Code" list="tr-d2" dir="ltr" autocomplete="off" />
          <datalist id="tr-d2"><option v-for="d in details2" :key="d.code" :value="d.code">{{ d.code }} – {{ d.name }} ({{ d.type }})</option></datalist>
        </label>
        <label>{{ t('معین واگذاری', 'Collection account') }}
          <input v-model="form.collectionAccountCode" list="tr-postable" dir="ltr" autocomplete="off" />
          <small>{{ ledgerName(form.collectionAccountCode.trim()) }}</small>
        </label>
        <label>{{ t('معین برگشتی', 'Bounced-cheque account') }}
          <input v-model="form.bouncedAccountCode" list="tr-postable" dir="ltr" autocomplete="off" />
          <small>{{ ledgerName(form.bouncedAccountCode.trim()) }}</small>
        </label>
        <label class="tr-check"><input v-model="form.hasCollectionPriority" type="checkbox" /> {{ t('اولویت واگذاری', 'Collection priority') }}</label>
        <label class="tr-check"><input v-model="form.isFacility" type="checkbox" /> {{ t('تسهیلات', 'Used for facilities') }}</label>
      </fieldset>

      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <table v-if="accounts.length" class="tr-table">
      <thead>
        <tr>
          <th>{{ t('شماره حساب', 'Number') }}</th><th>{{ t('بانک', 'Bank') }}</th><th>{{ t('شعبه', 'Branch') }}</th><th>{{ t('ارز', 'Currency') }}</th>
          <th>{{ t('معین', 'Ledger') }}</th><th>{{ t('تفصیل', 'Detail') }}</th><th>{{ t('پشتیبانِ', 'Backup of') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th />
        </tr>
      </thead>
      <tbody>
        <tr v-for="a in accounts" :key="a.id" :class="{ inactive: !a.isActive }">
          <td dir="ltr">{{ a.accountNumber }} <span v-if="a.hasCollectionPriority" :title="t('اولویت واگذاری', 'Collection priority')">★</span></td>
          <td>{{ name(banks, a.bankId) }}</td><td>{{ a.branchName }}</td><td>{{ a.currencyCode }}</td>
          <td dir="ltr">{{ a.ledgerAccountCode }}</td><td dir="ltr">{{ a.detail1Code }}</td>
          <td dir="ltr">{{ accounts.find(m => m.id === a.mainAccountId)?.accountNumber }}</td>
          <td>{{ a.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
          <td class="tr-actions">
            <button type="button" class="tr-link" @click="startEdit(a)">{{ t('ویرایش', 'Edit') }}</button>
            <button type="button" class="tr-link" @click="toggle(a)">{{ a.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else-if="!showForm" class="muted">{{ t('هنوز حساب بانکی تعریف نشده است.', 'No bank account yet.') }}</p>
  </div>
</template>

<style scoped>
h3 { margin-top: 0; }
.tr-form { display: grid; gap: 1rem; margin: 1rem 0; }
fieldset { border: 1px solid #e4e7eb; border-radius: 8px; display: grid; grid-template-columns: repeat(auto-fill, minmax(13rem, 1fr)); gap: .75rem; align-items: end; padding: .75rem; }
legend { font-weight: 600; padding: 0 .25rem; }
label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
.tr-check { flex-direction: row; align-items: center; }
input, select { padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; }
small { color: #52606d; min-height: 1em; }
button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .6; }
.tr-link { background: transparent; color: #1d4ed8; border-color: transparent; padding: .2rem .4rem; }
.tr-buttons { display: flex; gap: .5rem; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-table { width: 100%; border-collapse: collapse; margin-top: 1rem; }
.tr-table th, .tr-table td { padding: .4rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; }
.tr-table tr.inactive td { color: #9aa5b1; }
.tr-actions { white-space: nowrap; }
.muted { color: #7b8794; }
</style>
