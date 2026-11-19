<script setup lang="ts">
/**
 * The button «فهرست دریافت‌ها» at the bottom of every receipt screen (owner, 18 Nov 2026): on whichever form or cheque step the user is,
 * it opens the receipts of that kind — cheques, cash into a cash desk, deposits straight into a bank account — of the fiscal year in
 * use, newest first, with the latest status of each, in a scrolling box in the free space under the form; and prints them.
 * Owner's report no. 6 (18 Nov 2026):
 * - item 8: the statuses follow by themselves — the list is read again at once when a receipt is saved or a cheque step is recorded on
 *   this screen, every 15 seconds while it is open (for what other users record), and when the user comes back to the window; the
 *   button «به‌روزرسانی» stays for whoever wants it;
 * - item 9: a box «شماره رسید» finds exactly that number;
 * - item 10: «چاپ» first asks «از تاریخ» and «تا تاریخ», filled with the first and last date of the rows now listed and free to change,
 *   and prints the receipts of that range;
 * - items 2 and 6: «ویرایش» on a row opens the receipt in the form above while its voucher is not confirmed;
 * - owner's answer to Q59 (19 Nov 2026): «حذف» beside it deletes a receipt recorded by mistake, at the same stage.
 */
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { chequeStateName, createApi, dueMark, formatAmount, groupChequeStatuses, receiptNumber, searchKey, todayJalali, translate,
  type ChequeStatus, type Locale } from '@treasury/ui-kit'
import PrintRange from './PrintRange.vue'
import { listPage, printPage, rangeText } from './printing'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'
import { depositMethodName, depositMethods } from './depositMethods'

type Kind = 'Cash' | 'BankDeposit' | 'Instruments' | 'Notes'
interface Receipt {
  id: string; kind: Kind; number: number; date: string; payerDetailCode: string; payerName: string; purposeGroupName: string; purpose: string
  accountKind: string | null; accountId: string | null; method: string | null; trackingNumber: string | null; payerSheba: string | null
  amount: number | null; currency: string | null; totalRial: number; instrumentCount: number; state: string; chequeStatuses: ChequeStatus[] | null
}

