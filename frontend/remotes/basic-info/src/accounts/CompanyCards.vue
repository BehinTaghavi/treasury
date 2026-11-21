<script setup lang="ts">
/**
 * «کارت‌های بانک شرکت (بدون حساب بانکی)» (owner's report no. 6, item 12: «حساب کارت بانک خاص شرکت که فاقد حساب بانکی است»; his answer
 * to Q69, 20 Nov 2026): a bank card of the company that has no bank account in the system, with its 16-digit number, the name on
 * the card, the bank that issued it and the معین and تفصیل a deposit onto it debits. Money paid onto it is recorded on «دریافت‌ها ←
 * واریز به کارت بانک». A card that belongs to one of the company's bank accounts is entered on that account («حساب‌های بانکی»).
 * Saved by accounts-svc, which checks every rule again. Switched off, not deleted (like cash desks).
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { cardNumberHint, createApi, toLatinDigits, toPersianDigits, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { searchKey } from '../text'

interface Card { id: string; cardNumber: string; holderName: string; bankId: string; ledgerAccountCode: string; detailCode: string | null; isActive: boolean; version: number }
interface LedgerAccount { code: string; name: string; isPostable: boolean; isActive: boolean; detail1Types: string[] }
interface Detail { code: string; name: string; type: string | null; isActive: boolean }
interface Entry { id: string; name: string; isActive: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US')
const base = '/api/accounts/company-cards'

const cards = ref<Card[]>([])
const ledger = ref<LedgerAccount[]>([])
const banks = ref<Entry[]>([])
const details = ref<Detail[]>([])
const search = ref('')
const editing = ref<Card | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)

const empty = () => ({ cardNumber: '', holderName: '', bankId: '', ledgerAccountCode: '', detailCode: '' })
const form = reactive(empty())

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const ledgerChoices = computed<Choice[]>(() => ledger.value.filter(a => a.isPostable && a.isActive).map(a => ({ value: a.code, label: a.name, hint: a.code })))
const bankChoices = computed<Choice[]>(() => banks.value.filter(b => b.isActive || b.id === form.bankId).map(b => ({ value: b.id, label: b.name })))
const detailChoices = computed<Choice[]>(() => details.value.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive })))
const chosenLedger = computed(() => ledger.value.find(a => a.code === form.ledgerAccountCode))
const ledgerName = (code: string) => ledger.value.find(a => a.code === code)?.name ?? ''
const bankName = (id: string) => banks.value.find(b => b.id === id)?.name ?? ''
/** The card number in groups of four, as on the card. */
const cardText = (n: string) => { const s = n.replace(/(\d{4})(?=\d)/g, '$1-'); return fa.value ? toPersianDigits(s) : s }
const hint = computed(() => cardNumberHint(form.cardNumber, props.locale))
const shown = computed(() => {
  const q = searchKey(search.value)
  return q ? cards.value.filter(c => [c.holderName, c.cardNumber, bankName(c.bankId), c.ledgerAccountCode, c.detailCode ?? ''].some(f => searchKey(f).includes(q))) : cards.value
})

async function load() {
  const r = await api.get<Card[]>(`${base}?includeInactive=true`)
  if (r.ok) cards.value = r.value
  else fail(r.code)
}

async function loadLists() {
  const [l, b] = await Promise.all([
    api.get<LedgerAccount[]>('/api/reference/ledger/accounts?postableOnly=true'),
    api.get<Entry[]>('/api/reference/banks?includeInactive=true'),
  ])
  if (l.ok) ledger.value = l.value
  if (b.ok) banks.value = b.value
}

