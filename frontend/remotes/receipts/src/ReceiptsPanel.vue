<script setup lang="ts">
/**
 * The button «فهرست دریافت‌ها» at the bottom of every receipt screen (owner, 18 Nov 2026): on whichever form or cheque step the user is,
 * it opens the receipts of that kind — cheques, cash into a cash desk, deposits straight into a bank account — of the fiscal year in
 * use, newest first, with the latest status of each, in a scrolling box in the free space under the form; and prints them.
 */
import { computed, ref, watch } from 'vue'
import { chequeStateName, createApi, dueMark, formatAmount, groupChequeStatuses, searchKey, todayJalali, translate, type ChequeStatus, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'
import { depositMethodName, depositMethods } from './depositMethods'

type Kind = 'Cash' | 'BankDeposit' | 'Instruments'
interface Receipt {
  id: string; kind: Kind; number: number; date: string; payerDetailCode: string; payerName: string; purposeGroupName: string; purpose: string
  accountKind: string | null; accountId: string | null; method: string | null; trackingNumber: string | null; payerSheba: string | null
  amount: number | null; currency: string | null; totalRial: number; instrumentCount: number; state: string; chequeStatuses: ChequeStatus[] | null
}

const props = withDefaults(defineProps<{ kind: Kind; apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number; refreshKey?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined, refreshKey: 0 })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => (fa.value ? n.toLocaleString('fa-IR', { useGrouping: false }) : String(n))
const L = useLookups(api)

const year = computed(() => props.fiscalYear ?? Number(todayJalali().slice(0, 4)))
const kindNames: Record<Kind, [string, string]> = {
  Instruments: ['چک و سفته', 'cheques and notes'], Cash: ['نقدی صندوق', 'cash'], BankDeposit: ['واریز مستقیم به حساب بانکی', 'direct bank deposits'],
}
const title = computed(() => t(`فهرست دریافت‌های ${kindNames[props.kind][0]} – سال مالی ${num(year.value)}`, `Receipts: ${kindNames[props.kind][1]} – fiscal year ${year.value}`))

const open = ref(false)
const loading = ref(false)
const error = ref('')
const receipts = ref<Receipt[]>([])
const search = ref('')
const method = ref('')
const methodChoices = computed<Choice[]>(() => depositMethods.map(m => ({ value: m.value, label: fa.value ? m.fa : m.en })))
let lookupsLoaded = false

async function load() {
  loading.value = true
  error.value = ''
  if (!lookupsLoaded) { await L.load(); lookupsLoaded = true }
  // The API answers newest first (date, then number).
  const r = await api.get<Receipt[]>(`/api/cashflow/receipts?year=${year.value}&kind=${props.kind}`)
  loading.value = false
  if (r.ok) receipts.value = r.value
  else error.value = translate(r.code, props.locale)
}
function toggle() {
  open.value = !open.value
  if (open.value) void load()
}
watch(() => [props.refreshKey, props.kind, year.value], () => { if (open.value) void load() })

function accountName(r: Receipt) {
  if (r.accountKind === 'CashDesk') return L.desks.value.find(d => d.id === r.accountId)?.name ?? ''
  const a = L.bankAccounts.value.find(x => x.id === r.accountId)
  return a ? L.accountTitle(a) : ''
}
/** The latest status as text: of a cheque receipt its cheques' statuses, otherwise recorded or cancelled. */
function statusText(r: Receipt) {
  if (r.state === 'Cancelled') return t('ابطال شده', 'Cancelled')
  if (r.kind !== 'Instruments') return t('ثبت شده', 'Recorded')
  return groupChequeStatuses(r.chequeStatuses, props.locale)
    .map(g => `${r.instrumentCount > 1 ? t(`${num(g.count)} برگ: `, `${g.count} × `) : ''}${g.name}${g.due ? ` (${dueMark(props.locale)}${g.count > 1 ? ' ' + num(g.due) : ''})` : ''}`)
    .join(fa.value ? '، ' : ', ')
}
const shown = computed(() => {
  const key = searchKey(search.value)
  return receipts.value.filter(r => (!method.value || r.method === method.value)
    && (!key || searchKey(`${r.number} ${r.date} ${r.payerName} ${r.payerDetailCode} ${r.purpose} ${r.trackingNumber ?? ''} ${statusText(r)}`).includes(key)))
})
const total = computed(() => shown.value.filter(r => r.state !== 'Cancelled').reduce((s, r) => s + r.totalRial, 0))
const where = (r: Receipt) => (r.kind === 'Instruments' ? t(`${num(r.instrumentCount)} برگ`, `${r.instrumentCount} items`) : accountName(r))
const whereTitle = computed(() => (props.kind === 'Instruments' ? t('تعداد برگ', 'Items') : props.kind === 'Cash' ? t('صندوق', 'Cash desk') : t('حساب بانکی', 'Bank account')))

// ---- print: the rows now shown, on a page of their own (a hidden frame, so no pop-up window is needed) ----
const esc = (s: unknown) => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]!))
function printHtml() {
  const bank = props.kind === 'BankDeposit'
  const head = [t('ردیف', '#'), t('شماره رسید', 'Receipt no.'), t('تاریخ', 'Date'), t('پرداخت کننده', 'Payer'), t('بابت', 'Purpose'), whereTitle.value,
    ...(bank ? [t('نوع واریز', 'Kind of deposit'), t('پیگیری / شبا', 'Tracking / Sheba')] : []), t('مبلغ (ریال)', 'Amount (Rial)'), t('وضعیت', 'Status')]
  const body = shown.value.map((r, i) => [num(i + 1), num(r.number), r.date, `${r.payerName} (${r.payerDetailCode})`, r.purpose, where(r),
    ...(bank ? [depositMethodName(r.method, fa.value), [r.trackingNumber, r.payerSheba].filter(Boolean).join(' – ')] : []), money(r.totalRial), statusText(r)])
  const span = head.length - 2
  return `<!doctype html><html lang="${fa.value ? 'fa' : 'en'}" dir="${fa.value ? 'rtl' : 'ltr'}"><head><meta charset="utf-8"><title>${esc(title.value)}</title><style>
    @page { size: A4 landscape; margin: 12mm; }
    body { font-family: Vazirmatn, Tahoma, sans-serif; font-size: 10pt; color: #000; }
    h1 { font-size: 13pt; margin: 0 0 2mm; } p { margin: 0 0 3mm; color: #333; }
    table { width: 100%; border-collapse: collapse; } th, td { border: 1px solid #888; padding: 3px 6px; text-align: start; vertical-align: top; }
    th { background: #eee; } tfoot td { font-weight: bold; } tr { break-inside: avoid; }
  </style></head><body>
    <h1>${esc(title.value)}</h1>
    <p>${esc(t(`تاریخ چاپ: ${todayJalali()} – تعداد: ${num(shown.value.length)} – از جدید به قدیم`, `Printed ${todayJalali()} – ${shown.value.length} receipts – newest first`))}${
      search.value || method.value ? esc(t(' – با فیلتر', ' – filtered')) : ''}</p>
    <table><thead><tr>${head.map(h => `<th>${esc(h)}</th>`).join('')}</tr></thead>
    <tbody>${body.map(row => `<tr>${row.map(c => `<td>${esc(c)}</td>`).join('')}</tr>`).join('')}</tbody>
    <tfoot><tr><td colspan="${span}">${esc(t('جمع (بدون ابطال‌شده‌ها)', 'Total (without cancelled)'))}</td><td>${esc(money(total.value))}</td><td></td></tr></tfoot></table>
  </body></html>`
}
function print() {
  const frame = document.createElement('iframe')
  frame.setAttribute('aria-hidden', 'true')
  frame.style.cssText = 'position:fixed;inset-inline-end:0;bottom:0;width:0;height:0;border:0'
  document.body.appendChild(frame)
  const doc = frame.contentDocument!
  doc.open()
  doc.write(printHtml())
  doc.close()
  const w = frame.contentWindow!
  w.addEventListener('afterprint', () => frame.remove())
  setTimeout(() => { w.focus(); w.print() }, 150)
  setTimeout(() => frame.remove(), 60_000)
}
defineExpose({ printHtml, load })
</script>

