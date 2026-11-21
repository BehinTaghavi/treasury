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
 * Owner, 21 Nov 2026 (answers to Q76–Q82):
 * – every transfer pays an approved payment request of the kind «نقل و انتقال بین بانک‌های شرکت»; it is chosen first and sets the two
 *   accounts (Q82);
 * – the payer's Sayad registration is a step of its own: the tick «چک در سامانهٔ صیاد ثبت شد» is on by default («پرداخت ثبت صیاد شد»
 *   as the cheque is issued); unticked, the cheque is only «صادر شد», «ثبت صیادی پرداخت» is recorded later from the list, and until
 *   then the cheque can be cancelled with «ابطال چک» — its sheet becomes void, and a new transfer is a new, independent cheque (Q79);
 * – a post-dated cheque is credited to «اسناد پرداختنی» until «ثبت سررسید» is recorded on or after its due date, which makes the
 *   voucher بدهکار اسناد پرداختنی / بستانکار بانک (Q81).
 * The voucher panel under the list shows every voucher of the payment side with its own title and «تایید سند».
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { amountTextInWords, createApi, formatAmount, markMissing, normaliseJalali, parseAmount, searchKey, todayJalali, toLatinDigits, translate, type Locale } from '@treasury/ui-kit'
import AmountInput from '@treasury/ui-kit/AmountInput.vue'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'
import VoucherTable, { type Voucher } from './VoucherTable.vue'
import TransferVoucher from './TransferVoucher.vue'
import { amountText } from './savedReceipt'
import { accountText, chequeTransferStatus, groupSayad, isConfirmed, isOpenCheque, payableRequests, payerSteps, requestLabel, sayadParts, sheetNumber, sheetText, totalRial,
  type Transfer, type TransferChequeBook, type TransferRequest } from './transfers'

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
const form = reactive({
  date: todayJalali(), requestId: '', sourceId: '', bookId: '', sheet: '', sayadPrefix: '', sayadRegistered: true, amount: '', rate: '', dueDate: todayJalali(), destinationId: '',
  description: '' })

// ---- the approved payment request the transfer pays (answer to Q82) ----
const requests = ref<TransferRequest[]>([])
const requestsLoaded = ref(false)
async function loadRequests() {
  const r = await api.get<TransferRequest[]>(`/api/requests/payment-requests?year=${year.value}&state=Approved`)
  requests.value = r.ok ? payableRequests(r.value) : []
  requestsLoaded.value = true
  if (form.requestId && !requests.value.some(x => x.id === form.requestId)) form.requestId = ''
}
const accountOf = (id: string | null) => { const a = L.bankAccounts.value.find(x => x.id === id); return a ? L.accountTitle(a) : '—' }
const chosenRequest = computed(() => requests.value.find(r => r.id === form.requestId))
const requestChoices = computed<Choice[]>(() => requests.value.map(r => ({
  value: r.id, label: `${t('درخواست', 'Request')} ${num(r.number)} – ${requestLabel(r, accountOf(r.payFromId), accountOf(r.transferToAccountId), props.locale)}` })))
/** The request names the two accounts: they are set from it and locked; what is left on it is proposed as the amount. */
watch(chosenRequest, (r, before) => {
  // Without a request there are no accounts: they only ever come from it (a request paid in full leaves the list).
  if (!r) { if (before) { form.sourceId = ''; form.destinationId = '' } return }
  form.sourceId = r.payFromId ?? ''
  form.destinationId = r.transferToAccountId ?? ''
  if (!form.amount || (before && before.id !== r.id)) form.amount = amountText(r.remainingAmount)
})
/** The source account of the chosen request has no cheque book kind: such a request is paid by an electronic transfer. */
const sourceWithoutCheques = computed(() => !!chosenRequest.value && !!source.value && source.value.hasCheques === false)
const source = computed(() => L.bankAccounts.value.find(a => a.id === form.sourceId))
const destination = computed(() => L.bankAccounts.value.find(a => a.id === form.destinationId))
const currency = computed(() => source.value?.currencyCode ?? 'IRR')
const unit = computed(() => (currency.value === 'IRR' ? 'ریال' : L.currencies.value.find(c => c.code === currency.value)?.name ?? currency.value))
/** A cheque is issued from an account that has cheque books (current Qard-al-hasan; report no. 4, item 1). */
const sourceChoices = computed<Choice[]>(() => L.bankAccounts.value.filter(a => (a.isActive && a.hasCheques !== false && a.id !== form.destinationId) || a.id === form.sourceId)
  .map(a => ({ value: a.id, label: L.accountTitle(a), hint: a.currencyCode })))
