<script setup lang="ts">
/**
 * The printed receipt of promissory notes (owner's form «فرم دریافت سفته», 16 Nov 2026): one A4 sheet with the receipt, the persons,
 * the notes with their total in figures and words, the supplementary information, the text confirming receipt and the three
 * signatures (who handed the notes over, the treasury's receiver, the authorised approver). NoteReceiptPrint shows it on the screen
 * and hands a copy to the printer.
 * The company's name is written by hand on the dotted line until the company's particulars are kept in Basic Information.
 */
import { computed } from 'vue'
import { amountInWords, formatAmount, toPersianDigits, type Locale } from '@treasury/ui-kit'
import { attachmentNames, checkNames, confirmationText, formNames, maturityNames, reasonNames, type NoteGuarantor, type NoteReceipt } from './notes'

const props = withDefaults(defineProps<{
  receipt: NoteReceipt; locale?: Locale; departmentName?: string; ledgerAccount?: string; reference1Title?: string; reference2Title?: string
}>(), { locale: 'fa', departmentName: '', ledgerAccount: '', reference1Title: '', reference2Title: '' })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (fa.value ? f : e)
const digits = (s: string | number | null | undefined) => (s == null ? '' : fa.value ? toPersianDigits(String(s)) : String(s))
const money = (n: number) => formatAmount(n, props.locale)
const pick = (m: Record<string, [string, string]>, k: string) => (m[k] ? (fa.value ? m[k][0] : m[k][1]) : k)
const terms = computed(() => props.receipt.noteTerms)
const notes = computed(() => props.receipt.instruments ?? [])
/** The authorised signatories chosen by the signature rule; with none, one empty box is left for the approver. */
const signers = computed(() => (terms.value?.signers?.length ? terms.value.signers : [{ id: '', name: '', position: null }]))
/** Every guarantor once, with the notes it guarantees (all of them when it is on every note). */
const guarantors = computed(() => {
  const found = new Map<string, { g: NoteGuarantor; notes: string[] }>()
  for (const n of notes.value) for (const g of n.details?.guarantors ?? []) {
    const key = `${g.name}|${g.nationalCode ?? ''}|${g.detailCode ?? ''}`
    if (!found.has(key)) found.set(key, { g, notes: [] })
    found.get(key)!.notes.push(n.number ?? '')
  }
  return [...found.values()].map(x => ({ ...x.g, notes: x.notes.length === notes.value.length ? '' : x.notes.join('، ') }))
})
/** The ticks of article 308 that every note on the receipt has. */
const checked = (key: string) => notes.value.length > 0 && notes.value.every(n => (n.details?.checks as Record<string, boolean> | null | undefined)?.[key])
</script>

