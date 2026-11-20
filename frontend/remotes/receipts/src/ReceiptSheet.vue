<script setup lang="ts">
/**
 * The printed receipt of cheques, of cash and of money paid into a bank account (owner's report no. 6, item 21: «برای رسید دریافت چک
 * یا نقد یا سفته … فرم‌های مربوطه پیاده‌سازی و با زدن دگمهٔ چاپ بتوان آن را مشاهده و چاپ گرفت»). It follows the receipt of promissory
 * notes he designed on 16 Nov 2026 (NoteReceiptSheet.vue): the company's name and logo, the receipt's number and date, the payer, the
 * purpose, what was received with its total in figures and words, a sentence confirming the receipt, and the signatures of who
 * handed it over, who received it for the treasury and who approves. ReceiptPrint shows it on the screen and hands a copy to the printer.
 */
import { computed } from 'vue'
import { amountInWords, formatAmount, instrumentKindName, toPersianDigits, type Locale } from '@treasury/ui-kit'
import { depositMethodName } from './depositMethods'
import type { SavedReceipt } from './savedReceipt'

export interface PrintedReceipt extends SavedReceipt { totalRial: number; recordedBy: string; cancelledOn?: string | null; cancelReason?: string | null }

const props = withDefaults(defineProps<{
  receipt: PrintedReceipt; company: { name: string; logoUrl: string | null }; locale?: Locale
  /** The cash desk or the company's bank account the money went into. */
  accountTitle?: string
  /** The name of the currency of a foreign-currency amount (from Basic Information). */
  currencyName?: (code: string) => string
  departmentName?: string; reference1Title?: string; reference2Title?: string
}>(), { locale: 'fa', accountTitle: '', currencyName: undefined, departmentName: '', reference1Title: '', reference2Title: '' })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (fa.value ? f : e)
const digits = (s: string | number | null | undefined) => (s == null ? '' : fa.value ? toPersianDigits(String(s)) : String(s))
const money = (n: number | null | undefined) => (n == null ? '' : formatAmount(n, props.locale))
const r = computed(() => props.receipt)
const cheques = computed(() => r.value.instruments ?? [])
const unit = (code: string | null | undefined) => (!code || code === 'IRR' ? t('ریال', 'Rial') : props.currencyName?.(code) ?? code)
const foreign = computed(() => !!r.value.currency && r.value.currency !== 'IRR')
/** The Sayad number in four groups of four, as on the cheque. */
const sayad = (id: string | null | undefined) => digits((id ?? '').replace(/(\d{4})(?=\d)/g, '$1 '))
const sheba = (s: string | null | undefined) => (s ? s.replace(/(.{4})/g, '$1 ').trim() : '')

const titles: Record<string, [string, string]> = {
  Instruments: ['رسید دریافت چک', 'Receipt of cheques'],
  Cash: ['رسید دریافت وجه نقد', 'Cash receipt'],
  BankDeposit: ['رسید واریز به بانک', 'Receipt of a deposit into a bank account'],
}
const title = computed(() => { const x = titles[r.value.kind] ?? titles.Cash!; return fa.value ? x[0] : x[1] })
const confirmations: Record<string, [string, string]> = {
  Instruments: ['چک‌های مشروح بالا از پرداخت‌کنندهٔ نام‌برده دریافت شد. این رسید فقط تحویل چک را نشان می‌دهد؛ بدهی به میزان وجهی که از چک‌ها وصول شود تسویه می‌شود و صرف دریافت چک به معنای وصول وجه آن نیست.',
    'The cheques listed above were received from the payer named. This receipt only records that the cheques were handed over; the debt is settled to the extent of the money collected from them.'],
  Cash: ['مبلغ بالا به صورت وجه نقد از پرداخت‌کنندهٔ نام‌برده دریافت و به صندوق تحویل شد.', 'The amount above was received in cash from the payer named and put into the cash desk.'],
  BankDeposit: ['واریز مبلغ بالا از طرف پرداخت‌کنندهٔ نام‌برده به حساب بانکی شرکت ثبت شد.', "The deposit of the amount above into the company's bank account by the payer named was recorded."],
}
const confirmation = computed(() => { const x = confirmations[r.value.kind] ?? confirmations.Cash!; return fa.value ? x[0] : x[1] })
const stamp = computed(() => (r.value.state === 'Deleted' ? t('حذف شده', 'Deleted') : r.value.state === 'Cancelled' ? t('ابطال شده', 'Cancelled') : ''))
</script>

