<script setup lang="ts">
/**
 * Receipts of a fiscal year, newest first, with a search box. Opening a receipt shows its cheques and notes with their state and
 * the steps the server allows next (send for collection, collected, bounced, return, endorse). A cash or bank receipt typed by
 * mistake is cancelled with a reason; a cheque is returned to the payer instead.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, formatAmount, normaliseJalali, searchKey, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'

interface Instrument {
  id: string; kind: string; number: string | null; bankName: string | null; amount: number; currency: string; amountInRial: number
  dueDate: string | null; state: string; isPostDated: boolean; allowedActions: string[]; version: number
  details: { branchName: string | null; accountNumber: string | null; sayadId: string | null; guarantors: { name: string; nationalCode: string | null }[] | null } | null
}
interface Receipt {
  id: string; kind: 'Cash' | 'BankDeposit' | 'Instruments'; fiscalYear: number; number: number; date: string; payerDetailCode: string; payerName: string
  purposeGroupName: string; purpose: string; reference1: string | null; reference2: string | null; departmentId: string | null; salesProceeds: boolean; note: string | null
  accountKind: string | null; accountId: string | null; method: string | null; trackingNumber: string | null; amount: number | null; currency: string | null
  rateToRial: number | null; totalRial: number; instrumentCount: number; state: string; cancelledOn: string | null; cancelReason: string | null
  recordedBy: string; version: number; instruments: Instrument[] | null
}

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
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
  Cash: ['نقدی', 'Cash'], BankDeposit: ['واریز به حساب', 'Bank deposit'], Instruments: ['چک و سفته', 'Cheques and notes'],
}
const kindChoices = computed<Choice[]>(() => Object.entries(kindNames).map(([value, [f, e]]) => ({ value, label: fa.value ? f : e })))
const methodNames: Record<string, [string, string]> = {
  DepositSlip: ['فیش واریزی', 'Deposit slip'], Transfer: ['حواله / انتقال', 'Transfer'], Pos: ['کارتخوان', 'POS'], CardToCard: ['کارت به کارت', 'Card to card'],
}
const instrumentKinds: Record<string, [string, string]> = {
  OrdinaryCheque: ['چک', 'Cheque'], BankCheque: ['چک بانکی', 'Bank cheque'], GuaranteeCheque: ['چک تضمینی', 'Guarantee cheque'],
  PromissoryNote: ['سفته', 'Note'], GuaranteeNote: ['سفته تضمینی', 'Guarantee note'], Instalment: ['اقساط', 'Instalment'], Other: ['سایر', 'Other'],
}
const stateNames: Record<string, [string, string]> = {
  // Owner's report no. 4, item 9: «در جریان وصول نزد صندوق» → «در جریان وصول نزد بانک».
  InCashDesk: ['در جریان وصول نزد صندوق', 'In the cash desk'], DepositedForCollection: ['در جریان وصول نزد بانک', 'At the bank for collection'], Cleared: ['وصول شده', 'Collected'],
  Bounced: ['برگشت شده', 'Bounced'], ReturnedToIssuer: ['عودت شده', 'Returned'], Endorsed: ['واگذار به سایرین', 'Endorsed'],
}
const actionNames: Record<string, [string, string]> = {
  DepositForCollection: ['واگذاری جهت وصول', 'Send for collection'], RecordCleared: ['ثبت وصول', 'Record collected'],
  RecordBounced: ['ثبت برگشت', 'Record bounced'], ReturnToIssuer: ['عودت', 'Return to payer'], Endorse: ['خرج (واگذاری به سایرین)', 'Endorse'],
}
const name = (m: Record<string, [string, string]>, k: string | null | undefined) => (k && m[k] ? (fa.value ? m[k][0] : m[k][1]) : k ?? '')

const receipts = ref<Receipt[]>([])
const search = ref('')
const selected = ref<Receipt | null>(null)
const message = ref<{ kind: 'error' | 'ok'; text: string } | null>(null)
const show = (k: 'error' | 'ok', text: string) => { message.value = { kind: k, text } }

const shown = computed(() => {
  const key = searchKey(search.value)
  if (!key) return receipts.value
  return receipts.value.filter(r => searchKey(`${r.number} ${r.payerName} ${r.payerDetailCode} ${r.purpose} ${r.purposeGroupName} ${r.trackingNumber ?? ''} ${r.date}`).includes(key))
})
const totalShown = computed(() => shown.value.filter(r => r.state !== 'Cancelled').reduce((s, r) => s + r.totalRial, 0))

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
  const r = await api.get<Receipt>(`/api/cashflow/receipts/${id}`)
  if (r.ok) selected.value = r.value
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

// ---- next step of a cheque or note ----
interface Pending { id: string; action: string; date: string; destination: string; note: string }
const pending = ref<Pending | null>(null)
const needsDestination = new Set(['DepositForCollection', 'Endorse'])
async function confirmAction() {
  const p = pending.value
  if (!p || !selected.value) return
  const date = normaliseJalali(p.date)
  if (!date) return show('error', translate('FiscalDate.Invalid', props.locale))
  const r = await api.post<string>(`/api/cashflow/instruments/${p.id}/actions/${p.action}`, { date, destination: p.destination || null, note: p.note || null })
  if (!r.ok) return show('error', translate(r.code, props.locale))
  pending.value = null
  show('ok', t('عملیات ثبت شد.', 'Done.'))
  await open(selected.value.id)
}

defineExpose({ load })
watch([year, kind], () => { selected.value = null; void load() })
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
      <label style="flex: 1; min-width: 14rem">{{ t('جستجو (شماره، پرداخت کننده، بابت، پیگیری)', 'Search (number, payer, purpose, tracking)') }}
        <input v-model="search" />
      </label>
    </div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <div class="tr-scroll">
      <table class="tr-table">
        <thead>
          <tr>
            <th>{{ t('شماره', 'No.') }}</th><th>{{ t('نوع', 'Kind') }}</th><th>{{ t('تاریخ', 'Date') }}</th><th>{{ t('پرداخت کننده', 'Payer') }}</th>
            <th>{{ t('بابت', 'Purpose') }}</th><th>{{ t('صندوق / حساب', 'Cash desk / account') }}</th><th>{{ t('مبلغ', 'Amount') }}</th>
            <th>{{ t('ریالی', 'Rial') }}</th><th>{{ t('وضعیت', 'State') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in shown" :key="r.id" :class="['clickable', { cancelled: r.state === 'Cancelled', selected: selected?.id === r.id }]" @click="open(r.id)">
            <td class="num">{{ num(r.number) }}</td>
            <td>{{ name(kindNames, r.kind) }}<span v-if="r.method" class="muted small"> – {{ name(methodNames, r.method) }}</span></td>
            <td class="num">{{ r.date }}</td>
            <td>{{ r.payerName }} <span class="muted small">{{ r.payerDetailCode }}</span></td>
            <td>{{ r.purpose }} <span class="muted small">{{ r.purposeGroupName }}</span></td>
            <td>{{ r.kind === 'Instruments' ? t(`${num(r.instrumentCount)} برگ`, `${r.instrumentCount} items`) : accountName(r) }}</td>
            <td class="num">{{ r.amount !== null && r.currency !== 'IRR' ? `${money(r.amount)} ${r.currency}` : '' }}</td>
            <td class="num">{{ money(r.totalRial) }}</td>
            <td><span v-if="r.state === 'Cancelled'" class="tr-tag cancelled">{{ t('ابطال شده', 'Cancelled') }}</span></td>
          </tr>
          <tr v-if="!shown.length"><td colspan="9" class="muted">{{ t('رسیدی ثبت نشده است.', 'No receipts.') }}</td></tr>
        </tbody>
        <tfoot v-if="shown.length">
          <tr><td colspan="7"><strong>{{ t('جمع (بدون ابطال‌شده‌ها)', 'Total (without cancelled)') }}</strong></td><td class="num"><strong>{{ money(totalShown) }}</strong></td><td /></tr>
        </tfoot>
      </table>
    </div>

    <section v-if="selected" style="margin-top: 1.25rem">
      <h3>{{ t(`رسید ${name(kindNames, selected.kind)} شمارهٔ ${num(selected.number)}`, `${name(kindNames, selected.kind)} receipt no. ${selected.number}`) }}</h3>
      <p class="small">
        {{ t('تاریخ', 'Date') }}: <span class="num">{{ selected.date }}</span> ·
        {{ t('پرداخت کننده', 'Payer') }}: {{ selected.payerName }} ({{ selected.payerDetailCode }}) ·
        {{ selected.purposeGroupName }}: {{ selected.purpose }}
        <template v-if="selected.reference1"> · {{ selected.reference1 }}</template>
        <template v-if="selected.reference2"> · {{ selected.reference2 }}</template>
        <template v-if="selected.departmentId"> · {{ t('بخش دریافت کننده', 'Receiving department') }}: {{ L.departments.value.find(d => d.id === selected!.departmentId)?.name }}</template>
        <template v-if="selected.salesProceeds"> · {{ t('وجوه حاصل فروش', 'Sales proceeds') }}</template>
        <template v-if="selected.trackingNumber"> · {{ t('پیگیری', 'Tracking') }}: {{ selected.trackingNumber }}</template>
        <template v-if="selected.rateToRial"> · {{ t('نرخ', 'Rate') }}: {{ money(selected.rateToRial) }}</template>
        · {{ t('ثبت', 'By') }}: {{ selected.recordedBy }}
      </p>
      <p v-if="selected.note" class="small">{{ selected.note }}</p>
      <p v-if="selected.state === 'Cancelled'" class="tr-msg error">{{ t(`ابطال شده در ${selected.cancelledOn}: ${selected.cancelReason}`, `Cancelled on ${selected.cancelledOn}: ${selected.cancelReason}`) }}</p>

      <div v-if="selected.kind !== 'Instruments' && selected.state === 'Recorded'" class="tr-buttons">
        <button v-if="!cancel.open" type="button" class="tr-secondary" @click="cancel.open = true">{{ t('ابطال رسید', 'Cancel receipt') }}</button>
        <template v-else>
          <label>{{ t('تاریخ ابطال', 'Date') }} <input v-model="cancel.date" dir="ltr" /></label>
          <label style="flex: 1">{{ t('علت ابطال', 'Reason') }} <input v-model="cancel.reason" /></label>
          <button type="button" @click="confirmCancel">{{ t('تایید ابطال', 'Confirm') }}</button>
          <button type="button" class="tr-secondary" @click="cancel.open = false">{{ t('انصراف', 'Back') }}</button>
        </template>
      </div>

      <template v-if="selected.instruments?.length">
        <div v-if="pending" class="tr-buttons" style="margin: .75rem 0; padding: .75rem; background: #f5f7fa; border-radius: 8px">
          <strong>{{ name(actionNames, pending.action) }}</strong>
          <label>{{ t('تاریخ', 'Date') }} <input v-model="pending.date" dir="ltr" /></label>
          <label v-if="needsDestination.has(pending.action)">{{ pending.action === 'Endorse' ? t('گیرنده', 'Endorsee') : t('حساب واگذاری', 'Collection account') }}
            <input v-model="pending.destination" />
          </label>
          <label v-if="pending.action === 'RecordBounced' || pending.action === 'ReturnToIssuer'">{{ t('علت / توضیح', 'Reason') }} <input v-model="pending.note" /></label>
          <button type="button" @click="confirmAction">{{ t('تایید', 'Confirm') }}</button>
          <button type="button" class="tr-secondary" @click="pending = null">{{ t('انصراف', 'Back') }}</button>
        </div>
        <div class="tr-scroll">
          <table class="tr-table">
            <thead>
              <tr>
                <th>{{ t('نوع', 'Kind') }}</th><th>{{ t('شماره', 'No.') }}</th><th>{{ t('بانک / ضامن', 'Bank / guarantor') }}</th><th>{{ t('سررسید', 'Due') }}</th>
                <th>{{ t('مبلغ', 'Amount') }}</th><th>{{ t('ریالی', 'Rial') }}</th><th>{{ t('وضعیت', 'State') }}</th><th />
              </tr>
            </thead>
            <tbody>
              <tr v-for="i in selected.instruments" :key="i.id">
                <td>{{ name(instrumentKinds, i.kind) }}
                  <span :class="['tr-tag', i.isPostDated ? 'later' : 'current']">{{ i.isPostDated ? t('مدت‌دار', 'Post-dated') : t('روز', 'Current') }}</span>
                </td>
                <td class="num">{{ i.number }}<div v-if="i.details?.sayadId" class="muted small">{{ t('صیاد', 'Sayad') }}: {{ i.details.sayadId }}</div></td>
                <td>
                  <template v-if="i.bankName">{{ i.bankName }}<span v-if="i.details?.branchName" class="muted small"> – {{ i.details.branchName }}</span>
                    <div v-if="i.details?.accountNumber" class="muted small">{{ i.details.accountNumber }}</div></template>
                  <template v-else>{{ (i.details?.guarantors ?? []).map(g => g.name).join('، ') }}</template>
                </td>
                <td class="num">{{ i.dueDate }}</td>
                <td class="num">{{ money(i.amount) }} <span v-if="i.currency !== 'IRR'">{{ i.currency }}</span></td>
                <td class="num">{{ money(i.amountInRial) }}</td>
                <td>{{ name(stateNames, i.state) }}</td>
                <td>
                  <button v-for="a in i.allowedActions.filter(x => x !== 'DepositForCollection')" :key="a" type="button" class="tr-link"
                          @click="pending = { id: i.id, action: a, date: todayJalali(), destination: '', note: '' }">{{ name(actionNames, a) }}</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </section>
  </div>
</template>

<style scoped src="./receipts.css"></style>
