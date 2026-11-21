<script setup lang="ts">
/**
 * «نقل و انتقال بانکی – انتقال الکترونیکی» (owner's report no. 7 of 20 Nov 2026, «روش ساده»): money moved between two bank accounts of
 * the company without a cheque, by one of the four ways he listed — «انتقال داخلی (حساب به حساب)» within one bank, and «پل», «پایا»
 * and «ساتنا» between two banks. The form offers only the ways that fit the two accounts, asks whether the order was given at the
 * branch or remotely, and names the limits of the chosen way under it; the limits are the company's own figures, kept in
 * «اطلاعات پایه ← سقف‌های انتقال الکترونیکی» («فیلد محدودیت مبالغ … قابل کانفیگ … و در کد ثابت نباشد»), and cashflow-svc refuses an
 * order that breaks them. The voucher (بدهکار بانک انتقال گیرنده، بستانکار بانک انتقال دهنده) is shown while the form is filled.
 * Under the form: the electronic transfers of the fiscal year with «ویرایش» and «حذف» until the voucher is confirmed (as for receipts,
 * report no. 6), and «تایید سند». A deleted transfer stays in the list, light red, marked «حذف شده», so its number is seen to be taken.
 * The same screen stands in «دریافت‌ها» and in «پرداخت‌ها»: a transfer is a payment of the source bank and a receipt of the destination bank.
 * The form follows the receipt form the owner approved on 20 Nov 2026 (numbered cards, summary beside them, required fields marked by a dot).
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { amountTextInWords, createApi, formatAmount, markMissing, normaliseJalali, parseAmount, searchKey, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import AmountInput from '@treasury/ui-kit/AmountInput.vue'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'
import VoucherTable, { type Voucher } from './VoucherTable.vue'
import TransferVoucher from './TransferVoucher.vue'
import { amountText } from './savedReceipt'
import { accountText, channelName, channelShortName, channels, limitFor, limitLines, methodName, methodShortName, methodUse, totalRial, waysFor, type Transfer, type TransferChannel, type TransferLimit } from './transfers'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => (fa.value ? n.toLocaleString('fa-IR', { useGrouping: false }) : String(n))
const L = useLookups(api)
const year = computed(() => props.fiscalYear ?? Number(todayJalali().slice(0, 4)))

// ---- the form ----
const blank = () => ({ date: todayJalali(), sourceId: '', destinationId: '', method: '', channel: '' as TransferChannel | '', amount: '', rate: '', tracking: '', description: '' })
const form = reactive(blank())
/** The saved transfer being corrected («ویرایش»). */
const editing = ref<Transfer | null>(null)
const source = computed(() => L.bankAccounts.value.find(a => a.id === form.sourceId))
const destination = computed(() => L.bankAccounts.value.find(a => a.id === form.destinationId))
const currency = computed(() => source.value?.currencyCode ?? 'IRR')
const unit = computed(() => (currency.value === 'IRR' ? 'ریال' : L.currencies.value.find(c => c.code === currency.value)?.name ?? currency.value))
/** An account switched off stays only on the transfer that already names it. */
const sourceChoices = computed<Choice[]>(() => L.bankAccounts.value.filter(a => (a.isActive || a.id === form.sourceId) && a.id !== form.destinationId)
  .map(a => ({ value: a.id, label: L.accountTitle(a), hint: a.currencyCode, muted: !a.isActive })))
/** The destination is another account of the company in the source account's currency. */
const destinationChoices = computed<Choice[]>(() => L.bankAccounts.value
  .filter(a => (a.isActive || a.id === form.destinationId) && a.id !== form.sourceId && (!source.value || a.currencyCode === source.value.currencyCode))
  .map(a => ({ value: a.id, label: L.accountTitle(a), hint: a.currencyCode, muted: !a.isActive })))
/**
 * Owner's report no. 7: an internal transfer is between two accounts of the same bank; Pol, Paya and Satna between two different
 * banks, in Rial. Only the ways that fit the two accounts are offered.
 */
const ways = computed(() => waysFor(source.value?.bankId, destination.value?.bankId).filter(m => m === 'Internal' || currency.value === 'IRR'))
const methodChoices = computed<Choice[]>(() => ways.value.map(m => ({ value: m, label: `${methodName(m, props.locale)} – ${methodUse(m, props.locale)}` })))
const channelChoices = computed<Choice[]>(() => channels.map(c => ({ value: c, label: channelName(c, props.locale) })))
watch(() => [form.sourceId, form.destinationId], () => {
  if (form.destinationId && !destinationChoices.value.some(c => c.value === form.destinationId)) form.destinationId = ''
  if (form.method && !ways.value.some(m => m === form.method)) form.method = ''
  // Within one bank there is one way: it is chosen for the user.
  if (!form.method && source.value && destination.value && ways.value.length === 1) form.method = ways.value[0]!
})

