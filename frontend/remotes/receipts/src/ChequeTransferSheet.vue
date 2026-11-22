<script setup lang="ts">
/**
 * «برگ پرداخت – رسید واگذاری چک» (owner, 19 Nov 2026, item 6 of «ثبت انتقال (خرج چک)»): like every kind of payment the transfer of a
 * received cheque gets its payment sheet. It names the cheque's Sayad number, its series and number, its due date and its owner, the
 * sentence «چک جهت وصول بابت بدهی تحویل می‌شود» and, at the foot, the owner's text on what the handover settles. It is printed, signed
 * by who hands the cheque over and who receives it, and the cheque is handed over. Owner's answers to Q57 (19 Nov 2026): the sheet has
 * a serial number of its own, and it is signed too by the persons chosen on the form among the holders of a permission in the transfer
 * (Basic Information → «دارندگان مجوزها», owner's report no. 8; before it, by the signature rule «برگ پرداخت چک»); each box is headed
 * by the person's roles and job title.
 * The company's name and logo come from «مشخصات شرکت» (owner's answer of 16 Nov 2026: every printed form starts with them).
 * ChequeTransferPrint shows the sheet on the screen and hands a copy to the printer.
 */
import { computed } from 'vue'
import { amountInWords, formatAmount, toPersianDigits, type Locale } from '@treasury/ui-kit'
import { deliverySentence, footText, type TransferSheet } from './transferSheet'

const props = withDefaults(defineProps<{ sheet: TransferSheet; locale?: Locale }>(), { locale: 'fa' })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (fa.value ? f : e)
const digits = (s: string | number | null | undefined) => (s == null ? '' : fa.value ? toPersianDigits(String(s)) : String(s))
const money = (n: number) => formatAmount(n, props.locale)
const c = computed(() => props.sheet.cheque)
const currencyName = computed(() => (c.value.currency === 'IRR' ? t('ریال', 'Rial') : c.value.currency))
/** The Sayad number in four groups of four, as on the cheque. */
const sayad = computed(() => digits((c.value.sayadId ?? '').replace(/(\d{4})(?=\d)/g, '$1 ')))
</script>

