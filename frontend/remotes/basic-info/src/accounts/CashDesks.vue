<script setup lang="ts">
/**
 * Cash desks (صندوق‌ها), new-version manual item 10 "تعریف مشخصات دارندگان صندوق": the holder is chosen by their تفصیل in
 * the accounting system (the name comes from it), plus the معین of the cash desk. Saved by accounts-svc, which checks every
 * rule again. Switched off, not deleted (like bank accounts).
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, normaliseJalali, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '../components/SearchSelect.vue'
import { searchKey } from '../text'

interface Desk {
  id: string; name: string; ledgerAccountCode: string; holderDetailCode: string; holderName: string | null; detail2Code: string | null
  currencyCode: string; isActive: boolean; version: number; createdOn: string | null
}
interface LedgerAccount { code: string; name: string; isPostable: boolean; isActive: boolean; detail1Types: string[]; detail2Types: string[] }
interface Detail { code: string; name: string; type: string | null; isActive: boolean }
interface Entry { id: string; name: string; code: string | null; isActive: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US')
const base = '/api/accounts/cash-desks'

const desks = ref<Desk[]>([])
const ledger = ref<LedgerAccount[]>([])
const currencies = ref<Entry[]>([])
const holders = ref<Detail[]>([])
const details2 = ref<Detail[]>([])
const search = ref('')
const editing = ref<Desk | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)

/** The creation date (owner's report no. 2, item 11) starts as today. */
const empty = () => ({ name: '', ledgerAccountCode: '', holderDetailCode: '', detail2Code: '', isForeign: false, currencyCode: '', createdOn: todayJalali() })
const form = reactive(empty())

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const ledgerChoices = computed<Choice[]>(() => ledger.value.filter(a => a.isPostable && a.isActive).map(a => ({ value: a.code, label: a.name, hint: a.code })))
const detailChoices = (list: Detail[]): Choice[] => list.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive }))
const chosenLedger = computed(() => ledger.value.find(a => a.code === form.ledgerAccountCode))
const foreignCurrencies = computed(() => currencies.value.filter(c => c.isActive && c.code !== 'IRR'))
const ledgerName = (code: string) => ledger.value.find(a => a.code === code)?.name ?? ''
const shown = computed(() => {
  const q = searchKey(search.value)
  return q ? desks.value.filter(d => [d.name, d.holderName ?? '', d.holderDetailCode, d.ledgerAccountCode].some(f => searchKey(f).includes(q))) : desks.value
})

async function load() {
  const r = await api.get<Desk[]>(`${base}?includeInactive=true`)
  if (r.ok) desks.value = r.value
  else fail(r.code)
}

async function loadLists() {
  const [l, c] = await Promise.all([
    api.get<LedgerAccount[]>('/api/reference/ledger/accounts?postableOnly=true'),
    api.get<Entry[]>('/api/reference/currencies'),
  ])
  if (l.ok) ledger.value = l.value
  if (c.ok) currencies.value = c.value
}

/** تفصیل of the types the chosen account allows (all types when it lists none); the chosen one always stays in the list. */
async function searchDetails(position: 1 | 2, text: string) {
  const types = position === 1 ? chosenLedger.value?.detail1Types ?? [] : chosenLedger.value?.detail2Types ?? []
  const found = (await Promise.all((types.length ? types : [null]).map(type => {
    const q = new URLSearchParams({ take: '50' })
    if (text.trim()) q.set('search', text.trim())
    if (type) q.set('type', type)
    // The holder is staff, a cost centre or an organisational unit (owner's report no. 2, item 3).
    if (position === 1) q.set('roles', 'Staff,CostCentre,OrgUnit')
    return api.get<Detail[]>(`/api/reference/ledger/details?${q}`)
  }))).flatMap(r => (r.ok ? r.value : []))
  const current = position === 1 ? form.holderDetailCode : form.detail2Code
  if (current && !found.some(d => d.code === current)) {
    const r = await api.get<Detail>(`/api/reference/ledger/details/${encodeURIComponent(current)}`)
    if (r.ok) found.unshift(r.value)
  }
  ;(position === 1 ? holders : details2).value = found
}

