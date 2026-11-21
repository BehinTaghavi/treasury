<script setup lang="ts">
/**
 * «دریافت چک – نقل و انتقال (انتقال گیرنده)» (owner's report no. 7 of 20 Nov 2026): the cheques the company issued from one of its own
 * bank accounts for a transfer to another, seen from the receiving side. «در انتظار دریافت» lists the cheques that are only on the
 * payment side («پرداخت ثبت صیاد شد»); «ثبت دریافت» records such a cheque as a received cheque «نزد صندوق» (the same step as the button
 * of the payment form). «دریافت شده» lists the cheques that are on the receipts side with their latest status, the receiver's voucher
 * (بدهکار اسناد نزد صندوق، بستانکار وجوه در راه) with «تایید سند», and «برگرداندن ثبت دریافت» while no step is recorded on the cheque
 * and that voucher is not confirmed. The next steps — ثبت صیادی، واگذاری به بانک مقصد، وصول — are recorded in «عملیات چک‌های دریافتی»,
 * as for every received cheque.
 * Owner, 21 Nov 2026 (answer to Q79): the payer's Sayad registration is a step of its own on the payment side. Until it is recorded
 * the cheque is only «صادر شد», «ثبت صیادی» on the receipts side waits for it, and the cheque can be cancelled there («ابطال چک»): a
 * cancelled cheque that was already received stays in «دریافت شده» as «ابطال شده», with the receiver's side of the cancellation voucher.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, dueMark, formatAmount, normaliseJalali, searchKey, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import { useLookups } from './lookups'
import TransferVoucher from './TransferVoucher.vue'
import { accountText, chequeTransferStatus, groupSayad, isConfirmed, receiverSteps, totalRial, type Transfer } from './transfers'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
/** `changed`: a cheque came to, or left, the cash desk; `steps`: the user asks for the steps screen. */
const emit = defineEmits<{ changed: []; steps: [] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => (fa.value ? n.toLocaleString('fa-IR', { useGrouping: false }) : String(n))
const L = useLookups(api)
const year = computed(() => props.fiscalYear ?? Number(todayJalali().slice(0, 4)))

const transfers = ref<Transfer[]>([])
const message = ref<{ kind: 'error' | 'ok'; text: string } | null>(null)
const search = ref('')
/** The day each waiting cheque is received: the day it was issued unless another day is typed («بصورت همزمان»). */
const dates = reactive<Record<string, string>>({})
async function load() {
  const r = await api.get<Transfer[]>(`/api/cashflow/transfers?year=${year.value}&cheque=true`)
  if (!r.ok) return void (message.value = { kind: 'error', text: translate(r.code, props.locale) })
  transfers.value = r.value
  for (const x of r.value) if (x.state === 'ChequeIssued' && !dates[x.id]) dates[x.id] = x.date
}
const from = (x: Transfer) => accountText(x.sourceBankName, x.sourceAccountNumber)
const to = (x: Transfer) => accountText(x.destinationBankName, x.destinationAccountNumber)
const matches = (x: Transfer) => {
  const key = searchKey(search.value)
  return !key || searchKey(`${x.number} ${x.date} ${x.cheque?.number ?? ''} ${x.cheque?.sayadId ?? ''} ${from(x)} ${to(x)} ${x.description ?? ''} ${chequeTransferStatus(x, props.locale)}`).includes(key)
}
const waiting = computed(() => transfers.value.filter(x => x.state === 'ChequeIssued' && matches(x)))
/** On the receipts side now, or cancelled after it was received (answer to Q79). */
const received = computed(() => transfers.value.filter(x =>
  (x.state === 'ChequeReceived' || x.state === 'Collected' || (x.state === 'Cancelled' && !!x.cheque?.receivedChequeId)) && matches(x)))
/** Every voucher the receipts side has of this transfer is confirmed (the receiver's, and the receiver's side of a cancellation). */
const allConfirmed = (x: Transfer) => isConfirmed(x, 1) && (x.state !== 'Cancelled' || isConfirmed(x, 4))
const receivedTotal = computed(() => totalRial(received.value))

const busy = ref(false)
async function receive(x: Transfer) {
  if (busy.value) return
  const date = normaliseJalali(dates[x.id] ?? '')
  if (!date) return void (message.value = { kind: 'error', text: translate('FiscalDate.Invalid', props.locale) })
  busy.value = true
  message.value = null
  const r = await api.post<string>(`/api/cashflow/transfers/${x.id}/receive-cheque`, { version: x.version, date })
  busy.value = false
  if (!r.ok) return void (message.value = { kind: 'error', text: translate(r.code, props.locale) })
  message.value = { kind: 'ok', text: t(
    `چک ${x.cheque?.number ?? ''} دریافت شد و «نزد صندوق» است. سند انتقال گیرنده را در فهرست «دریافت شده» تایید کنید؛ سپس «ثبت صیادی»، واگذاری به بانک مقصد و وصول آن در «عملیات چک‌های دریافتی» ثبت می‌شود.`,
    `Cheque ${x.cheque?.number ?? ''} is received and in the cash desk. Confirm the receiver's voucher in the list of received cheques; then register it in Sayad, deposit it at the destination bank and record its collection under Received cheque steps.`) }
  delete dates[x.id]
  emit('changed')
  await load()
  voucherOf.value = x.id
}

/** «برگرداندن ثبت دریافت»: a receipts-side record made by mistake is taken back; the cheque is «پرداخت ثبت صیاد شد» again. */
async function undo(x: Transfer) {
  if (busy.value) return
  if (!window.confirm(t(`ثبت دریافت چک ${x.cheque?.number ?? ''} (نقل و انتقال شمارهٔ ${num(x.number)}) برگردانده شود؟ چک از «نزد صندوق» برداشته می‌شود و دوباره در انتظار دریافت است.`,
    `Take back the receipt of cheque ${x.cheque?.number ?? ''} (transfer no. ${x.number})? It leaves the cash desk and waits to be received again.`))) return
  busy.value = true
  message.value = null
  const r = await api.post<string>(`/api/cashflow/transfers/${x.id}/undo-receive`, { version: x.version })
  busy.value = false
  if (!r.ok) return void (message.value = { kind: 'error', text: translate(r.code, props.locale) })
  if (voucherOf.value === x.id) voucherOf.value = ''
  message.value = { kind: 'ok', text: t(`ثبت دریافت چک ${x.cheque?.number ?? ''} برگردانده شد؛ چک دوباره در انتظار دریافت است.`, `The receipt of cheque ${x.cheque?.number ?? ''} was taken back; it waits to be received again.`) }
  emit('changed')
  await load()
}

/** The transfer whose receiver's voucher is open under the list for «تایید سند». */
const voucherOf = ref('')

onMounted(async () => { await L.load(); await load() })
watch(year, () => void load())
defineExpose({ load })
</script>

<template>
  <div>
    <p class="tr-limits">
      {{ t('چک نقل و انتقال ابتدا در «پرداخت‌ها ← پرداخت چک – نقل و انتقال (انتقال دهنده)» صادر می‌شود و سپس در اینجا (یا با دگمهٔ همان فرم) دریافت آن ثبت می‌شود. مراحل بعدی — ثبت صیادی، واگذاری به بانک مقصد و وصول — مانند سایر چک‌های دریافتی در',
        'A transfer cheque is first issued under Payments → Cheque payment – transfer (the payer), and its receipt is recorded here (or with the button of that form). The next steps — Sayad registration, deposit at the destination bank, collection — are recorded, as for every received cheque, under') }}
      <button type="button" class="tr-link" @click="emit('steps')">{{ t('«عملیات چک‌های دریافتی»', '"Received cheque steps"') }}</button>
      {{ t('ثبت می‌شود.', '.') }}
    </p>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
    <label style="max-width: 28rem">{{ t('جستجو (شماره، چک، حساب، وضعیت)', 'Search (number, cheque, account, status)') }} <input v-model="search" /></label>

    <!-- Cheques that are only on the payment side: «پرداخت ثبت صیاد شد». -->
    <section class="tr-card tr-list">
      <div class="tr-list-head">
        <h3>{{ t('در انتظار دریافت (چک صادر شده در بخش پرداخت)', 'Waiting to be received (issued on the payment side)') }}</h3>
        <span class="tr-pill">{{ t(`${num(waiting.length)} چک`, `${waiting.length} cheque(s)`) }}</span>
      </div>
      <div class="tr-scroll">
        <table class="tr-table">
          <thead>
            <tr>
              <th>{{ t('شماره', 'No.') }}</th><th>{{ t('تاریخ صدور', 'Issued') }}</th><th>{{ t('شماره چک / صیادی', 'Cheque no. / Sayad ID') }}</th>
              <th>{{ t('حساب‌ها: انتقال دهنده ← انتقال گیرنده', 'Accounts: payer → receiver') }}</th><th>{{ t('مبلغ', 'Amount') }}</th>
              <th>{{ t('سررسید', 'Due') }}</th><th>{{ t('تاریخ دریافت', 'Received on') }}</th><th />
            </tr>
          </thead>
          <tbody>
            <tr v-for="x in waiting" :key="x.id">
              <td class="num">{{ num(x.number) }}</td>
              <td class="num">{{ x.date }}</td>
              <td><span class="num" dir="ltr">{{ x.cheque?.number }}</span><span class="muted small num" dir="ltr">{{ groupSayad(x.cheque?.sayadId) }}</span></td>
              <td><dl class="tr-accounts"><dt>{{ t('از', 'from') }}</dt><dd>{{ from(x) }}</dd><dt>{{ t('به', 'to') }}</dt><dd>{{ to(x) }}</dd></dl><span v-if="x.description" class="muted small">{{ x.description }}</span></td>
              <td class="num">{{ money(x.amount) }} <span v-if="x.currency !== 'IRR'">{{ x.currency }}</span></td>
              <td class="num">{{ x.cheque?.dueDate }}
                <span :class="['tr-tag', 'tr-under', x.cheque?.payerSayadOn ? 'current' : 'later']">{{ x.cheque?.payerSayadOn ? t('پرداخت ثبت صیاد شد', 'registered in Sayad by the payer') : t('ثبت صیادی پرداخت نشده', 'not yet registered in Sayad by the payer') }}</span></td>
              <td><input v-model="dates[x.id]" dir="ltr" style="width: 8rem" :aria-label="t('تاریخ دریافت', 'Received on')" /></td>
              <td class="tr-actions"><button type="button" :disabled="busy" @click="receive(x)">{{ t('ثبت دریافت', 'Record the receipt') }}</button></td>
            </tr>
            <tr v-if="!waiting.length"><td colspan="8" class="muted">{{ t('چک نقل و انتقالی در انتظار دریافت نیست.', 'No transfer cheque waits to be received.') }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <!-- Cheques on the receipts side. -->
    <section class="tr-card tr-list">
      <div class="tr-list-head">
        <h3>{{ t(`دریافت شده – سال مالی ${num(year)}`, `Received – fiscal year ${year}`) }}</h3>
        <span class="tr-list-total">{{ t('جمع:', 'Total:') }} <strong class="num">{{ money(receivedTotal) }}</strong> {{ t('ریال', 'Rial') }}</span>
      </div>
      <div class="tr-scroll">
        <table class="tr-table">
          <thead>
            <tr>
              <th>{{ t('شماره', 'No.') }}</th><th>{{ t('تاریخ دریافت', 'Received on') }}</th><th>{{ t('شماره چک / صیادی', 'Cheque no. / Sayad ID') }}</th>
              <th>{{ t('حساب‌ها: انتقال دهنده ← انتقال گیرنده', 'Accounts: payer → receiver') }}</th><th>{{ t('مبلغ', 'Amount') }}</th>
              <th>{{ t('سررسید', 'Due') }}</th><th>{{ t('آخرین وضعیت', 'Latest status') }}</th><th>{{ t('عملیات', 'Actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="x in received" :key="x.id" :class="{ deleted: x.state === 'Cancelled', selected: voucherOf === x.id }">
              <td class="num">{{ num(x.number) }}</td>
              <td class="num">{{ x.cheque?.receivedOn }}</td>
              <td><span class="num" dir="ltr">{{ x.cheque?.number }}</span><span class="muted small num" dir="ltr">{{ groupSayad(x.cheque?.sayadId) }}</span></td>
              <td><dl class="tr-accounts"><dt>{{ t('از', 'from') }}</dt><dd>{{ from(x) }}</dd><dt>{{ t('به', 'to') }}</dt><dd>{{ to(x) }}</dd></dl><span v-if="x.description" class="muted small">{{ x.description }}</span></td>
              <td class="num">{{ money(x.amount) }} <span v-if="x.currency !== 'IRR'">{{ x.currency }}</span></td>
              <td class="num">{{ x.cheque?.dueDate }}</td>
              <td class="tr-status">
                <template v-if="x.state === 'Cancelled'">
                  <span class="tr-deleted">{{ t('ابطال شده', 'Cancelled') }}</span>
                  <span class="muted small">{{ x.cheque?.cancelledOn }} – {{ x.cheque?.cancelReason }}</span>
                </template>
                <template v-else>
                  {{ chequeTransferStatus(x, locale) }}
                  <span v-if="x.cheque?.reachedDueDate" class="tr-tag due">{{ dueMark(locale) }}</span>
                  <span v-if="x.cheque?.collectedOn" class="muted small num">{{ x.cheque.collectedOn }}</span>
                  <!-- Answer to Q79: the receipts side registers the cheque in Sayad only after the payer did. -->
                  <span v-if="x.state === 'ChequeReceived' && !x.cheque?.payerSayadOn" class="muted small">
                    {{ t('«ثبت صیادی» این چک در بخش دریافت پس از «ثبت صیادی پرداخت» در «پرداخت‌ها» ممکن می‌شود.', "Its Sayad registration on the receipts side waits for the payer's Sayad registration under Payments.") }}</span>
                </template>
                <span v-if="allConfirmed(x)" class="tr-tag current">{{ t('سند تایید شد', 'Voucher confirmed') }}</span>
              </td>
              <td class="tr-actions">
                <button type="button" class="tr-link" @click="voucherOf = voucherOf === x.id ? '' : x.id">{{ allConfirmed(x) ? t('سند', 'Voucher') : t('تایید سند', 'Confirm voucher') }}</button>
                <button v-if="x.canUndoReceive" type="button" class="tr-link danger" :disabled="busy" @click="undo(x)">{{ t('برگرداندن ثبت دریافت', 'Take the receipt back') }}</button>
              </td>
            </tr>
            <tr v-if="!received.length"><td colspan="8" class="muted">{{ t('چک نقل و انتقالی در این سال دریافت نشده است.', 'No transfer cheque was received this year.') }}</td></tr>
          </tbody>
        </table>
      </div>
      <TransferVoucher v-if="voucherOf" :key="voucherOf" :transfer-id="voucherOf" :steps="receiverSteps" cheque :api-base="apiBase" :locale="locale" :token="token"
                       :account-name="L.accountName" @confirmed="load" @close="voucherOf = ''" />
    </section>
  </div>
</template>

<style scoped src="./receipts.css"></style>
<style scoped src="./transferForm.css"></style>
<style scoped>
.tr-limits { margin-bottom: .75rem; }
.tr-limits .tr-link { padding: 0 .2rem; font-weight: 600; }
</style>
