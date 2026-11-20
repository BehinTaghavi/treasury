<script setup lang="ts">
/**
 * One receipt form for the three kinds (owner, 14 Nov 2026): cash into a cash desk (old D_Naghd), money paid into a bank account (old
 * D_variz) by one of the kinds the owner listed on 18 Nov 2026 — Paya or Satna transfer with its Sheba, internal transfer, cash at a
 * branch, card to card (only into an account that has a bank card), payment gateway — or POS, and cheques and promissory notes from one payer (old D_Check with
 * «ثبت چک بعدی»). Current or post-dated is decided by the due date; Rial or foreign currency by the account or by each cheque.
 * Ctrl+Enter saves. Errors are shown from the API's codes, translated.
 * The voucher the receipt will produce is shown at the bottom while the form is filled (owner's report no. 4, item 8); the treasurer
 * confirms it from the receipts list after saving.
 * With `replaces` the form is the replacement of a received cheque (owner's item 14 (d)): the payer is the old cheque's, the new cheques
 * may differ in amount and number (answer to Q31), and the receipt is saved with the old cheque marked «تعویض شده». Owner's report no. 6,
 * item 11: the form opens with the old cheque's purpose group, purpose, department, amount and currency, all of which can be changed.
 * The old cheque was cancelled first (owner's answer to Q62, 19 Nov 2026); the new cheques are received «نزد صندوق».
 * With `edit` the form is «ویرایش» of a saved receipt (report no. 6, items 2 and 6): it opens with the receipt as it was saved and saves
 * it back under the same number, while its voucher is not confirmed. A cheque on which a step was recorded («ثبت صیادی» …) is shown
 * locked; the other cheques are corrected, added and removed, and the date and payer written on the locked cheque stay (answer to Q60).
 * «بابت» is chosen from the purposes of its group, may be changed after it was chosen, or typed outside the list; a text that is not
 * in the list is added to it when the receipt is saved (report no. 6, item 4).
 * Owner's report no. 6 (19 Nov 2026): after saving, «چاپ رسید» shows and prints the receipt (item 21); the Sheba box has «IR» fixed in
 * it (item 16); the kind called «چک بانکی (رمزدار)» is «چک تضمین شده صیاد» (item 18).
 * With kind `CardDeposit` the form is «واریز به کارت بانک» (report no. 6, item 12; the owner's answers to Q68–Q70 of 20 Nov 2026): the
 * money is paid onto the bank card of a petty-cash holder of the company or onto a company bank card that has no bank account, card
 * to card, with a deposit slip or through the company's payment gateway; always in Rial. Its voucher debits the petty cash's account
 * with the holder's تفصیل, or the company card's own معین and تفصیل.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { chequeStateName, createApi, formatAmount, instrumentKindName, normaliseJalali, parseAmount, searchKey, toLatinDigits, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import AmountInput from '@treasury/ui-kit/AmountInput.vue'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import ComboInput from '@treasury/ui-kit/ComboInput.vue'
import ShebaInput from '@treasury/ui-kit/ShebaInput.vue'
import { cardValue, useLookups, type Detail, type Purpose } from './lookups'
import VoucherTable, { type Voucher } from './VoucherTable.vue'
import { depositMethods, forCard, forForeignCurrency, needsSheba } from './depositMethods'
import { amountText, type SavedReceipt } from './savedReceipt'

type Kind = 'Cash' | 'BankDeposit' | 'Instruments' | 'CardDeposit'
type LineKind = 'OrdinaryCheque' | 'BankCheque' | 'GuaranteeCheque'
interface Line {
  /** The saved cheque this line corrects («ویرایش»); empty for a new line. */
  id?: string
  /** A saved cheque on which a step was recorded: shown, not corrected or removed (answer to Q60); with the name of its latest status. */
  locked?: boolean
  state?: string
  key: number; kind: LineKind; number: string; dueDate: string; currency: string; amount: string; rate: string
  bankId: string; branchName: string; branchCode: string; accountNumber: string; accountHolder: string; city: string; outOfTown: boolean; sayadId: string
  g1Name: string; g1Code: string; g2Name: string; g2Code: string
}