const props = withDefaults(defineProps<{
  kind: Kind; apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number; refreshKey?: number
  /** The receipt now open in the form above for «ویرایش». */
  editingId?: string
}>(), { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined, refreshKey: 0, editingId: undefined })
const emit = defineEmits<{ edit: [id: string]; deleted: [id: string] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => (fa.value ? n.toLocaleString('fa-IR', { useGrouping: false }) : String(n))
const L = useLookups(api)

const year = computed(() => props.fiscalYear ?? Number(todayJalali().slice(0, 4)))
const kindNames: Record<Kind, [string, string]> = {
  Instruments: ['چک', 'cheques'], Notes: ['سفته', 'promissory notes'], Cash: ['نقدی صندوق', 'cash'], BankDeposit: ['واریز مستقیم به حساب بانکی', 'direct bank deposits'],
}
const title = computed(() => t(`فهرست دریافت‌های ${kindNames[props.kind][0]} – سال مالی ${num(year.value)}`, `Receipts: ${kindNames[props.kind][1]} – fiscal year ${year.value}`))

const open = ref(false)
const loading = ref(false)
const error = ref('')
const receipts = ref<Receipt[]>([])
const search = ref('')
/** «شماره رسید»: exactly this number (item 9). */
const numberSearch = ref('')
const method = ref('')
const methodChoices = computed<Choice[]>(() => depositMethods.map(m => ({ value: m.value, label: fa.value ? m.fa : m.en })))
let lookupsLoaded = false

/** @param quiet A reading the user did not ask for: no «در حال خواندن», and a failure leaves the list as it is. */
async function load(quiet = false) {
  if (!quiet) { loading.value = true; error.value = '' }
  if (!lookupsLoaded) { await L.load(); lookupsLoaded = true }
  // The API answers newest first (date, then number).
  const r = await api.get<Receipt[]>(`/api/cashflow/receipts?year=${year.value}&kind=${props.kind}`)
  if (!quiet) loading.value = false
  if (r.ok) { receipts.value = r.value; error.value = '' }
  else if (!quiet) error.value = translate(r.code, props.locale)
}
function toggle() {
  open.value = !open.value
  if (open.value) void load()
}
watch(() => [props.refreshKey, props.kind, year.value], () => { if (open.value) void load(true) })

// ---- item 8: the statuses follow without the button ----
const RefreshEveryMs = 15_000
let timer: ReturnType<typeof setInterval> | undefined
const refreshIfSeen = () => { if (open.value && document.visibilityState === 'visible') void load(true) }
onMounted(() => {
  timer = setInterval(refreshIfSeen, RefreshEveryMs)
  document.addEventListener('visibilitychange', refreshIfSeen)
  window.addEventListener('focus', refreshIfSeen)
})
onBeforeUnmount(() => {
  clearInterval(timer)
  document.removeEventListener('visibilitychange', refreshIfSeen)
  window.removeEventListener('focus', refreshIfSeen)
})

function accountName(r: Receipt) {
  if (r.accountKind === 'CashDesk') return L.desks.value.find(d => d.id === r.accountId)?.name ?? ''
  const a = L.bankAccounts.value.find(x => x.id === r.accountId)
  return a ? L.accountTitle(a) : ''
}
/** The latest status as text: of a cheque receipt its cheques' statuses, otherwise recorded or cancelled. */
function statusText(r: Receipt) {
  if (r.state === 'Cancelled') return t('ابطال شده', 'Cancelled')
  if (r.kind !== 'Instruments' && r.kind !== 'Notes') return t('ثبت شده', 'Recorded')
  return groupChequeStatuses(r.chequeStatuses, props.locale, r.kind === 'Notes')
    .map(g => `${r.instrumentCount > 1 ? t(`${num(g.count)} برگ: `, `${g.count} × `) : ''}${g.name}${g.due ? ` (${dueMark(props.locale)}${g.count > 1 ? ' ' + num(g.due) : ''})` : ''}`)
    .join(fa.value ? '، ' : ', ')
}
const shown = computed(() => {
  const key = searchKey(search.value)
  const wanted = receiptNumber(numberSearch.value)
  return receipts.value.filter(r => (!method.value || r.method === method.value) && (wanted === null || r.number === wanted)
    && (!key || searchKey(`${r.date} ${r.payerName} ${r.payerDetailCode} ${r.purpose} ${r.trackingNumber ?? ''} ${statusText(r)}`).includes(key)))
})
const total = computed(() => shown.value.filter(r => r.state !== 'Cancelled').reduce((s, r) => s + r.totalRial, 0))
const where = (r: Receipt) => (r.kind === 'Instruments' || r.kind === 'Notes' ? t(`${num(r.instrumentCount)} برگ`, `${r.instrumentCount} items`) : accountName(r))
const whereTitle = computed(() => (props.kind === 'Instruments' || props.kind === 'Notes' ? t('تعداد برگ', 'Items') : props.kind === 'Cash' ? t('صندوق', 'Cash desk') : t('حساب بانکی', 'Bank account')))

// ---- print: the rows now shown, within a range of dates (item 10), on a page of their own ----
const inRange = (from: string | null, to: string | null) => shown.value.filter(r => (!from || r.date >= from) && (!to || r.date <= to))
function printHtml(from: string | null = null, to: string | null = null) {
  const rows = inRange(from, to)
  const bank = props.kind === 'BankDeposit'
  const range = rangeText(from, to, props.locale)
  return listPage({
    title: title.value,
    subtitle: `${range ? range + ' – ' : ''}${t(`تاریخ چاپ: ${todayJalali()} – تعداد: ${num(rows.length)} – از جدید به قدیم`, `Printed ${todayJalali()} – ${rows.length} receipts – newest first`)}${
      search.value || method.value || numberSearch.value ? t(' – با فیلتر', ' – filtered') : ''}`,
    head: [t('ردیف', '#'), t('شماره رسید', 'Receipt no.'), t('تاریخ', 'Date'), t('پرداخت کننده', 'Payer'), t('بابت', 'Purpose'), whereTitle.value,
      ...(bank ? [t('نوع واریز', 'Kind of deposit'), t('پیگیری / شبا', 'Tracking / Sheba')] : []), t('مبلغ (ریال)', 'Amount (Rial)'), t('وضعیت', 'Status')],
    rows: rows.map((r, i) => [num(i + 1), num(r.number), r.date, `${r.payerName} (${r.payerDetailCode})`, r.purpose, where(r),
      ...(bank ? [depositMethodName(r.method, fa.value), [r.trackingNumber, r.payerSheba].filter(Boolean).join(' – ')] : []), money(r.totalRial), statusText(r)]),
    total: { label: t('جمع (بدون ابطال‌شده‌ها)', 'Total (without cancelled)'), value: money(rows.filter(r => r.state !== 'Cancelled').reduce((sum, r) => sum + r.totalRial, 0)), cellsAfter: 1 },
    locale: props.locale,
  })
}

// ---- «حذف» of a receipt recorded by mistake, before its voucher is confirmed (owner's answer to Q59) ----
const busy = ref(false)
async function remove(r: Receipt) {
  if (busy.value) return
  error.value = ''
  busy.value = true
  // The opened receipt says whether it can still be deleted, and carries the version the delete needs.
  const full = await api.get<{ version: number; notDeletable: string | null }>(`/api/cashflow/receipts/${r.id}`)
  busy.value = false
  if (!full.ok) return void (error.value = translate(full.code, props.locale))
  if (full.value.notDeletable) return void (error.value = `${t(`رسید شمارهٔ ${num(r.number)} حذف نمی‌شود:`, `Receipt no. ${r.number} is not deleted:`)} ${translate(full.value.notDeletable, props.locale)}`)
  if (!window.confirm(t(`رسید شمارهٔ ${num(r.number)} (${r.payerName} – ${money(r.totalRial)} ریال) حذف شود؟ شمارهٔ این رسید دوباره استفاده نمی‌شود.`,
    `Delete receipt no. ${r.number} (${r.payerName} – ${money(r.totalRial)} Rial)? Its number is not used again.`))) return
  busy.value = true
  const done = await api.post<string>(`/api/cashflow/receipts/${r.id}/delete`, { version: full.value.version })
  busy.value = false
  if (!done.ok) return void (error.value = translate(done.code, props.locale))
  emit('deleted', r.id)
  await load(true)
}
defineExpose({ printHtml, load, inRange })
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
        <label style="width: 9rem">{{ t('شماره رسید', 'Receipt no.') }} <input v-model="numberSearch" dir="ltr" inputmode="numeric" :aria-label="t('شماره رسید', 'Receipt no.')" /></label>
        <label style="flex: 1; min-width: 14rem">{{ t('جستجو (تاریخ، پرداخت کننده، بابت، وضعیت)', 'Search (date, payer, purpose, status)') }} <input v-model="search" /></label>
        <label v-if="kind === 'BankDeposit'" style="width: 20rem">{{ t('نوع واریز', 'Kind of deposit') }}
          <SearchSelect v-model="method" :options="methodChoices" :empty-label="t('همه', 'All')" :aria-label="t('نوع واریز', 'Kind of deposit')" />
        </label>
        <button type="button" class="tr-secondary" :disabled="loading" @click="load()">{{ t('به‌روزرسانی', 'Refresh') }}</button>
        <!-- item 10: «چاپ» asks for the range of the printed report -->
        <PrintRange :dates="shown.map(r => r.date)" :count="(from, to) => inRange(from, to).length" :locale="locale" :disabled="!shown.length"
                    @print="(from, to) => printPage(printHtml(from, to))" />
      </div>
      <p class="muted small" style="margin: .25rem 0 0">{{ t('وضعیت‌ها خودکار به‌روز می‌شوند (پس از هر ثبت و هر ۱۵ ثانیه).', 'Statuses refresh by themselves (after every save and every 15 seconds).') }}</p>
      <p v-if="error" class="tr-msg error" role="status">{{ error }}</p>
      <div class="tr-box">
        <table class="tr-table">
          <thead>
            <tr>
              <th>{{ t('شماره', 'No.') }}</th><th>{{ t('تاریخ', 'Date') }}</th><th>{{ t('پرداخت کننده', 'Payer') }}</th><th>{{ t('بابت', 'Purpose') }}</th>
              <th>{{ whereTitle }}</th><th v-if="kind === 'BankDeposit'">{{ t('نوع واریز', 'Kind of deposit') }}</th>
              <th>{{ t('مبلغ (ریال)', 'Amount (Rial)') }}</th><th>{{ t('وضعیت', 'Latest status') }}</th><th />
            </tr>
          </thead>
          <tbody>
            <tr v-for="r in shown" :key="r.id" :class="{ cancelled: r.state === 'Cancelled', selected: r.id === editingId }">
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
              <td>
                <!-- items 2 and 6: opens the receipt in the form above; the server says when it can no longer be corrected -->
                <template v-if="r.state !== 'Cancelled'">
                  <button type="button" class="tr-link" @click="emit('edit', r.id)">{{ t('ویرایش', 'Correct') }}</button>
                  <button type="button" class="tr-link danger" :disabled="busy" @click="remove(r)">{{ t('حذف', 'Delete') }}</button>
                </template>
              </td>
            </tr>
            <tr v-if="!shown.length"><td :colspan="kind === 'BankDeposit' ? 9 : 8" class="muted">{{ loading ? t('در حال خواندن…', 'Loading…')
              : receipts.length ? t('با این جستجو رسیدی پیدا نشد.', 'No receipt matches.') : t('رسیدی در این سال مالی ثبت نشده است.', 'No receipts in this fiscal year.') }}</td></tr>
          </tbody>
          <tfoot v-if="shown.length">
            <tr><td :colspan="kind === 'BankDeposit' ? 6 : 5"><strong>{{ t('جمع (بدون ابطال‌شده‌ها)', 'Total (without cancelled)') }}</strong></td><td class="num"><strong>{{ money(total) }}</strong></td><td colspan="2" /></tr>
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
.tr-table td:last-child { white-space: nowrap; }
/* The list scrolls inside the free space under the form; the header and the total stay in view. */
.tr-box { max-height: 26rem; overflow: auto; border: 1px solid #e4e7eb; border-radius: 8px; margin-top: .5rem; }
.tr-box .tr-table { margin-top: 0; }
.tr-box thead th { position: sticky; top: 0; background: #f5f7fa; z-index: 1; }
.tr-box tfoot td { position: sticky; bottom: 0; background: #f5f7fa; }
</style>