<template>
  <div class="tr-print-sheet" :dir="fa ? 'rtl' : 'ltr'">
    <div class="head">
      <div class="side">
        <span>{{ t('نام شرکت / مؤسسه:', 'Company:') }} ..............................</span>
        <span>{{ t('واحد خزانه‌داری', 'Treasury') }}</span>
      </div>
      <strong class="title">{{ t('رسید دریافت سفته', 'Receipt of promissory notes') }}</strong>
      <div class="side end">
        <span>{{ t('شماره رسید:', 'Receipt no.:') }} <strong>{{ digits(receipt.number) }}</strong></span>
        <span>{{ t('تاریخ دریافت:', 'Date:') }} <strong>{{ digits(receipt.date) }}</strong></span>
      </div>
    </div>

    <table>
      <tbody>
        <tr><th>{{ t('شعبه / واحد سازمانی', 'Department') }}</th><td>{{ departmentName }}</td><th>{{ t('گروه بابت', 'Purpose group') }}</th><td>{{ receipt.purposeGroupName }}</td></tr>
        <tr><th>{{ t('حساب معین', 'Ledger account') }}</th><td>{{ digits(ledgerAccount) }}</td><th>{{ t('بابت', 'Purpose') }}</th><td>{{ receipt.purpose }}</td></tr>
        <tr>
          <th>{{ reference1Title || t('عطف ۱', 'Reference 1') }}</th><td>{{ digits(receipt.reference1) }}</td>
          <th>{{ reference2Title || t('عطف ۲', 'Reference 2') }}</th><td>{{ digits(receipt.reference2) }}</td>
        </tr>
      </tbody>
    </table>

    <h4>{{ t('مشخصات اشخاص', 'Persons') }}</h4>
    <table>
      <thead>
        <tr><th>{{ t('عنوان', 'Role') }}</th><th>{{ t('نام شخص / شرکت', 'Name') }}</th><th>{{ t('حساب تفصیلی', 'Detail account') }}</th>
          <th>{{ t('کد ملی / شناسه ملی', 'National code / ID') }}</th><th>{{ t('شماره تماس', 'Phone') }}</th></tr>
      </thead>
      <tbody>
        <tr><td>{{ t('صادرکننده و متعهد سفته', 'Issuer and obligor') }}</td><td>{{ receipt.payerName }}</td><td>{{ digits(receipt.payerDetailCode) }}</td>
          <td>{{ digits(terms?.issuerNationalCode) }}</td><td>{{ digits(terms?.issuerPhone) }}</td></tr>
        <tr v-for="(g, i) in guarantors" :key="i">
          <td>{{ t('ضامن / ظهرنویس', 'Guarantor / endorser') }}<span v-if="g.notes" class="small"> ({{ t('سفتهٔ', 'note') }} {{ digits(g.notes) }})</span></td>
          <td>{{ g.name }}</td><td>{{ digits(g.detailCode) }}</td><td>{{ digits(g.nationalCode) }}</td><td>{{ digits(g.phone) }}</td>
        </tr>
        <tr><td>{{ t('تحویل‌گیرنده سفته', 'Received by') }}</td><td>{{ t('خزانه‌داری', 'Treasury') }} – {{ receipt.recordedBy }}</td><td /><td /><td /></tr>
        <tr><td>{{ t('ذینفع سفته', 'Beneficiary') }}</td><td>{{ t('شرکت (دارندهٔ سیستم خزانه‌داری)', 'The company') }}</td><td /><td /><td /></tr>
      </tbody>
    </table>

    <h4>{{ t('مشخصات سفته‌های دریافتی', 'Notes received') }}</h4>
    <div v-if="terms" class="ticks">
      <span>{{ t('نوع سند:', 'Document:') }}</span>
      <span v-for="k in Object.keys(formNames)" :key="k" class="tick"><i :class="{ on: terms.form === k }" />{{ pick(formNames, k) }}</span>
      <span class="gap">{{ t('نوع سررسید:', 'Due:') }}</span>
      <span v-for="k in Object.keys(maturityNames)" :key="k" class="tick"><i :class="{ on: terms.maturity === k }" />{{ pick(maturityNames, k) }}</span>
    </div>
    <table>
      <thead>
        <tr><th class="narrow">{{ t('ردیف', 'No.') }}</th><th>{{ t('شماره / شناسه سفته', 'Note number / ID') }}</th><th>{{ t('تاریخ صدور', 'Issue date') }}</th>
          <th>{{ t('تاریخ سررسید', 'Due date') }}</th><th>{{ t('مبلغ تعهد (ریال)', 'Amount (Rial)') }}</th></tr>
      </thead>
      <tbody>
        <tr v-for="(n, i) in notes" :key="n.id">
          <td>{{ digits(i + 1) }}</td><td>{{ digits(n.number) }}</td><td>{{ digits(n.noteIssueDate) }}</td>
          <td>{{ n.dueDate ? digits(n.dueDate) : n.details?.onDemand ? t('عندالمطالبه', 'On demand') : '' }}</td><td class="amount">{{ money(n.amountInRial) }}</td>
        </tr>
        <tr class="sum"><td colspan="2">{{ t('تعداد کل:', 'Count:') }} {{ digits(notes.length) }} {{ t('فقره', '') }}</td>
          <td colspan="2">{{ t('جمع مبلغ تعهد به عدد', 'Total in figures') }}</td><td class="amount">{{ money(receipt.totalRial) }}</td></tr>
        <tr><td colspan="5">{{ t('جمع مبلغ تعهد به حروف:', 'Total in words:') }} <strong>{{ amountInWords(receipt.totalRial) }} {{ t('ریال', 'Rial') }}</strong></td></tr>
      </tbody>
    </table>

    <h4>{{ t('اطلاعات تکمیلی', 'Supplementary information') }}</h4>
    <table v-if="terms">
      <tbody>
        <tr>
          <th>{{ t('دلیل دریافت سفته', 'Reason') }}</th>
          <td><span v-for="k in Object.keys(reasonNames)" :key="k" class="tick"><i :class="{ on: terms.reason === k }" />{{ pick(reasonNames, k) }}</span></td>
          <th>{{ t('وضعیت دریافت اصل سند', 'Original') }}</th>
          <td>
            <span class="tick"><i :class="{ on: terms.originalReceived }" />{{ t('دریافت شد', 'Received') }}<template v-if="terms.originalReceivedOn"> – {{ digits(terms.originalReceivedOn) }}</template></span>
            <span class="tick"><i :class="{ on: !terms.originalReceived }" />{{ t('دریافت نشد', 'Not received') }}</span>
          </td>
        </tr>
        <tr><th>{{ t('موضوع تعهد (در سفتهٔ تضمینی)', 'What the guarantee secures') }}</th><td>{{ terms.commitmentSubject }}</td>
          <th>{{ t('شرایط و مرجع تأیید استرداد سفته', 'Terms of return') }}</th><td>{{ terms.returnTerms }}</td></tr>
        <tr><th>{{ t('محل نگهداری اصل سند / مرجع الکترونیکی', 'Kept at') }}</th><td>{{ terms.custodyPlace }}</td>
          <th>{{ t('شماره سند حسابداری', 'Voucher no.') }}</th><td /></tr>
        <tr>
          <th>{{ t('پیوست‌ها', 'Attachments') }}</th>
          <td><span v-for="[k, f, e] in attachmentNames" :key="k" class="tick"><i :class="{ on: terms.attachments[k] }" />{{ t(f, e) }}</span></td>
          <th>{{ t('شرح نقص یا مغایرت احتمالی', 'Defect or discrepancy') }}</th><td>{{ terms.discrepancy }}</td>
        </tr>
      </tbody>
    </table>

    <div class="ticks">
      <strong>{{ t('کنترل مشخصات روی هر سفته (مادهٔ ۳۰۸ قانون تجارت):', 'Checked on each note (art. 308):') }}</strong>
      <span v-for="[k, f, e] in checkNames" :key="k" class="tick"><i :class="{ on: checked(k) }" />{{ t(f, e) }}</span>
    </div>

    <p class="confirm">{{ t(confirmationText[0], confirmationText[1]) }}</p>

    <table class="signatures">
      <thead>
        <tr>
          <th>{{ t('تحویل‌دهنده', 'Handed over by') }}</th><th>{{ t('تحویل‌گیرندهٔ خزانه', 'Received by (treasury)') }}</th>
          <th v-for="s in signers" :key="s.id">{{ t('تأییدکنندهٔ مجاز', 'Authorised signatory') }}<template v-if="s.position"> – {{ s.position }}</template></th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><span>{{ t('نام:', 'Name:') }} {{ terms?.deliveredBy }}</span><span>{{ t('امضا:', 'Signature:') }}</span><span>{{ t('تاریخ:', 'Date:') }}</span></td>
          <td><span>{{ t('نام:', 'Name:') }} {{ receipt.recordedBy }}</span><span>{{ t('امضا:', 'Signature:') }}</span><span>{{ t('تاریخ:', 'Date:') }}</span></td>
          <td v-for="s in signers" :key="s.id"><span>{{ t('نام:', 'Name:') }} {{ s.name }}</span><span>{{ t('امضا:', 'Signature:') }}</span><span>{{ t('تاریخ:', 'Date:') }}</span></td>
        </tr>
      </tbody>
    </table>

    <div class="remarks">{{ t('توضیحات:', 'Remarks:') }} {{ receipt.note }}</div>
  </div>
