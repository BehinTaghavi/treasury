<script setup lang="ts">
/**
 * «کارت چک»: the whole course of a received cheque or note — every step with its date, the status after it, its details and its
 * user — and the voucher of each step with «تایید سند». It was part of the steps screen (owner's item 14 of 15 Nov 2026); since the
 * owner's report no. 6 (19 Nov 2026) the receipts lists show it too: item 14, «روی هر رکورد از فهرست دریافت‌ها دبل کلیک می‌کنیم امکان
 * مشاهدهٔ کارت چک برای مشاهدهٔ سیر فرآیندهایی که چک طی نموده … با کلیک کردن روی آن فرم بسته شود», and item 20, «با کلیک کردن روی هر رکورد
 * از فهرست در قسمت تحتانی فرم کارت چک مربوطه در اختیار کاربر قرار گیرد». With `closeOnClick` a click on the card (not on one of its
 * buttons, and not while text is being selected) closes it; «بستن» is always there.
 */
import { computed, onMounted, ref, watch } from 'vue'
import { createApi, dueMark, dueWaitsFor, formatAmount, instrumentKindName, translate, type Locale } from '@treasury/ui-kit'
import VoucherTable, { type Voucher } from './VoucherTable.vue'
import { actionNameOf, isNoteKind, isTransferCheque, noteWording, stateNameOf, transferChequeMark, type Cheque, type Step } from './cheque'

const props = withDefaults(defineProps<{
  chequeId: string; apiBase?: string; locale?: Locale; token?: () => string | undefined
  /** Raised by the screen when a step was recorded on the cheque, so the card reads it again. */
  refreshKey?: number
  closeOnClick?: boolean
  /** The title of one of the company's bank accounts (the screen's lookups). */
  bankAccountName?: (id: string | null | undefined) => string
  /** The name of a ledger account, for the vouchers. */
  ledgerAccountName?: (code: string) => string | undefined
}>(), { apiBase: '', locale: 'fa', token: undefined, refreshKey: 0, closeOnClick: false, bankAccountName: undefined, ledgerAccountName: undefined })
const emit = defineEmits<{ close: []; confirmed: [] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)

const cheque = ref<Cheque | null>(null)
const vouchers = ref<Voucher[]>([])
const error = ref('')
const note = computed(() => isNoteKind(cheque.value?.kind))
const t = (f: string, e: string) => { const text = fa.value ? f : e; return note.value ? noteWording(text, props.locale) : text }
const stateName = (s: string | null | undefined) => stateNameOf(s, cheque.value?.kind, props.locale)
const actionName = (a: string | null | undefined) => actionNameOf(a, cheque.value?.kind, props.locale)

async function load() {
  const id = props.chequeId
  const [c, v] = await Promise.all([api.get<Cheque>(`/api/cashflow/instruments/${id}`), api.get<Voucher[]>(`/api/cashflow/instruments/${id}/vouchers`)])
  if (id !== props.chequeId) return
  if (c.ok) { cheque.value = c.value; error.value = '' } else error.value = translate(c.code, props.locale)
  vouchers.value = v.ok ? v.value : []
}
watch(() => [props.chequeId, props.refreshKey], () => void load())
onMounted(load)

const confirming = ref(false)
const message = ref('')
async function confirmVoucher(step: number) {
  if (!cheque.value || confirming.value) return
  confirming.value = true
  const r = await api.post<string>(`/api/cashflow/instruments/${cheque.value.id}/vouchers/${step}/confirm`, {})
  confirming.value = false
  if (!r.ok) { error.value = translate(r.code, props.locale); return }
  error.value = ''
  message.value = t('سند تایید شد.', 'Voucher confirmed.')
  emit('confirmed')
  await load()
}

