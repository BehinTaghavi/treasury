<script setup lang="ts">
/**
 * Depositing received cheques at the bank for collection, and recording the bank's answer (owner's report no. 4, item 9).
 * Cheques «در جریان وصول نزد صندوق» (in the cash desk) are listed by due date (manual: "به ترتیب (Sort) سررسید"); the ticked ones
 * are deposited at one company bank account with the deposit slip number («شماره فیش واگذاری») and become «در جریان وصول نزد
 * بانک». Those at the bank are then recorded as collected or bounced (not before the due date, rule N2).
 * Each cheque is its own command (one aggregate per transaction); a refused cheque is listed with its reason, the others go on.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, formatAmount, normaliseJalali, searchKey, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'

interface Cheque {
  id: string; kind: string; number: string | null; bankName: string | null; amount: number; currency: string; amountInRial: number
  dueDate: string | null; counterparty: string; state: string; collectionAccountId: string | null; destination: string | null; isPostDated: boolean
  details: { branchName: string | null; sayadId: string | null } | null
}

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const L = useLookups(api)

const inDesk = ref<Cheque[]>([])
const atBank = ref<Cheque[]>([])
const chosen = reactive<Record<string, boolean>>({})
const filter = reactive({ dueUntil: '', search: '' })
const deposit = reactive({ date: todayJalali(), accountId: '', slip: '' })
const busy = ref(false)
const message = ref<{ kind: 'error' | 'ok' | 'warning'; text: string } | null>(null)
const show = (kind: 'error' | 'ok' | 'warning', text: string) => { message.value = { kind, text } }

const collectable = (c: Cheque) => c.kind === 'OrdinaryCheque' || c.kind === 'BankCheque'
const shownDesk = computed(() => {
  const until = normaliseJalali(filter.dueUntil)
  const key = searchKey(filter.search)
  return inDesk.value.filter(c => collectable(c)
    && (!until || (c.dueDate ?? '') <= until)
    && (!key || searchKey(`${c.number} ${c.counterparty} ${c.bankName ?? ''}`).includes(key)))
})
const selected = computed(() => shownDesk.value.filter(c => chosen[c.id]))
const selectedCurrency = computed(() => selected.value[0]?.currency ?? 'IRR')
const selectedTotal = computed(() => selected.value.reduce((s, c) => s + c.amount, 0))
const mixedCurrencies = computed(() => new Set(selected.value.map(c => c.currency)).size > 1)
/** A cheque is deposited at an account in its own currency. */
const accountChoices = computed<Choice[]>(() => L.bankAccounts.value
  .filter(a => a.isActive && a.currencyCode === selectedCurrency.value)
  .map(a => ({ value: a.id, label: L.accountTitle(a), hint: a.currencyCode })))
const accountName = (id: string | null) => { const a = L.bankAccounts.value.find(x => x.id === id); return a ? L.accountTitle(a) : '' }

function toggleAll(on: boolean) { for (const c of shownDesk.value) chosen[c.id] = on }

async function load() {
  const [d, b] = await Promise.all([
    api.get<Cheque[]>('/api/cashflow/instruments/received?state=InCashDesk'),
    api.get<Cheque[]>('/api/cashflow/instruments/received?state=DepositedForCollection'),
  ])
  if (d.ok) inDesk.value = d.value
  if (b.ok) atBank.value = b.value
  for (const k of Object.keys(chosen)) if (!inDesk.value.some(c => c.id === k)) delete chosen[k]
}

async function depositSelected() {
  message.value = null
  const date = normaliseJalali(deposit.date)
  if (!date) return show('error', translate('FiscalDate.Invalid', props.locale))
  if (!selected.value.length) return show('error', t('ابتدا چک‌ها را انتخاب کنید.', 'Tick the cheques first.'))
  if (mixedCurrencies.value) return show('error', t('چک‌های یک ارز را با هم واگذار کنید.', 'Deposit cheques of one currency together.'))
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
  const ok = t(`${done.toLocaleString('fa-IR')} چک به بانک واگذار شد (در جریان وصول نزد بانک).`, `${done} cheque(s) deposited at the bank.`)
  if (failed.length) show(done ? 'warning' : 'error', `${done ? ok + ' ' : ''}${t('واگذار نشد', 'Not deposited')} – ${failed.join('؛ ')}`)
  else { show('ok', ok); deposit.slip = '' }
}

// ---- the bank's answer ----
interface Answer { id: string; action: 'RecordCleared' | 'RecordBounced'; date: string; note: string }
const answer = ref<Answer | null>(null)
async function confirmAnswer() {
  const a = answer.value
  if (!a) return
  const date = normaliseJalali(a.date)
  if (!date) return show('error', translate('FiscalDate.Invalid', props.locale))
  const r = await api.post<string>(`/api/cashflow/instruments/${a.id}/actions/${a.action}`, { date, note: a.note || null })
  if (!r.ok) return show('error', translate(r.code, props.locale))
  answer.value = null
  show('ok', a.action === 'RecordCleared' ? t('وصول چک ثبت شد.', 'Recorded as collected.') : t('برگشت چک ثبت شد.', 'Recorded as bounced.'))
  await load()
}

onMounted(async () => { await L.load(); await load() })
</script>