const props = withDefaults(defineProps<{
  kind: Kind; apiBase?: string; locale?: Locale; token?: () => string | undefined
  /** The cheque being replaced; `receiptId` is its own receipt, which gives the new form its defaults. */
  replaces?: { id: string; number: string; amount: number; payerCode: string; receiptId?: string | null; currency?: string; originalAmount?: number; state?: string }
  /** The saved receipt being corrected. */
  edit?: SavedReceipt
}>(), { apiBase: '', locale: 'fa', token: undefined, replaces: undefined, edit: undefined })
const emit = defineEmits<{ saved: [id: string]; cancelled: []; print: [id: string] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const L = useLookups(api)

// Owner's report no. 6, item 18: «چک تضمین شده صیاد» replaces «چک رمزدار بانکی» (the kind BankCheque); its course is that of every cheque.
const lineKinds: { value: LineKind; fa: string; en: string }[] = [
  { value: 'OrdinaryCheque', fa: 'چک', en: 'Cheque' },
  { value: 'BankCheque', fa: instrumentKindName('BankCheque', 'fa'), en: instrumentKindName('BankCheque', 'en') },
  { value: 'GuaranteeCheque', fa: 'چک تضمینی (انتظامی)', en: 'Guarantee cheque' },
  // Promissory notes have a form of their own (owner's report no. 4, item 13).
]
const isCheque = (k: LineKind) => k.endsWith('Cheque')
/**
 * The kinds of deposit the chosen account can take: a foreign-currency account takes cash deposits and bank transfers only (owner's
 * report no. 4, item 7); card to card needs an account with a bank card (owner, 18 Nov 2026).
 */
const accountHasCard = computed(() => (L.bankAccounts.value.find(a => a.id === money1.accountId)?.cards?.length ?? 0) > 0)
const methodChoices = computed<Choice[]>(() => depositMethods
  .filter(m => !m.retired && (props.kind === 'CardDeposit'
    // A deposit onto a bank card: card to card, a deposit slip or the company's gateway (owner's answer to Q70).
    ? forCard(m.value)
    : (accountCurrency.value === 'IRR' || forForeignCurrency(m.value)) && (m.value !== 'CardToCard' || accountHasCard.value)))
  .map(m => ({ value: m.value, label: fa.value ? m.fa : m.en })))
const isGuarantee = (k: LineKind) => k.startsWith('Guarantee')
/** The name of a currency for the words under an amount box: «ریال», or the currency's name from Basic Information. */
const unitOf = (code: string) => (code === 'IRR' ? 'ریال' : L.currencies.value.find(c => c.code === code)?.name ?? code)

// ---- header (every kind) ----
const e0 = props.edit
const header = reactive({
  date: e0?.date ?? todayJalali(), payer: e0?.payerDetailCode ?? props.replaces?.payerCode ?? '', groupId: e0?.purposeGroupId ?? '',
  purpose: e0?.purpose ?? (props.replaces ? `تعویض چک ${props.replaces.number}` : ''),
  ref1: e0?.reference1 ?? '', ref2: e0?.reference2 ?? '', departmentId: e0?.departmentId ?? '', salesProceeds: e0?.salesProceeds ?? false, note: e0?.note ?? '' })
const payers = ref<Detail[]>([])
/** Owner, 14 Nov 2026 (answer to Q22): payers are customers and persons or companies. */
const PayerRoles = 'Customer,PersonOrCompany'
const purposes = ref<Purpose[]>([])
const group = computed(() => L.groups.value.find(g => g.id === header.groupId))
const refTitle = (id: string | null | undefined) => L.references.value.find(r => r.id === id)?.name
const groupChoices = computed<Choice[]>(() => L.groups.value.filter(g => g.direction === 'Receipt' && (g.isActive || g.id === header.groupId)).map(g => ({ value: g.id, label: g.name, hint: String(g.number) })))
const purposeNames = computed(() => purposes.value.filter(p => p.isActive).map(p => p.name))
const payerChoices = computed<Choice[]>(() => payers.value.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive })))
const departmentChoices = computed<Choice[]>(() => L.departments.value.filter(d => d.isActive).map(d => ({ value: d.id, label: d.name })))
/**
 * «نوع حساب تفصیل» (owner, 18 Nov 2026): the types of تفصیل of the accounting system that may pay; choosing one fills the payer list
 * with the accounts of that type, to pick directly or search within.
 */
const payerType = ref('')
const payerTypeChoices = computed<Choice[]>(() => L.detailTypes.value
  .filter(x => x.role !== null && PayerRoles.split(',').includes(x.role))
  .map(x => ({ value: x.type, label: x.type })))
let searchTimer: ReturnType<typeof setTimeout> | undefined
function onPayerSearch(text: string) {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(async () => { payers.value = await L.searchDetails(text, header.payer, PayerRoles, payerType.value || undefined) }, 250)
}
watch(payerType, async type => {
  // A payer of another type than the one now chosen is no longer the choice.
  if (type && header.payer && !payerFixed.value && payers.value.find(d => d.code === header.payer)?.type !== type) header.payer = ''
  clearTimeout(searchTimer)
  payers.value = await L.searchDetails('', header.payer || undefined, PayerRoles, type || undefined)
})
/** A cheque with a step recorded on it carries the receipt's date and payer, so they stay (answer to Q60). */
const anyLocked = computed(() => lines.value.some(l => l.locked))
/** A replacement, and the correction of a replacement receipt, keep the payer of the replaced cheque. */
const payerFixed = computed(() => !!props.replaces || !!props.edit?.replacedInstrumentId || anyLocked.value)
async function loadPurposes(id: string) {
  purposes.value = []
  if (!id) return
  const r = await api.get<Purpose[]>(`/api/reference/purposes?groupId=${id}`)
  if (r.ok && header.groupId === id) purposes.value = r.value
}
watch(() => header.groupId, id => void loadPurposes(id))

// ---- cash and bank deposit ----
const money1 = reactive({
  // A card deposit names its card by kind and id (a petty-cash holder's card, or a company card).
  accountId: (e0?.kind === 'CardDeposit' ? cardValue(e0.accountKind, e0.accountId) : e0?.accountId) ?? '', method: e0?.method ?? '', tracking: e0?.trackingNumber ?? '', amount: amountText(e0?.amount), rate: amountText(e0?.rateToRial),
  sheba: e0?.payerSheba ?? '' })
