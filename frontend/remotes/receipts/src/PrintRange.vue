<script setup lang="ts">
/**
 * «چاپ» with a range of dates (owner's report no. 6, item 10; his answer to Q63, 19 Nov 2026: the same on the receipts list and on the
 * list of cheques). The button opens two boxes, «از تاریخ» and «تا تاریخ», filled with the first and last date of the rows now listed
 * and free to change; «چاپ گزارش» hands the range to the screen, which prints the rows inside it. Dates are Jalali text
 * ("1405/08/23"), which sorts as text; an empty box leaves that side open.
 */
import { computed, reactive } from 'vue'
import { normaliseJalali, translate, type Locale } from '@treasury/ui-kit'

const props = withDefaults(defineProps<{
  /** The dates of the rows now listed (empty ones are ignored); they fill the two boxes. */
  dates: (string | null | undefined)[]
  /** How many rows the range holds, so the button says what it will print. */
  count: (from: string | null, to: string | null) => number
  locale?: Locale
  /** What the dates are, for the labels: the receipt date, or a cheque's due date. */
  dateName?: [string, string]
  /** What a row is, for the button's text. */
  rowName?: [string, string]
  disabled?: boolean
}>(), { locale: 'fa', dateName: () => ['تاریخ', 'date'], rowName: () => ['رسید', 'receipts'], disabled: false })
const emit = defineEmits<{ print: [from: string | null, to: string | null] }>()
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => (props.locale === 'fa' ? n.toLocaleString('fa-IR', { useGrouping: false }) : String(n))

const range = reactive({ open: false, from: '', to: '', error: '' })
function toggle() {
  if (range.open) { range.open = false; return }
  const dates = props.dates.filter((d): d is string => !!d).sort()
  Object.assign(range, { open: true, from: dates[0] ?? '', to: dates[dates.length - 1] ?? '', error: '' })
}
const from = computed(() => normaliseJalali(range.from))
const to = computed(() => normaliseJalali(range.to))
const inRange = computed(() => props.count(from.value, to.value))
function print() {
  if ((range.from.trim() && !from.value) || (range.to.trim() && !to.value)) return void (range.error = translate('FiscalDate.Invalid', props.locale))
  if (from.value && to.value && from.value > to.value) return void (range.error = t('«از تاریخ» بعد از «تا تاریخ» است.', '"From" is after "to".'))
  if (!inRange.value) return void (range.error = t('در این بازه ردیفی نیست.', 'Nothing in this range.'))
  range.error = ''
  emit('print', from.value, to.value)
}
defineExpose({ range, toggle, print })
</script>

<template>
  <button type="button" :disabled="disabled" :aria-expanded="range.open" @click="toggle">{{ t('چاپ', 'Print') }}</button>
  <fieldset v-if="range.open" class="tr-range">
    <legend>{{ t('بازهٔ گزارش چاپی', 'Range of the printed report') }}</legend>
    <label>{{ t(`از ${dateName[0]}`, `From (${dateName[1]})`) }} <input v-model="range.from" dir="ltr" placeholder="1405/01/01" /></label>
    <label>{{ t(`تا ${dateName[0]}`, `To (${dateName[1]})`) }} <input v-model="range.to" dir="ltr" placeholder="1405/12/29" /></label>
    <div class="tr-buttons">
      <button type="button" @click="print">{{ t(`چاپ گزارش (${num(inRange)} ${rowName[0]})`, `Print the report (${inRange} ${rowName[1]})`) }}</button>
      <button type="button" class="tr-secondary" @click="range.open = false">{{ t('انصراف', 'Back') }}</button>
    </div>
    <p class="muted small wide3">{{ t('تاریخ‌ها از روی فهرست پر شده‌اند (اولین و آخرین تاریخ ردیف‌های فهرست)؛ برای بازهٔ دلخواه آن‌ها را تغییر دهید.',
      'The dates come from the list (the first and last date of its rows); change them for the range you want.') }}</p>
    <p v-if="range.error" class="tr-msg error wide3" role="status">{{ range.error }}</p>
  </fieldset>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
/* The boxes take a line of their own under the buttons they were opened from. */
.tr-range { flex-basis: 100%; margin-top: .25rem; background: #f8fafc; grid-template-columns: repeat(auto-fill, minmax(11rem, 14rem)); }
</style>
