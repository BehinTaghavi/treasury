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
 * Owner, 19 Nov 2026 («ثبت انتقال (خرج چک)»): a cheque is transferred only when its latest status is «ثبت صیادی شد» and on the approved
 * payment request of the kind «ثبت انتقال (خرج چک)» written for that cheque; with the Sayad tracking code of the transfer its status
 * becomes «ثبت انتقال شد (خرج چک شد)», and the payment sheet («برگ پرداخت – رسید واگذاری چک») is printed for the two signatures.
 */
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { chequeActionName, chequeStateName, COMPANY_LOGO_PATH, COMPANY_PATH, createApi, dueMark, dueWaitsFor, formatAmount, normaliseJalali, searchKey, todayJalali, translate,
  type CompanyInfo, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'
import ReceiptForm from './ReceiptForm.vue'
import VoucherTable, { type Voucher } from './VoucherTable.vue'
import ChequeTransferPrint from './ChequeTransferPrint.vue'
import type { TransferSheet } from './transferSheet'

interface Step { action: string; from: string | null; to: string; date: string; userId: string; reference: string | null; note: string | null
  details: { accountId: string | null; partyName: string | null; documentDate: string | null; purpose: string | null; sayadTracking: string | null; receiptId: string | null
    trackingCode: string | null; partyDetailCode?: string | null; paymentRequestId?: string | null
    sheetNumber?: number | null; signers?: { id: string; name: string; position: string | null }[] | null } | null }
interface Cheque {
  id: string; kind: string; number: string | null; bankName: string | null; amount: number; currency: string; amountInRial: number
  dueDate: string | null; counterparty: string; state: string; collectionAccountId: string | null; destination: string | null; isPostDated: boolean
  reachedDueDate: boolean
  allowedActions: string[]; receiptId: string | null; history: Step[]; counterpartyDetailCode: string | null
  details: { branchName: string | null; sayadId: string | null; onDemand?: boolean; accountNumber?: string | null; accountHolder?: string | null } | null
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
  // Owner, 19 Nov 2026: the status of a transferred cheque is «ثبت انتقال شد (خرج چک شد)»; a note is «خرج شده».
  { id: 'endorsed', title: props.notes ? ['خرج‌شده', 'Endorsed'] : ['ثبت انتقال شده (خرج‌شده)', 'Transferred (endorsed)'], states: ['Endorsed'] },
]
// The owner's names, shared by every screen (ui-kit chequeStatus); on the note screen a bounce is «واخواست» and a return «استرداد» (#10).
const noteStateNames: Record<string, [string, string]> = {
  Bounced: ['واخواست شده', 'Protested'], ReturnedToIssuer: ['مسترد شده به متعهد', 'Handed back to the obligor'],
}
const noteActionNames: Record<string, [string, string]> = {
  RecordBounced: ['واخواست', 'Protest'], ReturnToIssuer: ['استرداد به متعهد', 'Hand back to the obligor'],
}
const stateName = (k: string | null | undefined) =>
  props.notes && k && noteStateNames[k] ? (fa.value ? noteStateNames[k][0] : noteStateNames[k][1]) : words(chequeStateName(k, props.locale, props.notes))
const actionName = (k: string | null | undefined) =>
  props.notes && k && noteActionNames[k] ? (fa.value ? noteActionNames[k][0] : noteActionNames[k][1]) : words(chequeActionName(k, props.locale, props.notes))
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
  sheet.value = null
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
// Owner, 19 Nov 2026: the deposit seemed not to work because the account box was left empty. With one suitable account it is
// chosen for the user; a choice that no longer fits the ticked cheques' currency is cleared.
watch(accountChoices, choices => {
  if (deposit.accountId && !choices.some(c => c.value === deposit.accountId)) deposit.accountId = ''
  if (!deposit.accountId && choices.length === 1) deposit.accountId = choices[0]!.value
}, { immediate: true })

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
  /** A request of the kind «ثبت انتقال (خرج چک)» names the cheque it hands on (owner, 19 Nov 2026). */
  cheque: { instrumentId: string } | null
}
const pending = ref<Pending | null>(null)
const approved = ref<PayRequest[]>([])
/**
 * A cheque: the approved request of the kind «ثبت انتقال (خرج چک)» written for this very cheque (owner, 19 Nov 2026; its amount is the
 * cheque's). A note: requests in the note's currency with at least its amount left to pay (answers to Q36 and Q51).
 */