</template>

<style scoped>
.tr-print-sheet { box-sizing: border-box; width: 210mm; max-width: 100%; min-height: 297mm; margin: 1rem auto; padding: 11mm 12mm; background: #fff; color: #111;
  border: 1px solid #cbd2d9; font-family: Vazirmatn, Tahoma, sans-serif; font-size: 9.5pt; line-height: 1.4; display: flex; flex-direction: column; gap: 2.2mm; }
.head { display: flex; align-items: flex-end; justify-content: space-between; gap: 4mm; padding-bottom: 2mm; border-bottom: 2px solid #111; }
.side { display: flex; flex-direction: column; gap: .5mm; flex: 1; }
.side.end { align-items: flex-end; }
.title { flex: 1; text-align: center; font-size: 15pt; }
h4 { margin: 0; font-size: 9.5pt; }
table { width: 100%; border-collapse: collapse; }
th, td { border: 1px solid #333; padding: .8mm 1.6mm; text-align: start; vertical-align: middle; }
th { background: #eef1f5; font-weight: 600; }
tbody th { width: 22%; }
th.narrow { width: 8%; }
.amount { text-align: end; font-variant-numeric: tabular-nums; }
tr.sum td { background: #eef1f5; font-weight: 600; }
.small { font-size: 9pt; }
.ticks { display: flex; flex-wrap: wrap; gap: 1mm 3.5mm; align-items: center; }
.ticks .gap { margin-inline-start: 6mm; }
.tick { display: inline-flex; align-items: center; gap: 1.3mm; margin-inline-end: 2.5mm; white-space: nowrap; }
.tick i { display: inline-block; width: 2.9mm; height: 2.9mm; border: 1px solid #111; }
.tick i.on { background: #111; }
.confirm { margin: 0; padding: 1.6mm 2mm; border: 1px solid #333; }
.signatures td { height: 20mm; vertical-align: top; }
.signatures td span { display: block; margin-top: 1.4mm; }
.signatures { table-layout: fixed; }
.remarks { flex: 1; min-height: 12mm; border: 1px solid #333; padding: 1.6mm 2mm; }
</style>