const deskChoices = computed<Choice[]>(() => L.desks.value.filter(d => d.isActive).map(d => ({ value: d.id, label: d.name, hint: d.currencyCode })))
const bankAccountChoices = computed<Choice[]>(() => L.bankAccounts.value.filter(a => a.isActive).map(a => ({ value: a.id, label: L.accountTitle(a), hint: a.currencyCode })))
/** The cards of «واریز به کارت بانک», the petty-cash holders' first; a card switched off stays only on the receipt that already names it. */
const cardChoices = computed<Choice[]>(() => L.cards.value.filter(c => c.isActive || c.value === money1.accountId)
  .map(c => ({ value: c.value, label: L.cardTitle(c, fa.value), muted: !c.isActive })))
const chosenCard = computed(() => L.cards.value.find(c => c.value === money1.accountId))
const accountCurrency = computed(() => props.kind === 'CardDeposit' ? 'IRR' :
  (props.kind === 'Cash' ? L.desks.value.find(d => d.id === money1.accountId)?.currencyCode : L.bankAccounts.value.find(a => a.id === money1.accountId)?.currencyCode) ?? 'IRR')
// The kind of deposit is chosen by the user (no default); a kind the newly chosen account cannot take is cleared.
watch(methodChoices, () => { if (money1.method && !methodChoices.value.some(c => c.value === money1.method)) money1.method = '' })
const rialOf = (amount: string, currency: string, rate: string) => {
  const a = parseAmount(amount)
  if (a === null) return null
  if (currency === 'IRR') return a
  const r = parseAmount(rate)
  return r === null ? null : Math.round(a * r)
}

// ---- cheques and notes ----
let nextKey = 1
const blankLine = (): Line => ({
  key: nextKey++, kind: 'OrdinaryCheque', number: '', dueDate: '', currency: 'IRR', amount: '', rate: '', bankId: '', branchName: '', branchCode: '',
  accountNumber: '', accountHolder: '', city: '', outOfTown: false, sayadId: '', g1Name: '', g1Code: '', g2Name: '', g2Code: '',
})
/** The lines of a saved cheque receipt, as the form shows them. */
function savedLines(r: SavedReceipt): Line[] {
  return (r.instruments ?? []).map(i => {
    const d = i.details
    const g = d?.guarantors ?? []
    return {
      ...blankLine(), id: i.id, locked: i.asReceived === false, state: i.state, kind: (lineKinds.some(k => k.value === i.kind) ? i.kind : 'OrdinaryCheque') as LineKind, number: i.number ?? '', dueDate: i.dueDate ?? '',
      currency: i.currency, amount: amountText(i.amount), rate: amountText(i.rateToRial), bankId: d?.bankId ?? '', branchName: d?.branchName ?? '', branchCode: d?.branchCode ?? '',
      accountNumber: d?.accountNumber ?? '', accountHolder: d?.accountHolder ?? '', city: d?.city ?? '', outOfTown: d?.outOfTown ?? false, sayadId: d?.sayadId ?? '',
      g1Name: g[0]?.name ?? '', g1Code: g[0]?.nationalCode ?? '', g2Name: g[1]?.name ?? '', g2Code: g[1]?.nationalCode ?? '',
    }
  })
}
const lines = ref<Line[]>(e0?.kind === 'Instruments' && e0.instruments?.length ? savedLines(e0) : [blankLine()])
const bankChoices = computed<Choice[]>(() => L.banks.value.filter(b => b.isActive !== false).map(b => ({ value: b.id, label: b.name })))
const currencyChoices = computed<Choice[]>(() => L.currencies.value.filter(c => c.code).map(c => ({ value: c.code!, label: c.name, hint: c.code! })))
const cityChoices = computed<Choice[]>(() => L.cities.value.map(c => ({ value: c.name, label: c.name })))
/**
 * «خارج از مرکز» (owner, 18 Nov 2026): ticked by the system when the city of the cheque's bank is not the city of the company's legal
 * address (Basic Information → «مشخصات شرکت»); the server decides the same way. Without either city the user ticks it by hand.
 */
const cityKey = (s: string) => s.replace(/[\s\u200c]/g, '').replace(/ي/g, 'ی').replace(/ك/g, 'ک')
const outOfTownIsAutomatic = (l: Line) => L.companyCity.value !== '' && l.city !== ''
function setCity(l: Line, city: string) {
  l.city = city
  if (outOfTownIsAutomatic(l)) l.outOfTown = cityKey(city) !== cityKey(L.companyCity.value)
}
const kindChoices = computed<Choice[]>(() => lineKinds.filter(k => !props.replaces || !isGuarantee(k.value)).map(k => ({ value: k.value, label: fa.value ? k.fa : k.en })))

/** «ثبت چک بعدی»: the next line starts as a copy of the last one (same bank, account, due date…), with the next cheque number. */
function nextLine() {
  const last = lines.value[lines.value.length - 1]
  if (!last) return void lines.value.push(blankLine())
  const digits = toLatinDigits(last.number)
  const next = /^\d+$/.test(digits) ? String(Number(digits) + 1).padStart(digits.length, '0') : ''
  lines.value.push({ ...last, id: undefined, locked: false, state: undefined, key: nextKey++, number: next, sayadId: '' })
}
function removeLine(i: number) {
  lines.value.splice(i, 1)
  if (!lines.value.length) lines.value.push(blankLine())
}
/** Current (روز) when due on or before the receipt date; post-dated (مدت‌دار) after it. Manual: «چک روز … به صورت اتوماتیک تیک می خورد». */
function timing(l: Line): 'current' | 'later' | null {
  const due = normaliseJalali(l.dueDate)
  const date = normaliseJalali(header.date)
  if (!due || !date) return null
  return due > date ? 'later' : 'current'
}
const linesRial = computed(() => lines.value.map(l => rialOf(l.amount, l.currency, l.rate)))
const total = computed(() => props.kind === 'Instruments'
  ? linesRial.value.reduce<number>((s, v) => s + (v ?? 0), 0)
  : rialOf(money1.amount, accountCurrency.value, money1.rate) ?? 0)