const requestChoices = computed<Choice[]>(() => {
  const c = pending.value?.cheque
  if (!c) return []
  return approved.value.filter(r => (props.notes
    ? r.method !== 'ChequeTransfer' && r.currency === c.currency && r.amount - r.paidAmount >= c.amount
    : r.method === 'ChequeTransfer' && r.cheque?.instrumentId === c.id))
    .map(r => ({ value: r.id, label: `${r.beneficiaryName} – ${r.purpose}`, hint: t(`درخواست ${r.number} – مانده ${money(r.amount - r.paidAmount)}`, `request ${r.number} – left ${money(r.amount - r.paidAmount)}`) }))
})
const chosenRequest = computed(() => approved.value.find(r => r.id === pending.value?.requestId))
async function loadApproved() {
  const y = Number(todayJalali().slice(0, 4))
  const results = await Promise.all([y - 1, y].map(year => api.get<PayRequest[]>(`/api/requests/payment-requests?year=${year}&state=Approved`)))
  approved.value = results.flatMap(r => (r.ok ? r.value : []))
}
const replacing = ref<Cheque | null>(null)

// ---- authorised signatories of the payment sheet: the signature rule «برگ پرداخت چک» of Basic Information (owner's answer to Q57) ----
interface RuleSignatory { id: string; fullName: string; position: string }
interface SignatureRule { required: number; compulsory: RuleSignatory[]; optional: RuleSignatory[] }
const rule = ref<SignatureRule>({ required: 0, compulsory: [], optional: [] })
const optionalSigners = reactive<Record<string, boolean>>({})
const signerIds = computed(() => [...rule.value.compulsory.map(s => s.id), ...rule.value.optional.filter(s => optionalSigners[s.id]).map(s => s.id)])
const hasRule = computed(() => rule.value.required > 0 || rule.value.compulsory.length + rule.value.optional.length > 0)
async function loadRule() {
  const r = await api.get<SignatureRule>('/api/reference/signature-rules/ChequePaymentSheet')
  rule.value = r.ok ? r.value : { required: 0, compulsory: [], optional: [] }
  for (const k of Object.keys(optionalSigners)) delete optionalSigners[k]
}

async function start(c: Cheque, action: string) {
  message.value = null
  if (action === 'SendToLegal') { emit('legal', c.id); return }
  if (action === 'Replace') { replacing.value = c; pending.value = null; return }
  replacing.value = null
  pending.value = { cheque: c, action, date: todayJalali(), reference: '', documentDate: todayJalali(), note: '', requestId: '', sayad: '', tracking: '' }
  if (action === 'Endorse') {
    await Promise.all([loadApproved(), props.notes ? Promise.resolve() : loadRule()])
    // The request written for this cheque is chosen for the user.
    if (pending.value && requestChoices.value.length === 1) pending.value.requestId = requestChoices.value[0]!.value
  }
}