watch(() => form.ledgerAccountCode, () => { void searchDetails(1, ''); void searchDetails(2, '') })
let timer: ReturnType<typeof setTimeout> | undefined
function onSearch(position: 1 | 2, text: string) {
  clearTimeout(timer)
  timer = setTimeout(() => searchDetails(position, text), 250)
}

function startNew() {
  Object.assign(form, empty())
  editing.value = null
  showForm.value = true
  message.value = null
  void searchDetails(1, '')
}

function startEdit(d: Desk) {
  Object.assign(form, {
    name: d.name, ledgerAccountCode: d.ledgerAccountCode, holderDetailCode: d.holderDetailCode, detail2Code: d.detail2Code ?? '',
    isForeign: d.currencyCode !== 'IRR', currencyCode: d.currencyCode === 'IRR' ? '' : d.currencyCode, createdOn: d.createdOn ?? '',
  })
  editing.value = d
  showForm.value = true
  message.value = null
  void searchDetails(1, '')
  void searchDetails(2, '')
}

async function save() {
  if (busy.value) return
  const body = {
    name: form.name, ledgerAccountCode: form.ledgerAccountCode, holderDetailCode: form.holderDetailCode, detail2Code: form.detail2Code || null,
    isForeignCurrency: form.isForeign, currencyCode: form.isForeign ? form.currencyCode : null,
    createdOn: form.createdOn ? normaliseJalali(form.createdOn) ?? form.createdOn : null,
  }
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { form: body, version: editing.value.version })
    : await api.post<string>(base, body)
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t(`صندوق «${form.name.trim()}» ثبت شد.`, `Cash desk "${form.name.trim()}" saved.`) }
  showForm.value = false
  search.value = ''
  await load()
}

async function toggle(d: Desk) {
  const r = await api.post<string>(`${base}/${d.id}/${d.isActive ? 'deactivate' : 'activate'}`, { version: d.version })
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: d.isActive ? t('غیرفعال شد.', 'Switched off.') : t('فعال شد.', 'Switched on.') }
  await load()
}

onMounted(async () => {
  await loadLists()
  await load()
})
</script>