// ---- save ----
const saving = ref(false)
/** The receipt just saved, for «چاپ رسید» beside the message (report no. 6, item 21). */
const lastSaved = ref<{ id: string; number: string } | null>(null)
const message = ref<{ kind: 'error' | 'warning' | 'ok'; text: string } | null>(null)
const show = (kind: 'error' | 'warning' | 'ok', text: string) => { message.value = { kind, text } }

/**
 * A purpose typed here, or chosen and then changed, that the group does not list yet is added to it (decision of 10 Nov 2026; owner's
 * report no. 6, item 4), so the next receipt or payment finds it in the list. A purpose the list already has (also one switched off
 * there) is simply used.
 */
async function addPurposeIfNew(): Promise<boolean> {
  const name = header.purpose.trim()
  if (!name || !header.groupId || purposes.value.some(p => searchKey(p.name) === searchKey(name))) return true
  const r = await api.post<string>('/api/reference/purposes', { groupId: header.groupId, name })
  if (!r.ok && r.code === 'Purpose.NameTaken') return true
  if (!r.ok) { show('error', translate(r.code, props.locale)); return false }
  purposes.value.push({ id: r.value, groupId: header.groupId, name, isActive: true })
  return true
}

function headerBody() {
  return {
    date: normaliseJalali(header.date) ?? header.date, payerDetailCode: header.payer, purposeGroupId: header.groupId || '00000000-0000-0000-0000-000000000000',
    purpose: header.purpose.trim(), reference1: header.ref1 || null, reference2: header.ref2 || null, departmentId: header.departmentId || null,
    salesProceeds: header.salesProceeds, note: header.note || null,
  }
}

function lineBody(l: Line) {
  const guarantors = [[l.g1Name, l.g1Code], [l.g2Name, l.g2Code]].filter(([n, c]) => n!.trim() || c!.trim())
    .map(([n, c]) => ({ name: n!.trim(), nationalCode: toLatinDigits(c!.trim()) || null }))
  return {
    kind: l.kind, number: toLatinDigits(l.number.trim()), dueDate: isGuarantee(l.kind) && !l.dueDate ? null : normaliseJalali(l.dueDate) ?? l.dueDate,
    amount: { amount: parseAmount(l.amount) ?? 0, currency: l.currency, rateToRial: l.currency === 'IRR' ? null : parseAmount(l.rate) },
    bankId: isCheque(l.kind) && l.bankId ? l.bankId : null, branchName: l.branchName || null, branchCode: l.branchCode || null,
    accountNumber: toLatinDigits(l.accountNumber) || null, accountHolder: l.accountHolder || null, city: l.city || null, outOfTown: l.outOfTown,
    sayadId: toLatinDigits(l.sayadId.trim()) || null, guarantors: isCheque(l.kind) ? [] : guarantors, id: l.id ?? null,
  }
}

/** The path of the request: a new receipt, the replacement of a cheque, or the saved receipt being corrected. */
const kindPath = computed(() => (props.kind === 'Cash' ? 'cash' : props.kind === 'BankDeposit' ? 'bank' : props.kind === 'CardDeposit' ? 'card' : 'instruments'))

/**
 * The request the form makes, or the code of the first thing missing (checked again by the server). `path` records a new receipt (or
 * previews the voucher); a correction is sent to `put` with the version the user saw.
 */
function request(): { path: string; body: Record<string, unknown>; put?: string } | { error: string } {
  const put = props.edit ? `/api/cashflow/receipts/${props.edit.id}/${kindPath.value}` : undefined
  if (!normaliseJalali(header.date)) return { error: 'FiscalDate.Invalid' }
  if (!header.payer) return { error: 'Receipt.PayerRequired' }
  if (!header.groupId) return { error: 'Receipt.PurposeGroupRequired' }
  if (!header.purpose.trim()) return { error: 'Receipt.PurposeRequired' }
  if (props.kind === 'Instruments') {
    for (const l of lines.value) {
      if (parseAmount(l.amount) === null) return { error: 'Money.NotPositive' }
      if (l.dueDate && !normaliseJalali(l.dueDate)) return { error: 'FiscalDate.Invalid' }
      if (l.currency !== 'IRR' && parseAmount(l.rate) === null) return { error: 'Money.InvalidRate' }
      if (isCheque(l.kind) && !l.sayadId.trim()) return { error: 'Receipt.SayadIdRequired' }
    }
    const path = props.replaces ? `/api/cashflow/instruments/${props.replaces.id}/replace` : '/api/cashflow/receipts/instruments'
    return { path, put, body: { header: headerBody(), instruments: lines.value.map(lineBody) } }
  }
  if (!money1.accountId) return { error: 'Receipt.AccountNotFound' }
  const amount = parseAmount(money1.amount)
  if (amount === null) return { error: 'Money.NotPositive' }
  const rate = accountCurrency.value === 'IRR' ? null : parseAmount(money1.rate)
  if (accountCurrency.value !== 'IRR' && rate === null) return { error: 'Money.InvalidRate' }
  if (props.kind === 'Cash') return { path: '/api/cashflow/receipts/cash', put, body: { header: headerBody(), cashDeskId: money1.accountId, amount, rateToRial: rate } }
  if (!money1.method) return { error: 'Receipt.DepositMethodRequired' }
  if (props.kind === 'CardDeposit') {
    const card = chosenCard.value
    if (!card) return { error: 'Receipt.AccountNotFound' }
    return { path: '/api/cashflow/receipts/card', put, body: { header: headerBody(), cardKind: card.kind, cardId: card.id, method: money1.method, trackingNumber: money1.tracking || null, amount } }
  }
  if (needsSheba(money1.method) && !money1.sheba.trim()) return { error: 'Receipt.ShebaRequired' }
  return { path: '/api/cashflow/receipts/bank', put, body: {
    header: headerBody(), bankAccountId: money1.accountId, method: money1.method, trackingNumber: money1.tracking || null, amount, rateToRial: rate,
    payerSheba: needsSheba(money1.method) ? money1.sheba : null } }
}