/** The computer's date and time, as written beside the stamp tick of a cancellation. */
const stampTime = (iso: string) => new Intl.DateTimeFormat(fa.value ? 'fa-IR' : 'en-GB', { dateStyle: 'short', timeStyle: 'short', timeZone: 'Asia/Tehran' }).format(new Date(iso))
function stepText(s: Step) {
  const d = s.details
  const parts = [s.reference && (s.action === 'RecordBounced' || s.action === 'RecordEndorsementReturned' ? t(`گواهی عدم پرداخت ${s.reference}`, `certificate ${s.reference}`)
    : s.action === 'Cancel' ? t(`دستور ${s.reference} مورخ ${d?.documentDate ?? ''}`, `order ${s.reference} of ${d?.documentDate ?? ''}`)
      + (d?.recordedAt ? ' · ' + (d.originalStamped ? t(`اصل چک ممهور به مهر ابطال شد – ${s.userId} – ${stampTime(d.recordedAt)}`, `original stamped «ابطال» – ${s.userId} – ${stampTime(d.recordedAt)}`)
        : t(`مهر ابطال روی اصل چک تایید نشده است – ${s.userId} – ${stampTime(d.recordedAt)}`, `stamp on the original not confirmed – ${s.userId} – ${stampTime(d.recordedAt)}`)) : '')
    : s.action === 'Endorse' && s.reference ? t(`کد رهگیری ثبت انتقال در صیاد ${s.reference}`, `Sayad transfer ${s.reference}`)
    : s.action === 'RegisterSayad' ? t(`پیگیری صیاد ${s.reference}`, `Sayad ${s.reference}`)
    : s.action === 'SendToLegal' ? t(`گواهی عدم پرداخت ${s.reference}${d?.documentDate ? ' مورخ ' + d.documentDate : ''}`, `certificate ${s.reference}${d?.documentDate ? ' of ' + d.documentDate : ''}`)
    : s.reference),
  d?.sheetNumber && t(`برگ پرداخت شمارهٔ ${d.sheetNumber.toLocaleString('fa-IR', { useGrouping: false })}`, `payment sheet no. ${d.sheetNumber}`),
  d?.trackingCode && t(`کد رهگیری ${d.trackingCode}`, `tracking ${d.trackingCode}`),
  d?.accountId && (props.bankAccountName?.(d.accountId) ?? ''), d?.partyName, d?.purpose, s.note]
  return parts.filter(Boolean).join(' · ')
}

/** Item 14: a click on the card closes it — but not a click on a button or link of the card, nor the end of a text selection. */
function onCardClick(e: MouseEvent) {
  if (!props.closeOnClick) return
  if ((e.target as HTMLElement).closest('button, a, input, select, textarea, label')) return
  if (window.getSelection()?.toString()) return
  emit('close')
}
</script>