const destinationChoices = computed<Choice[]>(() => L.bankAccounts.value
  .filter(a => (a.isActive && a.id !== form.sourceId && (!source.value || a.currencyCode === source.value.currencyCode)) || a.id === form.destinationId)
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
  if (!form.requestId) return { error: 'Transfer.RequestRequired' }
  if (!form.sourceId) return { error: 'Transfer.SourceRequired' }
  if (sourceWithoutCheques.value) return { error: 'Transfer.SourceHasNoCheques' }
  if (!form.bookId) return { error: 'ChequeBook.NotChosen' }
  if (sheet.value === null) return { error: 'Instrument.NumberRequired' }
  if (sheetOutside.value) return { error: 'ChequeBook.SheetOutOfRange' }
  if (sayadPrefix.value.length !== 4) return { error: 'ChequeBook.SayadPrefixInvalid' }
  const amount = parseAmount(form.amount)
  if (amount === null || amount <= 0) return { error: 'Money.NotPositive' }
  if (chosenRequest.value && amount > chosenRequest.value.remainingAmount) return { error: 'Transfer.RequestOverpaid' }
  const rate = currency.value === 'IRR' ? null : parseAmount(form.rate)
  if (currency.value !== 'IRR' && rate === null) return { error: 'Money.InvalidRate' }
  const due = normaliseJalali(form.dueDate)
  if (!form.dueDate.trim()) return { error: 'Instrument.DueDateRequired' }
  if (!due) return { error: 'FiscalDate.Invalid' }
  if (due < date) return { error: 'Instrument.DueBeforeIssue' }
  if (!form.destinationId) return { error: 'Transfer.DestinationRequired' }
  return { body: {
    date, sourceAccountId: form.sourceId, destinationAccountId: form.destinationId, chequeBookId: form.bookId, sheet: sheet.value, sayadPrefix: sayadPrefix.value,
    amount, dueDate: due, description: form.description.trim() || null, rateToRial: rate, paymentRequestId: form.requestId, sayadRegistered: form.sayadRegistered } }
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
    const chequeNo = saved.ok ? saved.value.cheque?.number ?? '' : ''
    show('ok', saved.ok && !saved.value.cheque?.payerSayadOn
      ? t(`نقل و انتقال شمارهٔ ${no} ثبت شد: چک ${chequeNo} صادر شد و هنوز در صیاد ثبت نشده است. پس از ثبت در سامانهٔ صیاد، «ثبت صیادی پرداخت» را در فهرست زیر بزنید؛ تا آن زمان چک قابل «ابطال» است.`,
        `Transfer no. ${no} saved: cheque ${chequeNo} is issued and not yet registered in Sayad. Once it is, press "Payer's Sayad registration" in the list below; until then the cheque can be cancelled.`)
      : t(`نقل و انتقال شمارهٔ ${no} ثبت شد: چک ${chequeNo} «پرداخت ثبت صیاد شد». اکنون آن را در دریافت (انتقال گیرنده) ثبت کنید.`,
        `Transfer no. ${no} saved: cheque ${chequeNo} is issued and registered in Sayad. Now record it on the receipts side.`))
    lastSaved.value = saved.ok ? saved.value : null
    // The Sayad tick goes back to its usual answer: «not registered yet» is said again for each cheque, never carried over.
    Object.assign(form, { amount: '', rate: '', sayadPrefix: '', description: '', sayadRegistered: true })
    listMessage.value = null
    // The next unused sheet of the book is offered for the next cheque; what is left on the request is proposed again.
    await Promise.all([loadBooks(form.sourceId), load(), loadRequests()])
    if (chosenRequest.value) form.amount = amountText(chosenRequest.value.remainingAmount)
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
  await Promise.all([loadBooks(form.sourceId), load(), loadRequests()])
}

/**
 * The dated steps of the cheque on the payment side (owner's answers of 21 Nov 2026): «ثبت صیادی پرداخت» (Q79), «ثبت سررسید» of a
 * post-dated cheque (Q81) and «ابطال چک» with its reason, only while the cheque is not registered in Sayad (Q79). Each asks its
 * date (today unless another day is typed) in a small panel under the list.
 */
