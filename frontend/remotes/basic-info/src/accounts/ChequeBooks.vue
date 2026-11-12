<script setup lang="ts">
/**
 * Cheque books (معرفی دسته چک‌ها), new-version manual item 6: choose one of the company's bank accounts (bank and branch are
 * filled in from it), then the series, first and last sheet; the number of sheets is counted. A book can be switched off and
 * on again ("امکان ابطال دسته چک ها"); corrected or deleted only while none of its sheets has been used. Saved by
 * cashflow-svc, which hands out the sheet numbers when cheques are issued. The print layout of each book (manual item 7)
 * comes with cheque printing.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '../components/SearchSelect.vue'
import { searchKey } from '../text'

interface BankAccount { id: string; accountNumber: string; bankId: string; branchName: string | null; branchCode: string | null; isActive: boolean }
interface Entry { id: string; name: string }
interface Book {
  id: string; bankAccountId: string; serial: string; firstSheet: number; lastSheet: number; sheetCount: number; usedSheets: number
  remainingSheets: number; nextSheet: number | null; isActive: boolean; isInUse: boolean; version: number
}

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US', { useGrouping: false })
const base = '/api/cashflow/cheque-books'

const books = ref<Book[]>([])
const accounts = ref<BankAccount[]>([])
const banks = ref<Entry[]>([])
const accountFilter = ref('')
const search = ref('')
const editing = ref<Book | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const form = reactive({ bankAccountId: '', serial: '', firstSheet: '', lastSheet: '' })

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const account = (id: string) => accounts.value.find(a => a.id === id)
const bankName = (id: string | undefined) => banks.value.find(b => b.id === id)?.name ?? ''
const accountLabel = (a: BankAccount | undefined) => (a ? `${bankName(a.bankId)} · ${a.branchName ?? ''}` : '')
const accountChoices = computed<Choice[]>(() =>
  accounts.value.filter(a => a.isActive || a.id === form.bankAccountId).map(a => ({ value: a.id, label: accountLabel(a), hint: a.accountNumber, muted: !a.isActive })))
const filterChoices = computed<Choice[]>(() => accounts.value.map(a => ({ value: a.id, label: accountLabel(a), hint: a.accountNumber })))
const chosen = computed(() => account(form.bankAccountId))
/** Digits typed in Persian or Arabic are accepted. */
const sheet = (s: string) => {
  const latin = searchKey(s).replace(/\D/g, '')
  return latin ? Number(latin) : NaN
}
const count = computed(() => {
  const f = sheet(form.firstSheet)
  const l = sheet(form.lastSheet)
  return Number.isFinite(f) && Number.isFinite(l) && l >= f ? l - f + 1 : null
})
const shown = computed(() => {
  const q = searchKey(search.value)
  return books.value.filter(b => (!accountFilter.value || b.bankAccountId === accountFilter.value)
    && (!q || [b.serial, String(b.firstSheet), String(b.lastSheet), account(b.bankAccountId)?.accountNumber ?? '', bankName(account(b.bankAccountId)?.bankId)]
      .some(f => searchKey(f).includes(q))))
})

async function load() {
  const r = await api.get<Book[]>(`${base}?includeInactive=true`)
  if (r.ok) books.value = r.value
  else fail(r.code)
}

async function loadLists() {
  const [a, b] = await Promise.all([
    api.get<BankAccount[]>('/api/accounts/bank-accounts?includeInactive=true'),
    api.get<Entry[]>('/api/reference/banks?includeInactive=true'),
  ])
  if (a.ok) accounts.value = a.value
  if (b.ok) banks.value = b.value
}

function startNew() {
  Object.assign(form, { bankAccountId: accountFilter.value, serial: '', firstSheet: '', lastSheet: '' })
  editing.value = null
  showForm.value = true
  message.value = null
}

function startEdit(b: Book) {
  Object.assign(form, { bankAccountId: b.bankAccountId, serial: b.serial, firstSheet: String(b.firstSheet), lastSheet: String(b.lastSheet) })
  editing.value = b
  showForm.value = true
  message.value = null
}

