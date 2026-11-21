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
 * - owner's answer to Q59 (19 Nov 2026): «حذف» beside it deletes a receipt recorded by mistake, at the same stage;
 * - owner's answer to Q67 (20 Nov 2026): a deleted receipt stays in the list, on a light red ground with «حذف شده» written large, so its
 *   number is seen to be taken; it is left out of the total.
 * Owner's report no. 6 as sent on 19 Nov 2026 (items 12–21):
 * - item 20: the lists of cheque and note receipts have a column «شماره چک», a box that finds a receipt by the number of one of its
 *   cheques, and the column in the printed list;
 * - items 14 and 20: a click (or double click) on a row shows the cheque card of each of its cheques under the list — the course the
 *   cheque has run; a click on a card closes it. A click on one cheque number shows that cheque's card only;
 * - item 21: «چاپ رسید» on a row shows and prints the receipt.
 */
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { createApi, dueMark, formatAmount, groupChequeStatuses, receiptNumber, searchKey, todayJalali, toLatinDigits, translate,
  type ChequeStatus, type Locale } from '@treasury/ui-kit'
import PrintRange from './PrintRange.vue'
import { listPage, printPage, rangeText } from './printing'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'
import { depositMethodName, depositMethods, forCard } from './depositMethods'
import ChequeCard from './ChequeCard.vue'

