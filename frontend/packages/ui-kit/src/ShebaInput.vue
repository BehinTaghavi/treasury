<script setup lang="ts">
/**
 * A Sheba box with «IR» fixed in front of it, inside the frame (owner's report no. 6, item 16: "حروف IR در ابتدای شماره حساب در داخل
 * کادر مربوطه ثابت باشد و نیاز به تایپ نباشد"): the user types the 24 digits only; a pasted number with IR, spaces or Persian digits
 * is read as well. The model is the whole number as the servers keep it ("IR062960000000100324200001"), or '' while the box is empty.
 * Under the box the number of digits typed is counted, and a complete number whose check digits do not fit is said to be wrong at once
 * (item 17: 24 digits were typed and the refusal seemed to speak of the length).
 * Like AmountInput the box is drawn here: a `class` or `style` given by the screen goes to the frame, every other attribute to the box.
 */
import { computed, useAttrs } from 'vue'
import { groupSheba, shebaCheckDigitsFit, shebaDigits, type Locale } from './format'

defineOptions({ inheritAttrs: false })
const props = withDefaults(defineProps<{ modelValue: string | null | undefined; locale?: Locale }>(), { locale: 'fa' })
const emit = defineEmits<{ 'update:modelValue': [string] }>()
const attrs = useAttrs()
const inputAttrs = computed(() => Object.fromEntries(Object.entries(attrs).filter(([k]) => k !== 'class' && k !== 'style')))

const digits = computed(() => shebaDigits(props.modelValue))
const shown = computed(() => groupSheba(digits.value))
const n = (x: number) => (props.locale === 'fa' ? x.toLocaleString('fa-IR') : String(x))
const hint = computed(() => {
  const count = digits.value.length
  if (!count) return null
  if (count < 24) return { bad: true, text: props.locale === 'fa' ? `${n(count)} رقم از ۲۴ رقم` : `${count} of 24 digits` }
  return shebaCheckDigitsFit(digits.value)
    ? { bad: false, text: props.locale === 'fa' ? '۲۴ رقم – رقم‌های کنترل درست است' : '24 digits – the check digits fit' }
    : { bad: true, text: props.locale === 'fa'
        ? '۲۴ رقم وارد شده است ولی رقم‌های کنترل شبا جور نیست: یکی از رقم‌ها اشتباه است یا این شماره، شبای واقعی نیست.'
        : '24 digits are entered but the check digits do not fit: a digit is wrong, or this is not a real Sheba number.' }
})

function onInput(e: Event) {
  const el = e.target as HTMLInputElement
  const caret = el.selectionStart ?? el.value.length
  // Digits before the cursor, so the cursor stays after the same digit when a space is added or removed.
  const before = shebaDigits(el.value.slice(0, caret).replace(/^\s*ir/i, '')).length
  const typed = shebaDigits(el.value)
  const formatted = groupSheba(typed)
  el.value = formatted
  let pos = 0
  for (let seen = 0; pos < formatted.length && seen < before; pos++) if (/\d/.test(formatted[pos]!)) seen++
  el.setSelectionRange(pos, pos)
  emit('update:modelValue', typed ? `IR${typed}` : '')
}
</script>

<template>
  <span class="tr-sheba" :class="attrs.class" :style="(attrs.style as string | undefined)">
    <span class="tr-sheba-box" dir="ltr">
      <span class="tr-sheba-ir" aria-hidden="true">IR</span>
      <input v-bind="inputAttrs" :value="shown" dir="ltr" inputmode="numeric" autocomplete="off" maxlength="34" placeholder="00 0000 0000 0000 0000 0000 00" @input="onInput" />
    </span>
    <small v-if="hint" :class="['tr-sheba-hint', { bad: hint.bad }]" aria-live="polite">{{ hint.text }}</small>
  </span>
</template>

<!-- Not scoped: a screen marks a refused field with `.bad` on the label around the box (a required one left empty: required.css). -->
<style>
.tr-sheba { display: flex; flex-direction: column; gap: .15rem; min-width: 0; }
.tr-sheba-box { display: flex; align-items: stretch; box-sizing: border-box; min-height: var(--tr-field-h, 0); border: 1px solid var(--tr-field-border, #cbd2d9); border-radius: var(--tr-field-radius, 6px); background: #fff; overflow: hidden; }
.tr-sheba-box:focus-within { outline: 2px solid #93b4f5; outline-offset: -1px; }
.tr-sheba-ir { display: flex; align-items: center; padding: 0 .55rem; background: #eef2f7; color: #1f2933; font-weight: 700; letter-spacing: .05em; border-inline-end: 1px solid #cbd2d9; user-select: none; }
.tr-sheba-box > input { flex: 1; min-width: 0; padding: var(--tr-field-pad, .45rem .5rem); border: 0 !important; border-radius: 0 !important; box-shadow: none !important; outline: 0; font: inherit; background: transparent;
  font-variant-numeric: tabular-nums; letter-spacing: .03em; }
.tr-sheba-box > input:disabled { background: #f5f7fa; color: #52606d; }
.bad > .tr-sheba > .tr-sheba-box { border-color: #d64545; box-shadow: inset 0 0 0 1px #f5c2c2; }
.tr-sheba-hint { color: #52606d; font-size: .78rem; line-height: 1.5; font-weight: 400; }
.tr-sheba-hint.bad { color: #b42318; }
</style>
