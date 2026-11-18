<script setup lang="ts">
/**
 * «عملیات چک‌های دریافتی» (owner's item 14, 15 Nov 2026; answers Q30–Q35; docs/domain/received-cheque-steps.md). One tab per place of
 * the cheque, like the old program's forms: in the cash desk, at the bank, back from the bank or bounced, endorsed. Each step is recorded
 * here, the cheque's latest state changes at once, and the step's voucher is shown for the treasurer's «تایید سند». Opening a cheque shows
 * its card: the whole history and every voucher.
 * Steps: deposit at the bank (several cheques on one slip), collected, returned to the company, bounced (bank's notice no. and date),
 * replaced by new cheques (a cheque receipt of its own), cancelled by management's order (its no. and date), endorsed to pay an approved
 * payment request (answer to Q36; its beneficiary receives the cheque; with the Sayad transfer's tracking no.), endorsement returned (the
 * request's payment is taken back), returned to the payer.
 * Owner, 18 Nov 2026 (the latest status of a received cheque): «ثبت صیادی» for a cheque in the cash desk — required before the cheque
 * can be deposited at the bank (rule N3), so only registered cheques have a tick box for the deposit —, the mark «سررسید چک» on
 * post-dated cheques that reached their due date in the cash desk or at the bank, with what they wait for, and «اقدام حقوقی» for a
 * bounced cheque, which is recorded on its own screen (LegalAction.vue): the button on a bounced cheque's row opens it with that cheque. The names of the statuses are the owner's and shared by every screen (ui-kit chequeStatus).
 * With `notes` the same screen is «عملیات سفته‌های دریافتی» (owner, 16 Nov 2026: واگذاری به بانک، وصول، استرداد، خرج سفته – ظهرنویسی
 * و انتقال به غیر): it lists promissory notes instead of cheques, a note the bank does not pay is protested («واخواست»), returning it to
 * the obligor is «استرداد», and an endorsement has no Sayad transfer. Guarantee and trust notes are only handed back or cancelled.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { chequeActionName, chequeStateName, createApi, dueMark, dueWaitsFor, formatAmount, normaliseJalali, searchKey, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'
import ReceiptForm from './ReceiptForm.vue'
import VoucherTable, { type Voucher } from './VoucherTable.vue'

interface Step { action: string; from: string | null; to: string; date: string; userId: string; reference: string | null; note: string | null
  details: { accountId: string | null; partyName: string | null; documentDate: string | null; purpose: string | null; sayadTracking: string | null; receiptId: string | null
    trackingCode: string | null } | null }
interface Cheque {
  id: string; kind: string; number: string | null; bankName: string | null; amount: number; currency: string; amountInRial: number
  dueDate: string | null; counterparty: string; state: string; collectionAccountId: string | null; destination: string | null; isPostDated: boolean
  reachedDueDate: boolean
  allowedActions: string[]; receiptId: string | null; history: Step[]; counterpartyDetailCode: string | null
  details: { branchName: string | null; sayadId: string | null; onDemand?: boolean } | null
}
type Tab = 'desk' | 'bank' | 'back' | 'endorsed'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; notes?: boolean }>(),
  { apiBase: '', locale: 'fa', token: undefined, notes: false })
/** «اقدام حقوقی» has its own screen: the parent opens it with the cheque. */
const emit = defineEmits<{ legal: [chequeId: string] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
/** The words of the cheque screen in the notes screen: سفته for چک, واخواست‌نامه for the bounce notice, متعهد for the payer. */
const noteWords: [string, string][] = props.locale === 'fa'
  ? [['چکی ', 'سفته‌ای '], ['چک', 'سفته'], ['اعلامیهٔ برگشت', 'واخواست‌نامه'], ['اعلامیه', 'واخواست‌نامه'], ['علت برگشت', 'علت واخواست'],
      ['پرداخت کننده', 'متعهد'], ['پرداخت‌کننده', 'متعهد']]
  : [['cheques', 'notes'], ['Cheque', 'Note'], ['cheque', 'note'], ['bounce notice', 'protest'], ['Bounce notice', 'Protest'], ['Payer', 'Obligor'], ['payer', 'obligor']]
const words = (text: string) => (props.notes ? noteWords.reduce((s, [a, b]) => s.replaceAll(a, b), text) : text)
const t = (f: string, e: string) => words(props.locale === 'fa' ? f : e)
const isNote = (kind: string) => kind.endsWith('Note')
const money = (n: number) => formatAmount(n, props.locale)
const L = useLookups(api)

const tabs: { id: Tab; title: [string, string]; states: string[] }[] = [
  { id: 'desk', title: ['نزد صندوق', 'In the cash desk'], states: ['InCashDesk', 'SayadRegistered'] },
  { id: 'bank', title: ['نزد بانک (در جریان وصول)', 'At the bank'], states: ['DepositedForCollection'] },
  { id: 'back', title: ['عودت از بانک، برگشتی و اقدام حقوقی', 'Back from the bank, bounced and legal action'], states: ['ReturnedFromBank', 'Bounced', 'InLegal'] },
  { id: 'endorsed', title: ['خرج‌شده', 'Endorsed'], states: ['Endorsed'] },
]
// The owner's names, shared by every screen (ui-kit chequeStatus); on the note screen a bounce is «واخواست» and a return «استرداد» (#10).
const noteStateNames: Record<string, [string, string]> = {
  Bounced: ['واخواست شده', 'Protested'], ReturnedToIssuer: ['مسترد شده به متعهد', 'Handed back to the obligor'],
}
const noteActionNames: Record<string, [string, string]> = {
  RecordBounced: ['واخواست', 'Protest'], ReturnToIssuer: ['استرداد به متعهد', 'Hand back to the obligor'],
}
const stateName = (k: string | null | undefined) =>
  props.notes && k && noteStateNames[k] ? (fa.value ? noteStateNames[k][0] : noteStateNames[k][1]) : words(chequeStateName(k, props.locale))
const actionName = (k: string | null | undefined) =>
  props.notes && k && noteActionNames[k] ? (fa.value ? noteActionNames[k][0] : noteActionNames[k][1]) : words(chequeActionName(k, props.locale))
if (props.notes) tabs[2]!.title = ['عودت‌شده از بانک، واخواستی و اقدام حقوقی', 'Back from the bank, protested and legal action']

const tab = ref<Tab>('desk')
const lists = reactive<Record<string, Cheque[]>>({})
const search = ref('')
const dueUntil = ref('')
/** Only the cheques marked «سررسید چک». */
const dueOnly = ref(false)
/** «سررسید چک» is only for cheques in the cash desk or at the bank. */
const dueTab = computed(() => tab.value === 'desk' || tab.value === 'bank')
const busy = ref(false)
const message = ref<{ kind: 'error' | 'ok' | 'warning'; text: string } | null>(null)
const show = (kind: 'error' | 'ok' | 'warning', text: string) => { message.value = { kind, text } }

const current = computed(() => tabs.find(x => x.id === tab.value)!)
/**
 * Another tab shows other cheques: the cheque card, the step being entered and the ticks of the tab left behind are closed (owner,
 * 18 Nov 2026: the card of a cheque at the bank stayed on the screen after switching to «نزد صندوق»).
 */
function openTab(id: Tab) {
  if (id === tab.value) return
  tab.value = id
  pending.value = null
  replacing.value = null
  card.value = null
  vouchers.value = []
  message.value = null
  for (const k of Object.keys(chosen)) delete chosen[k]
}
const shown = computed(() => {
  const until = normaliseJalali(dueUntil.value)
  const key = searchKey(search.value)
  return current.value.states.flatMap(s => lists[s] ?? []).filter(c =>
    (!until || (c.dueDate ?? '') <= until) && (!dueOnly.value || !dueTab.value || c.reachedDueDate) && (!key || searchKey(`${c.number} ${c.counterparty} ${c.bankName ?? ''} ${c.destination ?? ''}`).includes(key)))
})
const count = (x: (typeof tabs)[number]) => x.states.reduce((n, s) => n + (lists[s]?.length ?? 0), 0)
const accountName = (id: string | null | undefined) => { const a = L.bankAccounts.value.find(x => x.id === id); return a ? L.accountTitle(a) : '' }

async function load() {
  const states = tabs.flatMap(x => x.states)
  const results = await Promise.all(states.map(s => api.get<Cheque[]>(`/api/cashflow/instruments/received?state=${s}`)))
  // Cheques on the cheque screen, promissory notes on the notes screen.
  states.forEach((s, i) => { const r = results[i]!; if (r.ok) lists[s] = r.value.filter(c => isNote(c.kind) === props.notes) })
  for (const k of Object.keys(chosen)) if (!tickable.value.some(c => c.id === k)) delete chosen[k]
}

// ---- deposit at the bank: ticked cheques of the cash desk, or back from the bank (answer to Q33), on one slip ----
const chosen = reactive<Record<string, boolean>>({})
const deposit = reactive({ date: todayJalali(), accountId: '', slip: '' })
const tickable = computed(() => shown.value.filter(c => c.allowedActions.includes('DepositForCollection')))
const selected = computed(() => tickable.value.filter(c => chosen[c.id]))
const selectedCurrency = computed(() => selected.value[0]?.currency ?? 'IRR')
const selectedTotal = computed(() => selected.value.reduce((s, c) => s + c.amount, 0))
const accountChoices = computed<Choice[]>(() => L.bankAccounts.value
  .filter(a => a.isActive && a.currencyCode === selectedCurrency.value)
  .map(a => ({ value: a.id, label: L.accountTitle(a), hint: a.currencyCode })))
function toggleAll(on: boolean) { for (const c of tickable.value) chosen[c.id] = on }

async function depositSelected() {
  message.value = null
  const date = normaliseJalali(deposit.date)
  if (!date) return show('error', translate('FiscalDate.Invalid', props.locale))
  if (!selected.value.length) return show('error', t('ابتدا چک‌ها را انتخاب کنید.', 'Tick the cheques first.'))
  if (new Set(selected.value.map(c => c.currency)).size > 1) return show('error', t('چک‌های یک ارز را با هم واگذار کنید.', 'Deposit cheques of one currency together.'))
  if (!deposit.accountId) return show('error', translate('Instrument.CollectionAccountRequired', props.locale))
  busy.value = true
  const failed: string[] = []
  let done = 0
  for (const c of selected.value) {
    const r = await api.post<string>(`/api/cashflow/instruments/${c.id}/deposit`, { bankAccountId: deposit.accountId, date, slipNumber: deposit.slip || null })
    if (r.ok) done++
    else failed.push(`${c.number}: ${translate(r.code, props.locale)}`)
  }
  busy.value = false
  await load()
  const ok = t(`${done.toLocaleString('fa-IR')} چک به بانک واگذار شد. سند هر چک در کارت آن چک (کلیک روی ردیف) تایید می‌شود.`,
    `${done} cheque(s) deposited. Each cheque's voucher is confirmed on its card (click the row).`)
  if (failed.length) show(done ? 'warning' : 'error', `${done ? ok + ' ' : ''}${t('واگذار نشد', 'Not deposited')} – ${failed.join('؛ ')}`)
  else { show('ok', ok); deposit.slip = '' }
}

// ---- one step on one cheque ----
interface Pending {
  cheque: Cheque; action: string; date: string; reference: string; documentDate: string; note: string; requestId: string; sayad: string
  /** The tracking code of the certificate of non-payment, on a bounce. */
  tracking: string
}
/** An approved payment request (requests-svc) an endorsed cheque can pay (answer to Q36). */
interface PayRequest {
  id: string; fiscalYear: number; number: number; beneficiaryName: string; beneficiaryDetailCode: string; purposeGroupName: string; purpose: string
  amount: number; currency: string; paidAmount: number; method: string; priority: number
}
const pending = ref<Pending | null>(null)
const approved = ref<PayRequest[]>([])
/** Requests in the cheque's currency with at least the cheque's amount left to pay. */
const requestChoices = computed<Choice[]>(() => {
  const c = pending.value?.cheque
  if (!c) return []
  return approved.value.filter(r => r.currency === c.currency && r.amount - r.paidAmount >= c.amount)
    .map(r => ({ value: r.id, label: `${r.beneficiaryName} – ${r.purpose}`, hint: t(`درخواست ${r.number} – مانده ${money(r.amount - r.paidAmount)}`, `request ${r.number} – left ${money(r.amount - r.paidAmount)}`) }))
})
const chosenRequest = computed(() => approved.value.find(r => r.id === pending.value?.requestId))
async function loadApproved() {
  const y = Number(todayJalali().slice(0, 4))
  const results = await Promise.all([y - 1, y].map(year => api.get<PayRequest[]>(`/api/requests/payment-requests?year=${year}&state=Approved`)))
  approved.value = results.flatMap(r => (r.ok ? r.value : []))
}
const replacing = ref<Cheque | null>(null)

async function start(c: Cheque, action: string) {
  message.value = null
  if (action === 'SendToLegal') { emit('legal', c.id); return }
  if (action === 'Replace') { replacing.value = c; pending.value = null; return }
  replacing.value = null
  pending.value = { cheque: c, action, date: todayJalali(), reference: '', documentDate: todayJalali(), note: '', requestId: '', sayad: '', tracking: '' }
  if (action === 'Endorse') await loadApproved()
}

async function confirmStep() {
  const p = pending.value
  if (!p || busy.value) return
  const date = normaliseJalali(p.date)
  if (!date) return show('error', translate('FiscalDate.Invalid', props.locale))
  busy.value = true
  const r = p.action === 'Endorse'
    ? await api.post<string>(`/api/cashflow/instruments/${p.cheque.id}/endorse`,
        { date, paymentRequestId: p.requestId || '00000000-0000-0000-0000-000000000000', sayadTracking: props.notes ? '' : p.sayad, note: p.note || null })
    : p.action === 'RecordEndorsementReturned'
    ? await api.post<string>(`/api/cashflow/instruments/${p.cheque.id}/endorsement-returned`, { date, noticeNumber: p.reference || null, reason: p.note || null, trackingCode: p.tracking || null })
    : await api.post<string>(`/api/cashflow/instruments/${p.cheque.id}/actions/${p.action}`,
        { date, note: p.note || null, reference: p.reference || null, documentDate: p.action === 'Cancel' ? normaliseJalali(p.documentDate) ?? p.documentDate : null,
          trackingCode: p.action === 'RecordBounced' ? p.tracking || null : null })
  busy.value = false
  if (!r.ok) return show('error', translate(r.code, props.locale))
  pending.value = null
  const noVoucher = p.action === 'RegisterSayad'
  show('ok', noVoucher
    ? t(`«${actionName(p.action)}» چک ${p.cheque.number} ثبت شد و آخرین وضعیت چک تغییر کرد.`, `"${actionName(p.action)}" recorded for cheque ${p.cheque.number}; its latest status changed.`)
    : t(`«${actionName(p.action)}» چک ${p.cheque.number} ثبت شد. سند آن را در کارت چک تایید کنید.`, `"${actionName(p.action)}" recorded for cheque ${p.cheque.number}. Confirm its voucher on the cheque card.`))
  await load()
  await openCard(p.cheque.id)
}

async function replaced() {
  const c = replacing.value
  replacing.value = null
  show('ok', t(`چک ${c?.number} تعویض شد؛ چک‌های جدید نزد صندوق‌اند. سند تعویض در فهرست دریافت‌ها (رسید جدید) تایید می‌شود.`,
    `Cheque ${c?.number} replaced; the new cheques are in the cash desk. Confirm the replacement voucher on the new receipt in the receipts list.`))
  await load()
}

// ---- the cheque card: history and vouchers ----
const card = ref<Cheque | null>(null)
const vouchers = ref<Voucher[]>([])
const confirming = ref(false)
async function openCard(id: string) {
  const [c, v] = await Promise.all([api.get<Cheque>(`/api/cashflow/instruments/${id}`), api.get<Voucher[]>(`/api/cashflow/instruments/${id}/vouchers`)])
  if (c.ok) card.value = c.value
  vouchers.value = v.ok ? v.value : []
}
async function confirmVoucher(step: number) {
  if (!card.value || confirming.value) return
  confirming.value = true
  const r = await api.post<string>(`/api/cashflow/instruments/${card.value.id}/vouchers/${step}/confirm`, {})
  confirming.value = false
  if (!r.ok) return show('error', translate(r.code, props.locale))
  show('ok', t('سند تایید شد.', 'Voucher confirmed.'))
  await openCard(card.value.id)
}
function stepText(s: Step) {
  const d = s.details
  const parts = [s.reference && (s.action === 'RecordBounced' || s.action === 'RecordEndorsementReturned' ? t(`گواهی عدم پرداخت ${s.reference}`, `certificate ${s.reference}`)
    : s.action === 'Cancel' ? t(`دستور ${s.reference} مورخ ${d?.documentDate ?? ''}`, `order ${s.reference} of ${d?.documentDate ?? ''}`)
    : s.action === 'Endorse' || s.action === 'RegisterSayad' ? t(`پیگیری صیاد ${s.reference}`, `Sayad ${s.reference}`)
    : s.action === 'SendToLegal' ? t(`گواهی عدم پرداخت ${s.reference}${d?.documentDate ? ' مورخ ' + d.documentDate : ''}`, `certificate ${s.reference}${d?.documentDate ? ' of ' + d.documentDate : ''}`)
    : s.reference),
  d?.trackingCode && t(`کد رهگیری ${d.trackingCode}`, `tracking ${d.trackingCode}`),
  d?.accountId && accountName(d.accountId), d?.partyName, d?.purpose, s.note]
  return parts.filter(Boolean).join(' · ')
}

const guarantee = (c: Cheque) => c.kind === 'GuaranteeCheque' || c.kind === 'GuaranteeNote'
/** Steps offered on each row, the usual next step first; deposit is done with the tick boxes. A note is not replaced here (replacement is a cheque receipt). */
const stepOrder = ['RegisterSayad', 'RecordCleared', 'ReturnFromBank', 'RecordBounced', 'SendToLegal', 'ReturnFromLegal', 'Replace', 'ReturnToIssuer', 'Endorse', 'RecordEndorsementReturned', 'Cancel']
const rank = (a: string) => { const i = stepOrder.indexOf(a); return i < 0 ? stepOrder.length : i }
/**
 * «اقدام حقوقی» is offered on bounced cheques and notes and on guarantee cheques and notes; the rare action on an ordinary cheque still in
 * the cash desk starts on the legal-action screen.
 */
const rowActions = (c: Cheque) => c.allowedActions.filter(a => a !== 'DepositForCollection' && !(props.notes && a === 'Replace')
  && (a !== 'SendToLegal' || c.state === 'Bounced' || guarantee(c)))
  .sort((a, b) => rank(a) - rank(b))

onMounted(async () => { await L.load(); await load() })
</script>

<template>
  <div>
    <nav class="tr-subtabs" role="tablist">
      <button v-for="x in tabs" :key="x.id" type="button" role="tab" :aria-selected="tab === x.id" :class="{ active: tab === x.id }"
              @click="openTab(x.id)">
        {{ fa ? x.title[0] : x.title[1] }} <span class="muted small">({{ count(x).toLocaleString(fa ? 'fa-IR' : 'en-US') }})</span>
      </button>
    </nav>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <div class="tr-buttons">
      <label style="width: 10rem">{{ t('سررسید تا', 'Due until') }} <input v-model="dueUntil" placeholder="1405/09/30" dir="ltr" /></label>
      <label style="flex: 1; min-width: 14rem">{{ t('جستجو (شماره، پرداخت کننده، بانک، گیرنده)', 'Search (number, payer, bank, receiver)') }} <input v-model="search" /></label>
      <label v-if="dueTab" class="tr-check"><input v-model="dueOnly" type="checkbox" /> {{ t('فقط چک‌های سررسید شده', 'Only cheques that reached their due date') }}</label>
    </div>

    <!-- one step on one cheque -->
    <fieldset v-if="pending" class="tr-step">
      <legend>{{ actionName(pending.action) }} – {{ t('چک', 'cheque') }} {{ pending.cheque.number }} ({{ money(pending.cheque.amount) }})</legend>
      <label class="req">{{ pending.action === 'RecordBounced' || pending.action === 'RecordEndorsementReturned' ? t('تاریخ گواهی عدم پرداخت', 'Date of the bounce notice')
        : pending.action === 'Cancel' ? t('تاریخ ابطال', 'Date of cancellation')
        : pending.action === 'RegisterSayad' ? t('تاریخ ثبت در سامانهٔ صیاد', 'Date of the Sayad registration') : t('تاریخ', 'Date') }}
        <input v-model="pending.date" dir="ltr" /></label>
      <template v-if="pending.action === 'RecordBounced' || pending.action === 'RecordEndorsementReturned'">
        <label class="req">{{ t('شمارهٔ گواهی عدم پرداخت', 'Bounce notice no.') }} <input v-model="pending.reference" dir="ltr" /></label>
        <label>{{ t('کد رهگیری گواهی عدم پرداخت', 'Tracking code of the certificate') }} <input v-model="pending.tracking" dir="ltr" /></label>
        <label class="wide">{{ t('علت برگشت', 'Reason') }} <input v-model="pending.note" /></label>
      </template>
      <template v-else-if="pending.action === 'Cancel'">
        <label class="req">{{ t('شمارهٔ دستور مدیریت', "Management's order no.") }} <input v-model="pending.reference" /></label>
        <label class="req">{{ t('تاریخ دستور مدیریت', "Date of the order") }} <input v-model="pending.documentDate" dir="ltr" /></label>
        <label class="wide req">{{ t('علت ابطال', 'Reason') }} <input v-model="pending.note" /></label>
      </template>
      <template v-else-if="pending.action === 'RegisterSayad'">
        <label>{{ t('کد رهگیری ثبت در سامانهٔ صیاد', 'Sayad registration tracking no.') }} <input v-model="pending.reference" dir="ltr" /></label>
        <label class="wide">{{ t('توضیحات', 'Note') }} <input v-model="pending.note" /></label>
      </template>
      <template v-else-if="pending.action === 'Endorse'">
        <label class="wide">{{ t('درخواست وجه تاییدشده', 'Approved payment request') }}
          <SearchSelect v-model="pending.requestId" :options="requestChoices" required :aria-label="t('درخواست وجه تاییدشده', 'Approved payment request')"
                        :none-text="t('درخواست وجه تاییدشده‌ای با این ارز و مانده‌ای به اندازهٔ این چک نیست', 'No approved request in this currency with enough left to pay')" />
        </label>
        <p v-if="chosenRequest" class="wide small">
          {{ t('گیرنده', 'Receiver') }}: <strong>{{ chosenRequest.beneficiaryName }}</strong> ({{ chosenRequest.beneficiaryDetailCode }}) ·
          {{ chosenRequest.purposeGroupName }}: {{ chosenRequest.purpose }} ·
          {{ t('مبلغ درخواست', 'Request') }} {{ money(chosenRequest.amount) }} · {{ t('پرداخت‌شده', 'paid') }} {{ money(chosenRequest.paidAmount) }} ·
          {{ t('پس از این چک مانده', 'left after this cheque') }} {{ money(chosenRequest.amount - chosenRequest.paidAmount - pending.cheque.amount) }}
        </p>
        <label v-if="!notes" class="req">{{ t('کد پیگیری انتقال در سامانهٔ صیاد', 'Sayad transfer tracking no.') }} <input v-model="pending.sayad" dir="ltr" /></label>
        <label class="wide">{{ t('توضیحات', 'Note') }} <input v-model="pending.note" /></label>
      </template>
      <template v-else>
        <label v-if="pending.action === 'RecordCleared'">{{ t('شمارهٔ مرجع (صورتحساب بانک)', 'Reference (bank statement)') }} <input v-model="pending.reference" dir="ltr" /></label>
        <label class="wide">{{ t('توضیحات', 'Note') }} <input v-model="pending.note" /></label>
      </template>
      <div class="tr-buttons wide">
        <button type="button" :disabled="busy" @click="confirmStep">{{ t('ثبت', 'Record') }}</button>
        <button type="button" class="tr-secondary" @click="pending = null">{{ t('انصراف', 'Back') }}</button>
      </div>
    </fieldset>

    <!-- replacement: a cheque receipt of the new cheques (answer to Q31: they may differ in amount and number) -->
    <section v-if="replacing" class="tr-step">
      <div class="tr-buttons">
        <strong>{{ t(`تعویض چک ${replacing.number} – ${replacing.counterparty} – ${money(replacing.amount)}`, `Replace cheque ${replacing.number} – ${replacing.counterparty} – ${money(replacing.amount)}`) }}</strong>
        <button type="button" class="tr-secondary" @click="replacing = null">{{ t('انصراف', 'Back') }}</button>
      </div>
      <ReceiptForm :key="replacing.id" kind="Instruments" :replaces="{ id: replacing.id, number: replacing.number ?? '', amount: replacing.amountInRial, payerCode: replacing.counterpartyDetailCode ?? '' }"
                   :api-base="apiBase" :locale="locale" :token="token" @saved="replaced" />
    </section>

    <!-- A note has no Sayad registration (owner, 16 and 18 Nov 2026): an ordinary note is ticked straight from the cash desk. -->
    <p v-if="tab === 'desk' && notes" class="muted small">{{ t('سفته در سامانهٔ صیاد ثبت نمی‌شود: سفته‌های عادی نزد صندوق را تیک بزنید و «واگذاری به بانک» را بزنید. سفتهٔ تضمینی یا امانی به بانک واگذار نمی‌شود.',
      'A note has no Sayad registration: tick the ordinary notes in the cash desk and press «واگذاری به بانک». A guarantee or trust note is not deposited.') }}</p>
    <p v-else-if="tab === 'desk'" class="muted small">{{ t('چک پس از «ثبت صیادی» به بانک واگذار می‌شود: ابتدا «ثبت صیادی» را در ردیف چک ثبت کنید؛ سپس چک برای واگذاری قابل تیک‌زدن است.',
      'A cheque is deposited at the bank after «ثبت صیادی»: record it on the cheque\'s row first; then the cheque can be ticked for the deposit.') }}</p>
    <div class="tr-scroll">
      <table class="tr-table">
        <thead>
          <tr>
            <th v-if="tab === 'desk' || tab === 'back'">
              <input type="checkbox" :checked="tickable.length > 0 && selected.length === tickable.length" :aria-label="t('همه', 'All')"
                     @change="toggleAll(($event.target as HTMLInputElement).checked)" />
            </th>
            <th>{{ t('سررسید', 'Due') }}</th><th>{{ t('شماره چک', 'Cheque no.') }}</th><th>{{ notes ? t('نوع', 'Kind') : t('بانک', 'Bank') }}</th><th>{{ t('پرداخت کننده', 'Payer') }}</th>
            <th>{{ t('مبلغ', 'Amount') }}</th><th>{{ t('آخرین وضعیت', 'Latest status') }}</th><th>{{ t('عملیات', 'Steps') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="c in shown" :key="c.id" :class="['clickable', { selected: card?.id === c.id || chosen[c.id] }]" @click="openCard(c.id)">
            <td v-if="tab === 'desk' || tab === 'back'" @click.stop>
              <input v-if="c.allowedActions.includes('DepositForCollection')" v-model="chosen[c.id]" type="checkbox" :aria-label="c.number ?? ''" />
            </td>
            <td class="num">{{ c.dueDate ?? (c.details?.onDemand ? t('عندالمطالبه', 'On demand') : '') }}</td>
            <td class="num">{{ c.number }}</td>
            <td v-if="notes">{{ c.kind === 'GuaranteeNote' ? t('تضمینی / امانی', 'Guarantee / trust') : t('عادی', 'Ordinary') }}</td>
            <td v-else>{{ c.bankName }}<span v-if="c.details?.branchName" class="muted small"> – {{ c.details.branchName }}</span></td>
            <td>{{ c.counterparty }}</td>
            <td class="num">{{ money(c.amount) }} <span v-if="c.currency !== 'IRR'">{{ c.currency }}</span></td>
            <td>{{ stateName(c.state) }}
              <span v-if="c.reachedDueDate" class="tr-tag due" :title="dueWaitsFor(c.state, locale)">{{ dueMark(locale) }}</span>
              <div v-if="c.state === 'DepositedForCollection'" class="muted small">{{ accountName(c.collectionAccountId) || c.destination }}</div>
              <div v-else-if="c.state === 'Endorsed'" class="muted small">{{ c.destination }}</div>
              <div v-if="c.reachedDueDate" class="muted small">{{ dueWaitsFor(c.state, locale) }}</div>
            </td>
            <td class="tr-actions" @click.stop>
              <button v-for="a in rowActions(c)" :key="a" type="button" :class="['tr-link', { danger: a === 'Cancel' || a === 'RecordBounced' }]" @click="start(c, a)">
                {{ actionName(a) }}</button>
              <button v-if="c.state === 'InLegal'" type="button" class="tr-link" @click="emit('legal', c.id)">{{ t('شرح و ضمائم اقدام حقوقی', 'Legal action and scans') }}</button>
            </td>
          </tr>
          <tr v-if="!shown.length"><td colspan="8" class="muted">{{ t('چکی در این زبانه نیست.', 'No cheque here.') }}</td></tr>
        </tbody>
      </table>
    </div>

    <fieldset v-if="(tab === 'desk' || tab === 'back') && tickable.length" style="margin-top: .75rem">
      <legend>{{ t('واگذاری چک‌های تیک‌خورده به بانک جهت وصول', 'Deposit the ticked cheques for collection') }}</legend>
      <label class="req">{{ t('تاریخ واگذاری', 'Date') }} <input v-model="deposit.date" dir="ltr" /></label>
      <label class="wide">{{ t('حساب بانکی شرکت', 'Company bank account') }}
        <SearchSelect v-model="deposit.accountId" :options="accountChoices" required :aria-label="t('حساب بانکی شرکت', 'Company bank account')"
                      :none-text="t('حساب بانکی فعالی با ارز این چک‌ها نیست', 'No active account in this currency')" />
      </label>
      <label>{{ t('شماره فیش واگذاری', 'Deposit slip no.') }} <input v-model="deposit.slip" dir="ltr" /></label>
      <div class="tr-buttons wide">
        <strong>{{ t(`${selected.length.toLocaleString('fa-IR')} چک`, `${selected.length} cheques`) }} – <span class="num">{{ money(selectedTotal) }}</span> {{ selectedCurrency === 'IRR' ? t('ریال', 'Rial') : selectedCurrency }}</strong>
        <button type="button" :disabled="busy || !selected.length" @click="depositSelected">{{ t('واگذاری به بانک', 'Deposit at the bank') }}</button>
      </div>
    </fieldset>

    <!-- the cheque card -->
    <section v-if="card" class="tr-card">
      <div class="tr-buttons">
        <h3 style="margin: 0">{{ t(`کارت چک ${card.number}`, `Cheque ${card.number}`) }}</h3>
        <span class="tr-tag">{{ t('آخرین وضعیت', 'Latest status') }}: {{ stateName(card.state) }}</span>
        <span v-if="card.reachedDueDate" class="tr-tag due" :title="dueWaitsFor(card.state, locale)">{{ dueMark(locale) }}</span>
        <span class="muted small"><template v-if="card.bankName">{{ card.bankName }} · </template>{{ card.counterparty }} · {{ card.dueDate ? `${t('سررسید', 'due')} ${card.dueDate}` : card.details?.onDemand ? t('عندالمطالبه', 'on demand') : '' }} · {{ money(card.amount) }} {{ card.currency }}</span>
        <button type="button" class="tr-link" @click="card = null">{{ t('بستن', 'Close') }}</button>
      </div>
      <table class="tr-table">
        <thead><tr><th>#</th><th>{{ t('تاریخ', 'Date') }}</th><th>{{ t('مرحله', 'Step') }}</th><th>{{ t('وضعیت پس از آن', 'State after') }}</th><th>{{ t('جزئیات', 'Details') }}</th><th>{{ t('کاربر', 'User') }}</th></tr></thead>
        <tbody>
          <tr v-for="(s, i) in card.history" :key="i">
            <td class="num">{{ (i + 1).toLocaleString(fa ? 'fa-IR' : 'en-US') }}</td>
            <td class="num">{{ s.date }}</td>
            <td>{{ actionName(s.action) }}</td>
            <td>{{ stateName(s.to) }}</td>
            <td class="small">{{ stepText(s) }}</td>
            <td class="small muted">{{ s.userId }}</td>
          </tr>
        </tbody>
      </table>
      <p v-if="!vouchers.length" class="muted small">{{ t('سند دریافت این چک در فهرست دریافت‌ها (رسید آن) است؛ مرحله‌ای که سند حسابداری داشته باشد هنوز ثبت نشده است.',
        "This cheque's receipt voucher is on its receipt in the receipts list; no step with a voucher yet.") }}</p>
      <VoucherTable v-for="v in vouchers" :key="v.step" :voucher="v" :locale="locale" :account-name="L.accountName" can-confirm :busy="confirming"
                    @confirm="confirmVoucher(v.step)" />
    </section>
  </div>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
.tr-actions { white-space: nowrap; }
.tr-subtabs { display: flex; flex-wrap: wrap; gap: .35rem; margin-bottom: .75rem; }
.tr-subtabs button { background: #fff; color: #1f2933; border: 1px solid #cbd2d9; border-radius: 999px; padding: .35rem .85rem; }
.tr-subtabs button.active { background: #e8effc; color: #1d4ed8; border-color: #93b4f5; font-weight: 600; }
.tr-step { margin: .75rem 0; background: #f8fafc; }
.tr-card { margin-top: 1.25rem; padding: .75rem; border: 1px solid #d9e2ec; border-radius: 8px; }
</style>