async function confirmStep() {
  const p = pending.value
  if (!p || busy.value) return
  const date = normaliseJalali(p.date)
  if (!date) return show('error', translate('FiscalDate.Invalid', props.locale))
  if (p.action === 'Endorse' && !props.notes && signerIds.value.length < rule.value.required) return show('error', translate('Receipt.SignersTooFew', props.locale))
  busy.value = true
  const r = p.action === 'Endorse'
    ? await api.post<string>(`/api/cashflow/instruments/${p.cheque.id}/endorse`,
        { date, paymentRequestId: p.requestId || '00000000-0000-0000-0000-000000000000', sayadTracking: props.notes ? '' : p.sayad, note: p.note || null,
          signerIds: props.notes ? [] : signerIds.value })
    : p.action === 'RecordEndorsementReturned'
    ? await api.post<string>(`/api/cashflow/instruments/${p.cheque.id}/endorsement-returned`, { date, noticeNumber: p.reference || null, reason: p.note || null, trackingCode: p.tracking || null })
    : await api.post<string>(`/api/cashflow/instruments/${p.cheque.id}/actions/${p.action}`,
        { date, note: p.note || null, reference: p.reference || null, documentDate: p.action === 'Cancel' ? normaliseJalali(p.documentDate) ?? p.documentDate : null,
          trackingCode: p.action === 'RecordBounced' ? p.tracking || null : null })
  busy.value = false
  if (!r.ok) return show('error', translate(r.code, props.locale))
  pending.value = null
  const noVoucher = p.action === 'RegisterSayad'
  show('ok', p.action === 'Endorse' && !props.notes
    ? t(`ثبت انتقال چک ${p.cheque.number} ثبت شد و آخرین وضعیت آن «${stateName('Endorsed')}» است. «برگ پرداخت (رسید واگذاری چک)» را از کارت چک چاپ و سند آن را تایید کنید.`,
      `Transfer of cheque ${p.cheque.number} recorded; its latest status is "${stateName('Endorsed')}". Print the payment sheet from the cheque card and confirm its voucher.`)
    : noVoucher
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
    : s.action === 'Endorse' && s.reference ? t(`کد رهگیری ثبت انتقال در صیاد ${s.reference}`, `Sayad transfer ${s.reference}`)
    : s.action === 'RegisterSayad' ? t(`پیگیری صیاد ${s.reference}`, `Sayad ${s.reference}`)
    : s.action === 'SendToLegal' ? t(`گواهی عدم پرداخت ${s.reference}${d?.documentDate ? ' مورخ ' + d.documentDate : ''}`, `certificate ${s.reference}${d?.documentDate ? ' of ' + d.documentDate : ''}`)
    : s.reference),
  d?.sheetNumber && t(`برگ پرداخت شمارهٔ ${d.sheetNumber.toLocaleString('fa-IR', { useGrouping: false })}`, `payment sheet no. ${d.sheetNumber}`),
  d?.trackingCode && t(`کد رهگیری ${d.trackingCode}`, `tracking ${d.trackingCode}`),
  d?.accountId && accountName(d.accountId), d?.partyName, d?.purpose, s.note]
  return parts.filter(Boolean).join(' · ')
}

// ---- the payment sheet of a transfer («برگ پرداخت – رسید واگذاری چک», owner, 19 Nov 2026, item 6) ----
const sheet = ref<TransferSheet | null>(null)
const sheetChequeId = ref('')
const hasTransfer = (c: Cheque) => !props.notes && c.state === 'Endorsed'
/** The company's logo as the shell reads it (with the login token once there is one), kept for the next sheet. */
let logo: string | null | undefined
async function logoUrl(): Promise<string | null> {
  if (logo !== undefined) return logo
  try {
    const token = props.token?.()
    const res = await fetch(`${props.apiBase}${COMPANY_LOGO_PATH}`, { cache: 'no-store', headers: token ? { Authorization: `Bearer ${token}` } : {} })
    logo = res.ok ? URL.createObjectURL(await res.blob()) : null
  } catch { logo = null }
  return logo
}
/** Builds the sheet of the cheque's latest transfer and opens the browser's print window; only the sheet is printed. */
async function printTransfer(c: Cheque) {
  message.value = null
  const full = await api.get<Cheque>(`/api/cashflow/instruments/${c.id}`)
  if (!full.ok) return show('error', translate(full.code, props.locale))
  const step = [...full.value.history].reverse().find(h => h.action === 'Endorse')
  if (!step) return show('error', translate('Instrument.InvalidTransition', props.locale))
  const [request, company] = await Promise.all([
    step.details?.paymentRequestId ? api.get<PayRequest>(`/api/requests/payment-requests/${step.details.paymentRequestId}`) : Promise.resolve(null),
    api.get<CompanyInfo>(COMPANY_PATH),
  ])
  const d = full.value.details
  sheetChequeId.value = c.id
  sheet.value = {
    company: { name: company.ok ? company.value.name : '', logoUrl: company.ok && company.value.hasLogo ? await logoUrl() : null },
    sheetNumber: step.details?.sheetNumber ?? null, signers: step.details?.signers ?? [],
    requestNumber: request?.ok ? request.value.number : null,
    date: step.date, sayadTracking: step.details?.sayadTracking ?? step.reference ?? '',
    receiverName: step.details?.partyName ?? full.value.destination ?? '', receiverDetailCode: step.details?.partyDetailCode ?? null,
    purposeGroupName: request?.ok ? request.value.purposeGroupName : null, purpose: step.details?.purpose ?? null,
    cheque: {
      sayadId: d?.sayadId ?? null, number: full.value.number ?? '', bankName: full.value.bankName, branchName: d?.branchName ?? null, accountNumber: d?.accountNumber ?? null,
      dueDate: full.value.dueDate, amount: full.value.amount, currency: full.value.currency, accountHolder: d?.accountHolder ?? null, payerName: full.value.counterparty,
    },
    recordedBy: step.userId, note: step.note,
  }
  await nextTick()
  // The logo is fetched for the sheet: give it a moment so the print shows it.
  const img = document.querySelector<HTMLImageElement>('.tr-print-root .logo')
  if (img && !img.complete) await new Promise(done => { img.onload = img.onerror = () => done(null); setTimeout(() => done(null), 1500) })
  window.print()
}