async function save() {
  if (busy.value) return
  if (!form.bankAccountId) return fail('ChequeBook.BankAccountNotFound')
  const first = sheet(form.firstSheet)
  const last = sheet(form.lastSheet)
  if (!Number.isFinite(first) || !Number.isFinite(last)) return fail('ChequeBook.InvalidRange')
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { serial: form.serial, firstSheet: first, lastSheet: last, version: editing.value.version })
    : await api.post<string>(base, { bankAccountId: form.bankAccountId, serial: form.serial, firstSheet: first, lastSheet: last })
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t('دسته چک ثبت شد.', 'Cheque book saved.') }
  showForm.value = false
  await load()
}

async function change(b: Book, what: 'activate' | 'deactivate' | 'delete') {
  if (what === 'delete' && !window.confirm(t('این دسته چک حذف شود؟', 'Delete this cheque book?'))) return
  const r = await api.post<string>(`${base}/${b.id}/${what}`, { version: b.version })
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: what === 'delete' ? t('دسته چک حذف شد.', 'Cheque book deleted.') : what === 'activate' ? t('فعال شد.', 'Switched on.') : t('غیرفعال شد.', 'Switched off.') }
  await load()
}

onMounted(async () => {
  await loadLists()
  await load()
})
</script>

<template>
  <div>
    <h3>{{ t('دسته چک‌ها', 'Cheque books') }}</h3>
    <p v-if="!accounts.length" class="muted">{{ t('ابتدا در «حساب‌های بانکی» حساب بانکی شرکت را تعریف کنید.', 'Define the company\'s bank accounts first.') }}</p>
    <button v-if="!showForm && accounts.length" type="button" @click="startNew">{{ t('دسته چک جدید', 'New cheque book') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t('اصلاح دسته چک', 'Correct cheque book') : t('دسته چک جدید', 'New cheque book') }}</legend>
        <label class="req"><span>{{ t('شماره حساب', 'Bank account') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.bankAccountId" :options="accountChoices" required :disabled="!!editing" :aria-label="t('شماره حساب', 'Bank account')"
                        :none-text="t('حسابی پیدا نشد', 'No account found')" />
        </label>
        <label>{{ t('بانک', 'Bank') }}<input :value="bankName(chosen?.bankId)" readonly tabindex="-1" class="ro" /></label>
        <label>{{ t('شعبه', 'Branch') }}<input :value="chosen ? `${chosen.branchName ?? ''} ${chosen.branchCode ? '(' + chosen.branchCode + ')' : ''}` : ''" readonly tabindex="-1" class="ro" /></label>
        <label>{{ t('سری', 'Series') }}<input v-model="form.serial" maxlength="20" autocomplete="off" /></label>
        <label class="req"><span>{{ t('اولین برگ', 'First sheet') }} <span class="star">*</span></span><input v-model="form.firstSheet" dir="ltr" inputmode="numeric" autocomplete="off" /></label>
        <label class="req"><span>{{ t('آخرین برگ', 'Last sheet') }} <span class="star">*</span></span><input v-model="form.lastSheet" dir="ltr" inputmode="numeric" autocomplete="off" /></label>
        <label>{{ t('تعداد برگ', 'Sheets') }}<input :value="count === null ? '' : num(count)" readonly tabindex="-1" class="ro" /></label>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <template v-if="books.length">
      <div class="tr-search">
        <label>{{ t('حساب بانکی', 'Bank account') }}
          <SearchSelect v-model="accountFilter" :options="filterChoices" :empty-label="t('همه', 'All')" :aria-label="t('حساب بانکی', 'Bank account')" />
        </label>
        <label>{{ t('جستجو (سری، شماره برگ، بانک)', 'Search (series, sheet, bank)') }}<input v-model="search" type="search" autocomplete="off" /></label>
        <button type="button" class="tr-secondary" :disabled="!search && !accountFilter" @click="search = ''; accountFilter = ''">{{ t('نمایش همه', 'Show all') }}</button>
      </div>
      <div class="tr-scroll">
        <table class="tr-table">
          <thead>
            <tr>
              <th>{{ t('شماره حساب', 'Account') }}</th><th>{{ t('بانک', 'Bank') }}</th><th>{{ t('سری', 'Series') }}</th><th>{{ t('از برگ', 'From') }}</th>
              <th>{{ t('تا برگ', 'To') }}</th><th>{{ t('تعداد', 'Sheets') }}</th><th>{{ t('استفاده‌شده', 'Used') }}</th><th>{{ t('باقی‌مانده', 'Left') }}</th>
              <th>{{ t('برگ بعدی', 'Next') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th />
            </tr>
          </thead>
          <tbody>
            <tr v-for="b in shown" :key="b.id" :class="{ inactive: !b.isActive }">
              <td dir="ltr">{{ account(b.bankAccountId)?.accountNumber }}</td>
              <td>{{ bankName(account(b.bankAccountId)?.bankId) }}</td>
              <td>{{ b.serial }}</td>
              <td dir="ltr">{{ b.firstSheet }}</td><td dir="ltr">{{ b.lastSheet }}</td>
              <td>{{ num(b.sheetCount) }}</td><td>{{ num(b.usedSheets) }}</td><td>{{ num(b.remainingSheets) }}</td>
              <td dir="ltr">{{ b.nextSheet ?? '' }}</td>
              <td>{{ b.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
              <td class="tr-actions">
                <button v-if="!b.isInUse" type="button" class="tr-link" @click="startEdit(b)">{{ t('اصلاح', 'Correct') }}</button>
                <button type="button" class="tr-link" :disabled="!b.isActive && !b.remainingSheets" @click="change(b, b.isActive ? 'deactivate' : 'activate')">{{ b.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
                <button v-if="!b.isInUse" type="button" class="tr-link danger" @click="change(b, 'delete')">{{ t('حذف', 'Delete') }}</button>
              </td>
            </tr>
            <tr v-if="!shown.length"><td colspan="11" class="muted">{{ t('دسته چکی پیدا نشد.', 'No cheque book found.') }}</td></tr>
          </tbody>
        </table>
      </div>
    </template>
    <p v-else-if="!showForm && accounts.length" class="muted">{{ t('هنوز دسته چکی تعریف نشده است.', 'No cheque book yet.') }}</p>
  </div>
</template>

<style scoped>
h3 { margin-top: 0; }
.tr-form { display: grid; gap: .75rem; margin: 1rem 0; }
fieldset { border: 1px solid #e4e7eb; border-radius: 8px; display: grid; grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr)); gap: .75rem; align-items: start; padding: .75rem; }
legend { font-weight: 600; padding: 0 .25rem; }
label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
input { padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; background: #fff; }
input.ro { background: #f5f7fa; color: #52606d; }
.req > input { border-color: #d64545; box-shadow: inset 0 0 0 1px #f5c2c2; }
.star { color: #d64545; font-weight: 700; }
button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .5; cursor: default; }
.tr-secondary { background: #fff; color: #1d4ed8; }
.tr-link { background: transparent; color: #1d4ed8; border-color: transparent; padding: .2rem .4rem; }
.tr-link.danger { color: #b42318; }
.tr-buttons { display: flex; gap: .5rem; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-search { display: flex; flex-wrap: wrap; gap: .75rem; align-items: end; margin-top: 1rem; }
.tr-search label { width: 18rem; max-width: 100%; }
.tr-scroll { overflow-x: auto; }
.tr-table { width: 100%; border-collapse: collapse; margin-top: .75rem; }
.tr-table th, .tr-table td { padding: .4rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; }
.tr-table tr.inactive td { color: #9aa5b1; }
.tr-actions { white-space: nowrap; }
.muted { color: #7b8794; }
</style>