/** تفصیل of the types the chosen account allows (all types when it lists none); the chosen one always stays in the list. */
async function searchDetails(text: string) {
  const types = chosenLedger.value?.detail1Types ?? []
  const found = (await Promise.all((types.length ? types : [null]).map(type => {
    const q = new URLSearchParams({ take: '50' })
    if (text.trim()) q.set('search', text.trim())
    if (type) q.set('type', type)
    return api.get<Detail[]>(`/api/reference/ledger/details?${q}`)
  }))).flatMap(r => (r.ok ? r.value : []))
  if (form.detailCode && !found.some(d => d.code === form.detailCode)) {
    const r = await api.get<Detail>(`/api/reference/ledger/details/${encodeURIComponent(form.detailCode)}`)
    if (r.ok) found.unshift(r.value)
  }
  details.value = found
}
watch(() => form.ledgerAccountCode, () => void searchDetails(''))
let timer: ReturnType<typeof setTimeout> | undefined
function onSearch(text: string) {
  clearTimeout(timer)
  timer = setTimeout(() => searchDetails(text), 250)
}

function startNew() {
  Object.assign(form, empty())
  editing.value = null
  showForm.value = true
  message.value = null
  void searchDetails('')
}

function startEdit(c: Card) {
  Object.assign(form, { cardNumber: c.cardNumber, holderName: c.holderName, bankId: c.bankId, ledgerAccountCode: c.ledgerAccountCode, detailCode: c.detailCode ?? '' })
  editing.value = c
  showForm.value = true
  message.value = null
  void searchDetails('')
}

async function save() {
  if (busy.value) return
  const body = {
    cardNumber: toLatinDigits(form.cardNumber).replace(/\D/g, ''), holderName: form.holderName, bankId: form.bankId || '00000000-0000-0000-0000-000000000000',
    ledgerAccountCode: form.ledgerAccountCode, detailCode: form.detailCode || null,
  }
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { form: body, version: editing.value.version })
    : await api.post<string>(base, body)
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t(`کارت «${form.holderName.trim()}» ثبت شد.`, `Card "${form.holderName.trim()}" saved.`) }
  showForm.value = false
  search.value = ''
  await load()
}

async function toggle(c: Card) {
  const r = await api.post<string>(`${base}/${c.id}/${c.isActive ? 'deactivate' : 'activate'}`, { version: c.version })
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: c.isActive ? t('غیرفعال شد.', 'Switched off.') : t('فعال شد.', 'Switched on.') }
  await load()
}

onMounted(async () => {
  await loadLists()
  await load()
})
</script>