<template>
  <div class="tr-print-sheet tr-receipt-sheet" :dir="fa ? 'rtl' : 'ltr'">
    <div class="head">
      <div class="side brand">
        <img v-if="company.logoUrl" class="logo" :src="company.logoUrl" alt="" />
        <span><strong>{{ company.name || '..............................' }}</strong><br />{{ t('واحد خزانه‌داری', 'Treasury') }}</span>
      </div>
      <div class="title">
        <strong>{{ title }}</strong>
        <span v-if="r.replacedInstrumentId">{{ t('تعویض چک', 'Replacement of a cheque') }}</span>
        <span v-if="stamp" class="stamp">{{ stamp }}</span>
      </div>
      <div class="side end">
        <span>{{ t('شماره رسید:', 'Receipt no.:') }} <strong>{{ digits(r.number) }}</strong></span>
        <span>{{ t('تاریخ دریافت:', 'Date:') }} <strong>{{ digits(r.date) }}</strong></span>
      </div>
    </div>

    <table>
      <tbody>
        <tr><th>{{ t('پرداخت‌کننده', 'Payer') }}</th><td>{{ r.payerName }}</td><th>{{ t('حساب تفصیلی', 'Detail account') }}</th><td class="ltr">{{ digits(r.payerDetailCode) }}</td></tr>
        <tr><th>{{ t('گروه بابت', 'Purpose group') }}</th><td>{{ r.purposeGroupName }}</td><th>{{ t('بابت', 'Purpose') }}</th><td>{{ r.purpose }}</td></tr>
        <tr v-if="r.reference1 || r.reference2">
          <th>{{ reference1Title || t('عطف ۱', 'Reference 1') }}</th><td>{{ digits(r.reference1) }}</td>
          <th>{{ reference2Title || t('عطف ۲', 'Reference 2') }}</th><td>{{ digits(r.reference2) }}</td>
        </tr>
        <tr v-if="departmentName || r.salesProceeds">
          <th>{{ t('واحد سازمانی دریافت کننده', 'Receiving department') }}</th><td>{{ departmentName }}</td>
          <th>{{ t('وجوه حاصل فروش', 'Sales proceeds') }}</th><td>{{ r.salesProceeds ? t('بله', 'Yes') : t('خیر', 'No') }}</td>
        </tr>
      </tbody>
    </table>

    <template v-if="r.kind === 'Instruments'">
      <h4>{{ t('مشخصات چک‌های دریافتی', 'Cheques received') }}</h4>
      <table class="lines">
        <thead>
          <tr><th class="narrow">{{ t('ردیف', 'No.') }}</th><th>{{ t('نوع', 'Kind') }}</th><th>{{ t('شماره چک', 'Cheque no.') }}</th><th>{{ t('شناسهٔ صیاد', 'Sayad ID') }}</th>
            <th>{{ t('بانک و شعبه / شماره حساب', 'Bank and branch / account') }}</th><th>{{ t('سررسید', 'Due date') }}</th><th>{{ t('مبلغ', 'Amount') }}</th></tr>
        </thead>
        <tbody>
          <tr v-for="(c, i) in cheques" :key="c.id">
            <td>{{ digits(i + 1) }}</td>
            <td>{{ instrumentKindName(c.kind, locale) }}</td>
            <td class="ltr">{{ digits(c.number) }}</td>
            <td class="ltr">{{ sayad(c.details?.sayadId) }}</td>
            <td>{{ c.bankName }}<template v-if="c.details?.branchName"> – {{ c.details.branchName }}</template>
              <div v-if="c.details?.accountNumber" class="ltr small">{{ digits(c.details.accountNumber) }}</div>
              <div v-if="c.details?.accountHolder" class="small">{{ t('صاحب حساب:', 'Account holder:') }} {{ c.details.accountHolder }}</div></td>
            <td>{{ digits(c.dueDate) }}</td>
            <td class="amount">{{ money(c.amount) }}<span v-if="c.currency !== 'IRR'" class="small"> {{ unit(c.currency) }}</span></td>
          </tr>
          <tr class="sum"><td colspan="3">{{ t('تعداد:', 'Count:') }} {{ digits(cheques.length) }} {{ t('فقره', '') }}</td>
            <td colspan="3">{{ t('جمع به عدد (ریال)', 'Total in figures (Rial)') }}</td><td class="amount">{{ money(r.totalRial) }}</td></tr>
          <tr><td colspan="7">{{ t('جمع به حروف:', 'Total in words:') }} <strong>{{ amountInWords(r.totalRial) }} {{ t('ریال', 'Rial') }}</strong></td></tr>
        </tbody>
      </table>
    </template>

    <template v-else>
      <h4>{{ r.kind === 'Cash' ? t('مشخصات وجه نقد دریافتی', 'Cash received') : t('مشخصات واریز', 'The deposit') }}</h4>
      <table>
        <tbody>
          <tr v-if="r.kind === 'Cash'"><th>{{ t('صندوق', 'Cash desk') }}</th><td colspan="3">{{ accountTitle }}</td></tr>
          <template v-else>
            <tr><th>{{ t('حساب بانکی شرکت', 'Company bank account') }}</th><td>{{ accountTitle }}</td><th>{{ t('نوع واریز', 'Kind of deposit') }}</th><td>{{ depositMethodName(r.method, fa) }}</td></tr>
            <!-- Codes read from bank papers (tracking number, Sheba) keep the digits they were typed with. -->
            <tr><th>{{ r.method === 'DepositSlip' ? t('شماره فیش', 'Slip no.') : t('شماره پیگیری / مرجع', 'Tracking / reference no.') }}</th><td class="ltr">{{ r.trackingNumber }}</td>
              <th>{{ t('شبای واریزکننده', "Payer's Sheba") }}</th><td class="ltr nowrap">{{ sheba(r.payerSheba) }}</td></tr>
          </template>
          <tr><th>{{ t('مبلغ به عدد', 'Amount in figures') }}</th><td :colspan="foreign ? 1 : 3"><strong>{{ money(r.amount) }}</strong> {{ unit(r.currency) }}</td>
            <template v-if="foreign"><th>{{ t('نرخ ارز / هم‌ارز ریالی', 'Rate / Rial equivalent') }}</th>
              <td>{{ money(r.rateToRial) }} – <strong>{{ money(r.totalRial) }}</strong> {{ t('ریال', 'Rial') }}</td></template></tr>
          <tr><th>{{ t('مبلغ به حروف', 'Amount in words') }}</th><td colspan="3"><strong>{{ amountInWords(r.amount ?? 0) }} {{ unit(r.currency) }}</strong></td></tr>
        </tbody>
      </table>
    </template>

    <p class="confirm">{{ confirmation }}</p>

    <table class="signatures">
      <thead>
        <tr><th>{{ t('تحویل‌دهنده (پرداخت‌کننده)', 'Handed over by (payer)') }}</th><th>{{ t('تحویل‌گیرنده (خزانه‌داری)', 'Received by (treasury)') }}</th><th>{{ t('تأییدکننده', 'Approved by') }}</th></tr>
      </thead>
      <tbody>
        <tr>
          <td><span>{{ t('نام:', 'Name:') }} {{ r.payerName }}</span><span>{{ t('تاریخ:', 'Date:') }}</span><span>{{ t('امضا:', 'Signature:') }}</span></td>
          <td><span>{{ t('نام:', 'Name:') }} {{ r.recordedBy }}</span><span>{{ t('تاریخ:', 'Date:') }}</span><span>{{ t('امضا:', 'Signature:') }}</span></td>
          <td><span>{{ t('نام:', 'Name:') }}</span><span>{{ t('تاریخ:', 'Date:') }}</span><span>{{ t('امضا:', 'Signature:') }}</span></td>
        </tr>
      </tbody>
    </table>

    <div class="remarks">{{ t('توضیحات:', 'Remarks:') }} {{ r.note }}<template v-if="r.state === 'Cancelled'"> {{ t(`– ابطال شده در ${digits(r.cancelledOn)}: ${r.cancelReason ?? ''}`, `– cancelled on ${r.cancelledOn}: ${r.cancelReason ?? ''}`) }}</template></div>
  </div>