// ---- the voucher at the bottom of the form (nothing is saved) ----
const preview = ref<Voucher | null>(null)
const previewHint = ref('')
/** The voucher of a replacement receipt names the replaced cheque; while such a receipt is corrected it is shown after saving, in the list. */
const noPreview = computed(() => !!props.edit?.replacedInstrumentId)
let previewTimer: ReturnType<typeof setTimeout> | undefined
let previewRun = 0
watch([header, money1, lines], () => {
  clearTimeout(previewTimer)
  previewTimer = setTimeout(refreshPreview, 400)
}, { deep: true })
async function refreshPreview() {
  const run = ++previewRun
  if (noPreview.value) { preview.value = null; previewHint.value = ''; return }
  const req = request()
  if ('error' in req) { preview.value = null; previewHint.value = translate(req.error, props.locale); return }
  const r = await api.post<Voucher>(`${req.path}/voucher-preview`, req.body)
  if (run !== previewRun) return
  preview.value = r.ok ? r.value : null
  previewHint.value = r.ok ? '' : translate(r.code, props.locale)
}

async function save() {
  message.value = null
  lastSaved.value = null
  const req = request()
  if ('error' in req) return show('error', translate(req.error, props.locale))
  const { path, body, put } = req

  saving.value = true
  try {
    if (!(await addPurposeIfNew())) return
    const r = put ? await api.put<string>(put, { ...body, version: props.edit!.version }) : await api.post<string>(path, body)
    if (!r.ok) return show('error', translate(r.code, props.locale))
    const saved = await api.get<{ number: number; fiscalYear: number }>(`/api/cashflow/receipts/${r.value}`)
    const no = saved.ok ? (fa.value ? saved.value.number.toLocaleString('fa-IR') : String(saved.value.number)) : ''
    const text = put ? t(`ویرایش رسید شمارهٔ ${no} ثبت شد.`, `Receipt no. ${no} corrected.`) : t(`رسید شمارهٔ ${no} ثبت شد.`, `Receipt no. ${no} saved.`)
    if (r.warnings.length) show('warning', `${text} ${r.warnings.map(w => translate(w, props.locale)).join(' ')}`)
    else show('ok', text)
    lastSaved.value = { id: r.value, number: no }
    Object.assign(money1, { amount: '', rate: '', tracking: '', sheba: '' })
    Object.assign(header, { purpose: '', ref1: '', ref2: '', note: '' })
    lines.value = [blankLine()]
    emit('saved', r.value)
  } finally {
    saving.value = false
  }
}

function onKey(e: KeyboardEvent) {
  if (e.key === 'Enter' && e.ctrlKey) { e.preventDefault(); void save() }
}

/**
 * Report no. 6, item 11: the replacement opens with what the old cheque's receipt said — purpose group, purpose, department,
 * references — and with the old cheque's amount and currency on the first line; the user changes what differs.
 */
async function fillFromReplacedCheque() {
  const old = props.replaces
  if (!old) return
  const first = lines.value[0]
  if (first && old.originalAmount !== undefined) Object.assign(first, { amount: amountText(old.originalAmount), currency: old.currency ?? 'IRR' })
  if (!old.receiptId) return
  const r = await api.get<SavedReceipt>(`/api/cashflow/receipts/${old.receiptId}`)
  if (!r.ok) return
  const o = r.value
  const line = o.instruments?.find(i => i.id === old.id)
  Object.assign(header, { groupId: o.purposeGroupId, purpose: o.purpose, departmentId: o.departmentId ?? '', ref1: o.reference1 ?? '', ref2: o.reference2 ?? '', salesProceeds: o.salesProceeds })
  if (first && line?.rateToRial) first.rate = amountText(line.rateToRial)
}

onMounted(async () => {
  await L.load()
  payers.value = await L.searchDetails('', header.payer || undefined, PayerRoles)
  if (header.groupId) void loadPurposes(header.groupId)
  await fillFromReplacedCheque()
})
</script>