const printAgain = () => printTransfer({ id: sheetChequeId.value } as Cheque)

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
      <label class="req">{{ pending.action === 'Endorse' && !notes ? t('تاریخ ثبت انتقال در سامانهٔ صیاد', 'Date of the transfer in Sayad')
        : pending.action === 'RecordBounced' || pending.action === 'RecordEndorsementReturned' ? t('تاریخ گواهی عدم پرداخت', 'Date of the bounce notice')
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
        <!-- Owner, 19 Nov 2026: the transfer needs the approved request of the kind «ثبت انتقال (خرج چک)» written for this cheque. -->
        <p v-if="!notes && !requestChoices.length" class="wide3 tr-msg warning">{{ t(
          'برای این چک درخواست وجه تاییدشده‌ای از نوع «ثبت انتقال (خرج چک)» نیست. ابتدا در «درخواست وجه»، نوع پرداخت «ثبت انتقال (خرج چک)» را انتخاب و همین چک را برای شخص گیرنده ثبت کنید؛ پس از «تایید» درخواست، ثبت انتقال در اینجا انجام می‌شود.',
          'No approved payment request of the kind «ثبت انتقال (خرج چک)» names this cheque. Register one in Payment requests for the receiver and approve it first.') }}</p>
        <label class="wide">{{ notes ? t('درخواست وجه تاییدشده', 'Approved payment request') : t('درخواست وجه تاییدشدهٔ ثبت انتقال این چک', 'Approved transfer request of this cheque') }}
          <SearchSelect v-model="pending.requestId" :options="requestChoices" required :aria-label="t('درخواست وجه تاییدشده', 'Approved payment request')"
                        :none-text="notes ? t('درخواست وجه تاییدشده‌ای با این ارز و مانده‌ای به اندازهٔ این چک نیست', 'No approved request in this currency with enough left to pay')
                          : t('درخواست وجه تاییدشده‌ای برای ثبت انتقال این چک نیست', 'No approved transfer request for this cheque')" />
        </label>
        <p v-if="chosenRequest" class="wide small">
          {{ t('گیرنده', 'Receiver') }}: <strong>{{ chosenRequest.beneficiaryName }}</strong> ({{ chosenRequest.beneficiaryDetailCode }}) ·
          {{ chosenRequest.purposeGroupName }}: {{ chosenRequest.purpose }} ·
          {{ t('مبلغ درخواست', 'Request') }} {{ money(chosenRequest.amount) }} · {{ t('پرداخت‌شده', 'paid') }} {{ money(chosenRequest.paidAmount) }} ·
          {{ t('پس از این چک مانده', 'left after this cheque') }} {{ money(chosenRequest.amount - chosenRequest.paidAmount - pending.cheque.amount) }}
        </p>
        <label v-if="!notes" class="req">{{ t('کد رهگیری ثبت انتقال در سامانهٔ صیاد', 'Sayad tracking code of the transfer') }} <input v-model="pending.sayad" dir="ltr" /></label>
        <!-- Owner's answer to Q57: the authorised signatories of the payment sheet, by the rule «برگ پرداخت چک». -->
        <div v-if="!notes" class="tr-group wide3">
          <span>{{ t('امضاکنندگان مجاز برگ پرداخت', 'Authorised signatories of the payment sheet') }}
            <span v-if="hasRule" class="muted">{{ t(`– ${rule.required.toLocaleString('fa-IR')} امضا لازم است؛ ${signerIds.length.toLocaleString('fa-IR')} نفر انتخاب شده`, `– ${rule.required} needed; ${signerIds.length} chosen`) }}</span></span>
          <div v-if="hasRule" class="tr-ticks">
            <label v-for="s in rule.compulsory" :key="s.id"><input type="checkbox" checked disabled /> {{ s.fullName }} <span class="muted small">({{ s.position }} – {{ t('اجباری', 'compulsory') }})</span></label>
            <label v-for="s in rule.optional" :key="s.id"><input v-model="optionalSigners[s.id]" type="checkbox" /> {{ s.fullName }} <span class="muted small">({{ s.position }})</span></label>
          </div>
          <span v-else class="muted small">{{ t('قاعدهٔ امضای «برگ پرداخت چک» در اطلاعات پایه ← «دارندگان امضای مجاز» تعیین نشده است؛ برگ پرداخت فقط با امضای تحویل‌دهنده و تحویل‌گیرنده چاپ می‌شود.',
            'No signature rule is set for the cheque payment sheet (Basic Information → Authorised signatories); the sheet is printed with the two parties\' signatures only.') }}</span>
        </div>
        <label class="wide">{{ t('توضیحات', 'Note') }} <input v-model="pending.note" /></label>
      </template>
      <template v-else>
        <label v-if="pending.action === 'RecordCleared'">{{ t('شمارهٔ مرجع (صورتحساب بانک)', 'Reference (bank statement)') }} <input v-model="pending.reference" dir="ltr" /></label>
        <label class="wide">{{ t('توضیحات', 'Note') }} <input v-model="pending.note" /></label>
      </template>
      <div class="tr-buttons wide">
        <button type="button" :disabled="busy || (pending.action === 'Endorse' && !notes && !requestChoices.length)" @click="confirmStep">{{ t('ثبت', 'Record') }}</button>
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
    <p v-else-if="tab === 'desk'" class="muted small">{{ t('چک پس از «ثبت صیادی» به بانک واگذار می‌شود: ابتدا «ثبت صیادی» را در ردیف چک ثبت کنید؛ سپس چک برای واگذاری قابل تیک‌زدن است. «ثبت انتقال (خرج چک)» هم فقط برای چک «ثبت صیادی شد» و با درخواست وجه تاییدشدهٔ همان چک انجام می‌شود.',
      'A cheque is deposited at the bank after «ثبت صیادی»: record it on the cheque\'s row first; then the cheque can be ticked for the deposit. A transfer («ثبت انتقال») also needs it, and the approved payment request written for that cheque.') }}</p>
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
              <button v-if="hasTransfer(c)" type="button" class="tr-link" @click="printTransfer(c)">{{ t('چاپ برگ پرداخت', 'Print payment sheet') }}</button>
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
      <p v-if="selected.length && !accountChoices.length" class="wide tr-msg warning">{{ t(
        'برای ارز این چک‌ها حساب بانکی فعالی تعریف نشده است. ابتدا در «اطلاعات پایه ← حساب‌های بانکی» حساب شرکت را تعریف یا فعال کنید.',
        'There is no active company bank account in this currency. Add or switch one on in Basic Information → Bank accounts first.') }}</p>
      <p v-else-if="selected.length && !deposit.accountId" class="wide muted small">{{ t('حساب بانکی شرکت را که چک‌ها به آن واگذار می‌شوند انتخاب کنید.',
        'Choose the company bank account the cheques are deposited into.') }}</p>
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
        <button v-if="hasTransfer(card)" type="button" class="tr-secondary" @click="printTransfer(card)">{{ t('چاپ برگ پرداخت (رسید واگذاری چک)', 'Print the payment sheet') }}</button>
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

    <!-- the payment sheet of a transfer: a preview here, the same sheet alone on paper -->
    <section v-if="sheet" class="tr-card">
      <div class="tr-buttons">
        <strong>{{ t('برگ پرداخت – رسید واگذاری چک', 'Payment sheet – handover receipt of the cheque') }}</strong>
        <button type="button" @click="printAgain">{{ t('چاپ', 'Print') }}</button>
        <button type="button" class="tr-link" @click="sheet = null">{{ t('بستن', 'Close') }}</button>
      </div>
      <ChequeTransferPrint :sheet="sheet" :locale="locale" />
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
