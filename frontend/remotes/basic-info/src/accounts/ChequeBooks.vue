<script setup lang="ts">
/**
 * Cheque books (معرفی دسته چک‌ها), new-version manual item 6: choose one of the company's bank accounts (bank and branch are
 * filled in from it), then the numbers printed on the sheets (owner, 12 Nov 2026): the series (شماره سری, 4 digits, the same on
 * every sheet), the first serial (شماره سریال, 6–8 digits) and the number of sheets, from which the last serial is calculated, and
 * the Sayad ID (شماره صیادی, 16 digits in four boxes): the first 4 are typed when a cheque is issued, the middle 8 are the same on
 * every sheet, the last 4 run from the first Sayad serial typed here. A book can be switched off and
 * on again ("امکان ابطال دسته چک ها"); corrected or deleted only while none of its sheets has been used. Saved by
 * cashflow-svc, which hands out the sheet numbers when cheques are issued. The print layout of each book (manual item 7)
 * comes with cheque printing.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '../components/SearchSelect.vue'
import { searchKey } from '../text'

interface BankAccount { id: string; accountNumber: string; bankId: string; branchName: string | null; branchCode: string | null; isActive: boolean }
interface Entry { id: string; name: string }
interface Book {
  id: string; bankAccountId: string; serial: string; firstSheet: number; lastSheet: number; sheetCount: number; usedSheets: number
  remainingSheets: number; nextSheet: number | null; isActive: boolean; isInUse: boolean; version: number; formatId: string | null
  sayadMiddle: string | null; sayadFirstSerial: number | null; sayadLastSerial: number | null; partlyUsed: boolean; serialDigits: number
}
/** Banks issue books of 25, 50 or 100 sheets; a book partly used before go-live is entered with any number of unused sheets (owner, 12 Nov 2026). */
const BOOK_SIZES = ['25', '50', '100']
interface Format { id: string; number: number; bankAccountId: string | null; printType: string; isActive: boolean }
const PRINT_TYPES: Record<string, [string, string]> = {
  NewLarge: ['جدید – قطع بزرگ', 'New – large'], NewestLarge: ['جدیدترین – قطع بزرگ', 'Newest – large'], OldLarge: ['قدیمی – قطع بزرگ', 'Old – large'],
  NewSmall: ['جدید – قطع کوچک', 'New – small'], NewestSmall: ['جدیدترین – قطع کوچک', 'Newest – small'], OldSmall: ['قدیمی – قطع کوچک', 'Old – small'],
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
const formats = ref<Format[]>([])
const accountFilter = ref('')
const search = ref('')
const editing = ref<Book | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const form = reactive({ bankAccountId: '', serial: '', firstSheet: '', sheetCount: '50', formatId: '', sayadA: '', sayadB: '', sayadFirst: '', partlyUsed: false })

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const account = (id: string) => accounts.value.find(a => a.id === id)
const bankName = (id: string | undefined) => banks.value.find(b => b.id === id)?.name ?? ''
const accountLabel = (a: BankAccount | undefined) => (a ? `${bankName(a.bankId)} · ${a.branchName ?? ''}` : '')
const accountChoices = computed<Choice[]>(() =>
  accounts.value.filter(a => a.isActive || a.id === form.bankAccountId).map(a => ({ value: a.id, label: accountLabel(a), hint: a.accountNumber, muted: !a.isActive })))
const filterChoices = computed<Choice[]>(() => accounts.value.map(a => ({ value: a.id, label: accountLabel(a), hint: a.accountNumber })))
const chosen = computed(() => account(form.bankAccountId))
const formatLabel = (f: Format | undefined) => (f
  ? `${fa.value ? PRINT_TYPES[f.printType]?.[0] : PRINT_TYPES[f.printType]?.[1]} (${num(f.number)})${f.bankAccountId ? '' : t(' – استاندارد', ' – standard')}`
  : '')
const formatName = (id: string | null) => formatLabel(formats.value.find(f => f.id === id))
/** Manual item 6 "شماره / نوع چک": a cheque type of the chosen account (manual item 7) or a standard type of all accounts (report no. 3, item 3). */
const formatChoices = computed<Choice[]>(() => formats.value
  .filter(f => (f.bankAccountId === form.bankAccountId || !f.bankAccountId) && (f.isActive || f.id === form.formatId))
  .map(f => ({ value: f.id, label: formatLabel(f) })))
/** A new book of an account without its own cheque type takes the standard one. */
watch(() => form.bankAccountId, id => {
  if (editing.value || form.formatId) return
  const own = formats.value.filter(f => f.bankAccountId === id && f.isActive)
  const standard = formats.value.filter(f => !f.bankAccountId && f.isActive)
  if (id && !own.length && standard.length) form.formatId = standard[0]!.id
})
/** Sheets were used: only the cheque type can still change. */
const locked = computed(() => !!editing.value?.isInUse)
/** Digits typed in Persian or Arabic are accepted. */
const sheet = (s: string) => {
  const latin = searchKey(s).replace(/\D/g, '')
  return latin ? Number(latin) : NaN
}
/** Owner: the last serial is calculated from the first serial and the number of sheets. */
const count = computed(() => { const c = sheet(form.sheetCount); return Number.isFinite(c) && c > 0 ? c : null })
const lastSheet = computed(() => { const f = sheet(form.firstSheet); return Number.isFinite(f) && count.value ? f + count.value - 1 : null })
const sayadLast = computed(() => { const s = sheet(form.sayadFirst); return Number.isFinite(s) && count.value ? String(s + count.value - 1).padStart(4, '0') : '' })
const pad4 = (n: number | null) => (n === null ? '' : String(n).padStart(4, '0'))
const digitsOnly = (s: string) => searchKey(s).replace(/\D/g, '')
/** A serial may start with 0 (owner, 12 Nov 2026): it is shown with as many digits as were typed, e.g. 056780. */
const serialText = (n: number | null, digits: number) => (n === null ? '' : String(n).padStart(digits, '0'))
const typedDigits = computed(() => digitsOnly(form.firstSheet).length)
const lastSerialText = computed(() => serialText(lastSheet.value, typedDigits.value))
const shown = computed(() => {
  const q = searchKey(search.value)
  return books.value.filter(b => (!accountFilter.value || b.bankAccountId === accountFilter.value)
    && (!q || [b.serial, serialText(b.firstSheet, b.serialDigits), serialText(b.lastSheet, b.serialDigits), account(b.bankAccountId)?.accountNumber ?? '', bankName(account(b.bankAccountId)?.bankId)]
      .some(f => searchKey(f).includes(q))))
})

async function load() {
  const r = await api.get<Book[]>(`${base}?includeInactive=true`)
  if (r.ok) books.value = r.value
  else fail(r.code)
}

async function loadLists() {
  const [a, b, f] = await Promise.all([
    api.get<BankAccount[]>('/api/accounts/bank-accounts?includeInactive=true'),
    api.get<Entry[]>('/api/reference/banks?includeInactive=true'),
    api.get<Format[]>('/api/cashflow/cheque-formats?includeInactive=true'),
  ])
  if (a.ok) accounts.value = a.value
  if (b.ok) banks.value = b.value
  if (f.ok) formats.value = f.value
}

function startNew() {
  Object.assign(form, { bankAccountId: accountFilter.value, serial: '', firstSheet: '', sheetCount: '50', formatId: '', sayadA: '', sayadB: '', sayadFirst: '', partlyUsed: false })
  editing.value = null
  showForm.value = true
  message.value = null
}

/** Switching back to a new book puts the count on one of the three sizes. */
function choosePartlyUsed(partly: boolean) {
  form.partlyUsed = partly
  if (!partly && !BOOK_SIZES.includes(String(count.value ?? ''))) form.sheetCount = '50'
}

function startEdit(b: Book) {
  Object.assign(form, {
    bankAccountId: b.bankAccountId, serial: b.serial, firstSheet: serialText(b.firstSheet, b.serialDigits), sheetCount: String(b.sheetCount), formatId: b.formatId ?? '',
    sayadA: b.sayadMiddle?.slice(0, 4) ?? '', sayadB: b.sayadMiddle?.slice(4) ?? '', sayadFirst: pad4(b.sayadFirstSerial), partlyUsed: b.partlyUsed,
  })
  editing.value = b
  showForm.value = true
  message.value = null
}

async function save() {
  if (busy.value) return
  if (!form.bankAccountId) return fail('ChequeBook.BankAccountNotFound')
  const first = sheet(form.firstSheet)
  const last = lastSheet.value
  if (!Number.isFinite(first)) return fail('ChequeBook.SerialInvalid')
  if (last === null) return fail('ChequeBook.InvalidRange')
  const sayad = { sayadMiddle: digitsOnly(form.sayadA) + digitsOnly(form.sayadB), sayadFirstSerial: digitsOnly(form.sayadFirst), partlyUsed: form.partlyUsed, serialDigits: typedDigits.value }
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { serial: form.serial, firstSheet: first, lastSheet: last, version: editing.value.version, formatId: form.formatId || null, ...sayad })
    : await api.post<string>(base, { bankAccountId: form.bankAccountId, serial: form.serial, firstSheet: first, lastSheet: last, formatId: form.formatId || null, ...sayad })
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
    <button v-if="accounts.length" type="button" class="tr-new" @click="startNew">{{ t('دسته چک جدید', 'New cheque book') }}</button>
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
        <div class="kind wide-note" role="radiogroup" :aria-label="t('نوع دسته چک', 'Kind of book')">
          <label class="choice"><input type="radio" name="kind" :checked="!form.partlyUsed" :disabled="locked" @change="choosePartlyUsed(false)" />
            {{ t('دسته چک نو (۲۵، ۵۰ یا ۱۰۰ برگ)', 'New book (25, 50 or 100 sheets)') }}</label>
          <label class="choice"><input type="radio" name="kind" :checked="form.partlyUsed" :disabled="locked" @change="choosePartlyUsed(true)" />
            {{ t('دسته چک نیمه‌مصرف (از پیش از شروع کار با نرم‌افزار)', 'Partly used book (from before the company started with this software)') }}</label>
          <small v-if="form.partlyUsed" class="muted">{{ t('فقط برگ‌های استفاده‌نشده را وارد کنید: سریال اولین برگ استفاده‌نشده، تعداد برگ‌های باقی‌مانده (هر تعداد تا ۱۰۰) و سریال صیادی همان برگ.', 'Enter only the unused sheets: the serial of the first unused sheet, how many are left (any number up to 100) and the Sayad serial of that sheet.') }}</small>
        </div>
        <label class="req"><span>{{ t('شماره سری (۴ رقم)', 'Series (4 digits)') }} <span class="star">*</span></span>
          <input v-model="form.serial" maxlength="4" dir="ltr" inputmode="numeric" autocomplete="off" :disabled="locked" placeholder="1234" />
        </label>
        <label class="req"><span>{{ form.partlyUsed ? t('سریال اولین برگ استفاده‌نشده', 'Serial of the first unused sheet') : t('شماره سریال آغازین', 'First serial') }} <span class="star">*</span></span>
          <input v-model="form.firstSheet" maxlength="8" dir="ltr" inputmode="numeric" autocomplete="off" :disabled="locked" placeholder="056780" />
        </label>
        <label v-if="form.partlyUsed" class="req"><span>{{ t('تعداد برگ‌های استفاده‌نشده', 'Unused sheets') }} <span class="star">*</span></span>
          <input v-model="form.sheetCount" maxlength="3" dir="ltr" inputmode="numeric" autocomplete="off" :disabled="locked" placeholder="17" />
        </label>
        <label v-else class="req"><span>{{ t('تعداد برگ دسته چک', 'Number of sheets') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.sheetCount" :disabled="locked" required :aria-label="t('تعداد برگ دسته چک', 'Number of sheets')"
                        :options="BOOK_SIZES.map(s => ({ value: s, label: t(`${num(Number(s))} برگی`, `${s} sheets`) }))" />
        </label>
        <label>{{ t('شماره سریال پایانی', 'Last serial') }}<input :value="lastSerialText" dir="ltr" readonly tabindex="-1" class="ro" /></label>
        <div class="req sayad">
          <span>{{ t('شماره صیادی (۱۶ رقم)', 'Sayad ID (16 digits)') }} <span class="star">*</span></span>
          <div class="sayad-boxes" dir="ltr">
            <input value="" disabled class="ro" :placeholder="t('هنگام صدور', 'at issue')" :aria-label="t('چهار رقم اول (هنگام صدور چک)', 'First 4 digits (typed at issue)')" />
            <input v-model="form.sayadA" maxlength="4" inputmode="numeric" :disabled="locked" :aria-label="t('رقم‌های ۵ تا ۸', 'Digits 5 to 8')" />
            <input v-model="form.sayadB" maxlength="4" inputmode="numeric" :disabled="locked" :aria-label="t('رقم‌های ۹ تا ۱۲', 'Digits 9 to 12')" />
            <input v-model="form.sayadFirst" maxlength="4" inputmode="numeric" :disabled="locked" :aria-label="form.partlyUsed ? t('سریال صیادی اولین برگ استفاده‌نشده', 'Sayad serial of the first unused sheet') : t('سریال صیادی برگ اول', 'Sayad serial of the first sheet')" placeholder="0001" />
          </div>
          <small class="muted">{{ t(form.partlyUsed ? '۴ رقم اول در هنگام صدور هر چک وارد می‌شود؛ ۸ رقم وسط در همهٔ برگ‌ها یکسان است؛ ۴ رقم آخر سریال صیادی اولین برگ استفاده‌نشده است.' : '۴ رقم اول در هنگام صدور هر چک وارد می‌شود؛ ۸ رقم وسط در همهٔ برگ‌ها یکسان است؛ ۴ رقم آخر سریال صیادی برگ اول است.', form.partlyUsed ? 'First 4: typed when each cheque is issued; middle 8: the same on every sheet; last 4: the Sayad serial of the first unused sheet.' : 'First 4: typed when each cheque is issued; middle 8: the same on every sheet; last 4: the Sayad serial of the first sheet.') }}
            <template v-if="sayadLast"> {{ t(`سریال صیادی برگ آخر: ${sayadLast}`, `Last sheet: ${sayadLast}`) }}</template>
          </small>
        </div>
        <label>{{ t('شماره / نوع چک', 'Cheque type') }}
          <SearchSelect v-model="form.formatId" :options="formatChoices" :empty-label="t('تعیین نشده', 'Not chosen')" :aria-label="t('نوع چک', 'Cheque type')"
                        :none-text="t('برای این حساب نوع چکی تعریف نشده است', 'No cheque type for this account')" />
        </label>
        <p v-if="locked" class="muted small wide-note">{{ t('از برگ‌های این دسته چک استفاده شده؛ فقط نوع چک قابل تغییر است.', 'Sheets were used; only the cheque type can change.') }}</p>
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
              <th>{{ t('شماره حساب', 'Account') }}</th><th>{{ t('بانک', 'Bank') }}</th><th>{{ t('سری / سریال', 'Series / serial') }}</th>
              <th>{{ t('شماره صیادی', 'Sayad ID') }}</th><th>{{ t('تعداد', 'Sheets') }}</th><th>{{ t('استفاده‌شده', 'Used') }}</th><th>{{ t('باقی‌مانده', 'Left') }}</th>
              <th>{{ t('برگ بعدی', 'Next') }}</th><th>{{ t('نوع چک', 'Type') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th />
            </tr>
          </thead>
          <tbody>
            <tr v-for="b in shown" :key="b.id" :class="{ inactive: !b.isActive }">
              <td dir="ltr">{{ account(b.bankAccountId)?.accountNumber }}</td>
              <td>{{ bankName(account(b.bankAccountId)?.bankId) }}</td>
              <td dir="ltr">{{ b.serial }}/{{ serialText(b.firstSheet, b.serialDigits) }} – {{ serialText(b.lastSheet, b.serialDigits) }}</td>
              <td dir="ltr" class="small">{{ b.sayadMiddle ? `____ ${b.sayadMiddle.slice(0, 4)} ${b.sayadMiddle.slice(4)} ${pad4(b.sayadFirstSerial)}–${pad4(b.sayadLastSerial)}` : '' }}</td>
              <td>{{ num(b.sheetCount) }} <small v-if="b.partlyUsed" class="tr-badge" :title="t('دسته چک نیمه‌مصرف از پیش از شروع کار؛ فقط برگ‌های استفاده‌نشده ثبت شده است.', 'Partly used before go-live; only its unused sheets are registered.')">{{ t('نیمه‌مصرف', 'partly used') }}</small></td><td>{{ num(b.usedSheets) }}</td><td>{{ num(b.remainingSheets) }}</td>
              <td dir="ltr">{{ serialText(b.nextSheet, b.serialDigits) }}</td>
              <td>{{ formatName(b.formatId) }}</td>
              <td>{{ b.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
              <td class="tr-actions">
                <button type="button" class="tr-link" @click="startEdit(b)">{{ t('اصلاح', 'Correct') }}</button>
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
.small { font-size: .8rem; }
.wide-note { grid-column: 1 / -1; margin: 0; }
.sayad { grid-column: span 2; display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
.sayad-boxes { display: grid; grid-template-columns: repeat(4, 1fr); gap: .35rem; }
.sayad-boxes input { text-align: center; letter-spacing: .15em; min-width: 0; }
.req .sayad-boxes input:not(.ro) { border-color: #d64545; box-shadow: inset 0 0 0 1px #f5c2c2; }
input:disabled { background: #f5f7fa; color: #7b8794; }
select { padding: .45rem .5rem; border: 1px solid #d64545; border-radius: 6px; font: inherit; background: #fff; }
.kind { display: flex; flex-wrap: wrap; gap: .5rem 1.5rem; align-items: center; }
.kind small { flex-basis: 100%; }
.choice { flex-direction: row; align-items: center; gap: .4rem; font-size: .9rem; }
.tr-badge { background: #fff4e5; color: #8a4b00; border-radius: 4px; padding: 0 .3rem; font-size: .75rem; white-space: nowrap; }
</style>