<template>
  <div class="tr-print-sheet tr-transfer-sheet" :dir="fa ? 'rtl' : 'ltr'">
    <div class="head">
      <div class="side brand">
        <img v-if="sheet.company.logoUrl" class="logo" :src="sheet.company.logoUrl" alt="" />
        <span><strong>{{ sheet.company.name || '..............................' }}</strong><br />{{ t('واحد خزانه‌داری', 'Treasury') }}</span>
      </div>
      <div class="title">
        <strong>{{ t('برگ پرداخت', 'Payment sheet') }}</strong>
        <span>{{ t('رسید واگذاری چک – ثبت انتقال (خرج چک)', 'Handover receipt of a cheque – transfer (endorsement)') }}</span>
      </div>
      <div class="side end">
        <span>{{ t('شماره برگ پرداخت:', 'Payment sheet no.:') }} <strong>{{ digits(sheet.sheetNumber) }}</strong></span>
        <span>{{ t('شماره درخواست وجه:', 'Payment request no.:') }} <strong>{{ digits(sheet.requestNumber) }}</strong></span>
        <span>{{ t('تاریخ ثبت انتقال:', 'Date of transfer:') }} <strong>{{ digits(sheet.date) }}</strong></span>
      </div>
    </div>

    <h4>{{ t('مشخصات چک', 'The cheque') }}</h4>
    <table>
      <tbody>
        <tr><th>{{ t('شماره صیادی چک', 'Sayad number') }}</th><td class="ltr">{{ sayad }}</td><th>{{ t('سریال و شماره چک', 'Series and number') }}</th><td class="ltr">{{ digits(c.number) }}</td></tr>
        <tr><th>{{ t('بانک و شعبه', 'Bank and branch') }}</th><td>{{ c.bankName }}<template v-if="c.branchName"> – {{ c.branchName }}</template></td>
          <th>{{ t('شماره حساب', 'Account number') }}</th><td class="ltr">{{ digits(c.accountNumber) }}</td></tr>
        <tr><th>{{ t('تاریخ سررسید چک', 'Due date') }}</th><td>{{ digits(c.dueDate) }}</td>
          <th>{{ t('مبلغ چک', 'Amount') }}</th><td><strong>{{ money(c.amount) }}</strong> {{ currencyName }}</td></tr>
        <tr><th>{{ t('مبلغ به حروف', 'Amount in words') }}</th><td colspan="3">{{ amountInWords(c.amount) }} {{ currencyName }}</td></tr>
        <tr><th>{{ t('مشخصات صاحب چک (صادرکننده)', 'Owner of the cheque (drawer)') }}</th><td>{{ c.accountHolder || c.payerName }}</td>
          <th>{{ t('واگذارنده چک به شرکت', 'Given to the company by') }}</th><td>{{ c.payerName }}</td></tr>
      </tbody>
    </table>

    <h4>{{ t('انتقال', 'The transfer') }}</h4>
    <table>
      <tbody>
        <tr><th>{{ t('گیرنده چک (ذینفع)', 'Receiver (beneficiary)') }}</th><td>{{ sheet.receiverName }}<span v-if="sheet.receiverDetailCode" class="small"> ({{ digits(sheet.receiverDetailCode) }})</span></td>
          <th>{{ t('کد رهگیری ثبت انتقال در سامانه صیاد', 'Sayad tracking code of the transfer') }}</th><td class="ltr">{{ digits(sheet.sayadTracking) }}</td></tr>
        <tr><th>{{ t('بابت', 'Purpose') }}</th><td colspan="3">{{ sheet.purposeGroupName }}<template v-if="sheet.purposeGroupName && sheet.purpose"> – </template>{{ sheet.purpose }}</td></tr>
      </tbody>
    </table>

    <p class="sentence">{{ t(deliverySentence[0], deliverySentence[1]) }}</p>

    <table class="signatures">
      <thead>
        <tr><th>{{ t('تحویل‌دهنده چک', 'Handed over by') }}</th><th>{{ t('تحویل‌گیرنده چک', 'Received by') }}</th>
          <th v-for="s in sheet.signers" :key="s.id">{{ s.position || t('تأییدکننده', 'Confirmer') }}</th></tr>
      </thead>
      <tbody>
        <tr>
          <td><span>{{ t('نام:', 'Name:') }} {{ sheet.company.name }}<template v-if="sheet.company.name"> – </template>{{ t('خزانه‌داری', 'Treasury') }} ({{ sheet.recordedBy }})</span>
            <span>{{ t('تاریخ:', 'Date:') }}</span><span>{{ t('امضا:', 'Signature:') }}</span></td>
          <td><span>{{ t('نام:', 'Name:') }} {{ sheet.receiverName }}</span><span>{{ t('تاریخ:', 'Date:') }}</span><span>{{ t('امضا:', 'Signature:') }}</span></td>
          <td v-for="s in sheet.signers" :key="s.id"><span>{{ t('نام:', 'Name:') }} {{ s.name }}</span><span>{{ t('تاریخ:', 'Date:') }}</span><span>{{ t('امضا:', 'Signature:') }}</span></td>
        </tr>
      </tbody>
    </table>

    <div class="remarks">{{ t('توضیحات:', 'Remarks:') }} {{ sheet.note }}</div>
    <p class="foot">{{ t(footText[0], footText[1]) }}</p>
  </div>
</template>

<style scoped>
.tr-print-sheet { box-sizing: border-box; width: 210mm; max-width: 100%; min-height: 200mm; margin: 1rem auto; padding: 11mm 12mm; background: #fff; color: #111;
  border: 1px solid #cbd2d9; font-family: Vazirmatn, Tahoma, sans-serif; font-size: 10.5pt; line-height: 1.5; display: flex; flex-direction: column; gap: 3mm; }
.head { display: flex; align-items: flex-end; justify-content: space-between; gap: 4mm; padding-bottom: 2.5mm; border-bottom: 2px solid #111; }
.side { display: flex; flex-direction: column; gap: .8mm; flex: 1; }
.side.brand { flex-direction: row; align-items: center; gap: 3mm; }
.side.end { align-items: flex-end; }
.logo { max-height: 16mm; max-width: 28mm; object-fit: contain; }
.title { flex: 1.2; display: flex; flex-direction: column; align-items: center; text-align: center; }
.title strong { font-size: 16pt; }
h4 { margin: 0; font-size: 10.5pt; }
table { width: 100%; border-collapse: collapse; }
th, td { border: 1px solid #333; padding: 1.4mm 2mm; text-align: start; vertical-align: middle; }
th { background: #eef1f5; font-weight: 600; width: 22%; }
.ltr { direction: ltr; unicode-bidi: isolate; text-align: end; font-variant-numeric: tabular-nums; }
[dir=ltr] .ltr { text-align: start; }
.small { font-size: 9pt; }
.sentence { margin: 0; padding: 2mm 2.5mm; border: 1px solid #333; font-weight: 600; text-align: center; }
.signatures { table-layout: fixed; }
.signatures th { width: auto; text-align: center; }
.signatures td { height: 30mm; vertical-align: top; }
.signatures td span { display: block; margin-top: 1.6mm; }
.remarks { min-height: 12mm; border: 1px solid #333; padding: 1.6mm 2mm; }
.foot { margin: 0; margin-top: auto; padding-top: 2.5mm; border-top: 1px solid #333; text-align: justify; }
</style>