<template>
  <div>
    <h3>{{ t('صندوق‌ها', 'Cash desks') }}</h3>
    <p v-if="!ledger.length" class="muted">{{ t('ابتدا در «کدینگ حساب‌ها» و «حساب‌های تفصیل» حساب‌های سیستم حسابداری را وارد کنید؛ دارندهٔ صندوق باید در سیستم مالی تعریف شده باشد.', 'Import the chart of accounts and the detail accounts first; the holder must exist in the accounting system.') }}</p>
    <button v-if="!showForm" type="button" @click="startNew">{{ t('صندوق جدید', 'New cash desk') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t('ویرایش صندوق', 'Edit cash desk') : t('صندوق جدید', 'New cash desk') }}</legend>
        <label class="req"><span>{{ t('نام صندوق', 'Name') }} <span class="star">*</span></span>
          <input v-model="form.name" maxlength="100" autocomplete="off" :placeholder="t('مثلاً صندوق مرکزی', 'e.g. Head office cash')" />
        </label>
        <label class="req"><span>{{ t('معین / معین در معین صندوق', 'Ledger account') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.ledgerAccountCode" :options="ledgerChoices" required :aria-label="t('معین صندوق', 'Ledger account')" :none-text="t('حسابی پیدا نشد', 'No account found')" />
        </label>
        <label class="req"><span>{{ t('دارندهٔ صندوق (تفصیل)', 'Holder (detail account)') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.holderDetailCode" :options="detailChoices(holders)" remote required :aria-label="t('دارندهٔ صندوق', 'Holder')"
                        :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @search="s => onSearch(1, s)" />
          <small v-if="chosenLedger?.detail1Types.length" class="muted">{{ t('نوع مجاز:', 'Allowed:') }} {{ chosenLedger.detail1Types.join('، ') }}</small>
        </label>
        <label>{{ t('تفصیل ۲', 'Detail 2') }}
          <SearchSelect v-model="form.detail2Code" :options="detailChoices(details2)" remote :empty-label="t('ندارد', 'None')" :aria-label="t('تفصیل ۲', 'Detail 2')"
                        :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @search="s => onSearch(2, s)" />
        </label>
        <label class="tr-check"><input v-model="form.isForeign" type="checkbox" /> {{ t('ارزی', 'Foreign currency') }}</label>
        <label v-if="form.isForeign" class="req"><span>{{ t('نوع ارز', 'Currency') }} <span class="star">*</span></span>
          <select v-model="form.currencyCode"><option value="" /><option v-for="c in foreignCurrencies" :key="c.id" :value="c.code">{{ c.name }} ({{ c.code }})</option></select>
        </label>
        <label class="req"><span>{{ t('تاریخ ایجاد', 'Created on') }} <span class="star">*</span></span><input v-model="form.createdOn" dir="ltr" placeholder="1405/08/22" /></label>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <template v-if="desks.length">
      <div class="tr-search">
        <label>{{ t('جستجو (نام، دارنده، کد حساب)', 'Search (name, holder, account)') }}<input v-model="search" type="search" autocomplete="off" /></label>
        <button type="button" class="tr-secondary" :disabled="!search" @click="search = ''">{{ t('نمایش همه', 'Show all') }}</button>
        <span class="muted small">{{ t(`${num(shown.length)} از ${num(desks.length)} صندوق`, `${shown.length} of ${desks.length}`) }}</span>
      </div>
      <table class="tr-table">
        <thead>
          <tr><th>{{ t('نام صندوق', 'Name') }}</th><th>{{ t('دارنده', 'Holder') }}</th><th>{{ t('معین', 'Ledger account') }}</th><th>{{ t('ارز', 'Currency') }}</th><th>{{ t('تاریخ ایجاد', 'Created') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th /></tr>
        </thead>
        <tbody>
          <tr v-for="d in shown" :key="d.id" :class="{ inactive: !d.isActive }">
            <td>{{ d.name }}</td>
            <td>{{ d.holderName }} <small class="muted" dir="ltr">{{ d.holderDetailCode }}</small></td>
            <td><span dir="ltr">{{ d.ledgerAccountCode }}</span> <small class="muted">{{ ledgerName(d.ledgerAccountCode) }}</small></td>
            <td>{{ d.currencyCode }}</td>
            <td dir="ltr">{{ d.createdOn ?? '' }}</td>
            <td>{{ d.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="startEdit(d)">{{ t('ویرایش', 'Edit') }}</button>
              <button type="button" class="tr-link" @click="toggle(d)">{{ d.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
            </td>
          </tr>
          <tr v-if="!shown.length"><td colspan="7" class="muted">{{ t('با این جستجو صندوقی پیدا نشد.', 'No cash desk matches.') }}</td></tr>
        </tbody>
      </table>
    </template>
    <p v-else-if="!showForm" class="muted">{{ t('هنوز صندوقی تعریف نشده است.', 'No cash desk yet.') }}</p>
  </div>
</template>

<style scoped>
h3 { margin-top: 0; }
.tr-form { display: grid; gap: .75rem; margin: 1rem 0; }
fieldset { border: 1px solid #e4e7eb; border-radius: 8px; display: grid; grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr)); gap: .75rem; align-items: start; padding: .75rem; }
legend { font-weight: 600; padding: 0 .25rem; }
label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
.tr-check { flex-direction: row; align-items: center; align-self: center; }
input:not([type=checkbox]), select { padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; background: #fff; }
.req > input, .req > select { border-color: #d64545; box-shadow: inset 0 0 0 1px #f5c2c2; }
.star { color: #d64545; font-weight: 700; }
button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .5; cursor: default; }
.tr-secondary { background: #fff; color: #1d4ed8; }
.tr-link { background: transparent; color: #1d4ed8; border-color: transparent; padding: .2rem .4rem; }
.tr-buttons { display: flex; gap: .5rem; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-search { display: flex; flex-wrap: wrap; gap: .75rem; align-items: end; margin-top: 1rem; }
.tr-search label { width: 22rem; max-width: 100%; }
.tr-table { width: 100%; border-collapse: collapse; margin-top: .75rem; }
.tr-table th, .tr-table td { padding: .4rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; }
.tr-table tr.inactive td { color: #9aa5b1; }
.tr-actions { white-space: nowrap; }
.muted { color: #7b8794; }
.small { font-size: .8rem; }
</style>