type StepKind = 'sayad' | 'mature' | 'cancel'
const step = reactive<{ kind: StepKind | ''; id: string; date: string; reason: string }>({ kind: '', id: '', date: '', reason: '' })
const stepOf = computed(() => transfers.value.find(x => x.id === step.id))
function openStep(kind: StepKind, x: Transfer) {
  listMessage.value = null
  voucherOf.value = ''
  Object.assign(step, { kind, id: x.id, date: todayJalali(), reason: '' })
}
function closeStep() { step.kind = ''; step.id = '' }
const stepTitle = computed(() => {
  const no = stepOf.value?.cheque?.number ?? ''
  return step.kind === 'sayad' ? t(`ثبت صیادی پرداخت – چک ${no}`, `Payer's Sayad registration – cheque ${no}`)
    : step.kind === 'mature' ? t(`ثبت سررسید – چک ${no}`, `Maturity – cheque ${no}`)
    : t(`ابطال چک ${no}`, `Cancel cheque ${no}`)
})
async function recordStep() {
  const x = stepOf.value
  if (!x || busy.value || !step.kind) return
  const date = normaliseJalali(step.date)
  if (!date) return void (listMessage.value = { kind: 'error', text: translate('FiscalDate.Invalid', props.locale) })
  if (step.kind === 'cancel' && !step.reason.trim()) return void (listMessage.value = { kind: 'error', text: translate('Transfer.CancelReasonRequired', props.locale) })
  const kind = step.kind
  const path = kind === 'sayad' ? 'register-sayad' : kind === 'mature' ? 'mature' : 'cancel-cheque'
  busy.value = true
  listMessage.value = null
  const r = await api.post<string>(`/api/cashflow/transfers/${x.id}/${path}`, kind === 'cancel' ? { version: x.version, date, reason: step.reason.trim() } : { version: x.version, date })
  busy.value = false
  if (!r.ok) return void (listMessage.value = { kind: 'error', text: translate(r.code, props.locale) })
  const no = x.cheque?.number ?? ''
  listMessage.value = { kind: 'ok', text: kind === 'sayad'
    ? t(`چک ${no} «پرداخت ثبت صیاد شد». از این پس ابطال نمی‌شود و «ثبت صیادی» آن در بخش دریافت قابل ثبت است.`,
      `Cheque ${no} is registered in Sayad by the payer. It can no longer be cancelled, and its Sayad registration on the receipts side can be recorded.`)
    : kind === 'mature'
      ? t(`سررسید چک ${no} ثبت شد: چک از «اسناد پرداختنی» به حساب بانک انتقال دهنده رفت. «سند سررسید چک پرداختنی» را تایید کنید.`,
        `The maturity of cheque ${no} is recorded: it left notes payable for the source bank. Confirm its maturity voucher.`)
      : t(`چک ${no} ابطال شد: برگ آن باطل است و دیگر استفاده نمی‌شود، و مبلغ آن به ماندهٔ درخواست وجه برگشت. برای نقل و انتقال دوباره، چک تازه‌ای صادر کنید. سندهای ابطال را تایید کنید.`,
        `Cheque ${no} is cancelled: its sheet is void and never used again, and its amount is to be paid on the request again. A new transfer needs a new cheque. Confirm the cancellation vouchers.`) }
  closeStep()
  if (lastSaved.value?.id === x.id) { lastSaved.value = null; message.value = null }
  if (kind !== 'sayad') { voucherOf.value = x.id; voucherRun.value++ }
  emit('changed')
  await Promise.all([loadBooks(form.sourceId), load(), loadRequests()])
}

/** The transfer whose vouchers of the payment side are open under the list for «تایید سند». */
const voucherOf = ref('')
/** Counts the steps recorded while a voucher panel is open, so the panel reads the new voucher. */
const voucherRun = ref(0)
/** Every voucher the payment side has of this transfer is confirmed (the payer's, the maturity, the payer's side of a cancellation). */
const allConfirmed = (x: Transfer) => isConfirmed(x, 0) && (!x.cheque?.maturedOn || isConfirmed(x, 2)) && (x.state !== 'Cancelled' || isConfirmed(x, 3))