// ---- the limits of the chosen way (data of the company, not code) ----
const limits = ref<TransferLimit[]>([])
const applied = computed(() => (form.method && source.value ? limitFor(limits.value, form.method, source.value.bankId) : undefined))
const appliedLines = computed(() => limitLines(applied.value, form.channel, props.locale))
const ownLimit = computed(() => applied.value?.bankId != null)

const amountRial = computed(() => {
  const a = parseAmount(form.amount)
  if (a === null) return 0
  if (currency.value === 'IRR') return a
  const rate = parseAmount(form.rate)
  return rate === null ? 0 : Math.round(a * rate)
})
const totalWords = computed(() => (amountRial.value > 0 ? amountTextInWords(String(amountRial.value), 'ریال') : ''))

/** The request the form makes, or the code of the first thing missing (checked again by the server). */
function request(): { body: Record<string, unknown> } | { error: string } {
  const date = normaliseJalali(form.date)
  if (!date) return { error: 'FiscalDate.Invalid' }
  if (!form.sourceId) return { error: 'Transfer.SourceRequired' }
  if (!form.destinationId) return { error: 'Transfer.DestinationRequired' }
  if (!form.method) return { error: 'Transfer.MethodRequired' }
  if (!form.channel) return { error: 'Transfer.ChannelRequired' }
  const amount = parseAmount(form.amount)
  if (amount === null || amount <= 0) return { error: 'Money.NotPositive' }
  const rate = currency.value === 'IRR' ? null : parseAmount(form.rate)
  if (currency.value !== 'IRR' && rate === null) return { error: 'Money.InvalidRate' }
  return { body: {
    date, sourceAccountId: form.sourceId, destinationAccountId: form.destinationId, method: form.method, channel: form.channel, amount, rateToRial: rate,
    trackingNumber: form.tracking.trim() || null, description: form.description.trim() || null } }
}

// ---- the voucher while the form is filled (nothing is saved) ----
const preview = ref<Voucher | null>(null)
const previewHint = ref('')
let previewTimer: ReturnType<typeof setTimeout> | undefined
let previewRun = 0
watch(form, () => {
  // A refusal shown beside the form is about the form as it was; it goes when the user changes something.
  if (message.value?.kind === 'error' && !saving.value) message.value = null
  clearTimeout(previewTimer)
  previewTimer = setTimeout(refreshPreview, 400)
}, { deep: true })
async function refreshPreview() {
  const run = ++previewRun
  const req = request()
  if ('error' in req) { preview.value = null; previewHint.value = translate(req.error, props.locale); return }
  const r = await api.post<Voucher>(`/api/cashflow/transfers/electronic/voucher-preview${editing.value ? `?correcting=${editing.value.id}` : ''}`, req.body)
  if (run !== previewRun) return
  preview.value = r.ok ? r.value : null
  previewHint.value = r.ok ? '' : translate(r.code, props.locale)
}

// ---- save ----
const formEl = ref<HTMLFormElement | null>(null)
const saving = ref(false)
const message = ref<{ kind: 'error' | 'ok'; text: string } | null>(null)
const show = (kind: 'error' | 'ok', text: string) => { message.value = { kind, text } }
async function save() {
  message.value = null
  const req = request()
  markMissing(formEl.value)
  if ('error' in req) return show('error', translate(req.error, props.locale))
  saving.value = true
  try {
    const old = editing.value
    const r = old
      ? await api.put<string>(`/api/cashflow/transfers/${old.id}/electronic`, { form: req.body, version: old.version })
      : await api.post<string>('/api/cashflow/transfers/electronic', req.body)
    if (!r.ok) return show('error', translate(r.code, props.locale))
    const saved = await api.get<Transfer>(`/api/cashflow/transfers/${r.value}`)
    const no = saved.ok ? num(saved.value.number) : ''
    show('ok', old ? t(`ویرایش نقل و انتقال شمارهٔ ${no} ثبت شد.`, `Transfer no. ${no} corrected.`)
      : t(`نقل و انتقال شمارهٔ ${no} ثبت شد. سند آن را در فهرست زیر «تایید سند» کنید.`, `Transfer no. ${no} saved. Confirm its voucher in the list below.`))
    editing.value = null
    // After a correction the form is empty again; after a new transfer the accounts and the way stay for the next one.
    Object.assign(form, old ? blank() : { amount: '', rate: '', tracking: '', description: '' })
    await load()
  } finally {
    saving.value = false
  }
}
function onKey(e: KeyboardEvent) {
  if (e.key === 'Enter' && e.ctrlKey) { e.preventDefault(); void save() }
}

