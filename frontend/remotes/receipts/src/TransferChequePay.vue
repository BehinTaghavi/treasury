<script setup lang="ts">
/**
 * «پرداخت چک – نقل و انتقال (انتقال دهنده)» (owner's report no. 7 of 20 Nov 2026, «نحوه انتقال با چک صیادی»): when a document is
 * needed or the transfer is post-dated, the authorised signatories issue a cheque from the cheque book of the source account and
 * register it in Sayad — saving this form means «پرداخت ثبت صیاد شد». The sheet is taken from a cheque book of the source account
 * (the lowest unused sheet is offered); its Sayad ID is shown in four boxes as on the cheque books screen: the first four digits are
 * typed from the sheet, the other twelve come from the book. The payer's voucher (بدهکار وجوه در راه، بستانکار بانک انتقال دهنده) is
 * shown while the form is filled.
 * Under the form: the cheque transfers of the fiscal year with their status and, on each row, the button the owner asked for —
 * «ثبت در دریافت – نقل و انتقال (انتقال گیرنده)» — which records the same cheque on the receipts side («نزد صندوق»), where it goes on
 * like every received cheque (ثبت صیادی، واگذاری به بانک مقصد، وصول); «تایید سند» of the payer's voucher; and «حذف» while the cheque
 * is not on the receipts side and the voucher is not confirmed (the sheet is freed, so it can be typed again).
 * The form follows the receipt form the owner approved on 20 Nov 2026.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { amountTextInWords, createApi, formatAmount, markMissing, normaliseJalali, parseAmount, searchKey, todayJalali, toLatinDigits, translate, type Locale } from '@treasury/ui-kit'
import AmountInput from '@treasury/ui-kit/AmountInput.vue'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'
import VoucherTable, { type Voucher } from './VoucherTable.vue'
import TransferVoucher from './TransferVoucher.vue'
import { accountText, chequeTransferStatus, groupSayad, sayadParts, sheetNumber, sheetText, totalRial, type Transfer, type TransferChequeBook } from './transfers'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
/** `changed`: a cheque was recorded on the receipts side, so the screens of «دریافت‌ها» read their lists again. */
const emit = defineEmits<{ changed: [] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => (fa.value ? n.toLocaleString('fa-IR', { useGrouping: false }) : String(n))
const L = useLookups(api)
const year = computed(() => props.fiscalYear ?? Number(todayJalali().slice(0, 4)))

// ---- the form ----
const form = reactive({ date: todayJalali(), sourceId: '', bookId: '', sheet: '', sayadPrefix: '', amount: '', rate: '', dueDate: todayJalali(), destinationId: '', description: '' })
const source = computed(() => L.bankAccounts.value.find(a => a.id === form.sourceId))
const destination = computed(() => L.bankAccounts.value.find(a => a.id === form.destinationId))
const currency = computed(() => source.value?.currencyCode ?? 'IRR')
const unit = computed(() => (currency.value === 'IRR' ? 'ریال' : L.currencies.value.find(c => c.code === currency.value)?.name ?? currency.value))
/** A cheque is issued from an account that has cheque books (current Qard-al-hasan; report no. 4, item 1). */
const sourceChoices = computed<Choice[]>(() => L.bankAccounts.value.filter(a => a.isActive && a.hasCheques !== false && a.id !== form.destinationId)
  .map(a => ({ value: a.id, label: L.accountTitle(a), hint: a.currencyCode })))
const destinationChoices = computed<Choice[]>(() => L.bankAccounts.value
  .filter(a => a.isActive && a.id !== form.sourceId && (!source.value || a.currencyCode === source.value.currencyCode))
  .map(a => ({ value: a.id, label: L.accountTitle(a), hint: a.currencyCode })))

// ---- the cheque book and the sheet ----
const books = ref<TransferChequeBook[]>([])
const booksLoaded = ref(false)
const book = computed(() => books.value.find(b => b.id === form.bookId))
const bookChoices = computed<Choice[]>(() => books.value.map(b => ({
  value: b.id, label: `${b.serial}/${sheetText(b.firstSheet, b.serialDigits)} ${t('تا', 'to')} ${sheetText(b.lastSheet, b.serialDigits)}`,
  hint: t(`${num(b.remainingSheets)} برگ مانده`, `${b.remainingSheets} left`) })))
async function loadBooks(accountId: string) {
  const chosen = form.bookId
  if (!accountId) { books.value = []; booksLoaded.value = false; form.bookId = ''; return }
  const r = await api.get<TransferChequeBook[]>(`/api/cashflow/cheque-books?bankAccountId=${accountId}`)
  if (form.sourceId !== accountId) return
  // Active books of the account that still have an unused sheet.
  books.value = r.ok ? r.value.filter(b => b.isActive && b.nextSheet !== null) : []
  booksLoaded.value = true
  // The book in use stays chosen while it has sheets left; an account with one book needs no choice.
  form.bookId = books.value.some(b => b.id === chosen) ? chosen : books.value.length === 1 ? books.value[0]!.id : ''
}
watch(() => form.sourceId, id => {
  if (form.destinationId && !destinationChoices.value.some(c => c.value === form.destinationId)) form.destinationId = ''
  books.value = []
  booksLoaded.value = false
  form.bookId = ''
  void loadBooks(id)
})
/** The lowest unused sheet of the chosen book is offered, with the leading zeros of its serial; another unused sheet may be typed. */
watch(book, b => { form.sheet = b && b.nextSheet !== null ? sheetText(b.nextSheet, b.serialDigits) : ''; form.sayadPrefix = '' })
const sheet = computed(() => sheetNumber(form.sheet))
/** The twelve digits of the Sayad ID that come from the book: 8 fixed middle digits, then the sheet's Sayad serial. */
const sayad = computed(() => sayadParts(book.value, sheet.value))
const sayadPrefix = computed(() => toLatinDigits(form.sayadPrefix).replace(/\D/g, ''))
const sheetOutside = computed(() => !!book.value && sheet.value !== null && (sheet.value < book.value.firstSheet || sheet.value > book.value.lastSheet))

const amountRial = computed(() => {
  const a = parseAmount(form.amount)
  if (a === null) return 0
  if (currency.value === 'IRR') return a
  const rate = parseAmount(form.rate)
  return rate === null ? 0 : Math.round(a * rate)
})
const totalWords = computed(() => (amountRial.value > 0 ? amountTextInWords(String(amountRial.value), 'ریال') : ''))
/** A cheque dated the day it is written is a current cheque; with a later due date it is post-dated («مدت‌دار»). */
const timing = computed(() => {
  const date = normaliseJalali(form.date)
  const due = normaliseJalali(form.dueDate)
  return !date || !due ? null : due > date ? 'later' : due === date ? 'current' : 'before'
})

/** The request the form makes, or the code of the first thing missing (checked again by the server). */
function request(): { body: Record<string, unknown> } | { error: string } {
  const date = normaliseJalali(form.date)
  if (!date) return { error: 'FiscalDate.Invalid' }
  if (!form.sourceId) return { error: 'Transfer.SourceRequired' }
  if (!form.bookId) return { error: 'ChequeBook.NotChosen' }
  if (sheet.value === null) return { error: 'Instrument.NumberRequired' }
  if (sheetOutside.value) return { error: 'ChequeBook.SheetOutOfRange' }
  if (sayadPrefix.value.length !== 4) return { error: 'ChequeBook.SayadPrefixInvalid' }
  const amount = parseAmount(form.amount)
  if (amount === null || amount <= 0) return { error: 'Money.NotPositive' }
  const rate = currency.value === 'IRR' ? null : parseAmount(form.rate)
  if (currency.value !== 'IRR' && rate === null) return { error: 'Money.InvalidRate' }
  const due = normaliseJalali(form.dueDate)
  if (!form.dueDate.trim()) return { error: 'Instrument.DueDateRequired' }
  if (!due) return { error: 'FiscalDate.Invalid' }
  if (due < date) return { error: 'Instrument.DueBeforeIssue' }
  if (!form.destinationId) return { error: 'Transfer.DestinationRequired' }
  return { body: {
    date, sourceAccountId: form.sourceId, destinationAccountId: form.destinationId, chequeBookId: form.bookId, sheet: sheet.value, sayadPrefix: sayadPrefix.value,
    amount, dueDate: due, description: form.description.trim() || null, rateToRial: rate } }
}
/** «دسته چک را انتخاب کنید» has no code of the server's: the box is never sent empty. */
const say = (code: string) => (code === 'ChequeBook.NotChosen' ? t('دسته چک حساب مبدأ را انتخاب کنید.', 'Choose the cheque book of the source account.') : translate(code, props.locale))

// ---- the payer's voucher while the form is filled (nothing is saved, no sheet is taken) ----
const preview = ref<Voucher | null>(null)
const previewHint = ref('')
let previewTimer: ReturnType<typeof setTimeout> | undefined
let previewRun = 0
watch(form, () => {
  if (message.value?.kind === 'error' && !saving.value) message.value = null
  clearTimeout(previewTimer)
  previewTimer = setTimeout(refreshPreview, 400)
}, { deep: true })
async function refreshPreview() {
  const run = ++previewRun
  const req = request()
  if ('error' in req) { preview.value = null; previewHint.value = say(req.error); return }
  const r = await api.post<Voucher>('/api/cashflow/transfers/cheque/voucher-preview', req.body)
  if (run !== previewRun) return
  preview.value = r.ok ? r.value : null
  previewHint.value = r.ok ? '' : translate(r.code, props.locale)
}

// ---- save: the cheque is issued ----
const formEl = ref<HTMLFormElement | null>(null)
const saving = ref(false)
const message = ref<{ kind: 'error' | 'ok'; text: string } | null>(null)
const show = (kind: 'error' | 'ok', text: string) => { message.value = { kind, text } }
/** The transfer just saved, for the button «ثبت در دریافت» beside the message. */
const lastSaved = ref<Transfer | null>(null)
async function save() {
  message.value = null
  lastSaved.value = null
  const req = request()
  markMissing(formEl.value)
  if ('error' in req) return show('error', say(req.error))
  saving.value = true
  try {
    const r = await api.post<string>('/api/cashflow/transfers/cheque', req.body)
    if (!r.ok) return show('error', translate(r.code, props.locale))
    const saved = await api.get<Transfer>(`/api/cashflow/transfers/${r.value}`)
    const no = saved.ok ? num(saved.value.number) : ''
    show('ok', t(`نقل و انتقال شمارهٔ ${no} ثبت شد: چک ${saved.ok ? saved.value.cheque?.number ?? '' : ''} «پرداخت ثبت صیاد شد». اکنون آن را در دریافت (انتقال گیرنده) ثبت کنید.`,
      `Transfer no. ${no} saved: cheque ${saved.ok ? saved.value.cheque?.number ?? '' : ''} is issued and registered in Sayad. Now record it on the receipts side.`))
    lastSaved.value = saved.ok ? saved.value : null
    Object.assign(form, { amount: '', rate: '', sayadPrefix: '', description: '' })
    // The next unused sheet of the book is offered for the next cheque.
    await Promise.all([loadBooks(form.sourceId), load()])
  } finally {
    saving.value = false
  }
}
function onKey(e: KeyboardEvent) {
  if (e.key === 'Enter' && e.ctrlKey) { e.preventDefault(); void save() }
}

// ---- the list under the form ----
const transfers = ref<Transfer[]>([])
const listMessage = ref<{ kind: 'error' | 'ok'; text: string } | null>(null)
const search = ref('')
async function load() {
  const r = await api.get<Transfer[]>(`/api/cashflow/transfers?year=${year.value}&cheque=true`)
  if (r.ok) transfers.value = r.value
  else listMessage.value = { kind: 'error', text: translate(r.code, props.locale) }
}
const from = (x: Transfer) => accountText(x.sourceBankName, x.sourceAccountNumber)
const to = (x: Transfer) => accountText(x.destinationBankName, x.destinationAccountNumber)
const shown = computed(() => {
  const key = searchKey(search.value)
  return transfers.value.filter(x => !key || searchKey(`${x.number} ${x.date} ${x.cheque?.number ?? ''} ${x.cheque?.sayadId ?? ''} ${from(x)} ${to(x)} ${x.description ?? ''} ${chequeTransferStatus(x, props.locale)}`).includes(key))
})
const total = computed(() => totalRial(shown.value))

/**
 * The owner's button (report no. 7): «از طریق دگمه‌ای مشخصات آن در فرم دریافت نقل و انتقال همزمان ثبت می‌شود». The same cheque is
 * recorded on the receipts side, on the day it was issued; a later day can be given on the receipts screen instead.
 */
const busy = ref(false)
async function receive(x: Transfer) {
  if (busy.value) return
  busy.value = true
  listMessage.value = null
  const r = await api.post<string>(`/api/cashflow/transfers/${x.id}/receive-cheque`, { version: x.version, date: null })
  busy.value = false
  if (!r.ok) return void (listMessage.value = { kind: 'error', text: translate(r.code, props.locale) })
  if (lastSaved.value?.id === x.id) { lastSaved.value = null; message.value = null }
  listMessage.value = { kind: 'ok', text: t(
    `چک ${x.cheque?.number ?? ''} در بخش دریافت – نقل و انتقال (انتقال گیرنده) ثبت شد و اکنون «نزد صندوق» است. مراحل بعدی (ثبت صیادی، واگذاری به بانک مقصد، وصول) در «دریافت‌ها ← عملیات چک‌های دریافتی» انجام می‌شود؛ سند انتقال گیرنده در «دریافت‌ها ← دریافت چک – نقل و انتقال (انتقال گیرنده)» تایید می‌شود.`,
    `Cheque ${x.cheque?.number ?? ''} is recorded on the receipts side and is now in the cash desk. Its next steps (Sayad registration, deposit at the destination bank, collection) are done under Receipts → Received cheque steps; the receiver's voucher is confirmed under Receipts → Cheque receipt – transfer.`) }
  emit('changed')
  await load()
}

/** «حذف» while the cheque is not on the receipts side and the payer's voucher is not confirmed: the sheet is freed. */
async function remove(x: Transfer) {
  if (busy.value) return
  if (x.whyNotDeletable) return void (listMessage.value = { kind: 'error', text: translate(x.whyNotDeletable, props.locale) })
  if (!window.confirm(t(`نقل و انتقال شمارهٔ ${num(x.number)} (چک ${x.cheque?.number ?? ''} – ${money(x.amountRial)} ریال) حذف شود؟ با عبارت «حذف شده» در فهرست می‌ماند و برگ چک آن آزاد می‌شود تا دوباره قابل ثبت باشد.`,
    `Delete transfer no. ${x.number} (cheque ${x.cheque?.number ?? ''} – ${money(x.amountRial)} Rial)? It stays in the list marked as deleted, and its sheet is freed so it can be entered again.`))) return
  busy.value = true
  const r = await api.post<string>(`/api/cashflow/transfers/${x.id}/delete`, { version: x.version })
  busy.value = false
  if (!r.ok) return void (listMessage.value = { kind: 'error', text: translate(r.code, props.locale) })
  if (voucherOf.value === x.id) voucherOf.value = ''
  if (lastSaved.value?.id === x.id) { lastSaved.value = null; message.value = null }
  listMessage.value = { kind: 'ok', text: t(`نقل و انتقال شمارهٔ ${num(x.number)} حذف شد؛ برگ چک ${x.cheque?.number ?? ''} آزاد شد.`, `Transfer no. ${x.number} was deleted; sheet ${x.cheque?.number ?? ''} is free again.`) }
  await Promise.all([loadBooks(form.sourceId), load()])
}

/** The transfer whose payer's voucher is open under the list for «تایید سند». */
const voucherOf = ref('')

onMounted(async () => { await L.load(); await load() })
watch(year, () => void load())
</script>

<template>
  <div>
    <form ref="formEl" class="tr-form tr-rf" novalidate @keydown="onKey" @submit.prevent="save">
      <div class="tr-rf-main">
        <!-- 1. the source account and the cheque -->
        <section class="tr-card">
          <header class="tr-card-head">
            <span class="tr-step" aria-hidden="true">{{ num(1) }}</span>
            <h3>{{ t('چک حساب مبدأ (انتقال دهنده)', 'Cheque of the source account (the payer)') }}</h3>
            <span class="tr-req-note"><span class="tr-req-dot" aria-hidden="true" />{{ t('پر کردن الزامی است', 'must be filled in') }}</span>
          </header>
          <fieldset class="tr-fields">
            <label class="req">{{ t('تاریخ صدور', 'Issue date') }} <input v-model="form.date" placeholder="1405/08/29" dir="ltr" /></label>
            <label class="wide">{{ t('حساب مبدأ (انتقال دهنده)', 'Source account (the payer)') }}
              <SearchSelect v-model="form.sourceId" :options="sourceChoices" required :aria-label="t('حساب مبدأ (انتقال دهنده)', 'Source account')"
                            :none-text="t('حساب جاری فعالی (با دسته چک) تعریف نشده است', 'No active current account with cheque books')" />
            </label>
            <label class="wide">{{ t('دسته چک', 'Cheque book') }}
              <SearchSelect v-model="form.bookId" :options="bookChoices" required :disabled="!form.sourceId" :aria-label="t('دسته چک', 'Cheque book')"
                            :none-text="t('این حساب دسته چک فعالی با برگ استفاده‌نشده ندارد', 'This account has no active cheque book with an unused sheet')" />
            </label>
            <p v-if="form.sourceId && booksLoaded && !books.length" class="tr-msg warning wide3">
              {{ t('این حساب دسته چک فعالی با برگ استفاده‌نشده ندارد. دسته چک را در «اطلاعات پایه ← دسته چک‌ها» ثبت یا فعال کنید.',
                'This account has no active cheque book with an unused sheet. Enter or switch one on in Basic Information → Cheque books.') }}
            </p>
            <label class="req">{{ t('شماره برگ چک (سریال)', 'Sheet (serial)') }} <input v-model="form.sheet" dir="ltr" inputmode="numeric" :disabled="!book" /></label>
            <!-- The Sayad ID as on the cheque books screen (owner, 12 Nov 2026): the first four digits are typed from the sheet. -->
            <div class="req tr-sayad4">
              <span><span class="tr-req-dot" aria-hidden="true" />{{ t('شماره صیادی (۱۶ رقم)', 'Sayad ID (16 digits)') }}</span>
              <div class="tr-sayad4-boxes" dir="ltr">
                <input v-model="form.sayadPrefix" maxlength="4" inputmode="numeric" :disabled="!book" placeholder="____" :aria-label="t('۴ رقم اول شماره صیادی', 'First 4 digits of the Sayad ID')" />
                <input class="ro" :value="sayad?.[0] ?? ''" readonly tabindex="-1" :aria-label="t('رقم‌های ۵ تا ۸', 'Digits 5 to 8')" />
                <input class="ro" :value="sayad?.[1] ?? ''" readonly tabindex="-1" :aria-label="t('رقم‌های ۹ تا ۱۲', 'Digits 9 to 12')" />
                <input class="ro" :value="sayad?.[2] ?? ''" readonly tabindex="-1" :aria-label="t('سریال صیادی برگ', 'Sayad serial of the sheet')" />
              </div>
              <small class="muted">{{ book && !sayad && !sheetOutside
                ? t('این دسته چک شماره صیادی ندارد؛ آن را در «اطلاعات پایه ← دسته چک‌ها» کامل کنید.', 'This cheque book has no Sayad numbers; complete it in Basic Information → Cheque books.')
                : t('۴ رقم اول را از روی برگ چک وارد کنید؛ ۱۲ رقم دیگر از دسته چک می‌آید.', 'Type the first 4 digits from the sheet; the other 12 come from the cheque book.') }}</small>
            </div>
            <p v-if="sheetOutside" class="tr-msg warning wide3">{{ translate('ChequeBook.SheetOutOfRange', locale) }}</p>
          </fieldset>
        </section>

        <!-- 2. the amount, the due date and the destination account -->
        <section class="tr-card">
          <header class="tr-card-head">
            <span class="tr-step" aria-hidden="true">{{ num(2) }}</span>
            <h3>{{ t('مبلغ، سررسید و حساب مقصد (انتقال گیرنده)', 'Amount, due date and destination account (the receiver)') }}</h3>
            <span v-if="timing === 'current'" class="tr-tag current">{{ t('چک روز', 'Current') }}</span>
            <span v-else-if="timing === 'later'" class="tr-tag later">{{ t('مدت‌دار', 'Post-dated') }}</span>
          </header>
          <fieldset class="tr-fields">
            <label class="req wide">{{ t('مبلغ', 'Amount') }} ({{ currency }}) <AmountInput v-model="form.amount" :decimals="currency === 'IRR' ? 0 : 2" :unit="unit" /></label>
            <template v-if="currency !== 'IRR'">
              <label class="req">{{ t('نرخ ارز (ریال)', 'Rate (Rial)') }} <AmountInput v-model="form.rate" :decimals="4" unit="ریال" /></label>
              <label>{{ t('هم‌ارز ریالی', 'Rial equivalent') }} <input class="ro num" :value="amountRial ? money(amountRial) : ''" readonly /></label>
            </template>
            <label class="req">{{ t('سررسید', 'Due date') }} <input v-model="form.dueDate" placeholder="1405/09/30" dir="ltr" /></label>
            <label class="wide">{{ t('حساب مقصد (انتقال گیرنده)', 'Destination account (the receiver)') }}
              <SearchSelect v-model="form.destinationId" :options="destinationChoices" required :aria-label="t('حساب مقصد (انتقال گیرنده)', 'Destination account')"
                            :none-text="source ? t('حساب دیگری با همین ارز نیست', 'No other account in this currency') : t('حساب بانکی فعالی تعریف نشده است', 'No active bank account')" />
            </label>
            <label class="wide3">{{ t('شرح', 'Description') }} <input v-model="form.description" maxlength="500" :placeholder="t('اختیاری', 'Optional')" /></label>
            <p v-if="timing === 'before'" class="tr-msg warning wide3">{{ translate('Instrument.DueBeforeIssue', locale) }}</p>
            <p class="tr-limits">
              {{ t('با ثبت این فرم، چک از دسته چک حساب مبدأ صادر می‌شود و وضعیت آن «پرداخت ثبت صیاد شد» است. سپس با دگمهٔ «ثبت در دریافت – نقل و انتقال (انتقال گیرنده)» در فهرست زیر، همین چک در بخش دریافت ثبت می‌شود و مانند سایر چک‌های دریافتی ثبت صیادی، به بانک مقصد واگذار و وصول می‌شود.',
                'Saving this form issues the cheque from the cheque book of the source account; its status is "issued and registered in Sayad". Then the button "Record on the receipts side" in the list below records the same cheque as received, and it goes on like every received cheque: Sayad registration, deposit at the destination bank, collection.') }}
            </p>
          </fieldset>
        </section>

        <!-- 3. the payer's voucher (nothing is saved) -->
        <section class="tr-card">
          <header class="tr-card-head">
            <span class="tr-step" aria-hidden="true">{{ num(3) }}</span>
            <h3>{{ t('سند حسابداری انتقال دهنده', "The payer's voucher") }}</h3>
            <span class="tr-pill">{{ t('پیش‌نمایش', 'Preview') }}</span>
          </header>
          <VoucherTable v-if="preview" :voucher="preview" :locale="locale" :account-name="L.accountName" />
          <p v-else class="muted small">{{ t('سند انتقال دهنده (بدهکار: وجوه در راه، بستانکار: بانک انتقال دهنده) پس از تکمیل فرم اینجا نمایش داده می‌شود.',
            "The payer's voucher (debit: funds in transit, credit: the paying bank) is shown here once the form is filled in.") }}
            <template v-if="previewHint"> ({{ previewHint }})</template></p>
        </section>
      </div>

      <!-- The summary beside the form. -->
      <aside class="tr-rf-side" :aria-label="t('خلاصهٔ چک نقل و انتقال', 'Summary of the transfer cheque')">
        <div class="tr-card tr-summary">
          <h3>{{ t('خلاصهٔ چک نقل و انتقال', 'Summary') }}</h3>
          <dl>
            <div><dt>{{ t('از حساب (انتقال دهنده)', 'From') }}</dt><dd>{{ source ? L.accountTitle(source) : '—' }}</dd></div>
            <div><dt>{{ t('به حساب (انتقال گیرنده)', 'To') }}</dt><dd>{{ destination ? L.accountTitle(destination) : '—' }}</dd></div>
            <div><dt>{{ t('شماره چک', 'Cheque no.') }}</dt><dd class="num" dir="ltr">{{ book && form.sheet ? `${book.serial}/${form.sheet}` : '—' }}</dd></div>
            <div><dt>{{ t('شماره صیادی', 'Sayad ID') }}</dt><dd class="num" dir="ltr">{{ sayad && sayadPrefix.length === 4 ? groupSayad(sayadPrefix + sayad.join('')) : '—' }}</dd></div>
            <div><dt>{{ t('تاریخ صدور', 'Issued') }}</dt><dd class="num" dir="ltr">{{ form.date || '—' }}</dd></div>
            <div><dt>{{ t('سررسید', 'Due') }}</dt><dd class="num" dir="ltr">{{ form.dueDate || '—' }}</dd></div>
          </dl>
          <div class="tr-total">
            <span>{{ t('مبلغ (ریال)', 'Amount (Rial)') }}</span>
            <strong class="num">{{ amountRial ? money(amountRial) : '—' }}</strong>
            <small v-if="totalWords && fa">{{ totalWords }}</small>
          </div>
          <button type="submit" class="tr-save" :disabled="saving">{{ t('ثبت پرداخت چک (ثبت صیاد شد)', 'Save the cheque (registered in Sayad)') }}</button>
          <p class="tr-kbd">{{ t('میان‌بر صفحه‌کلید:', 'Keyboard shortcut:') }} <kbd dir="ltr">Ctrl + Enter</kbd></p>
        </div>
        <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}
          <!-- Owner's report no. 7: the button that records the same cheque on the receipts side. -->
          <button v-if="lastSaved && message.kind === 'ok'" type="button" class="tr-receive-now" :disabled="busy" @click="receive(lastSaved)">
            {{ t('ثبت در دریافت – نقل و انتقال (انتقال گیرنده)', 'Record on the receipts side (the receiver)') }}</button>
        </p>
      </aside>
    </form>

    <!-- The cheque transfers of the fiscal year, newest first. -->
    <section class="tr-card tr-list">
      <div class="tr-list-head">
        <h3>{{ t(`چک‌های نقل و انتقال (انتقال دهنده) – سال مالی ${num(year)}`, `Transfer cheques (payment side) – fiscal year ${year}`) }}</h3>
        <label style="flex: 1; min-width: 14rem">{{ t('جستجو (شماره، چک، حساب، وضعیت)', 'Search (number, cheque, account, status)') }} <input v-model="search" /></label>
        <span class="tr-list-total">{{ t('جمع (بدون حذف‌شده‌ها):', 'Total (without deleted):') }} <strong class="num">{{ money(total) }}</strong> {{ t('ریال', 'Rial') }}</span>
      </div>
      <p v-if="listMessage" :class="['tr-msg', listMessage.kind]" role="status">{{ listMessage.text }}</p>
      <div class="tr-scroll">
        <table class="tr-table">
          <thead>
            <tr>
              <th>{{ t('شماره', 'No.') }}</th><th>{{ t('تاریخ صدور', 'Issued') }}</th><th>{{ t('شماره چک / صیادی', 'Cheque no. / Sayad ID') }}</th>
              <th>{{ t('حساب‌ها: انتقال دهنده ← انتقال گیرنده', 'Accounts: payer → receiver') }}</th><th>{{ t('مبلغ', 'Amount') }}</th>
              <th>{{ t('سررسید', 'Due') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th>{{ t('عملیات', 'Actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="x in shown" :key="x.id" :class="{ deleted: x.state === 'Deleted', selected: voucherOf === x.id }">
              <td class="num">{{ num(x.number) }}</td>
              <td class="num">{{ x.date }}</td>
              <td><span class="num" dir="ltr">{{ x.cheque?.number }}</span><span class="muted small num" dir="ltr">{{ groupSayad(x.cheque?.sayadId) }}</span></td>
              <td><dl class="tr-accounts"><dt>{{ t('از', 'from') }}</dt><dd>{{ from(x) }}</dd><dt>{{ t('به', 'to') }}</dt><dd>{{ to(x) }}</dd></dl><span v-if="x.description" class="muted small">{{ x.description }}</span></td>
              <td class="num">{{ money(x.amount) }} <span v-if="x.currency !== 'IRR'">{{ x.currency }}</span></td>
              <td class="num">{{ x.cheque?.dueDate }}</td>
              <td class="tr-status">
                <span v-if="x.state === 'Deleted'" class="tr-deleted">{{ t('حذف شده', 'Deleted') }}</span>
                <template v-else>
                  {{ chequeTransferStatus(x, locale) }}
                  <span v-if="x.payerVoucherConfirmed" class="tr-tag current">{{ t('سند تایید شد', 'Voucher confirmed') }}</span>
                </template>
              </td>
              <td class="tr-actions">
                <template v-if="x.state !== 'Deleted'">
                  <!-- Owner's report no. 7: the button that records the same cheque in «دریافت چک – نقل و انتقال (انتقال گیرنده)». -->
                  <button v-if="x.state === 'ChequeIssued'" type="button" :disabled="busy" :title="t('ثبت همین چک در «دریافت چک – نقل و انتقال (انتقال گیرنده)»', 'Record this cheque under Cheque receipt – transfer (the receiver)')"
                          @click="receive(x)">{{ t('ثبت در دریافت (انتقال گیرنده)', 'Record on the receipts side') }}</button>
                  <button type="button" class="tr-link" @click="voucherOf = voucherOf === x.id ? '' : x.id">{{ x.payerVoucherConfirmed ? t('سند', 'Voucher') : t('تایید سند', 'Confirm voucher') }}</button>
                  <button v-if="!x.whyNotDeletable" type="button" class="tr-link danger" :disabled="busy" @click="remove(x)">{{ t('حذف', 'Delete') }}</button>
                </template>
                <span v-else class="muted small">{{ x.deletedBy }}</span>
              </td>
            </tr>
            <tr v-if="!shown.length"><td colspan="8" class="muted">{{ t('چک نقل و انتقالی در این سال ثبت نشده است.', 'No transfer cheque this year.') }}</td></tr>
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
<style scoped>
.tr-receive-now { display: block; margin-top: .5rem; }
</style>