<template>
  <section :class="['tr-card', { closable: closeOnClick }]" :title="closeOnClick ? t('برای بستن کارت چک روی آن کلیک کنید', 'Click the card to close it') : undefined" @click="onCardClick">
    <p v-if="error" class="tr-msg error" role="status">{{ error }}</p>
    <template v-if="cheque">
      <div class="tr-buttons">
        <h3 style="margin: 0">{{ t(`کارت چک ${cheque.number ?? ''}`, `Cheque ${cheque.number ?? ''}`) }}</h3>
        <span v-if="!note && cheque.kind !== 'OrdinaryCheque'" class="tr-tag">{{ instrumentKindName(cheque.kind, locale) }}</span>
        <!-- Owner's report no. 7: the company's own cheque, issued for a transfer between two of its bank accounts. -->
        <span v-if="isTransferCheque(cheque)" class="tr-tag later">{{ transferChequeMark(locale) }}</span>
        <span class="tr-tag">{{ t('آخرین وضعیت', 'Latest status') }}: {{ stateName(cheque.state) }}</span>
        <span v-if="cheque.reachedDueDate" class="tr-tag due" :title="dueWaitsFor(cheque.state, locale)">{{ dueMark(locale) }}</span>
        <span class="muted small"><template v-if="cheque.bankName">{{ cheque.bankName }} · </template>{{ cheque.counterparty }} · {{ cheque.dueDate ? `${t('سررسید', 'due')} ${cheque.dueDate}` : cheque.details?.onDemand ? t('عندالمطالبه', 'on demand') : '' }} · {{ money(cheque.amount) }} {{ cheque.currency }}<template v-if="cheque.details?.sayadId"> · {{ t('صیاد', 'Sayad') }} {{ cheque.details.sayadId }}</template><template v-if="cheque.purpose"> · {{ t('شرح', 'Description') }}: {{ cheque.purpose }}</template></span>
        <slot name="actions" :cheque="cheque" />
        <button type="button" class="tr-link" @click="emit('close')">{{ t('بستن', 'Close') }}</button>
      </div>
      <p v-if="closeOnClick" class="muted small" style="margin: .25rem 0 0">{{ t('سیر مراحلی که این چک طی کرده است. برای بستن، روی کارت کلیک کنید.', 'The course of this cheque. Click the card to close it.') }}</p>
      <p v-if="message" class="tr-msg ok" role="status">{{ message }}</p>
      <table class="tr-table">
        <thead><tr><th>#</th><th>{{ t('تاریخ', 'Date') }}</th><th>{{ t('مرحله', 'Step') }}</th><th>{{ t('وضعیت پس از آن', 'State after') }}</th><th>{{ t('جزئیات', 'Details') }}</th><th>{{ t('کاربر', 'User') }}</th></tr></thead>
        <tbody>
          <tr v-for="(s, i) in cheque.history" :key="i">
            <td class="num">{{ (i + 1).toLocaleString(fa ? 'fa-IR' : 'en-US') }}</td>
            <td class="num">{{ s.date }}</td>
            <td>{{ actionName(s.action) }}</td>
            <td>{{ stateName(s.to) }}</td>
            <td class="small">{{ stepText(s) }}</td>
            <td class="small muted">{{ s.userId }}</td>
          </tr>
        </tbody>
      </table>
      <p v-if="isTransferCheque(cheque)" class="muted small">{{ t('این چک، چک نقل و انتقال بین بانک‌های شرکت است: سند دریافت آن (بدهکار اسناد نزد صندوق، بستانکار وجوه در راه) در «دریافت چک – نقل و انتقال (انتقال گیرنده)» تایید می‌شود و فقط ثبت صیادی، به بانک مقصد واگذار و وصول می‌شود.',
        "This is a cheque of a transfer between the company's own accounts: its receipt voucher (debit cheques in the cash desk, credit funds in transit) is confirmed under «Cheque receipt – transfer (the receiver)», and it is only registered in Sayad, deposited at the destination bank and collected.") }}</p>
      <p v-else-if="!vouchers.length" class="muted small">{{ t('سند دریافت این چک در فهرست دریافت‌ها (رسید آن) است؛ مرحله‌ای که سند حسابداری داشته باشد هنوز ثبت نشده است.',
        "This cheque's receipt voucher is on its receipt in the receipts list; no step with a voucher yet.") }}</p>
      <VoucherTable v-for="v in vouchers" :key="v.step" :voucher="v" :locale="locale" :account-name="ledgerAccountName" can-confirm :busy="confirming"
                    @confirm="confirmVoucher(v.step)" />
    </template>
    <p v-else-if="!error" class="muted small">{{ fa ? 'در حال خواندن کارت چک…' : 'Loading the cheque card…' }}</p>
  </section>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
.tr-card { margin-top: 1.25rem; padding: .75rem; border: 1px solid #d9e2ec; border-radius: 8px; background: #fff; }
.tr-card.closable { cursor: pointer; border-color: #93b4f5; box-shadow: 0 1px 6px rgba(37, 99, 235, .12); }
.tr-card.closable :is(button, a) { cursor: pointer; }
</style>