// ---- the list under the form ----
const transfers = ref<Transfer[]>([])
const listError = ref('')
const search = ref('')
async function load() {
  const r = await api.get<Transfer[]>(`/api/cashflow/transfers?year=${year.value}&cheque=false`)
  if (r.ok) { transfers.value = r.value; listError.value = '' } else listError.value = translate(r.code, props.locale)
}
const from = (x: Transfer) => accountText(x.sourceBankName, x.sourceAccountNumber)
const to = (x: Transfer) => accountText(x.destinationBankName, x.destinationAccountNumber)
const shown = computed(() => {
  const key = searchKey(search.value)
  return transfers.value.filter(x => !key || searchKey(`${x.number} ${x.date} ${methodName(x.method, props.locale)} ${from(x)} ${to(x)} ${x.trackingNumber ?? ''} ${x.description ?? ''}`).includes(key))
})
const total = computed(() => totalRial(shown.value))

/** «ویرایش»: the saved transfer is opened in the form above and saved back under its number, while its voucher is not confirmed. */
function startEdit(x: Transfer) {
  if (x.whyNotCorrectable) return show('error', translate(x.whyNotCorrectable, props.locale))
  message.value = null
  voucherOf.value = ''
  editing.value = x
  Object.assign(form, {
    date: x.date, sourceId: x.sourceAccountId, destinationId: x.destinationAccountId, method: x.method, channel: x.channel ?? '', amount: amountText(x.amount),
    rate: amountText(x.rateToRial), tracking: x.trackingNumber ?? '', description: x.description ?? '' })
  window.scrollTo({ top: 0, behavior: 'smooth' })
}
function cancelEdit() {
  editing.value = null
  message.value = null
  Object.assign(form, blank())
}

/** «حذف» of a transfer recorded by mistake, before its voucher is confirmed; it stays in the list marked «حذف شده». */
const busy = ref(false)
async function remove(x: Transfer) {
  if (busy.value) return
  if (x.whyNotDeletable) return void (listError.value = translate(x.whyNotDeletable, props.locale))
  if (!window.confirm(t(`نقل و انتقال شمارهٔ ${num(x.number)} (${from(x)} به ${to(x)} – ${money(x.amountRial)} ریال) حذف شود؟ با عبارت «حذف شده» در فهرست می‌ماند و شمارهٔ آن به نقل و انتقال دیگری داده نمی‌شود.`,
    `Delete transfer no. ${x.number} (${from(x)} to ${to(x)} – ${money(x.amountRial)} Rial)? It stays in the list marked as deleted, and its number is not given again.`))) return
  busy.value = true
  const r = await api.post<string>(`/api/cashflow/transfers/${x.id}/delete`, { version: x.version })
  busy.value = false
  if (!r.ok) return void (listError.value = translate(r.code, props.locale))
  if (editing.value?.id === x.id) cancelEdit()
  if (voucherOf.value === x.id) voucherOf.value = ''
  show('ok', t(`نقل و انتقال شمارهٔ ${num(x.number)} حذف شد و با عبارت «حذف شده» در فهرست مانده است.`, `Transfer no. ${x.number} was deleted; it stays in the list marked as deleted.`))
  await load()
}

/** The transfer whose voucher is open under the list for «تایید سند». */
const voucherOf = ref('')

onMounted(async () => {
  const [, l] = await Promise.all([L.load(), api.get<TransferLimit[]>('/api/cashflow/transfer-limits')])
  if (l.ok) limits.value = l.value
  await load()
})
watch(year, () => void load())
</script>