onMounted(async () => { await L.load(); await Promise.all([load(), loadRequests()]) })
watch(year, () => { void load(); void loadRequests() })
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
            <!-- Owner, 21 Nov 2026 (answer to Q82): every transfer pays an approved payment request; it names the two accounts. -->
            <label class="wide3">{{ t('درخواست وجه تأییدشده', 'Approved payment request') }}
              <SearchSelect v-model="form.requestId" :options="requestChoices" required :aria-label="t('درخواست وجه تأییدشده', 'Approved payment request')"
                            :none-text="t('درخواست وجه تأییدشده‌ای برای نقل و انتقال نیست', 'No approved request for a transfer')" />
            </label>
            <p v-if="requestsLoaded && !requests.length" class="tr-msg warning wide3">
              {{ t('برای هر نقل و انتقال ابتدا باید در «درخواست وجه» یک درخواست با نوع پرداخت «نقل و انتقال بین بانک‌های شرکت» ثبت و تایید شود؛ سپس همان درخواست در اینجا انتخاب می‌شود.',
                "Every transfer first needs a payment request of the kind \"transfer between the company's accounts\", written and approved under Payment requests; that request is then chosen here.") }}
            </p>
            <p v-if="sourceWithoutCheques" class="tr-msg warning wide3">{{ translate('Transfer.SourceHasNoCheques', locale) }}</p>
            <label class="req">{{ t('تاریخ صدور', 'Issue date') }} <input v-model="form.date" placeholder="1405/08/29" dir="ltr" /></label>
            <label class="wide">{{ t('حساب مبدأ (انتقال دهنده)', 'Source account (the payer)') }}
              <SearchSelect v-model="form.sourceId" :options="sourceChoices" required disabled :aria-label="t('حساب مبدأ (انتقال دهنده)', 'Source account')"
                            :empty-label="t('— از درخواست وجه —', '— from the request —')" />
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
            <!-- Owner, 21 Nov 2026 (answer to Q79): the payer's Sayad registration; unticked, it is recorded later and the cheque can still be cancelled. -->
            <label class="tr-check wide3"><input v-model="form.sayadRegistered" type="checkbox" /> {{ t('چک در سامانهٔ صیاد ثبت شد (پرداخت ثبت صیاد شد)', 'The cheque is registered in Sayad (by the payer)') }}</label>
            <p class="muted small wide3">{{ form.sayadRegistered
              ? t('چک با ثبت این فرم «پرداخت ثبت صیاد شد» است و دیگر ابطال نمی‌شود.', 'Saving this form records the cheque as registered in Sayad; it is then no longer cancelled.')
              : t('چک فقط «صادر شد» ثبت می‌شود. تا زمانی که «ثبت صیادی پرداخت» آن در فهرست زیر ثبت نشده است، چک قابل «ابطال» است و «ثبت صیادی» آن در بخش دریافت هم ممکن نیست.',
                'The cheque is recorded as issued only. Until its Sayad registration is recorded in the list below it can still be cancelled, and its Sayad registration on the receipts side is not possible either.') }}</p>
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
            <label class="req wide">{{ t('مبلغ', 'Amount') }} ({{ currency }}) <AmountInput v-model="form.amount" :decimals="currency === 'IRR' ? 0 : 2" :unit="unit" />
              <small v-if="chosenRequest" class="muted">{{ t(`ماندهٔ درخواست وجه: ${money(chosenRequest.remainingAmount)} – مبلغ می‌تواند کمتر باشد، ولی بیشتر نه.`,
                `Left on the request: ${money(chosenRequest.remainingAmount)} – the amount may be less, never more.`) }}</small>
            </label>
            <template v-if="currency !== 'IRR'">
              <label class="req">{{ t('نرخ ارز (ریال)', 'Rate (Rial)') }} <AmountInput v-model="form.rate" :decimals="4" unit="ریال" /></label>
              <label>{{ t('هم‌ارز ریالی', 'Rial equivalent') }} <input class="ro num" :value="amountRial ? money(amountRial) : ''" readonly /></label>
            </template>
            <label class="req">{{ t('سررسید', 'Due date') }} <input v-model="form.dueDate" placeholder="1405/09/30" dir="ltr" />
              <!-- Owner, 21 Nov 2026 (answer to Q81): «حساب اسناد پرداختنی بستانکار تا سررسید چک». -->
              <small v-if="timing === 'later'" class="muted">{{ t('چک مدت‌دار تا ثبت «سررسید» در «اسناد پرداختنی» می‌ماند.', 'A post-dated cheque stays in notes payable until its maturity is recorded.') }}</small>
            </label>
            <label class="wide">{{ t('حساب مقصد (انتقال گیرنده)', 'Destination account (the receiver)') }}
              <SearchSelect v-model="form.destinationId" :options="destinationChoices" required disabled :aria-label="t('حساب مقصد (انتقال گیرنده)', 'Destination account')"
                            :empty-label="t('— از درخواست وجه —', '— from the request —')" />
            </label>
            <label class="wide3">{{ t('شرح', 'Description') }} <input v-model="form.description" maxlength="500" :placeholder="t('اختیاری', 'Optional')" /></label>
            <p v-if="timing === 'before'" class="tr-msg warning wide3">{{ translate('Instrument.DueBeforeIssue', locale) }}</p>
            <p class="tr-limits">
              {{ t('با ثبت این فرم، چک از دسته چک حساب مبدأ صادر می‌شود و، اگر تیک «چک در سامانهٔ صیاد ثبت شد» خورده باشد، وضعیت آن «پرداخت ثبت صیاد شد» است. سپس با دگمهٔ «ثبت در دریافت – نقل و انتقال (انتقال گیرنده)» در فهرست زیر، همین چک در بخش دریافت ثبت می‌شود و مانند سایر چک‌های دریافتی ثبت صیادی، به بانک مقصد واگذار و وصول می‌شود.',
                'Saving this form issues the cheque from the cheque book of the source account; with the tick "registered in Sayad" its status is "issued and registered in Sayad". Then the button "Record on the receipts side" in the list below records the same cheque as received, and it goes on like every received cheque: Sayad registration, deposit at the destination bank, collection.') }}
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
          <p v-else class="muted small">{{ t('سند انتقال دهنده (بدهکار: وجوه در راه، بستانکار: بانک انتقال دهنده؛ برای چک مدت‌دار بستانکار: اسناد پرداختنی) پس از تکمیل فرم اینجا نمایش داده می‌شود.',
            "The payer's voucher (debit: funds in transit, credit: the paying bank; for a post-dated cheque credit: notes payable) is shown here once the form is filled in.") }}
            <template v-if="previewHint"> ({{ previewHint }})</template></p>
        </section>
      </div>

      <!-- The summary beside the form. -->
      <aside class="tr-rf-side" :aria-label="t('خلاصهٔ چک نقل و انتقال', 'Summary of the transfer cheque')">
        <div class="tr-card tr-summary">
          <h3>{{ t('خلاصهٔ چک نقل و انتقال', 'Summary') }}</h3>
          <dl>
            <div><dt>{{ t('درخواست وجه', 'Payment request') }}</dt><dd class="num">{{ chosenRequest ? num(chosenRequest.number) : '—' }}</dd></div>
            <div><dt>{{ t('از حساب (انتقال دهنده)', 'From') }}</dt><dd>{{ source ? L.accountTitle(source) : '—' }}</dd></div>
            <div><dt>{{ t('به حساب (انتقال گیرنده)', 'To') }}</dt><dd>{{ destination ? L.accountTitle(destination) : '—' }}</dd></div>
            <div><dt>{{ t('شماره چک', 'Cheque no.') }}</dt><dd class="num" dir="ltr">{{ book && form.sheet ? `${book.serial}/${form.sheet}` : '—' }}</dd></div>
            <div><dt>{{ t('شماره صیادی', 'Sayad ID') }}</dt><dd class="num" dir="ltr">{{ sayad && sayadPrefix.length === 4 ? groupSayad(sayadPrefix + sayad.join('')) : '—' }}</dd></div>
            <div><dt>{{ t('تاریخ صدور', 'Issued') }}</dt><dd class="num" dir="ltr">{{ form.date || '—' }}</dd></div>
            <div><dt>{{ t('سررسید', 'Due') }}</dt><dd class="num" dir="ltr">{{ form.dueDate || '—' }}</dd></div>
            <div><dt>{{ t('ثبت صیادی پرداخت', "Payer's Sayad registration") }}</dt><dd>{{ form.sayadRegistered ? t('انجام شد', 'done') : t('هنوز نه', 'not yet') }}</dd></div>
          </dl>
          <div class="tr-total">
            <span>{{ t('مبلغ (ریال)', 'Amount (Rial)') }}</span>
            <strong class="num">{{ amountRial ? money(amountRial) : '—' }}</strong>
            <small v-if="totalWords && fa">{{ totalWords }}</small>
          </div>
          <button type="submit" class="tr-save" :disabled="saving">{{ form.sayadRegistered ? t('ثبت پرداخت چک (ثبت صیاد شد)', 'Save the cheque (registered in Sayad)') : t('ثبت پرداخت چک (صدور)', 'Save the cheque (issued)') }}</button>
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
            <tr v-for="x in shown" :key="x.id" :class="{ deleted: x.state === 'Deleted' || x.state === 'Cancelled', selected: voucherOf === x.id || step.id === x.id }">
              <td class="num">{{ num(x.number) }}</td>
              <td class="num">{{ x.date }}</td>
              <td><span class="num" dir="ltr">{{ x.cheque?.number }}</span><span class="muted small num" dir="ltr">{{ groupSayad(x.cheque?.sayadId) }}</span></td>
              <td><dl class="tr-accounts"><dt>{{ t('از', 'from') }}</dt><dd>{{ from(x) }}</dd><dt>{{ t('به', 'to') }}</dt><dd>{{ to(x) }}</dd></dl><span v-if="x.paymentRequestNumber" class="muted small">{{ t('درخواست وجه', 'payment request') }} {{ num(x.paymentRequestNumber) }}</span><span v-if="x.description" class="muted small">{{ x.description }}</span></td>
              <td class="num">{{ money(x.amount) }} <span v-if="x.currency !== 'IRR'">{{ x.currency }}</span></td>
              <td class="num">{{ x.cheque?.dueDate }}</td>
              <td class="tr-status">
                <span v-if="x.state === 'Deleted'" class="tr-deleted">{{ t('حذف شده', 'Deleted') }}</span>
                <template v-else-if="x.state === 'Cancelled'">
                  <span class="tr-deleted">{{ t('ابطال شده', 'Cancelled') }}</span>
                  <span class="muted small">{{ x.cheque?.cancelledOn }} – {{ x.cheque?.cancelReason }}</span>
                </template>
                <template v-else>
                  {{ chequeTransferStatus(x, locale) }}
                  <span v-if="x.cheque?.maturedOn" class="tr-tag current" :title="x.cheque.maturedOn">{{ t('سررسید ثبت شد', 'Matured') }}</span>
                  <span v-else-if="x.cheque?.canMature" class="tr-tag due">{{ t('سررسید رسیده – ثبت نشده', 'Due – maturity not recorded') }}</span>
                  <span v-if="allConfirmed(x)" class="tr-tag current">{{ t('سند تایید شد', 'Voucher confirmed') }}</span>
                </template>
              </td>
              <td class="tr-actions">
                <template v-if="x.state !== 'Deleted'">
                  <!-- Owner's report no. 7: the button that records the same cheque in «دریافت چک – نقل و انتقال (انتقال گیرنده)». -->
                  <button v-if="x.state === 'ChequeIssued'" type="button" :disabled="busy" :title="t('ثبت همین چک در «دریافت چک – نقل و انتقال (انتقال گیرنده)»', 'Record this cheque under Cheque receipt – transfer (the receiver)')"
                          @click="receive(x)">{{ t('ثبت در دریافت (انتقال گیرنده)', 'Record on the receipts side') }}</button>
                  <!-- Owner's answers of 21 Nov 2026: the payer's Sayad registration (Q79), the maturity of a post-dated cheque (Q81). -->
                  <button v-if="isOpenCheque(x) && !x.cheque?.payerSayadOn" type="button" class="tr-link" :disabled="busy" @click="openStep('sayad', x)">{{ t('ثبت صیادی پرداخت', "Payer's Sayad registration") }}</button>
                  <button v-if="x.cheque?.canMature" type="button" class="tr-link" :disabled="busy" @click="openStep('mature', x)">{{ t('ثبت سررسید', 'Record maturity') }}</button>
                  <button type="button" class="tr-link" @click="closeStep(); voucherOf = voucherOf === x.id ? '' : x.id">{{ allConfirmed(x) ? t('سند', 'Voucher') : t('تایید سند', 'Confirm voucher') }}</button>
                  <button v-if="x.cheque && !x.cheque.whyNotCancellable" type="button" class="tr-link danger" :disabled="busy" @click="openStep('cancel', x)">{{ t('ابطال چک', 'Cancel the cheque') }}</button>
                  <button v-if="!x.whyNotDeletable" type="button" class="tr-link danger" :disabled="busy" @click="remove(x)">{{ t('حذف', 'Delete') }}</button>
                </template>
                <span v-else class="muted small">{{ x.deletedBy }}</span>
              </td>
            </tr>
            <tr v-if="!shown.length"><td colspan="8" class="muted">{{ t('چک نقل و انتقالی در این سال ثبت نشده است.', 'No transfer cheque this year.') }}</td></tr>
          </tbody>
        </table>
      </div>
      <!-- The dated step chosen on a row: its date, and for «ابطال چک» its reason. -->
      <section v-if="step.kind && stepOf" class="tr-card tr-step-panel">
        <h4>{{ stepTitle }}</h4>
        <p v-if="step.kind === 'cancel'" class="tr-msg warning">
          {{ t('با ابطال، برگ این چک باطل می‌شود و دیگر هرگز استفاده نمی‌شود؛ چک در بخش دریافت (اگر ثبت شده باشد) هم ابطال می‌شود و مبلغ به ماندهٔ درخواست وجه برمی‌گردد. برای نقل و انتقال دوباره باید چک تازه‌ای صادر شود. اگر فقط در ورود اطلاعات اشتباه شده و چک واقعی صادر نشده است، به‌جای ابطال «حذف» را بزنید.',
            'Cancelling makes the sheet of this cheque void — it is never used again; the cheque on the receipts side (if it is there) is cancelled too, and the amount is to be paid on the request again. A new transfer needs a new cheque. If only the entry was wrong and no real cheque was written, use "Delete" instead.') }}
        </p>
        <p v-else-if="step.kind === 'mature'" class="muted small">
          {{ t(`سررسید این چک ${stepOf.cheque?.dueDate ?? ''} است؛ تاریخ ثبت سررسید نمی‌تواند پیش از آن باشد. با ثبت، سند «بدهکار: اسناد پرداختنی، بستانکار: بانک انتقال دهنده» ساخته می‌شود.`,
            `This cheque is due ${stepOf.cheque?.dueDate ?? ''}; the maturity cannot be dated before it. Recording it makes the voucher "debit: notes payable, credit: the source bank".`) }}
        </p>
        <p v-else class="muted small">
          {{ t('پس از ثبت چک در سامانهٔ صیاد توسط دارندگان امضای مجاز، تاریخ آن را ثبت کنید. از این پس چک ابطال نمی‌شود.', 'Once the authorised signatories have registered the cheque in Sayad, record its date. From then on the cheque is not cancelled.') }}
        </p>
        <div class="tr-fields">
          <label class="req">{{ t('تاریخ', 'Date') }} <input v-model="step.date" placeholder="1405/08/30" dir="ltr" /></label>
          <label v-if="step.kind === 'cancel'" class="req wide">{{ t('علت ابطال', 'Reason') }} <input v-model="step.reason" maxlength="200" /></label>
        </div>
        <div class="tr-buttons">
          <button type="button" :class="{ 'tr-danger': step.kind === 'cancel' }" :disabled="busy" @click="recordStep">{{ step.kind === 'sayad' ? t('ثبت صیادی پرداخت', "Record the payer's Sayad registration")
            : step.kind === 'mature' ? t('ثبت سررسید', 'Record the maturity') : t('ابطال چک', 'Cancel the cheque') }}</button>
          <button type="button" class="tr-secondary" :disabled="busy" @click="closeStep">{{ t('انصراف', 'Leave') }}</button>
        </div>
      </section>
      <TransferVoucher v-if="voucherOf" :key="voucherOf" :transfer-id="voucherOf" :steps="payerSteps" cheque :refresh-key="voucherRun" :api-base="apiBase" :locale="locale" :token="token"
                       :account-name="L.accountName" @confirmed="load" @close="voucherOf = ''" />
    </section>
  </div>
</template>

<style scoped src="./receipts.css"></style>
<style scoped src="./transferForm.css"></style>
<style scoped>
.tr-receive-now { display: block; margin-top: .5rem; }
.tr-step-panel { margin-top: .75rem; display: grid; gap: .6rem; }
.tr-step-panel h4 { margin: 0; font-size: .95rem; color: #1b2f55; }
.tr-step-panel p { margin: 0; }
.tr-danger { background: #b42318; border-color: #b42318; color: #fff; }
</style>