<template>
  <form class="tr-form" @keydown="onKey" @submit.prevent="save">
    <fieldset>
      <legend>{{ edit ? t(`ویرایش رسید شمارهٔ ${edit.number.toLocaleString('fa-IR', { useGrouping: false })}`, `Correct receipt no. ${edit.number}`) : t('مشخصات دریافت', 'Receipt') }}</legend>
      <label class="req">{{ t('تاریخ دریافت', 'Date') }} <input v-model="header.date" placeholder="1405/08/23" dir="ltr" :disabled="anyLocked" :class="{ ro: anyLocked }"
        :title="anyLocked ? t('روی یکی از چک‌های این رسید مرحله‌ای ثبت شده است؛ تاریخ و پرداخت‌کنندهٔ رسید تغییر نمی‌کند.', 'A cheque of this receipt has a step recorded on it; the date and payer stay.') : undefined" /></label>
      <label v-if="payerTypeChoices.length && !payerFixed">{{ t('نوع حساب تفصیل', 'Type of detail account') }}
        <SearchSelect v-model="payerType" :options="payerTypeChoices" :empty-label="t('— همه —', '— all —')" :aria-label="t('نوع حساب تفصیل', 'Type of detail account')" />
      </label>
      <label class="wide">{{ t('پرداخت کننده (حساب تفصیلی)', 'Payer (detail account)') }}
        <SearchSelect v-model="header.payer" :options="payerChoices" remote required :disabled="payerFixed" :aria-label="t('پرداخت کننده', 'Payer')"
                      :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @search="onPayerSearch" />
      </label>
      <label>{{ t('گروه بابت', 'Purpose group') }}
        <SearchSelect v-model="header.groupId" :options="groupChoices" required :aria-label="t('گروه بابت', 'Purpose group')"
                      :none-text="t('گروه بابت دریافتی تعریف نشده است', 'No receipt purpose group')" />
      </label>
      <label class="wide">{{ t('بابت', 'Purpose') }}
        <ComboInput v-model="header.purpose" :options="purposeNames" required :maxlength="150" :aria-label="t('بابت', 'Purpose')"
                    :placeholder="t('از فهرست انتخاب کنید، آن را تغییر دهید یا بنویسید', 'Choose from the list, change it, or type')"
                    :none-text="header.groupId ? t('این گروه هنوز بابتی ندارد؛ بابت را بنویسید', 'No purpose in this group yet; type one') : t('ابتدا گروه بابت را انتخاب کنید', 'Choose the purpose group first')"
                    :new-hint="header.groupId ? t('بابت تازه: با ثبت رسید به فهرست بابت‌های این گروه اضافه می‌شود.', 'New purpose: saving the receipt adds it to this group\'s list.') : ''" />
      </label>
      <label v-if="group?.reference1Id">{{ refTitle(group.reference1Id) ?? t('عطف ۱', 'Reference 1') }} <input v-model="header.ref1" /></label>
      <label v-if="group?.reference2Id">{{ refTitle(group.reference2Id) ?? t('عطف ۲', 'Reference 2') }} <input v-model="header.ref2" /></label>
      <!-- «واحد سازمانی دریافت کننده» (owner, 15 Nov 2026: keep it, with this title): the department that received the money (manual, form «دریافت چک و اسناد دریافتنی»). Shown only when departments are
           defined in Basic Information; Iran Nazho Co. defines none (owner, 13 Nov 2026). -->
      <label v-if="departmentChoices.length">{{ t('واحد سازمانی دریافت کننده', 'Receiving department') }}
        <SearchSelect v-model="header.departmentId" :options="departmentChoices" :empty-label="t('— ندارد —', '— none —')" :aria-label="t('واحد سازمانی دریافت کننده', 'Receiving department')" />
      </label>
      <label class="tr-check"><input v-model="header.salesProceeds" type="checkbox" /> {{ t('وجوه حاصل فروش', 'Sales proceeds') }}</label>
      <label class="wide">{{ t('توضیحات', 'Note') }} <input v-model="header.note" maxlength="500" /></label>
    </fieldset>

    <fieldset v-if="kind !== 'Instruments'">
      <legend>{{ kind === 'Cash' ? t('دریافت نقدی', 'Cash') : kind === 'CardDeposit' ? t('واریز به کارت بانک', 'Paid onto a bank card') : t('واریز به بانک', 'Paid into a bank account') }}</legend>
      <label v-if="kind === 'Cash'" class="wide">{{ t('صندوق', 'Cash desk') }}
        <SearchSelect v-model="money1.accountId" :options="deskChoices" required :aria-label="t('صندوق', 'Cash desk')" :none-text="t('صندوقی تعریف نشده است', 'No cash desk')" />
      </label>
      <!-- «واریز به کارت بانک» (report no. 6, item 12): the card of a petty-cash holder, or a company card without a bank account. -->
      <template v-else-if="kind === 'CardDeposit'">
        <label class="wide">{{ t('کارت بانک (تنخواه‌گردان یا کارت شرکت)', 'Bank card (petty-cash holder or company card)') }}
          <SearchSelect v-model="money1.accountId" :options="cardChoices" required :aria-label="t('کارت بانک', 'Bank card')" :none-text="t('کارت بانکی تعریف نشده است', 'No bank card')" />
        </label>
        <label class="wide">{{ t('نوع واریز', 'Kind of deposit') }}
          <SearchSelect v-model="money1.method" :options="methodChoices" required :aria-label="t('نوع واریز', 'Kind of deposit')" />
        </label>
        <label>{{ money1.method === 'DepositSlip' ? t('شماره فیش', 'Slip no.') : t('شماره پیگیری / مرجع', 'Tracking / reference no.') }}
          <input v-model="money1.tracking" dir="ltr" />
        </label>
        <p v-if="!cardChoices.length" class="muted small wide">
          {{ t('کارت بانکی تعریف نشده است. کارت تنخواه‌گردان در «اطلاعات پایه ← تنخواه گردان‌ها» (نحوهٔ نگهداری وجوه: کارت بانک) و کارت بانک شرکت که حساب بانکی ندارد در «اطلاعات پایه ← کارت‌های بانک شرکت (بدون حساب بانکی)» تعریف می‌شود.',
            'No bank card is defined. A petty-cash holder\'s card is entered in Basic Information → Petty cash (kept on a bank card), a company card without a bank account in Basic Information → Company bank cards.') }}
        </p>
        <p v-else-if="chosenCard" class="muted small wide">
          {{ chosenCard.kind === 'PettyCashCard'
            ? t('این وجه به تنخواهِ همین تنخواه‌گردان اضافه می‌شود: سند، حساب تنخواه گردان را با تفصیل او بدهکار می‌کند.', "This money is added to this holder's petty cash: the voucher debits the petty-cash account with the holder's detail account.")
            : t('سند، حساب معین و تفصیلِ همین کارت را بدهکار می‌کند.', "The voucher debits this card's ledger and detail account.") }}
        </p>
      </template>
      <template v-else>
        <label class="wide">{{ t('حساب بانکی', 'Bank account') }}
          <SearchSelect v-model="money1.accountId" :options="bankAccountChoices" required :aria-label="t('حساب بانکی', 'Bank account')" />
        </label>
        <label class="wide">{{ t('نوع واریز', 'Kind of deposit') }}
          <SearchSelect v-model="money1.method" :options="methodChoices" required :aria-label="t('نوع واریز', 'Kind of deposit')" />
        </label>
        <label v-if="needsSheba(money1.method)" class="wide req">{{ t('شماره شبای واریزکننده (حساب مبدأ حواله)', "Payer's Sheba (the transfer's source account)") }}
          <!-- Owner's report no. 6, item 16: «IR» is fixed in the box; the 24 digits are typed after it. -->
          <ShebaInput v-model="money1.sheba" :locale="locale" :aria-label="t('شماره شبای واریزکننده', 'Payer\'s Sheba')" />
        </label>
        <label>{{ money1.method === 'DepositSlip' ? t('شماره فیش', 'Slip no.') : t('شماره پیگیری / مرجع', 'Tracking / reference no.') }}
          <input v-model="money1.tracking" dir="ltr" />
        </label>
        <p v-if="money1.accountId && accountCurrency === 'IRR' && !accountHasCard" class="muted small wide">
          {{ t('این حساب کارت بانک ندارد؛ برای ثبت «کارت به کارت» ابتدا کارت بانک را در اطلاعات پایه ← حساب‌های بانکی تعریف کنید.',
            'This account has no bank card; define one in Basic Information → Bank accounts to record card-to-card deposits.') }}
        </p>
      </template>
      <label class="req">{{ t('مبلغ', 'Amount') }} ({{ accountCurrency }}) <AmountInput v-model="money1.amount" :decimals="accountCurrency === 'IRR' ? 0 : 2" :unit="unitOf(accountCurrency)" /></label>
      <label v-if="accountCurrency !== 'IRR'" class="req">{{ t('نرخ ارز (ریال)', 'Rate (Rial)') }} <AmountInput v-model="money1.rate" :decimals="4" unit="ریال" /></label>
      <label v-if="accountCurrency !== 'IRR'">{{ t('هم‌ارز ریالی', 'Rial equivalent') }} <input class="ro num" :value="money(total)" readonly /></label>
    </fieldset>

    <template v-else>
      <!-- A cheque on which a step was recorded is shown locked: it is not corrected or removed (owner's answer to Q60). -->
      <fieldset v-for="(l, i) in lines" :key="l.key" :disabled="l.locked" :class="{ locked: l.locked }">
        <legend>
          {{ t(`ردیف ${(i + 1).toLocaleString('fa-IR')}`, `Line ${i + 1}`) }}
          <span v-if="timing(l) === 'current'" class="tr-tag current">{{ t('روز', 'Current') }}</span>
          <span v-else-if="timing(l) === 'later'" class="tr-tag later">{{ t('مدت‌دار', 'Post-dated') }}</span>
          <span v-if="l.locked" class="tr-tag cancelled">{{ t(`قفل – ${chequeStateName(l.state, locale)}: این چک ویرایش یا حذف نمی‌شود`, `Locked – ${chequeStateName(l.state, locale)}: not corrected or removed`) }}</span>
          <button v-else type="button" class="tr-link danger" @click="removeLine(i)">{{ t('حذف ردیف', 'Remove') }}</button>
        </legend>
        <label>{{ t('نوع', 'Kind') }}
          <SearchSelect v-model="l.kind" :options="kindChoices" required :aria-label="t('نوع', 'Kind')" />
        </label>
        <label class="req">{{ t('شماره چک', 'Cheque no.') }} <input v-model="l.number" dir="ltr" /></label>
        <label :class="{ req: !isGuarantee(l.kind) }">{{ t('سررسید', 'Due date') }} <input v-model="l.dueDate" placeholder="1405/09/30" dir="ltr" /></label>
        <label>{{ t('ارز', 'Currency') }}
          <SearchSelect v-model="l.currency" :options="currencyChoices" required :aria-label="t('ارز', 'Currency')" />
        </label>
        <label class="req">{{ t('مبلغ', 'Amount') }} ({{ l.currency }}) <AmountInput v-model="l.amount" :decimals="l.currency === 'IRR' ? 0 : 2" :unit="unitOf(l.currency)" /></label>
        <template v-if="l.currency !== 'IRR'">
          <label class="req">{{ t('نرخ ارز (ریال)', 'Rate (Rial)') }} <AmountInput v-model="l.rate" :decimals="4" unit="ریال" /></label>
          <label>{{ t('هم‌ارز ریالی', 'Rial equivalent') }} <input class="ro num" :value="linesRial[i] === null ? '' : money(linesRial[i]!)" readonly /></label>
        </template>
        <template v-if="isCheque(l.kind)">
          <label>{{ t('بانک', 'Bank') }}
            <SearchSelect v-model="l.bankId" :options="bankChoices" required :aria-label="t('بانک', 'Bank')" />
          </label>
          <label>{{ t('شعبه', 'Branch') }} <input v-model="l.branchName" /></label>
          <label>{{ t('کد شعبه', 'Branch code') }} <input v-model="l.branchCode" dir="ltr" /></label>
          <label>{{ t('شماره حساب', 'Account no.') }} <input v-model="l.accountNumber" dir="ltr" /></label>
          <label class="req">{{ t('شناسهٔ صیاد (۱۶ رقم)', 'Sayad ID (16 digits)') }} <input v-model="l.sayadId" maxlength="16" dir="ltr" inputmode="numeric" /></label>
          <label>{{ t('نام دارندهٔ حساب', 'Account holder') }} <input v-model="l.accountHolder" :placeholder="t('اگر چک شخص دیگری است', 'If someone else\'s cheque')" /></label>
          <label>{{ t('شهر', 'City') }}
            <SearchSelect :model-value="l.city" :options="cityChoices" :empty-label="t('—', '—')" :aria-label="t('شهر', 'City')" @update:model-value="setCity(l, $event)" />
          </label>
          <label class="tr-check" :title="outOfTownIsAutomatic(l) ? t(`خودکار: شهر شرکت «${L.companyCity.value}» است.`, `Automatic: the company is in ${L.companyCity.value}.`) : undefined">
            <input v-model="l.outOfTown" type="checkbox" :disabled="outOfTownIsAutomatic(l)" /> {{ t('خارج از مرکز', 'Out of town') }}
            <span v-if="outOfTownIsAutomatic(l)" class="muted small">{{ t('(خودکار)', '(automatic)') }}</span>
          </label>
          <p v-if="!L.companyCity.value" class="muted small wide">
            {{ t('شهر شرکت در اطلاعات پایه ← مشخصات شرکت ثبت نشده است؛ تا آن زمان «خارج از مرکز» دستی تیک می‌خورد.',
              'The company\'s city is not entered in Basic Information → Company particulars; until then tick "out of town" by hand.') }}
          </p>
        </template>
      </fieldset>
      <div class="tr-buttons">
        <button type="button" class="tr-secondary" @click="nextLine">{{ t('+ ثبت چک بعدی', '+ Next cheque') }}</button>
        <span class="muted">{{ t(`${lines.length.toLocaleString('fa-IR')} ردیف`, `${lines.length} lines`) }}</span>
      </div>
    </template>

    <div class="tr-buttons">
      <strong>{{ t('جمع (ریال):', 'Total (Rial):') }} <span class="num">{{ money(total) }}</span></strong>
      <span v-if="replaces" class="muted">{{ t(`چک قبلی: ${money(replaces.amount)} – تفاوت: ${money(total - replaces.amount)}`,
        `Old cheque: ${money(replaces.amount)} – difference: ${money(total - replaces.amount)}`) }}</span>
      <button type="submit" :disabled="saving">{{ edit ? t('ثبت ویرایش (Ctrl+Enter)', 'Save the correction (Ctrl+Enter)') : t('ثبت رسید (Ctrl+Enter)', 'Save receipt (Ctrl+Enter)') }}</button>
      <button v-if="edit" type="button" class="tr-secondary" :disabled="saving" @click="emit('cancelled')">{{ t('انصراف از ویرایش', 'Leave without saving') }}</button>
    </div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}
      <!-- report no. 6, item 21: the saved receipt is shown and printed -->
      <button v-if="lastSaved && message.kind !== 'error'" type="button" class="tr-secondary" style="margin-inline-start: .75rem" @click="emit('print', lastSaved.id)">
        {{ t(`چاپ رسید شمارهٔ ${lastSaved.number}`, `Print receipt no. ${lastSaved.number}`) }}</button>
    </p>
    <VoucherTable v-if="preview" :voucher="preview" :locale="locale" :account-name="L.accountName" />
    <p v-else-if="noPreview" class="muted small">{{ t('این رسید، رسید تعویض چک است؛ سند آن پس از ثبت ویرایش در «فهرست دریافت‌ها» دیده می‌شود.',
      'This receipt replaced a cheque; its voucher is shown in the receipts list after the correction is saved.') }}</p>
    <p v-else class="muted small">{{ t('سند حسابداری این رسید پس از تکمیل فرم اینجا نمایش داده می‌شود.', 'The voucher of this receipt is shown here once the form is filled in.') }}
      <template v-if="previewHint"> ({{ previewHint }})</template></p>
  </form>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
fieldset.locked { background: #f5f7fa; }
</style>