</template>

<style scoped>
.tr-print-sheet { box-sizing: border-box; width: 210mm; max-width: 100%; min-height: 148mm; margin: 1rem auto; padding: 11mm 12mm; background: #fff; color: #111;
  border: 1px solid #cbd2d9; font-family: Vazirmatn, Tahoma, sans-serif; font-size: 10pt; line-height: 1.5; display: flex; flex-direction: column; gap: 3mm; }
.head { display: flex; align-items: flex-end; justify-content: space-between; gap: 4mm; padding-bottom: 2.5mm; border-bottom: 2px solid #111; }
.side { display: flex; flex-direction: column; gap: .8mm; flex: 1; }
.side.brand { flex-direction: row; align-items: center; gap: 3mm; }
.side.end { align-items: flex-end; }
.logo { max-height: 16mm; max-width: 28mm; object-fit: contain; }
.title { flex: 1.2; display: flex; flex-direction: column; align-items: center; text-align: center; }
.title strong { font-size: 16pt; }
.stamp { margin-top: 1mm; padding: 0 3mm; border: 2px solid #b42318; color: #b42318; font-weight: 700; font-size: 12pt; }
h4 { margin: 0; font-size: 10pt; }
table { width: 100%; border-collapse: collapse; }
th, td { border: 1px solid #333; padding: 1.2mm 2mm; text-align: start; vertical-align: middle; }
th { background: #eef1f5; font-weight: 600; }
tbody th { width: 20%; }
.lines th { width: auto; }
th.narrow { width: 7%; }
.ltr { direction: ltr; unicode-bidi: isolate; text-align: end; font-variant-numeric: tabular-nums; }
.lines td.ltr, .nowrap { white-space: nowrap; }
[dir=ltr] .ltr { text-align: start; }
.amount { text-align: end; font-variant-numeric: tabular-nums; white-space: nowrap; }
tr.sum td { background: #eef1f5; font-weight: 600; }
.small { font-size: 8.5pt; }
.confirm { margin: 0; padding: 2mm 2.5mm; border: 1px solid #333; text-align: justify; }
.signatures { table-layout: fixed; }
.signatures th { width: auto; text-align: center; }
.signatures td { height: 26mm; vertical-align: top; }
.signatures td span { display: block; margin-top: 1.5mm; }
.remarks { min-height: 10mm; border: 1px solid #333; padding: 1.6mm 2mm; }
</style>
