<script setup lang="ts">
/**
 * Receipts of a fiscal year, newest first, with a search box. Opening a receipt shows its cheques and notes with their latest state;
 * their next steps are recorded in «عملیات چک‌های دریافتی» (owner's item 14). A cash or bank receipt typed by
 * mistake is cancelled with a reason; a cheque is returned to the payer instead.
 * The receipt's voucher and the vouchers of each cheque's later steps (deposit, collected, bounced) are shown with «تایید سند»
 * (owner's report no. 4, item 8: one voucher per receipt or operation, the treasurer confirms).
 * Owner, 18 Nov 2026: the last column «وضعیت» is the latest status: of a cheque receipt, the latest status of its cheques (with the mark
 * «سررسید چک»); of a cash or bank receipt, recorded or cancelled. The names are the owner's, shared by every screen (ui-kit chequeStatus).
 * A receipt of promissory notes (owner's form «فرم دریافت سفته», 16 Nov 2026) shows its terms and can be printed for the signatures.
 * Owner's report no. 6 (18 Nov 2026): «ویرایش» on an opened receipt while its voucher is not confirmed (items 2 and 6; the parent opens
 * the form of its kind); a box of its own for the receipt number, which finds exactly that number (item 9: typed into the general
 * search box, a number also matched every date and code that contains it).
 * Owner's answers of 19 Nov 2026: «حذف رسید» beside «ویرایش رسید», at the same stage (Q59); «چاپ» of the list with «از تاریخ / تا تاریخ»
 * filled from the list (Q63). Owner, 20 Nov 2026 (answer to Q67): a deleted receipt stays in the list, on a light red ground with «حذف شده»
 * written large; opened, it says who deleted it and when.
 * Owner's report no. 6 as sent on 19 Nov 2026: this is «فهرست جامع دریافت‌ها» (item 12). Item 20: a column «شماره چک», a box that finds
 * a receipt by the number of one of its cheques, and the column in the printed list. Items 14 and 20: a click (or double click) on a
 * row of a cheque or note receipt shows, under the receipt, the cheque card of each of its cheques — the course the cheque has run;
 * a click on a card closes it. Item 21: «چاپ رسید» shows and prints the receipt of any kind (the parent prints it).
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, dueMark, dueWaitsFor, formatAmount, groupChequeStatuses, instrumentKindName, normaliseJalali, receiptNumber, searchKey, todayJalali, toLatinDigits, translate,
  type ChequeStatus, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'
import VoucherTable, { type Voucher } from './VoucherTable.vue'
import { depositMethodName } from './depositMethods'
import ChequeCard from './ChequeCard.vue'
import { stateNameOf } from './cheque'
import PrintRange from './PrintRange.vue'
import { listPage, printPage, rangeText } from './printing'
import { attachmentNames, checkNames, formNames, maturityNames, reasonNames, type NoteChecks, type NoteTerms } from './notes'

interface Instrument {
  id: string; kind: string; number: string | null; bankName: string | null; amount: number; currency: string; amountInRial: number
  dueDate: string | null; state: string; isPostDated: boolean; allowedActions: string[]; version: number; reachedDueDate: boolean; noteIssueDate?: string | null
  details: { branchName: string | null; accountNumber: string | null; sayadId: string | null; onDemand?: boolean; checks?: NoteChecks | null
    guarantors: { name: string; nationalCode: string | null; detailCode?: string | null; phone?: string | null }[] | null } | null
}
interface Receipt {
  id: string; kind: 'Cash' | 'BankDeposit' | 'Instruments' | 'Notes'; fiscalYear: number; number: number; date: string; payerDetailCode: string; payerName: string
  purposeGroupName: string; purpose: string; reference1: string | null; reference2: string | null; departmentId: string | null; salesProceeds: boolean; note: string | null
  accountKind: string | null; accountId: string | null; method: string | null; trackingNumber: string | null; amount: number | null; currency: string | null
  rateToRial: number | null; totalRial: number; instrumentCount: number; state: string; cancelledOn: string | null; cancelReason: string | null
  recordedBy: string; version: number; instruments: Instrument[] | null; replacedInstrumentId: string | null
  /** The latest status of each cheque of a cheque receipt. */
  chequeStatuses: ChequeStatus[] | null
  payerSheba: string | null
  noteTerms: NoteTerms | null; purposeGroupId: string
  /** The code of the rule that closes «ویرایش», or null while the receipt can be corrected (only on an opened receipt). */
  notCorrectable?: string | null
  /** The same for «حذف» (owner's answer to Q59). */
  notDeletable?: string | null
  /** A deleted receipt («حذف شده»): who deleted it, and the computer's date and time then. */
  deletedBy?: string | null; deletedAt?: string | null
}

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
const emit = defineEmits<{ edit: [id: string]; print: [id: string] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => (fa.value ? n.toLocaleString('fa-IR', { useGrouping: false }) : String(n))
const L = useLookups(api)

const thisYear = Number(todayJalali().slice(0, 4))
const year = ref(String(props.fiscalYear ?? thisYear))
const yearChoices = computed<Choice[]>(() => Array.from({ length: 6 }, (_, i) => thisYear - 4 + i).map(y => ({ value: String(y), label: num(y) })))
const kind = ref('')
const kindNames: Record<string, [string, string]> = {
  Cash: ['نقدی', 'Cash'], BankDeposit: ['واریز به بانک', 'Bank deposit'], Instruments: ['چک', 'Cheques'], Notes: ['سفته', 'Promissory notes'],
}
/** Receipts that bring in instruments: cheques, or promissory notes (which have a receipt of their own). */
const hasInstruments = (r: Receipt) => r.kind === 'Instruments' || r.kind === 'Notes'
const kindChoices = computed<Choice[]>(() => Object.entries(kindNames).map(([value, [f, e]]) => ({ value, label: fa.value ? f : e })))
/** The «وضعیت» column: the cheques of a cheque receipt grouped by their latest status. */
const statusGroups = (r: Receipt) => groupChequeStatuses(r.chequeStatuses, props.locale, r.kind === 'Notes')
const name = (m: Record<string, [string, string]>, k: string | null | undefined) => (k && m[k] ? (fa.value ? m[k][0] : m[k][1]) : k ?? '')

const receipts = ref<Receipt[]>([])
const search = ref('')
/** «شماره رسید»: exactly this number (report no. 6, item 9). */
const numberSearch = ref('')
/** «شماره چک» (item 20): the receipts holding a cheque or note whose number contains these digits. */
const chequeSearch = ref('')
const plain = (text: string) => toLatinDigits(text).replace(/[^0-9A-Za-z]/g, '')
const chequeNumbers = (r: Receipt) => (r.chequeStatuses ?? []).map(c => c.number ?? '').filter(Boolean)
const selected = ref<Receipt | null>(null)
const message = ref<{ kind: 'error' | 'ok'; text: string } | null>(null)
const show = (k: 'error' | 'ok', text: string) => { message.value = { kind: k, text } }

const shown = computed(() => {
  const key = searchKey(search.value)
  const wanted = receiptNumber(numberSearch.value)
  const cheque = plain(chequeSearch.value)
  return receipts.value.filter(r => (wanted === null || r.number === wanted)
    && (!cheque || chequeNumbers(r).some(n => plain(n).includes(cheque)))
    && (!key || searchKey(`${r.payerName} ${r.payerDetailCode} ${r.purpose} ${r.purposeGroupName} ${r.trackingNumber ?? ''} ${r.date} ${
      statusGroups(r).map(g => g.name + (g.due ? ` ${dueMark(props.locale)}` : '')).join(' ')}`).includes(key)))
})
const totalShown = computed(() => shown.value.filter(r => r.state === 'Recorded').reduce((s, r) => s + r.totalRial, 0))
/** The computer's date and time of a deletion, in Iran's time. */
const when = (iso: string | null | undefined) => (iso
  ? new Intl.DateTimeFormat(fa.value ? 'fa-IR' : 'en-GB', { dateStyle: 'short', timeStyle: 'short', timeZone: 'Asia/Tehran' }).format(new Date(iso)) : '')

function accountName(r: Receipt) {
  if (r.accountKind === 'CashDesk') return L.desks.value.find(d => d.id === r.accountId)?.name ?? ''
  const a = L.bankAccounts.value.find(x => x.id === r.accountId)
  return a ? L.accountTitle(a) : ''
}

async function load() {
  const q = new URLSearchParams({ year: year.value })
  if (kind.value) q.set('kind', kind.value)
  const r = await api.get<Receipt[]>(`/api/cashflow/receipts?${q}`)
  if (r.ok) receipts.value = r.value
  else show('error', translate(r.code, props.locale))
}

async function open(id: string) {
  const [r, v] = await Promise.all([api.get<Receipt>(`/api/cashflow/receipts/${id}`), api.get<Voucher>(`/api/cashflow/receipts/${id}/voucher`)])
  if (r.ok) selected.value = r.value
  voucher.value = v.ok ? v.value : null
  for (const k of Object.keys(steps)) delete steps[k]
}

// ---- items 14 and 20: the cheque cards of the row clicked, under the opened receipt ----
const cards = ref<string[]>([])
const cardKey = ref(0)
const bankAccountTitle = (id: string | null | undefined) => { const a = L.bankAccounts.value.find(x => x.id === id); return a ? L.accountTitle(a) : '' }
/** A click or a double click on a row opens the receipt and, of a cheque or note receipt, the card of each of its cheques. */
async function openRow(r: Receipt) {
  cards.value = hasInstruments(r) && r.state !== 'Deleted' ? (r.chequeStatuses ?? []).map(c => c.id).filter((id): id is string => !!id) : []
  cardKey.value++
  await open(r.id)
}
/** The second press of a double click would select a word of the row. */
const noSelectOnDoubleClick = (e: MouseEvent) => { if (e.detail > 1) e.preventDefault() }
function showCard(id: string) {
  if (!cards.value.includes(id)) cards.value = [...cards.value, id]
  cardKey.value++
}

// ---- a receipt of promissory notes: its terms ----
const terms = computed(() => (selected.value?.kind === 'Notes' ? selected.value.noteTerms : null))
const ticked = (c: NoteChecks | null | undefined) => checkNames.filter(([k]) => c?.[k]).map(([, f, e]) => t(f, e)).join('، ')

// ---- vouchers ----
const voucher = ref<Voucher | null>(null)
const steps = reactive<Record<string, Voucher[]>>({})
const confirming = ref(false)
async function confirmReceiptVoucher() {
  const r0 = selected.value
  if (!r0 || confirming.value) return
  confirming.value = true
  const r = await api.post<string>(`/api/cashflow/receipts/${r0.id}/voucher/confirm`, {})
  confirming.value = false
  if (!r.ok) return show('error', translate(r.code, props.locale))
  show('ok', t('سند رسید تایید شد.', 'The receipt\'s voucher is confirmed.'))
  await open(r0.id)
}
async function loadSteps(id: string) {
  const r = await api.get<Voucher[]>(`/api/cashflow/instruments/${id}/vouchers`)
  if (r.ok) steps[id] = r.value
  else show('error', translate(r.code, props.locale))
}
function toggleSteps(id: string) {
  if (steps[id]) delete steps[id]
  else void loadSteps(id)
}
async function confirmStep(id: string, step: number) {
  if (confirming.value) return
  confirming.value = true
  const r = await api.post<string>(`/api/cashflow/instruments/${id}/vouchers/${step}/confirm`, {})
  confirming.value = false
  if (!r.ok) return show('error', translate(r.code, props.locale))
  show('ok', t('سند تایید شد.', 'Voucher confirmed.'))
  await loadSteps(id)
}

// ---- «حذف» of a receipt recorded by mistake, before its voucher is confirmed (owner's answer to Q59, 19 Nov 2026) ----
const deleting = ref(false)
async function removeSelected() {
  const r0 = selected.value
  if (!r0 || deleting.value) return
  if (!window.confirm(t(`رسید ${name(kindNames, r0.kind)} شمارهٔ ${num(r0.number)} (${r0.payerName} – ${money(r0.totalRial)} ریال)${hasInstruments(r0) ? ' با چک‌ها و سفته‌های آن' : ''} حذف شود؟ رسید با عبارت «حذف شده» در فهرست می‌ماند و شمارهٔ آن به رسید دیگری داده نمی‌شود.`,
    `Delete ${name(kindNames, r0.kind)} receipt no. ${r0.number} (${r0.payerName} – ${money(r0.totalRial)} Rial)? It stays in the list marked as deleted, and its number is not given to another receipt.`))) return
  deleting.value = true
  const r = await api.post<string>(`/api/cashflow/receipts/${r0.id}/delete`, { version: r0.version })
  deleting.value = false
  if (!r.ok) return show('error', translate(r.code, props.locale))
  show('ok', t(`رسید شمارهٔ ${num(r0.number)} حذف شد و با عبارت «حذف شده» در فهرست مانده است.`, `Receipt no. ${r0.number} deleted; it stays in the list marked as deleted.`))
  await Promise.all([open(r0.id), load()])
}

// ---- «چاپ» of the list with a range of dates (owner's answer to Q63) ----
const inRange = (from: string | null, to: string | null) => shown.value.filter(r => (!from || r.date >= from) && (!to || r.date <= to))
const statusText = (r: Receipt) => (r.state === 'Deleted' ? t('حذف شده', 'Deleted') : r.state === 'Cancelled' ? t('ابطال شده', 'Cancelled')
  : hasInstruments(r) ? statusGroups(r).map(g => `${r.instrumentCount > 1 ? t(`${num(g.count)} برگ: `, `${g.count} × `) : ''}${g.name}${g.due ? ` (${dueMark(props.locale)})` : ''}`).join(fa.value ? '، ' : ', ')
  : t('ثبت شده', 'Recorded'))
function printHtml(from: string | null = null, to: string | null = null) {
  const rows = inRange(from, to)
  const range = rangeText(from, to, props.locale)
  return listPage({
    title: t(`فهرست جامع دریافت‌ها${kind.value ? ' – ' + name(kindNames, kind.value) : ''} – سال مالی ${num(Number(year.value))}`, `Receipts${kind.value ? ' – ' + name(kindNames, kind.value) : ''} – fiscal year ${year.value}`),
    subtitle: `${range ? range + ' – ' : ''}${t(`تاریخ چاپ: ${todayJalali()} – تعداد: ${num(rows.length)} – از جدید به قدیم`, `Printed ${todayJalali()} – ${rows.length} receipts – newest first`)}${
      search.value || numberSearch.value || chequeSearch.value ? t(' – با فیلتر', ' – filtered') : ''}`,
    head: [t('ردیف', '#'), t('شماره رسید', 'Receipt no.'), t('شماره چک / سفته', 'Cheque / note no.'), t('نوع', 'Kind'), t('تاریخ', 'Date'), t('پرداخت کننده', 'Payer'), t('بابت', 'Purpose'), t('صندوق / حساب / تعداد برگ', 'Cash desk / account / items'),
      t('مبلغ (ریال)', 'Amount (Rial)'), t('وضعیت', 'Status')],
    rows: rows.map((r, i) => [num(i + 1), num(r.number), chequeNumbers(r).join(fa.value ? '، ' : ', '), name(kindNames, r.kind) + (r.method ? ` – ${depositMethodName(r.method, fa.value)}` : ''), r.date,
      `${r.payerName} (${r.payerDetailCode})`, r.purpose, hasInstruments(r) ? t(`${num(r.instrumentCount)} برگ`, `${r.instrumentCount} items`) : accountName(r), money(r.totalRial), statusText(r)]),
    total: { label: t('جمع (بدون ابطال‌شده‌ها و حذف‌شده‌ها)', 'Total (without cancelled and deleted)'), value: money(rows.filter(r => r.state === 'Recorded').reduce((sum, r) => sum + r.totalRial, 0)), cellsAfter: 1 },
    locale: props.locale,
  })
}

// ---- cancel a cash / bank receipt ----
const cancel = reactive({ open: false, date: todayJalali(), reason: '' })
async function confirmCancel() {
  const r0 = selected.value
  if (!r0) return
  const date = normaliseJalali(cancel.date)
  if (!date) return show('error', translate('FiscalDate.Invalid', props.locale))
  const r = await api.post<string>(`/api/cashflow/receipts/${r0.id}/cancel`, { date, reason: cancel.reason, version: r0.version })
  if (!r.ok) return show('error', translate(r.code, props.locale))
  cancel.open = false
  cancel.reason = ''
  show('ok', t('رسید ابطال شد.', 'Receipt cancelled.'))
  await Promise.all([open(r0.id), load()])
}


defineExpose({ load, printHtml })
watch([year, kind], () => { selected.value = null; cards.value = []; void load() })
onMounted(async () => { await L.load(); await load() })
</script>

<template>
  <div>
    <div class="tr-buttons">
      <label style="width: 9rem">{{ t('سال مالی', 'Fiscal year') }}
        <SearchSelect v-model="year" :options="yearChoices" :aria-label="t('سال مالی', 'Fiscal year')" />
      </label>
      <label style="width: 12rem">{{ t('نوع دریافت', 'Kind') }}
        <SearchSelect v-model="kind" :options="kindChoices" :empty-label="t('همه', 'All')" :aria-label="t('نوع دریافت', 'Kind')" />
      </label>
      <label style="width: 9rem">{{ t('شماره رسید', 'Receipt no.') }}
        <input v-model="numberSearch" dir="ltr" inputmode="numeric" :aria-label="t('شماره رسید', 'Receipt no.')" />
      </label>
      <!-- item 20: a receipt is found by the number of one of its cheques or notes -->
      <label style="width: 10rem">{{ t('شماره چک / سفته', 'Cheque / note no.') }}
        <input v-model="chequeSearch" dir="ltr" inputmode="numeric" :aria-label="t('شماره چک / سفته', 'Cheque / note no.')" />
      </label>
      <label style="flex: 1; min-width: 14rem">{{ t('جستجو (پرداخت کننده، بابت، پیگیری، تاریخ، وضعیت)', 'Search (payer, purpose, tracking, date, status)') }}
        <input v-model="search" />
      </label>
      <PrintRange :dates="shown.map(r => r.date)" :count="(from, to) => inRange(from, to).length" :locale="locale" :disabled="!shown.length"
                  @print="(from, to) => printPage(printHtml(from, to))" />
    </div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
    <p class="muted small" style="margin: .25rem 0 0">{{ t('با کلیک یا دبل‌کلیک روی هر ردیف، رسید و «کارت چک» چک‌های آن (سیر مراحل هر چک) در پایین صفحه باز می‌شود؛ با کلیک روی کارت چک، کارت بسته می‌شود.',
      'Click or double-click a row to open the receipt and the card of each of its cheques (the course of its steps) below; click a cheque card to close it.') }}</p>

    <div class="tr-scroll">
      <table class="tr-table">
        <thead>
          <tr>
            <th>{{ t('شماره', 'No.') }}</th><th>{{ t('شماره چک / سفته', 'Cheque / note no.') }}</th><th>{{ t('نوع', 'Kind') }}</th><th>{{ t('تاریخ', 'Date') }}</th><th>{{ t('پرداخت کننده', 'Payer') }}</th>
            <th>{{ t('بابت', 'Purpose') }}</th><th>{{ t('صندوق / حساب', 'Cash desk / account') }}</th><th>{{ t('مبلغ', 'Amount') }}</th>
            <th>{{ t('ریالی', 'Rial') }}</th><th>{{ t('وضعیت', 'Latest status') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in shown" :key="r.id" :class="['clickable', { cancelled: r.state === 'Cancelled', deleted: r.state === 'Deleted', selected: selected?.id === r.id }]" @click="openRow(r)" @mousedown="noSelectOnDoubleClick">
            <td class="num">{{ num(r.number) }}</td>
            <td class="num"><div v-for="c in r.chequeStatuses ?? []" :key="c.id ?? c.number ?? ''">{{ c.number }}</div></td>
            <td>{{ name(kindNames, r.kind) }}<span v-if="r.method" class="muted small"> – {{ depositMethodName(r.method, fa) }}</span></td>
            <td class="num">{{ r.date }}</td>
            <td>{{ r.payerName }} <span class="muted small">{{ r.payerDetailCode }}</span></td>
            <td>{{ r.purpose }} <span class="muted small">{{ r.purposeGroupName }}</span></td>
            <td>{{ hasInstruments(r) ? t(`${num(r.instrumentCount)} برگ`, `${r.instrumentCount} items`) : accountName(r) }}</td>
            <td class="num">{{ r.amount !== null && r.currency !== 'IRR' ? `${money(r.amount)} ${r.currency}` : '' }}</td>
            <td class="num">{{ money(r.totalRial) }}</td>
            <td>
              <span v-if="r.state === 'Deleted'" class="tr-deleted">{{ t('حذف شده', 'Deleted') }}</span>
              <span v-else-if="r.state === 'Cancelled'" class="tr-tag cancelled">{{ t('ابطال شده', 'Cancelled') }}</span>
              <template v-else-if="r.kind === 'Instruments'">
                <div v-for="g in statusGroups(r)" :key="g.state + g.name">
                  <span v-if="r.instrumentCount > 1" class="muted small">{{ t(`${num(g.count)} برگ:`, `${g.count} ×`) }}</span> {{ g.name }}
                  <span v-if="g.due" class="tr-tag due" :title="dueWaitsFor(g.state, locale)">{{ dueMark(locale) }}<template v-if="g.count > 1"> ({{ num(g.due) }})</template></span>
                </div>
              </template>
              <template v-else>{{ t('ثبت شده', 'Recorded') }}</template>
            </td>
          </tr>
          <tr v-if="!shown.length"><td colspan="10" class="muted">{{ receipts.length ? t('با این جستجو رسیدی پیدا نشد.', 'No receipt matches.') : t('رسیدی ثبت نشده است.', 'No receipts.') }}</td></tr>
        </tbody>
        <tfoot v-if="shown.length">
          <tr><td colspan="8"><strong>{{ t('جمع (بدون ابطال‌شده‌ها و حذف‌شده‌ها)', 'Total (without cancelled and deleted)') }}</strong></td><td class="num"><strong>{{ money(totalShown) }}</strong></td><td /></tr>
        </tfoot>
      </table>
    </div>

    <section v-if="selected" style="margin-top: 1.25rem">
      <h3>{{ t(`رسید ${name(kindNames, selected.kind)} شمارهٔ ${num(selected.number)}`, `${name(kindNames, selected.kind)} receipt no. ${selected.number}`) }}
        <span v-if="selected.replacedInstrumentId" class="tr-tag later">{{ t('تعویض چک', 'Replacement') }}</span></h3>
      <p class="small">
        {{ t('تاریخ', 'Date') }}: <span class="num">{{ selected.date }}</span> ·
        {{ t('پرداخت کننده', 'Payer') }}: {{ selected.payerName }} ({{ selected.payerDetailCode }}) ·
        {{ selected.purposeGroupName }}: {{ selected.purpose }}
        <template v-if="selected.reference1"> · {{ selected.reference1 }}</template>
        <template v-if="selected.reference2"> · {{ selected.reference2 }}</template>
        <template v-if="selected.departmentId"> · {{ t('واحد سازمانی دریافت کننده', 'Receiving department') }}: {{ L.departments.value.find(d => d.id === selected!.departmentId)?.name }}</template>
        <template v-if="selected.salesProceeds"> · {{ t('وجوه حاصل فروش', 'Sales proceeds') }}</template>
        <template v-if="selected.method"> · {{ t('نوع واریز', 'Kind of deposit') }}: {{ depositMethodName(selected.method, fa) }}</template>
        <template v-if="selected.payerSheba"> · {{ t('شبای واریزکننده', "Payer's Sheba") }}: <span class="num">{{ selected.payerSheba }}</span></template>
        <template v-if="selected.trackingNumber"> · {{ t('پیگیری', 'Tracking') }}: {{ selected.trackingNumber }}</template>
        <template v-if="selected.rateToRial"> · {{ t('نرخ', 'Rate') }}: {{ money(selected.rateToRial) }}</template>
        · {{ t('ثبت', 'By') }}: {{ selected.recordedBy }}
      </p>
      <p v-if="selected.note" class="small">{{ selected.note }}</p>
      <!-- Owner's report no. 6, items 2 and 6: a receipt is corrected until its voucher is confirmed. Item 21: «چاپ رسید». -->
      <div v-if="selected.state !== 'Deleted'" class="tr-buttons">
        <button type="button" class="tr-secondary" @click="emit('print', selected.id)">{{ t('چاپ رسید', 'Print receipt') }}</button>
      </div>
      <div v-if="selected.state === 'Recorded'" class="tr-buttons">
        <button v-if="!selected.notCorrectable" type="button" @click="emit('edit', selected.id)">{{ t('ویرایش رسید', 'Correct the receipt') }}</button>
        <span v-else class="muted small">{{ t('ویرایش بسته است:', 'Correction is closed:') }} {{ translate(selected.notCorrectable, locale) }}</span>
        <!-- Owner's answer to Q59: a receipt recorded by mistake is deleted at the same stage. -->
        <button v-if="!selected.notDeletable" type="button" class="tr-secondary danger" :disabled="deleting" @click="removeSelected">{{ t('حذف رسید', 'Delete the receipt') }}</button>
        <span v-else-if="selected.notDeletable !== selected.notCorrectable" class="muted small">{{ t('حذف بسته است:', 'Deletion is closed:') }} {{ translate(selected.notDeletable, locale) }}</span>
      </div>
      <p v-if="selected.state === 'Deleted'" class="tr-msg error">
        <span class="tr-deleted">{{ t('حذف شده', 'Deleted') }}</span>
        {{ t(`– این رسید را ${selected.deletedBy ?? ''} در ${when(selected.deletedAt)} حذف کرده است. شمارهٔ آن به رسید دیگری داده نمی‌شود${hasInstruments(selected) ? ' و چک‌ها و سفته‌های آن از فهرست چک‌ها برداشته شده‌اند' : ''}.`,
          `– deleted by ${selected.deletedBy ?? ''} on ${when(selected.deletedAt)}. Its number is not given to another receipt${hasInstruments(selected) ? ', and its cheques and notes were taken off the cheque lists' : ''}.`) }}
      </p>
      <p v-if="selected.state === 'Cancelled'" class="tr-msg error">{{ t(`ابطال شده در ${selected.cancelledOn}: ${selected.cancelReason}`, `Cancelled on ${selected.cancelledOn}: ${selected.cancelReason}`) }}</p>

      <template v-if="terms">
        <p class="small">
          {{ t('دلیل دریافت', 'Reason') }}: <strong>{{ name(reasonNames, terms.reason) }}</strong> ·
          {{ t('نوع سند', 'Document') }}: {{ name(formNames, terms.form) }} ·
          {{ t('نوع سررسید', 'Due') }}: {{ name(maturityNames, terms.maturity) }} ·
          {{ t('اصل سند', 'Original') }}: {{ terms.originalReceived ? t(`دریافت شد (${terms.originalReceivedOn})`, `received (${terms.originalReceivedOn})`) : t('دریافت نشد', 'not received') }}
          <template v-if="terms.issuerNationalCode"> · {{ t('کد / شناسه ملی متعهد', 'Obligor national code') }}: <span class="num">{{ terms.issuerNationalCode }}</span></template>
          <template v-if="terms.issuerPhone"> · {{ t('تماس', 'Phone') }}: <span class="num">{{ terms.issuerPhone }}</span></template>
        </p>
        <p v-if="terms.commitmentSubject || terms.returnTerms || terms.custodyPlace || terms.discrepancy" class="small">
          <template v-if="terms.commitmentSubject">{{ t('موضوع تعهد', 'Secures') }}: {{ terms.commitmentSubject }} · </template>
          <template v-if="terms.returnTerms">{{ t('شرایط استرداد', 'Return terms') }}: {{ terms.returnTerms }} · </template>
          <template v-if="terms.custodyPlace">{{ t('محل نگهداری', 'Kept at') }}: {{ terms.custodyPlace }} · </template>
          <template v-if="terms.discrepancy">{{ t('نقص یا مغایرت', 'Discrepancy') }}: {{ terms.discrepancy }}</template>
        </p>
        <p class="small">
          {{ t('پیوست‌ها', 'Attachments') }}: {{ attachmentNames.filter(([k]) => terms!.attachments[k]).map(([, f, e]) => t(f, e)).join('، ') || '—' }}
          <template v-if="terms.deliveredBy"> · {{ t('تحویل‌دهنده', 'Handed over by') }}: {{ terms.deliveredBy }}</template>
          <template v-if="terms.signers?.length"> · {{ t('امضاکنندگان مجاز', 'Authorised signatories') }}: {{ terms.signers.map(s => s.position ? `${s.name} (${s.position})` : s.name).join('، ') }}</template>
        </p>
      </template>

      <div v-if="!hasInstruments(selected) && selected.state === 'Recorded' && !voucher?.confirmedBy" class="tr-buttons">
        <button v-if="!cancel.open" type="button" class="tr-secondary" @click="cancel.open = true">{{ t('ابطال رسید', 'Cancel receipt') }}</button>
        <template v-else>
          <label>{{ t('تاریخ ابطال', 'Date') }} <input v-model="cancel.date" dir="ltr" /></label>
          <label style="flex: 1">{{ t('علت ابطال', 'Reason') }} <input v-model="cancel.reason" /></label>
          <button type="button" @click="confirmCancel">{{ t('تایید ابطال', 'Confirm') }}</button>
          <button type="button" class="tr-secondary" @click="cancel.open = false">{{ t('انصراف', 'Back') }}</button>
        </template>
      </div>

      <template v-if="selected.instruments?.length">
        <p class="muted small">{{ selected.kind === 'Notes'
          ? t('مراحل بعدی هر سفته (واگذاری به بانک، وصول، واخواست، استرداد، خرج، ابطال) در زبانهٔ «عملیات سفته‌های دریافتی» ثبت می‌شوند.',
            'The next steps of each note (to the bank, collected, protested, handed back, endorsed, cancelled) are recorded on the «Received note steps» tab.')
          : t('مراحل بعدی هر چک (ثبت صیادی، واگذاری، وصول، عودت، برگشت، حقوقی، تعویض، ابطال، خرج) در زبانهٔ «عملیات چک‌های دریافتی» ثبت می‌شوند.',
            'The next steps of each cheque are recorded on the «Received cheque steps» tab.') }}</p>
        <div class="tr-scroll">
          <table class="tr-table">
            <thead>
              <tr>
                <th>{{ t('نوع', 'Kind') }}</th><th>{{ t('شماره', 'No.') }}</th><th>{{ t('بانک / ضامن', 'Bank / guarantor') }}</th>
                <th v-if="selected.kind === 'Notes'">{{ t('تاریخ صدور', 'Issue date') }}</th><th>{{ t('سررسید', 'Due') }}</th>
                <th>{{ t('مبلغ', 'Amount') }}</th><th>{{ t('ریالی', 'Rial') }}</th><th>{{ t('آخرین وضعیت', 'Latest status') }}</th><th />
              </tr>
            </thead>
            <tbody>
              <tr v-for="i in selected.instruments" :key="i.id">
                <td>{{ instrumentKindName(i.kind, locale) }}
                  <span v-if="!i.details?.onDemand" :class="['tr-tag', i.isPostDated ? 'later' : 'current']">{{ i.isPostDated ? t('مدت‌دار', 'Post-dated') : t('روز', 'Current') }}</span>
                </td>
                <td class="num">{{ i.number }}<div v-if="i.details?.sayadId" class="muted small">{{ t('صیاد', 'Sayad') }}: {{ i.details.sayadId }}</div></td>
                <td>
                  <template v-if="i.bankName">{{ i.bankName }}<span v-if="i.details?.branchName" class="muted small"> – {{ i.details.branchName }}</span>
                    <div v-if="i.details?.accountNumber" class="muted small">{{ i.details.accountNumber }}</div></template>
                  <template v-else>{{ (i.details?.guarantors ?? []).map(g => g.detailCode ? `${g.name} (${g.detailCode})` : g.name).join('، ') }}
                    <div v-if="ticked(i.details?.checks)" class="muted small">{{ t('کنترل شد', 'Checked') }}: {{ ticked(i.details?.checks) }}</div></template>
                </td>
                <td v-if="selected.kind === 'Notes'" class="num">{{ i.noteIssueDate }}</td>
                <td class="num">{{ i.dueDate ?? (i.details?.onDemand ? t('عندالمطالبه', 'On demand') : '') }}</td>
                <td class="num">{{ money(i.amount) }} <span v-if="i.currency !== 'IRR'">{{ i.currency }}</span></td>
                <td class="num">{{ money(i.amountInRial) }}</td>
                <td>{{ stateNameOf(i.state, i.kind, locale) }}
                  <span v-if="i.reachedDueDate" class="tr-tag due" :title="dueWaitsFor(i.state, locale)">{{ dueMark(locale) }}</span>
                  <div v-if="i.reachedDueDate" class="muted small">{{ dueWaitsFor(i.state, locale) }}</div>
                </td>
                <td>
                  <button type="button" class="tr-link" @click="showCard(i.id)">{{ selected.kind === 'Notes' ? t('کارت سفته', 'Note card') : t('کارت چک', 'Cheque card') }}</button>
                  <button type="button" class="tr-link" @click="toggleSteps(i.id)">{{ steps[i.id] ? t('بستن اسناد مراحل', 'Hide step vouchers') : t('اسناد مراحل چک', 'Step vouchers') }}</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <template v-for="i in selected.instruments" :key="'v' + i.id">
          <div v-if="steps[i.id]">
            <h4>{{ t(`اسناد مراحل چک ${i.number}`, `Vouchers of the steps of cheque ${i.number}`) }}</h4>
            <p v-if="!steps[i.id]!.length" class="muted small">{{ t('هنوز مرحله‌ای که سند داشته باشد (واگذاری، وصول، عودت، برگشت …) ثبت نشده است.', 'No step with a voucher (deposit, collection, return, bounce…) recorded yet.') }}</p>
            <VoucherTable v-for="v in steps[i.id]" :key="v.step" :voucher="v" :locale="locale" :account-name="L.accountName" can-confirm :busy="confirming"
                          @confirm="confirmStep(i.id, v.step)" />
          </div>
        </template>
      </template>

      <VoucherTable v-if="voucher" :voucher="voucher" :locale="locale" :account-name="L.accountName" :can-confirm="selected.state === 'Recorded'" :busy="confirming"
                    @confirm="confirmReceiptVoucher" />

      <!-- items 14 and 20: the cheque cards of this receipt, at the bottom of the screen; a click on a card closes it -->
      <ChequeCard v-for="id in cards" :key="id" :cheque-id="id" :refresh-key="cardKey" close-on-click :api-base="apiBase" :locale="locale" :token="token"
                  :bank-account-name="bankAccountTitle" :ledger-account-name="L.accountName" @close="cards = cards.filter(x => x !== id)" />
    </section>
  </div>
</template>

<style scoped src="./receipts.css"></style>