<template>
  <div>
    <form ref="formEl" class="tr-form tr-rf" novalidate @keydown="onKey" @submit.prevent="save">
      <div class="tr-rf-main">
        <!-- 1. the two accounts -->
        <section class="tr-card">
          <header class="tr-card-head">
            <span class="tr-step" aria-hidden="true">{{ num(1) }}</span>
            <h3>{{ editing ? t(`ویرایش نقل و انتقال شمارهٔ ${num(editing.number)}`, `Correct transfer no. ${editing.number}`) : t('حساب‌های نقل و انتقال', 'Accounts of the transfer') }}</h3>
            <span class="tr-req-note"><span class="tr-req-dot" aria-hidden="true" />{{ t('پر کردن الزامی است', 'must be filled in') }}</span>
          </header>
          <fieldset class="tr-fields">
            <label class="req">{{ t('تاریخ انتقال', 'Date') }} <input v-model="form.date" placeholder="1405/08/29" dir="ltr" /></label>
            <label class="wide">{{ t('حساب مبدأ (انتقال دهنده)', 'Source account (the payer)') }}
              <SearchSelect v-model="form.sourceId" :options="sourceChoices" required :aria-label="t('حساب مبدأ (انتقال دهنده)', 'Source account')"
                            :none-text="t('حساب بانکی فعالی تعریف نشده است', 'No active bank account')" />
            </label>
            <label class="wide">{{ t('حساب مقصد (انتقال گیرنده)', 'Destination account (the receiver)') }}
              <SearchSelect v-model="form.destinationId" :options="destinationChoices" required :aria-label="t('حساب مقصد (انتقال گیرنده)', 'Destination account')"
                            :none-text="source ? t('حساب دیگری با همین ارز نیست', 'No other account in this currency') : t('حساب بانکی فعالی تعریف نشده است', 'No active bank account')" />
            </label>
            <p v-if="L.bankAccounts.value.length < 2" class="muted small wide3">
              {{ t('برای نقل و انتقال دست‌کم دو حساب بانکی شرکت لازم است؛ حساب‌ها در «اطلاعات پایه ← حساب‌های بانکی» تعریف می‌شوند.',
                'A transfer needs at least two bank accounts of the company; they are entered in Basic Information → Bank accounts.') }}
            </p>
          </fieldset>
        </section>

        <!-- 2. the way, the channel and the amount -->
        <section class="tr-card">
          <header class="tr-card-head">
            <span class="tr-step" aria-hidden="true">{{ num(2) }}</span>
            <h3>{{ t('روش انتقال و مبلغ', 'Way of transfer and amount') }}</h3>
          </header>
          <fieldset class="tr-fields">
            <label class="wide">{{ t('روش انتقال', 'Way of transfer') }}
              <SearchSelect v-model="form.method" :options="methodChoices" required :aria-label="t('روش انتقال', 'Way of transfer')" />
            </label>
            <label class="wide">{{ t('نحوهٔ دستور', 'How the order was given') }}
              <SearchSelect v-model="form.channel" :options="channelChoices" required :aria-label="t('نحوهٔ دستور', 'How the order was given')" />
            </label>
            <!-- The limits of the chosen way: the company's own figures (owner's report no. 7). -->
            <div v-if="form.method" class="tr-limits">
              <template v-if="appliedLines.length">
                <strong>{{ t(`محدودیت‌های ${methodName(form.method, locale)}${ownLimit ? ` – سقف ویژهٔ ${L.bankName(source?.bankId)}` : ''}:`,
                  `Limits of ${methodName(form.method, locale)}${ownLimit ? ` – ${L.bankName(source?.bankId)}'s own` : ''}:`) }}</strong>
                <ul><li v-for="line in appliedLines" :key="line">{{ line }}</li></ul>
              </template>
              <template v-else>{{ form.method === 'Internal'
                ? t('برای «انتقال داخلی» سقفی ثبت نشده است؛ هر بانک ضوابط خود را دارد و سقف ویژهٔ آن بانک را می‌توان ثبت کرد.', 'No limit is kept for the internal transfer; every bank has its own rules, and a bank\'s own limit can be entered.')
                : t('برای این روش سقفی ثبت نشده است.', 'No limit is kept for this way.') }}</template>
              <span class="muted"> {{ t('این سقف‌ها در «اطلاعات پایه ← سقف‌های انتقال الکترونیکی» تغییر می‌کنند.', 'These limits are changed in Basic Information → Electronic transfer limits.') }}</span>
            </div>
            <p v-else-if="source && destination" class="tr-limits">
              {{ source.bankId === destination.bankId
                ? t('دو حساب در یک بانک هستند: «انتقال داخلی (حساب به حساب)».', 'Both accounts are in the same bank: internal transfer.')
                : t('دو حساب در دو بانک متفاوت هستند: «پل» برای مبالغ کم و فوری، «پایا» برای مبالغ متوسط، «ساتنا» برای مبالغ بالا.',
                  'The accounts are in two different banks: Pol for small urgent amounts, Paya for middle amounts, Satna for large amounts.') }}
            </p>
            <label class="req wide">{{ t('مبلغ', 'Amount') }} ({{ currency }}) <AmountInput v-model="form.amount" :decimals="currency === 'IRR' ? 0 : 2" :unit="unit" /></label>
            <template v-if="currency !== 'IRR'">
              <label class="req">{{ t('نرخ ارز (ریال)', 'Rate (Rial)') }} <AmountInput v-model="form.rate" :decimals="4" unit="ریال" /></label>
              <label>{{ t('هم‌ارز ریالی', 'Rial equivalent') }} <input class="ro num" :value="amountRial ? money(amountRial) : ''" readonly /></label>
            </template>
            <label>{{ t('شماره پیگیری', 'Tracking no.') }} <input v-model="form.tracking" dir="ltr" :placeholder="t('اختیاری', 'Optional')" /></label>
            <label class="wide3">{{ t('شرح', 'Description') }} <input v-model="form.description" maxlength="500" :placeholder="t('اختیاری', 'Optional')" /></label>
          </fieldset>
        </section>

        <!-- 3. the voucher the transfer will make (nothing is saved) -->
        <section class="tr-card">
          <header class="tr-card-head">
            <span class="tr-step" aria-hidden="true">{{ num(3) }}</span>
            <h3>{{ t('سند حسابداری', 'Voucher') }}</h3>
            <span class="tr-pill">{{ t('پیش‌نمایش', 'Preview') }}</span>
          </header>
          <VoucherTable v-if="preview" :voucher="preview" :locale="locale" :account-name="L.accountName" />
          <p v-else class="muted small">{{ t('سند این نقل و انتقال (بدهکار: بانک انتقال گیرنده، بستانکار: بانک انتقال دهنده) پس از تکمیل فرم اینجا نمایش داده می‌شود.',
            'The voucher of the transfer (debit: the receiving bank, credit: the paying bank) is shown here once the form is filled in.') }}
            <template v-if="previewHint"> ({{ previewHint }})</template></p>
        </section>
      </div>

      <!-- The summary beside the form. -->
      <aside class="tr-rf-side" :aria-label="t('خلاصهٔ نقل و انتقال', 'Summary of the transfer')">
        <div class="tr-card tr-summary">
          <h3>{{ t('خلاصهٔ نقل و انتقال', 'Summary') }}</h3>
          <dl>
            <div><dt>{{ t('از حساب (انتقال دهنده)', 'From') }}</dt><dd>{{ source ? L.accountTitle(source) : '—' }}</dd></div>
            <div><dt>{{ t('به حساب (انتقال گیرنده)', 'To') }}</dt><dd>{{ destination ? L.accountTitle(destination) : '—' }}</dd></div>
            <div><dt>{{ t('روش', 'Way') }}</dt><dd>{{ form.method ? methodName(form.method, locale) : '—' }}</dd></div>
            <div><dt>{{ t('نحوهٔ دستور', 'Order') }}</dt><dd>{{ form.channel ? channelName(form.channel, locale) : '—' }}</dd></div>
            <div><dt>{{ t('تاریخ', 'Date') }}</dt><dd class="num" dir="ltr">{{ form.date || '—' }}</dd></div>
          </dl>
          <div class="tr-total">
            <span>{{ t('مبلغ (ریال)', 'Amount (Rial)') }}</span>
            <strong class="num">{{ amountRial ? money(amountRial) : '—' }}</strong>
            <small v-if="totalWords && fa">{{ totalWords }}</small>
          </div>
          <button type="submit" class="tr-save" :disabled="saving">{{ editing ? t('ثبت ویرایش', 'Save the correction') : t('ثبت نقل و انتقال', 'Save the transfer') }}</button>
          <button v-if="editing" type="button" class="tr-secondary" :disabled="saving" @click="cancelEdit">{{ t('انصراف از ویرایش', 'Leave without saving') }}</button>
          <p class="tr-kbd">{{ t('میان‌بر صفحه‌کلید:', 'Keyboard shortcut:') }} <kbd dir="ltr">Ctrl + Enter</kbd></p>
        </div>
        <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
      </aside>
    </form>

    <!-- The electronic transfers of the fiscal year, newest first. -->
    <section class="tr-card tr-list">
      <div class="tr-list-head">
        <h3>{{ t(`نقل و انتقال‌های الکترونیکی – سال مالی ${num(year)}`, `Electronic transfers – fiscal year ${year}`) }}</h3>
        <label style="flex: 1; min-width: 14rem">{{ t('جستجو (شماره، حساب، روش، پیگیری)', 'Search (number, account, way, tracking)') }} <input v-model="search" /></label>
        <span class="tr-list-total">{{ t('جمع (بدون حذف‌شده‌ها):', 'Total (without deleted):') }} <strong class="num">{{ money(total) }}</strong> {{ t('ریال', 'Rial') }}</span>
      </div>
      <p v-if="listError" class="tr-msg error" role="status">{{ listError }}</p>
      <div class="tr-scroll">
        <table class="tr-table">
          <thead>
            <tr>
              <th>{{ t('شماره', 'No.') }}</th><th>{{ t('تاریخ', 'Date') }}</th><th>{{ t('روش', 'Way') }}</th><th>{{ t('حساب‌ها: انتقال دهنده ← انتقال گیرنده', 'Accounts: payer → receiver') }}</th>
              <th>{{ t('مبلغ', 'Amount') }}</th><th>{{ t('پیگیری / شرح', 'Tracking / description') }}</th>
              <th>{{ t('وضعیت سند', 'Voucher') }}</th><th>{{ t('عملیات', 'Actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="x in shown" :key="x.id" :class="{ deleted: x.state === 'Deleted', selected: editing?.id === x.id || voucherOf === x.id }">
              <td class="num">{{ num(x.number) }}</td>
              <td class="num">{{ x.date }}</td>
              <td class="tr-nowrap" :title="`${methodName(x.method, locale)} – ${channelName(x.channel, locale)}`">{{ methodShortName(x.method, locale) }}<span class="muted small">{{ channelShortName(x.channel, locale) }}</span></td>
              <td><dl class="tr-accounts"><dt>{{ t('از', 'from') }}</dt><dd>{{ from(x) }}</dd><dt>{{ t('به', 'to') }}</dt><dd>{{ to(x) }}</dd></dl></td>
              <td class="num">{{ money(x.amount) }} <span v-if="x.currency !== 'IRR'">{{ x.currency }}</span></td>
              <td><span class="num" dir="ltr">{{ x.trackingNumber }}</span><span v-if="x.description" class="muted small">{{ x.description }}</span></td>
              <td class="tr-nowrap">
                <span v-if="x.state === 'Deleted'" class="tr-deleted">{{ t('حذف شده', 'Deleted') }}</span>
                <span v-else-if="x.payerVoucherConfirmed" class="tr-tag current">{{ t('سند تایید شد', 'Confirmed') }}</span>
                <span v-else class="tr-tag later">{{ t('سند تایید نشده', 'Not confirmed') }}</span>
              </td>
              <td class="tr-actions">
                <template v-if="x.state !== 'Deleted'">
                  <button type="button" class="tr-link" @click="voucherOf = voucherOf === x.id ? '' : x.id">{{ x.payerVoucherConfirmed ? t('سند', 'Voucher') : t('تایید سند', 'Confirm voucher') }}</button>
                  <button v-if="!x.whyNotCorrectable" type="button" class="tr-link" @click="startEdit(x)">{{ t('ویرایش', 'Edit') }}</button>
                  <button v-if="!x.whyNotDeletable" type="button" class="tr-link danger" :disabled="busy" @click="remove(x)">{{ t('حذف', 'Delete') }}</button>
                </template>
                <span v-else class="muted small">{{ x.deletedBy }}</span>
              </td>
            </tr>
            <tr v-if="!shown.length"><td colspan="8" class="muted">{{ t('نقل و انتقال الکترونیکی‌ای در این سال ثبت نشده است.', 'No electronic transfer this year.') }}</td></tr>
          </tbody>
        </table>
      </div>
      <TransferVoucher v-if="voucherOf" :key="voucherOf" :transfer-id="voucherOf" :step="0" :api-base="apiBase" :locale="locale" :token="token"
                       :account-name="L.accountName" @confirmed="load" @close="voucherOf = ''" />
    </section>
  </div>
</template>

<style scoped src="./receipts.css"></style>
<style scoped src="./transferForm.css"></style>