<template>
  <section class="tr-panel">
    <div class="tr-buttons">
      <button type="button" class="tr-secondary" :aria-expanded="open" @click="toggle">
        {{ open ? t('بستن فهرست دریافت‌ها', 'Hide the receipts list') : t('فهرست دریافت‌ها', 'Receipts list') }}
      </button>
      <template v-if="open">
        <strong>{{ title }}</strong>
        <span class="muted small">{{ t(`${num(shown.length)} رسید – از جدید به قدیم`, `${shown.length} receipts – newest first`) }}</span>
      </template>
    </div>
    <template v-if="open">
      <div class="tr-buttons tr-filters">
        <label style="flex: 1; min-width: 14rem">{{ t('جستجو (شماره، تاریخ، پرداخت کننده، بابت، وضعیت)', 'Search (number, date, payer, purpose, status)') }} <input v-model="search" /></label>
        <label v-if="kind === 'BankDeposit'" style="width: 20rem">{{ t('نوع واریز', 'Kind of deposit') }}
          <SearchSelect v-model="method" :options="methodChoices" :empty-label="t('همه', 'All')" :aria-label="t('نوع واریز', 'Kind of deposit')" />
        </label>
        <button type="button" class="tr-secondary" :disabled="loading" @click="load">{{ t('به‌روزرسانی', 'Refresh') }}</button>
        <button type="button" :disabled="!shown.length" @click="print">{{ t('چاپ', 'Print') }}</button>
      </div>
      <p v-if="error" class="tr-msg error" role="status">{{ error }}</p>
      <div class="tr-box">
        <table class="tr-table">
          <thead>
            <tr>
              <th>{{ t('شماره', 'No.') }}</th><th>{{ t('تاریخ', 'Date') }}</th><th>{{ t('پرداخت کننده', 'Payer') }}</th><th>{{ t('بابت', 'Purpose') }}</th>
              <th>{{ whereTitle }}</th><th v-if="kind === 'BankDeposit'">{{ t('نوع واریز', 'Kind of deposit') }}</th>
              <th>{{ t('مبلغ (ریال)', 'Amount (Rial)') }}</th><th>{{ t('وضعیت', 'Latest status') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="r in shown" :key="r.id" :class="{ cancelled: r.state === 'Cancelled' }">
              <td class="num">{{ num(r.number) }}</td>
              <td class="num">{{ r.date }}</td>
              <td>{{ r.payerName }} <span class="muted small">{{ r.payerDetailCode }}</span></td>
              <td>{{ r.purpose }} <span class="muted small">{{ r.purposeGroupName }}</span></td>
              <td>{{ where(r) }}</td>
              <td v-if="kind === 'BankDeposit'">{{ depositMethodName(r.method, fa) }}
                <div v-if="r.trackingNumber || r.payerSheba" class="muted small num">{{ [r.trackingNumber, r.payerSheba].filter(Boolean).join(' – ') }}</div>
              </td>
              <td class="num">{{ money(r.totalRial) }}<span v-if="r.amount !== null && r.currency !== 'IRR'" class="muted small"> ({{ money(r.amount) }} {{ r.currency }})</span></td>
              <td>
                <span v-if="r.state === 'Cancelled'" class="tr-tag cancelled">{{ t('ابطال شده', 'Cancelled') }}</span>
                <template v-else-if="r.kind === 'Instruments'">
                  <div v-for="g in groupChequeStatuses(r.chequeStatuses, locale)" :key="g.state">
                    <span v-if="r.instrumentCount > 1" class="muted small">{{ t(`${num(g.count)} برگ:`, `${g.count} ×`) }}</span> {{ chequeStateName(g.state, locale) }}
                    <span v-if="g.due" class="tr-tag due">{{ dueMark(locale) }}<template v-if="g.count > 1"> ({{ num(g.due) }})</template></span>
                  </div>
                </template>
                <template v-else>{{ t('ثبت شده', 'Recorded') }}</template>
              </td>
            </tr>
            <tr v-if="!shown.length"><td :colspan="kind === 'BankDeposit' ? 8 : 7" class="muted">{{ loading ? t('در حال خواندن…', 'Loading…') : t('رسیدی در این سال مالی ثبت نشده است.', 'No receipts in this fiscal year.') }}</td></tr>
          </tbody>
          <tfoot v-if="shown.length">
            <tr><td :colspan="kind === 'BankDeposit' ? 6 : 5"><strong>{{ t('جمع (بدون ابطال‌شده‌ها)', 'Total (without cancelled)') }}</strong></td><td class="num"><strong>{{ money(total) }}</strong></td><td /></tr>
          </tfoot>
        </table>
      </div>
    </template>
  </section>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
.tr-panel { margin-top: 1.5rem; padding-top: .75rem; border-top: 1px dashed #cbd2d9; }
.tr-filters { align-items: end; margin-top: .5rem; }
/* The list scrolls inside the free space under the form; the header and the total stay in view. */
.tr-box { max-height: 26rem; overflow: auto; border: 1px solid #e4e7eb; border-radius: 8px; margin-top: .5rem; }
.tr-box .tr-table { margin-top: 0; }
.tr-box thead th { position: sticky; top: 0; background: #f5f7fa; z-index: 1; }
.tr-box tfoot td { position: sticky; bottom: 0; background: #f5f7fa; }
</style>