<template>
  <div>
    <h3>{{ t('کارت‌های بانک شرکت (بدون حساب بانکی)', "The company's bank cards (without a bank account)") }}</h3>
    <p class="muted">{{ t('کارت بانکی از شرکت که در سیستم حساب بانکی ندارد. وجهی که به این کارت واریز می‌شود در «دریافت‌ها ← واریز به کارت بانک» ثبت می‌شود و حساب معین و تفصیل همین کارت بدهکار می‌شود. کارتی که متعلق به یکی از حساب‌های بانکی شرکت است در «حساب‌های بانکی» و کارت تنخواه‌گردان در «تنخواه گردان‌ها» تعریف می‌شود.',
      "A bank card of the company that has no bank account in the system. Money paid onto it is recorded on «Receipts → Deposit onto a bank card» and debits the card's ledger and detail account. A card of one of the company's bank accounts is entered on «Bank accounts», a petty-cash holder's card on «Petty cash».") }}</p>
    <p v-if="!ledger.length" class="muted">{{ t('ابتدا در «کدینگ حساب‌ها» حساب‌های سیستم حسابداری را وارد کنید.', 'Import the chart of accounts first.') }}</p>
    <button type="button" class="tr-new" @click="startNew">{{ t('کارت جدید', 'New card') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t('ویرایش کارت', 'Edit card') : t('کارت جدید', 'New card') }}</legend>
        <label class="req"><span>{{ t('شماره ۱۶ رقمی کارت بانک', '16-digit card number') }} <span class="star">*</span></span>
          <input v-model="form.cardNumber" dir="ltr" inputmode="numeric" maxlength="19" autocomplete="off" placeholder="6037-9900-0000-0000" />
          <small v-if="hint" :class="hint.bad ? 'tr-card-bad' : 'muted'">{{ hint.text }}</small>
        </label>
        <label class="req"><span>{{ t('عنوان دارندهٔ کارت', 'Name on the card') }} <span class="star">*</span></span>
          <input v-model="form.holderName" maxlength="100" autocomplete="off" />
        </label>
        <label class="req"><span>{{ t('بانک صادرکنندهٔ کارت', 'Issuing bank') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.bankId" :options="bankChoices" required :aria-label="t('بانک صادرکنندهٔ کارت', 'Issuing bank')" :none-text="t('بانکی پیدا نشد', 'No bank found')" />
        </label>
        <label class="req"><span>{{ t('معین / معین در معین کارت', 'Ledger account') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.ledgerAccountCode" :options="ledgerChoices" required :aria-label="t('معین کارت', 'Ledger account')" :none-text="t('حسابی پیدا نشد', 'No account found')" />
        </label>
        <label :class="{ req: !!chosenLedger?.detail1Types.length }"><span>{{ t('تفصیل', 'Detail account') }} <span v-if="chosenLedger?.detail1Types.length" class="star">*</span></span>
          <SearchSelect v-model="form.detailCode" :options="detailChoices" remote :required="!!chosenLedger?.detail1Types.length" :empty-label="t('ندارد', 'None')" :aria-label="t('تفصیل', 'Detail account')"
                        :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @search="onSearch" />
          <small v-if="chosenLedger?.detail1Types.length" class="muted">{{ t('نوع مجاز:', 'Allowed:') }} {{ chosenLedger.detail1Types.join('، ') }}</small>
        </label>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <template v-if="cards.length">
      <div class="tr-search">
        <label>{{ t('جستجو (دارندهٔ کارت، شماره کارت، بانک، کد حساب)', 'Search (name, number, bank, account)') }}<input v-model="search" type="search" autocomplete="off" /></label>
        <button type="button" class="tr-secondary" :disabled="!search" @click="search = ''">{{ t('نمایش همه', 'Show all') }}</button>
        <span class="muted small">{{ t(`${num(shown.length)} از ${num(cards.length)} کارت`, `${shown.length} of ${cards.length}`) }}</span>
      </div>
      <table class="tr-table">
        <thead>
          <tr><th>{{ t('عنوان دارندهٔ کارت', 'Name on the card') }}</th><th>{{ t('شماره کارت', 'Card number') }}</th><th>{{ t('بانک', 'Bank') }}</th><th>{{ t('معین', 'Ledger account') }}</th>
            <th>{{ t('تفصیل', 'Detail') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th /></tr>
        </thead>
        <tbody>
          <tr v-for="c in shown" :key="c.id" :class="{ inactive: !c.isActive }">
            <td>{{ c.holderName }}</td>
            <td dir="ltr">{{ cardText(c.cardNumber) }}</td>
            <td>{{ bankName(c.bankId) }}</td>
            <td><span dir="ltr">{{ c.ledgerAccountCode }}</span> <small class="muted">{{ ledgerName(c.ledgerAccountCode) }}</small></td>
            <td dir="ltr">{{ c.detailCode ?? '' }}</td>
            <td>{{ c.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="startEdit(c)">{{ t('ویرایش', 'Edit') }}</button>
              <button type="button" class="tr-link" @click="toggle(c)">{{ c.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
            </td>
          </tr>
          <tr v-if="!shown.length"><td colspan="7" class="muted">{{ t('با این جستجو کارتی پیدا نشد.', 'No card matches.') }}</td></tr>
        </tbody>
      </table>
    </template>
    <p v-else class="muted">{{ t('هنوز کارتی تعریف نشده است.', 'No card yet.') }}</p>
  </div>
</template>

<style scoped src="../organization/organization.css"></style>
<style scoped>
.tr-card-bad { color: #b42318; font-size: .78rem; }
</style>
