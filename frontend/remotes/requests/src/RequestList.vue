<script setup lang="ts">
/**
 * Payment requests of a fiscal year. In `approved` mode it is «فهرست درخواست وجه های تایید شده جهت پرداخت»: the requests treasury
 * is to pay, sorted by payment method and then priority (manual), or by priority alone. A person allowed to approve ticks
 * «تایید» (approve); the approval can be taken back while nothing is paid; a request is cancelled («ابطال») with a reason.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, formatAmount, searchKey, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { methods, stateNames, useLookups } from './lookups'
import { openFile, sizeText, type RequestFile } from './files'

interface Request {
  id: string; number: number; date: string; requesterName: string; beneficiaryName: string; beneficiaryDetailCode: string; purposeGroupName: string
  purpose: string; reference1: string | null; amount: number; currency: string; method: string; priority: number; transferAccount: string | null
  cardNumber: string | null; payFromKind: string | null; payFromId: string | null; dueDate: string | null; hasMainBeneficiaries: boolean
  attachments: number; note: string | null; state: string; recordedBy: string; approvedBy: string | null; cancelReason: string | null; version: number
  files: RequestFile[] | null
  paidAmount: number; payments: { key: string; kind: string; reference: string | null; amount: number; date: string; userId: string }[] | null
  /** «ثبت انتقال (خرج چک)»: the received cheque the request hands on (owner, 19 Nov 2026). */
  cheque: { instrumentId: string; number: string; dueDate: string | null; payerName: string; bankName: string | null; sayadId: string | null } | null
}

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number; approved?: boolean }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined, approved: false })
const emit = defineEmits<{ edit: [id: string] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => (fa.value ? n.toLocaleString('fa-IR', { useGrouping: false }) : String(n))
const L = useLookups(api)

const thisYear = Number(todayJalali().slice(0, 4))
const year = ref(String(props.fiscalYear ?? thisYear))
const yearChoices = computed<Choice[]>(() => Array.from({ length: 6 }, (_, i) => thisYear - 4 + i).map(y => ({ value: String(y), label: num(y) })))
const state = ref('')
const stateChoices = computed<Choice[]>(() => Object.entries(stateNames).map(([value, [f, e]]) => ({ value, label: fa.value ? f : e })))
const order = ref('method')
const orderChoices = computed<Choice[]>(() => [
  { value: 'method', label: t('نوع پرداخت، سپس اولویت', 'Payment method, then priority') },
  { value: 'priority', label: t('اولویت', 'Priority') },
])
const methodName = (m: string) => { const x = methods.find(y => y.value === m); return x ? (fa.value ? x.fa : x.en) : m }
const stateName = (s: string) => (stateNames[s] ? (fa.value ? stateNames[s][0] : stateNames[s][1]) : s)

const requests = ref<Request[]>([])
const search = ref('')
const selected = ref<Request | null>(null)
const message = ref<{ kind: 'error' | 'ok'; text: string } | null>(null)
const show = (kind: 'error' | 'ok', text: string) => { message.value = { kind, text } }

const shown = computed(() => {
  const key = searchKey(search.value)
  let list = key
    ? requests.value.filter(r => searchKey(`${r.number} ${r.requesterName} ${r.beneficiaryName} ${r.beneficiaryDetailCode} ${r.purpose} ${r.purposeGroupName} ${r.reference1 ?? ''}`).includes(key))
    : requests.value
  if (props.approved && order.value === 'priority') list = [...list].sort((a, b) => a.priority - b.priority || a.number - b.number)
  return list
})
const totals = computed(() => {
  const by = new Map<string, number>()
  for (const r of shown.value) if (r.state !== 'Cancelled') by.set(r.currency, (by.get(r.currency) ?? 0) + r.amount)
  return [...by.entries()]
})

function payFromName(r: Request) {
  if (!r.payFromId) return ''
  if (r.payFromKind === 'CashDesk') return L.desks.value.find(d => d.id === r.payFromId)?.name ?? ''
  const a = L.bankAccounts.value.find(x => x.id === r.payFromId)
  return a ? L.accountTitle(a) : ''
}

async function load() {
  const q = new URLSearchParams({ year: year.value })
  const s = props.approved ? 'Approved' : state.value
  if (s) q.set('state', s)
  const r = await api.get<Request[]>(`/api/requests/payment-requests?${q}`)
  if (r.ok) {
    requests.value = r.value
    if (selected.value) selected.value = r.value.find(x => x.id === selected.value!.id) ?? null
  } else show('error', translate(r.code, props.locale))
}

const cancel = reactive({ open: false, reason: '' })
async function change(r: Request, what: 'approve' | 'withdraw-approval' | 'cancel') {
  const res = await api.post<string>(`/api/requests/payment-requests/${r.id}/${what}`, { version: r.version, reason: what === 'cancel' ? cancel.reason : null })
  if (!res.ok) return show('error', translate(res.code, props.locale))
  if (what === 'cancel') Object.assign(cancel, { open: false, reason: '' })
  show('ok', what === 'approve' ? t(`درخواست شمارهٔ ${num(r.number)} تایید شد و به فهرست تایید شده جهت پرداخت رفت.`, `Request no. ${r.number} approved for payment.`)
    : what === 'cancel' ? t(`درخواست شمارهٔ ${num(r.number)} ابطال شد.`, `Request no. ${r.number} cancelled.`)
    : t(`تایید درخواست شمارهٔ ${num(r.number)} برداشته شد.`, `Approval of request no. ${r.number} withdrawn.`))
  await load()
}

defineExpose({ load })
watch([year, state], () => { selected.value = null; void load() })
onMounted(async () => { await L.load(); await load() })
</script>

<template>
  <div>
    <div class="tr-buttons">
      <label style="width: 9rem">{{ t('سال مالی', 'Fiscal year') }}
        <SearchSelect v-model="year" :options="yearChoices" :aria-label="t('سال مالی', 'Fiscal year')" />
      </label>
      <label v-if="!approved" style="width: 11rem">{{ t('وضعیت', 'State') }}
        <SearchSelect v-model="state" :options="stateChoices" :empty-label="t('همه', 'All')" :aria-label="t('وضعیت', 'State')" />
      </label>
      <label v-else style="width: 15rem">{{ t('مرتب‌سازی', 'Sort by') }}
        <SearchSelect v-model="order" :options="orderChoices" :aria-label="t('مرتب‌سازی', 'Sort by')" />
      </label>
      <label style="flex: 1; min-width: 14rem">{{ t('جستجو (شماره، درخواست کننده، ذینفع، بابت)', 'Search (number, requester, beneficiary, purpose)') }}
        <input v-model="search" />
      </label>
    </div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <div class="tr-scroll">
      <table class="tr-table">
        <thead>
          <tr>
            <th>{{ t('شماره', 'No.') }}</th><th>{{ t('تاریخ', 'Date') }}</th><th>{{ t('درخواست کننده', 'Requester') }}</th><th>{{ t('ذینفع', 'Beneficiary') }}</th>
            <th>{{ t('بابت', 'Purpose') }}</th><th>{{ t('نوع پرداخت', 'Method') }}</th><th>{{ t('مبلغ', 'Amount') }}</th><th>{{ t('اولویت', 'Priority') }}</th>
            <th>{{ t('وضعیت', 'State') }}</th><th />
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in shown" :key="r.id" :class="['clickable', { cancelled: r.state === 'Cancelled', selected: selected?.id === r.id }]" @click="selected = r">
            <td class="num">{{ num(r.number) }}</td>
            <td class="num">{{ r.date }}</td>
            <td>{{ r.requesterName }}</td>
            <td>{{ r.beneficiaryName }} <span class="muted small">{{ r.beneficiaryDetailCode }}</span></td>
            <td>{{ r.purpose }} <span class="muted small">{{ r.purposeGroupName }}</span> <span v-if="r.files?.length" class="tr-tag" :title="t('تصاویر ضمائم', 'Scans')">📎 {{ num(r.files.length) }}</span></td>
            <td>{{ methodName(r.method) }}
              <div v-if="r.cheque" class="muted small">{{ t('چک', 'cheque') }} <span class="num">{{ r.cheque.number }}</span> · {{ t('سررسید', 'due') }} <span class="num">{{ r.cheque.dueDate }}</span></div>
            </td>
            <td class="num">{{ money(r.amount) }} <span v-if="r.currency !== 'IRR'">{{ r.currency }}</span>
              <div v-if="r.paidAmount && r.state !== 'Paid'" class="muted small">{{ t(`پرداخت‌شده ${money(r.paidAmount)} – مانده ${money(r.amount - r.paidAmount)}`, `paid ${money(r.paidAmount)} – left ${money(r.amount - r.paidAmount)}`) }}</div></td>
            <td class="num">{{ num(r.priority) }}</td>
            <td><span :class="['tr-tag', r.state === 'Approved' ? 'current' : r.state === 'Cancelled' ? 'cancelled' : '']">{{ stateName(r.state) }}</span></td>
            <td class="tr-actions" @click.stop>
              <button v-if="r.state === 'Registered'" type="button" class="tr-link" @click="emit('edit', r.id)">{{ t('اصلاح', 'Correct') }}</button>
              <button v-if="r.state === 'Registered'" type="button" class="tr-link" @click="change(r, 'approve')">{{ t('تایید', 'Approve') }}</button>
              <button v-if="r.state === 'Approved' && !r.paidAmount" type="button" class="tr-link" @click="change(r, 'withdraw-approval')">{{ t('برداشتن تایید', 'Withdraw approval') }}</button>
            </td>
          </tr>
          <tr v-if="!shown.length"><td colspan="10" class="muted">{{ approved ? t('درخواست تایید شده‌ای برای پرداخت نیست.', 'No approved request to pay.') : t('درخواستی ثبت نشده است.', 'No requests.') }}</td></tr>
        </tbody>
        <tfoot v-if="totals.length">
          <tr><td colspan="6"><strong>{{ t('جمع (بدون ابطال‌شده‌ها)', 'Total (without cancelled)') }}</strong></td>
            <td class="num" colspan="4"><strong v-for="[c, s] in totals" :key="c" style="margin-inline-end: 1rem">{{ money(s) }} {{ c === 'IRR' ? t('ریال', 'Rial') : c }}</strong></td></tr>
        </tfoot>
      </table>
    </div>

    <section v-if="selected" style="margin-top: 1.25rem">
      <h3>{{ t(`درخواست وجه شمارهٔ ${num(selected.number)}`, `Payment request no. ${selected.number}`) }} <span class="tr-tag">{{ stateName(selected.state) }}</span></h3>
      <p class="small">
        {{ t('ثبت', 'By') }}: {{ selected.recordedBy }}<template v-if="selected.approvedBy"> · {{ t('تایید', 'Approved by') }}: {{ selected.approvedBy }}</template>
        <template v-if="selected.reference1"> · {{ selected.reference1 }}</template>
        <template v-if="selected.dueDate"> · {{ t('سررسید', 'Due') }}: <span class="num">{{ selected.dueDate }}</span></template>
        <template v-if="selected.cheque"> · {{ t('چک دریافتی', 'Received cheque') }}: <span class="num">{{ selected.cheque.number }}</span><template v-if="selected.cheque.bankName"> {{ selected.cheque.bankName }}</template>
          · {{ t('تاریخ سررسید چک', 'Cheque due') }}: <span class="num">{{ selected.cheque.dueDate }}</span> · {{ t('واگذارنده چک', 'Given by') }}: {{ selected.cheque.payerName }}</template>
        <template v-if="selected.transferAccount"> · {{ t('به حساب', 'To account') }}: <span dir="ltr">{{ selected.transferAccount }}</span></template>
        <template v-if="selected.cardNumber"> · {{ t('به کارت', 'To card') }}: <span dir="ltr">{{ selected.cardNumber }}</span></template>
        <template v-if="payFromName(selected)"> · {{ t('پرداخت از', 'Pay from') }}: {{ payFromName(selected) }}</template>
        <template v-if="selected.hasMainBeneficiaries"> · {{ t('پرداخت جمعی دارد', 'Paid on to main beneficiaries') }}</template>
        <template v-if="selected.attachments"> · {{ t('ضمائم', 'Attachments') }}: {{ num(selected.attachments) }}</template>
      </p>
      <p v-if="selected.note" class="small">{{ selected.note }}</p>
      <p v-if="selected.files?.length" class="small">{{ t('تصاویر ضمائم', 'Scans') }}:
        <template v-for="f in selected.files" :key="f.id">
          <button type="button" class="tr-link" @click="openFile(apiBase, token, selected!.id, f.id)">{{ f.fileName }}</button>
          <span class="muted">({{ sizeText(f.size, fa) }})</span>
        </template>
      </p>
      <!-- Payments against the request (answer to Q36): endorsed received cheques. -->
      <div v-if="selected.payments?.length" class="small">
        <strong>{{ t('پرداخت‌ها', 'Payments') }}</strong>
        <ul>
          <li v-for="p in selected.payments" :key="p.key">
            <span class="num">{{ p.date }}</span> – {{ p.kind === 'EndorsedCheque' ? t('ثبت انتقال (خرج) چک یا سفتهٔ دریافتی', 'Transferred (endorsed) received cheque or note') : p.kind }} {{ p.reference }} –
            <span class="num">{{ money(p.amount) }}</span> <span class="muted">({{ p.userId }})</span>
          </li>
        </ul>
        <span>{{ t(`جمع پرداخت ${money(selected.paidAmount)} – مانده ${money(selected.amount - selected.paidAmount)}`, `Paid ${money(selected.paidAmount)} – left ${money(selected.amount - selected.paidAmount)}`) }}</span>
      </div>
      <p v-if="selected.cancelReason" class="tr-msg error">{{ t('علت ابطال', 'Reason') }}: {{ selected.cancelReason }}</p>
      <div v-if="(selected.state === 'Registered' || selected.state === 'Approved') && !selected.paidAmount" class="tr-buttons">
        <button v-if="!cancel.open" type="button" class="tr-secondary" @click="cancel.open = true">{{ t('ابطال درخواست', 'Cancel request') }}</button>
        <template v-else>
          <label style="flex: 1">{{ t('علت ابطال', 'Reason') }} <input v-model="cancel.reason" /></label>
          <button type="button" @click="change(selected, 'cancel')">{{ t('تایید ابطال', 'Confirm') }}</button>
          <button type="button" class="tr-secondary" @click="cancel.open = false">{{ t('انصراف', 'Back') }}</button>
        </template>
      </div>
    </section>
  </div>
</template>

<style scoped src="./requests.css"></style>
<style scoped>
.tr-actions { white-space: nowrap; }
</style>