<template>
  <div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <h3>{{ t('چک‌های در جریان وصول نزد صندوق', 'Cheques in the cash desk') }}</h3>
    <div class="tr-buttons">
      <label style="width: 10rem">{{ t('سررسید تا', 'Due until') }} <input v-model="filter.dueUntil" placeholder="1405/09/30" dir="ltr" /></label>
      <label style="flex: 1; min-width: 14rem">{{ t('جستجو (شماره، صادرکننده، بانک)', 'Search (number, drawer, bank)') }} <input v-model="filter.search" /></label>
    </div>
    <div class="tr-scroll">
      <table class="tr-table">
        <thead>
          <tr>
            <th><input type="checkbox" :checked="shownDesk.length > 0 && selected.length === shownDesk.length" :aria-label="t('همه', 'All')" @change="toggleAll(($event.target as HTMLInputElement).checked)" /></th>
            <th>{{ t('سررسید', 'Due') }}</th><th>{{ t('شماره چک', 'Cheque no.') }}</th><th>{{ t('بانک', 'Bank') }}</th><th>{{ t('صادرکننده / پرداخت کننده', 'Payer') }}</th>
            <th>{{ t('مبلغ', 'Amount') }}</th><th />
          </tr>
        </thead>
        <tbody>
          <tr v-for="c in shownDesk" :key="c.id" :class="{ selected: chosen[c.id] }">
            <td><input v-model="chosen[c.id]" type="checkbox" :aria-label="c.number ?? ''" /></td>
            <td class="num">{{ c.dueDate }}</td>
            <td class="num">{{ c.number }}</td>
            <td>{{ c.bankName }}<span v-if="c.details?.branchName" class="muted small"> – {{ c.details.branchName }}</span></td>
            <td>{{ c.counterparty }}</td>
            <td class="num">{{ money(c.amount) }} <span v-if="c.currency !== 'IRR'">{{ c.currency }}</span></td>
            <td><span :class="['tr-tag', c.isPostDated ? 'later' : 'current']">{{ c.isPostDated ? t('مدت‌دار', 'Post-dated') : t('روز', 'Current') }}</span></td>
          </tr>
          <tr v-if="!shownDesk.length"><td colspan="7" class="muted">{{ t('چکی نزد صندوق نیست.', 'No cheque in the cash desk.') }}</td></tr>
        </tbody>
      </table>
    </div>

    <fieldset style="margin-top: .75rem">
      <legend>{{ t('واگذاری چک‌های انتخاب‌شده جهت وصول', 'Deposit the ticked cheques for collection') }}</legend>
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

    <h3 style="margin-top: 1.5rem">{{ t('چک‌های در جریان وصول نزد بانک', 'Cheques at the bank for collection') }}</h3>
    <div v-if="answer" class="tr-buttons" style="margin: .5rem 0; padding: .75rem; background: #f5f7fa; border-radius: 8px">
      <strong>{{ answer.action === 'RecordCleared' ? t('ثبت وصول', 'Record collected') : t('ثبت برگشت', 'Record bounced') }}</strong>
      <label>{{ t('تاریخ', 'Date') }} <input v-model="answer.date" dir="ltr" /></label>
      <label v-if="answer.action === 'RecordBounced'" style="flex: 1">{{ t('علت برگشت', 'Reason') }} <input v-model="answer.note" /></label>
      <button type="button" @click="confirmAnswer">{{ t('تایید', 'Confirm') }}</button>
      <button type="button" class="tr-secondary" @click="answer = null">{{ t('انصراف', 'Back') }}</button>
    </div>
    <div class="tr-scroll">
      <table class="tr-table">
        <thead>
          <tr>
            <th>{{ t('سررسید', 'Due') }}</th><th>{{ t('شماره چک', 'Cheque no.') }}</th><th>{{ t('بانک چک', 'Drawer bank') }}</th><th>{{ t('صادرکننده', 'Payer') }}</th>
            <th>{{ t('مبلغ', 'Amount') }}</th><th>{{ t('واگذار شده به حساب', 'Deposited at') }}</th><th />
          </tr>
        </thead>
        <tbody>
          <tr v-for="c in atBank" :key="c.id">
            <td class="num">{{ c.dueDate }}</td>
            <td class="num">{{ c.number }}</td>
            <td>{{ c.bankName }}</td>
            <td>{{ c.counterparty }}</td>
            <td class="num">{{ money(c.amount) }} <span v-if="c.currency !== 'IRR'">{{ c.currency }}</span></td>
            <td>{{ accountName(c.collectionAccountId) || c.destination }}</td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="answer = { id: c.id, action: 'RecordCleared', date: todayJalali(), note: '' }">{{ t('وصول شد', 'Collected') }}</button>
              <button type="button" class="tr-link danger" @click="answer = { id: c.id, action: 'RecordBounced', date: todayJalali(), note: '' }">{{ t('برگشت خورد', 'Bounced') }}</button>
            </td>
          </tr>
          <tr v-if="!atBank.length"><td colspan="7" class="muted">{{ t('چکی در جریان وصول نزد بانک نیست.', 'No cheque at the bank.') }}</td></tr>
        </tbody>
      </table>
    </div>
    <p class="muted small" style="margin-top: .5rem">{{ fa ? 'سند حسابداری هر مرحله پس از تعیین حساب‌های آن (پرسش‌های بند ۸) نمایش داده می‌شود.' : 'The accounting voucher of each step is shown once its accounts are settled (questions on item 8).' }}</p>
  </div>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
.tr-actions { white-space: nowrap; }
</style>