type Kind = 'Cash' | 'BankDeposit' | 'Instruments' | 'Notes' | 'CardDeposit'
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
const emit = defineEmits<{ edit: [id: string]; deleted: [id: string]; print: [id: string] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => (fa.value ? n.toLocaleString('fa-IR', { useGrouping: false }) : String(n))
const L = useLookups(api)

const year = computed(() => props.fiscalYear ?? Number(todayJalali().slice(0, 4)))
const kindNames: Record<Kind, [string, string]> = {
  Instruments: ['چک', 'cheques'], Notes: ['سفته', 'promissory notes'], Cash: ['نقدی صندوق', 'cash'], BankDeposit: ['واریز مستقیم به حساب بانکی', 'direct bank deposits'],
  CardDeposit: ['واریز به کارت بانک', 'deposits onto a bank card'],
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
/** «شماره چک» (item 20): the receipts holding a cheque whose number contains these digits. */
const chequeSearch = ref('')
/** Receipts of cheques or notes: their rows name their cheques and open the cheque cards. */
const hasCheques = computed(() => props.kind === 'Instruments' || props.kind === 'Notes')
const paper = computed<[string, string]>(() => (props.kind === 'Notes' ? ['سفته', 'note'] : ['چک', 'cheque']))
const chequeNumbers = (r: Receipt) => (r.chequeStatuses ?? []).map(c => c.number ?? '').filter(Boolean)
/** Receipts with a kind of deposit: into a bank account, or onto a bank card. */
const hasMethod = computed(() => props.kind === 'BankDeposit' || props.kind === 'CardDeposit')
const methodChoices = computed<Choice[]>(() => depositMethods.filter(m => props.kind !== 'CardDeposit' || forCard(m.value)).map(m => ({ value: m.value, label: fa.value ? m.fa : m.en })))
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
watch(() => [props.refreshKey, props.kind, year.value], () => { if (open.value) void load(true); cardKey.value++ })
watch(() => [props.kind, year.value], () => { cards.value = []; cardsOf.value = '' })

// ---- items 14 and 20: the cheque cards of the row clicked, under the list ----
/** The cheques whose cards are open, and the receipt they were opened from. */
const cards = ref<string[]>([])
const cardsOf = ref('')
/** Raised when something was recorded on this screen, so the open cards read their cheque again. */
const cardKey = ref(0)
const cardBox = ref<HTMLElement | null>(null)
/** A click or a double click on a row opens the cards of its cheques (a double click is two clicks: the cards stay open). */
function showCards(r: Receipt, only?: string) {
  if (!hasCheques.value || r.state === 'Deleted') return
  const ids = (r.chequeStatuses ?? []).map(c => c.id).filter((id): id is string => !!id)
  cards.value = only ? [only] : ids
  cardsOf.value = cards.value.length ? r.id : ''
  if (cards.value.length) void nextTick(() => cardBox.value?.scrollIntoView({ behavior: 'smooth', block: 'nearest' }))
}
/** The second press of a double click would select a word of the row. */
const noSelectOnDoubleClick = (e: MouseEvent) => { if (e.detail > 1) e.preventDefault() }
function closeCard(id: string) {
  cards.value = cards.value.filter(x => x !== id)
  if (!cards.value.length) cardsOf.value = ''
}

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

const accountName = (r: Receipt) => L.receiptAccountTitle(r, fa.value)
const bankAccountTitle = (id: string | null | undefined) => { const a = L.bankAccounts.value.find(x => x.id === id); return a ? L.accountTitle(a) : '' }
/** The latest status as text: of a cheque receipt its cheques' statuses, otherwise recorded or cancelled. */
function statusText(r: Receipt) {
  if (r.state === 'Deleted') return t('حذف شده', 'Deleted')
  if (r.state === 'Cancelled') return t('ابطال شده', 'Cancelled')
  if (r.kind !== 'Instruments' && r.kind !== 'Notes') return t('ثبت شده', 'Recorded')
  return groupChequeStatuses(r.chequeStatuses, props.locale, r.kind === 'Notes')
    .map(g => `${r.instrumentCount > 1 ? t(`${num(g.count)} برگ: `, `${g.count} × `) : ''}${g.name}${g.due ? ` (${dueMark(props.locale)}${g.count > 1 ? ' ' + num(g.due) : ''})` : ''}`)
    .join(fa.value ? '، ' : ', ')
}
const shown = computed(() => {
  const key = searchKey(search.value)
  const wanted = receiptNumber(numberSearch.value)
  const cheque = toLatinDigits(chequeSearch.value).replace(/[^0-9A-Za-z]/g, '')
  return receipts.value.filter(r => (!method.value || r.method === method.value) && (wanted === null || r.number === wanted)
    && (!cheque || chequeNumbers(r).some(n => toLatinDigits(n).replace(/[^0-9A-Za-z]/g, '').includes(cheque)))
    && (!key || searchKey(`${r.date} ${r.payerName} ${r.payerDetailCode} ${r.purpose} ${r.trackingNumber ?? ''} ${statusText(r)}`).includes(key)))
})
const total = computed(() => shown.value.filter(r => r.state === 'Recorded').reduce((s, r) => s + r.totalRial, 0))
const where = (r: Receipt) => (r.kind === 'Instruments' || r.kind === 'Notes' ? t(`${num(r.instrumentCount)} برگ`, `${r.instrumentCount} items`) : accountName(r))
const whereTitle = computed(() => (props.kind === 'Instruments' || props.kind === 'Notes' ? t('تعداد برگ', 'Items') : props.kind === 'Cash' ? t('صندوق', 'Cash desk')
  : props.kind === 'CardDeposit' ? t('کارت بانک', 'Bank card') : t('حساب بانکی', 'Bank account')))

// ---- print: the rows now shown, within a range of dates (item 10), on a page of their own ----
const inRange = (from: string | null, to: string | null) => shown.value.filter(r => (!from || r.date >= from) && (!to || r.date <= to))
function printHtml(from: string | null = null, to: string | null = null) {
  const rows = inRange(from, to)
  const bank = hasMethod.value
  const range = rangeText(from, to, props.locale)
  return listPage({
    title: title.value,
    subtitle: `${range ? range + ' – ' : ''}${t(`تاریخ چاپ: ${todayJalali()} – تعداد: ${num(rows.length)} – از جدید به قدیم`, `Printed ${todayJalali()} – ${rows.length} receipts – newest first`)}${
      search.value || method.value || numberSearch.value || chequeSearch.value ? t(' – با فیلتر', ' – filtered') : ''}`,
    head: [t('ردیف', '#'), t('شماره رسید', 'Receipt no.'), ...(hasCheques.value ? [t(`شماره ${paper.value[0]}`, `${paper.value[1]} no.`)] : []),
      t('تاریخ', 'Date'), t('پرداخت کننده', 'Payer'), t('بابت', 'Purpose'), whereTitle.value,
      ...(bank ? [t('نوع واریز', 'Kind of deposit'), t('پیگیری / شبا', 'Tracking / Sheba')] : []), t('مبلغ (ریال)', 'Amount (Rial)'), t('وضعیت', 'Status')],
    rows: rows.map((r, i) => [num(i + 1), num(r.number), ...(hasCheques.value ? [chequeNumbers(r).join(fa.value ? '، ' : ', ')] : []),
      r.date, `${r.payerName} (${r.payerDetailCode})`, r.purpose, where(r),
      ...(bank ? [depositMethodName(r.method, fa.value), [r.trackingNumber, r.payerSheba].filter(Boolean).join(' – ')] : []), money(r.totalRial), statusText(r)]),
    total: { label: t('جمع (بدون ابطال‌شده‌ها و حذف‌شده‌ها)', 'Total (without cancelled and deleted)'), value: money(rows.filter(r => r.state === 'Recorded').reduce((sum, r) => sum + r.totalRial, 0)), cellsAfter: 1 },
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
  if (!window.confirm(t(`رسید شمارهٔ ${num(r.number)} (${r.payerName} – ${money(r.totalRial)} ریال) حذف شود؟ رسید با عبارت «حذف شده» در فهرست می‌ماند و شمارهٔ آن به رسید دیگری داده نمی‌شود.`,
    `Delete receipt no. ${r.number} (${r.payerName} – ${money(r.totalRial)} Rial)? It stays in the list marked as deleted, and its number is not given to another receipt.`))) return
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
        <!-- item 20: a receipt is found by the number of one of its cheques -->
        <label v-if="hasCheques" style="width: 10rem">{{ t(`شماره ${paper[0]}`, `${paper[1]} no.`) }} <input v-model="chequeSearch" dir="ltr" inputmode="numeric" :aria-label="t(`شماره ${paper[0]}`, `${paper[1]} no.`)" /></label>
        <label style="flex: 1; min-width: 14rem">{{ t('جستجو (تاریخ، پرداخت کننده، بابت، وضعیت)', 'Search (date, payer, purpose, status)') }} <input v-model="search" /></label>
        <label v-if="hasMethod" style="width: 20rem">{{ t('نوع واریز', 'Kind of deposit') }}
          <SearchSelect v-model="method" :options="methodChoices" :empty-label="t('همه', 'All')" :aria-label="t('نوع واریز', 'Kind of deposit')" />
        </label>
        <button type="button" class="tr-secondary" :disabled="loading" @click="load()">{{ t('به‌روزرسانی', 'Refresh') }}</button>
        <!-- item 10: «چاپ» asks for the range of the printed report -->
        <PrintRange :dates="shown.map(r => r.date)" :count="(from, to) => inRange(from, to).length" :locale="locale" :disabled="!shown.length"
                    @print="(from, to) => printPage(printHtml(from, to))" />
      </div>
      <p class="muted small" style="margin: .25rem 0 0">{{ t('وضعیت‌ها خودکار به‌روز می‌شوند (پس از هر ثبت و هر ۱۵ ثانیه).', 'Statuses refresh by themselves (after every save and every 15 seconds).') }}
        <template v-if="hasCheques"> {{ t(`با کلیک یا دبل‌کلیک روی هر ردیف، «کارت ${paper[0]}» آن (سیر مراحل) زیر فهرست باز می‌شود؛ با کلیک روی کارت بسته می‌شود.`,
          `Click or double-click a row to open its ${paper[1]} card (the course of its steps) under the list; click the card to close it.`) }}</template></p>
      <p v-if="error" class="tr-msg error" role="status">{{ error }}</p>
      <div class="tr-box">
        <table class="tr-table">
          <thead>
            <tr>
              <th>{{ t('شماره', 'No.') }}</th><th v-if="hasCheques">{{ t(`شماره ${paper[0]}`, `${paper[1]} no.`) }}</th><th>{{ t('تاریخ', 'Date') }}</th><th>{{ t('پرداخت کننده', 'Payer') }}</th><th>{{ t('بابت', 'Purpose') }}</th>
              <th>{{ whereTitle }}</th><th v-if="hasMethod">{{ t('نوع واریز', 'Kind of deposit') }}</th>
              <th>{{ t('مبلغ (ریال)', 'Amount (Rial)') }}</th><th>{{ t('وضعیت', 'Latest status') }}</th><th />
            </tr>
          </thead>
          <tbody>
            <tr v-for="r in shown" :key="r.id" :class="{ cancelled: r.state === 'Cancelled', deleted: r.state === 'Deleted', selected: r.id === editingId || r.id === cardsOf,
                                                       clickable: hasCheques && r.state !== 'Deleted' }" @click="showCards(r)" @mousedown="noSelectOnDoubleClick">
              <td class="num">{{ num(r.number) }}</td>
              <!-- item 20: the numbers of the receipt's cheques; one number opens that cheque's card -->
              <td v-if="hasCheques" class="num">
                <div v-for="c in r.chequeStatuses ?? []" :key="c.id ?? c.number ?? ''">
                  <button v-if="c.id && r.state !== 'Deleted'" type="button" class="tr-link tr-cheque-no" :title="t(`کارت ${paper[0]}`, `${paper[1]} card`)" @click.stop="showCards(r, c.id)">{{ c.number }}</button>
                  <template v-else>{{ c.number }}</template>
                </div>
              </td>
              <td class="num">{{ r.date }}</td>
              <td>{{ r.payerName }} <span class="muted small">{{ r.payerDetailCode }}</span></td>
              <td>{{ r.purpose }} <span class="muted small">{{ r.purposeGroupName }}</span></td>
              <td>{{ where(r) }}</td>
              <td v-if="hasMethod">{{ depositMethodName(r.method, fa) }}
                <div v-if="r.trackingNumber || r.payerSheba" class="muted small num">{{ [r.trackingNumber, r.payerSheba].filter(Boolean).join(' – ') }}</div>
              </td>
              <td class="num">{{ money(r.totalRial) }}<span v-if="r.amount !== null && r.currency !== 'IRR'" class="muted small"> ({{ money(r.amount) }} {{ r.currency }})</span></td>
              <td>
                <span v-if="r.state === 'Deleted'" class="tr-deleted">{{ t('حذف شده', 'Deleted') }}</span>
                <span v-else-if="r.state === 'Cancelled'" class="tr-tag cancelled">{{ t('ابطال شده', 'Cancelled') }}</span>
                <template v-else-if="r.kind === 'Instruments'">
                  <div v-for="g in groupChequeStatuses(r.chequeStatuses, locale)" :key="g.state + g.name">
                    <span v-if="r.instrumentCount > 1" class="muted small">{{ t(`${num(g.count)} برگ:`, `${g.count} ×`) }}</span> {{ g.name }}
                    <span v-if="g.due" class="tr-tag due">{{ dueMark(locale) }}<template v-if="g.count > 1"> ({{ num(g.due) }})</template></span>
                  </div>
                </template>
                <template v-else>{{ t('ثبت شده', 'Recorded') }}</template>
              </td>
              <td @click.stop>
                <!-- items 2 and 6: opens the receipt in the form above; the server says when it can no longer be corrected -->
                <template v-if="r.state === 'Recorded'">
                  <button type="button" class="tr-link" @click="emit('edit', r.id)">{{ t('ویرایش', 'Correct') }}</button>
                  <button type="button" class="tr-link danger" :disabled="busy" @click="remove(r)">{{ t('حذف', 'Delete') }}</button>
                </template>
                <!-- item 21: the printed receipt -->
                <button v-if="r.state !== 'Deleted'" type="button" class="tr-link" @click="emit('print', r.id)">{{ t('چاپ رسید', 'Print receipt') }}</button>
              </td>
            </tr>
            <tr v-if="!shown.length"><td :colspan="hasMethod || hasCheques ? 9 : 8" class="muted">{{ loading ? t('در حال خواندن…', 'Loading…')
              : receipts.length ? t('با این جستجو رسیدی پیدا نشد.', 'No receipt matches.') : t('رسیدی در این سال مالی ثبت نشده است.', 'No receipts in this fiscal year.') }}</td></tr>
          </tbody>
          <tfoot v-if="shown.length">
            <tr><td :colspan="hasMethod || hasCheques ? 6 : 5"><strong>{{ t('جمع (بدون ابطال‌شده‌ها و حذف‌شده‌ها)', 'Total (without cancelled and deleted)') }}</strong></td><td class="num"><strong>{{ money(total) }}</strong></td><td colspan="2" /></tr>
          </tfoot>
        </table>
      </div>
      <!-- items 14 and 20: the cheque cards of the row clicked -->
      <div v-if="cards.length" ref="cardBox">
        <ChequeCard v-for="id in cards" :key="id" :cheque-id="id" :refresh-key="cardKey" close-on-click :api-base="apiBase" :locale="locale" :token="token"
                    :bank-account-name="bankAccountTitle" :ledger-account-name="L.accountName" @close="closeCard(id)" @confirmed="load(true)" />
      </div>
    </template>
  </section>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
.tr-panel { margin-top: 1.5rem; padding-top: .75rem; border-top: 1px dashed #cbd2d9; }
.tr-filters { align-items: end; margin-top: .5rem; }
.tr-table td:last-child { white-space: nowrap; }
.tr-cheque-no { padding: 0 .2rem; font-variant-numeric: tabular-nums; text-decoration: underline dotted; }
/* The list scrolls inside the free space under the form; the header and the total stay in view. */
.tr-box { max-height: 26rem; overflow: auto; border: 1px solid #e4e7eb; border-radius: 8px; margin-top: .5rem; }
.tr-box .tr-table { margin-top: 0; }
.tr-box thead th { position: sticky; top: 0; background: #f5f7fa; z-index: 1; }
.tr-box tfoot td { position: sticky; bottom: 0; background: #f5f7fa; }
</style>
