<script setup lang="ts">
/**
 * An amount box that shows thousand separators while the user types (owner's report no. 4, item 10) and, under the box, the amount
 * in words as it is typed (owner's report no. 6, items 1 and 5: "هرگاه مبلغی را به عدد وارد میکنیم بصورت آنلاین مبلغ به حروف در زیر
 * فیلد مربوطه درج شود"). The model is the text as shown ("12,500,000"); parseAmount reads it. The cursor stays after the same digit
 * when a separator is added or removed.
 * The box is drawn by this component (as SearchSelect is), so it looks the same on every screen; a `class` or `style` given by the
 * screen goes to the frame around the box and the words, every other attribute (aria-label, disabled, placeholder) to the box itself.
 */
import { computed, useAttrs } from 'vue'
import { groupAmount } from './format'
import { amountTextInWords } from './words'

defineOptions({ inheritAttrs: false })
const props = withDefaults(defineProps<{
  modelValue: string | number | null | undefined; decimals?: number; allowNegative?: boolean
  /** The amount in words under the box; off only where the screen writes the words itself. */
  words?: boolean
  /** The unit written after the words, e.g. «ریال». */
  unit?: string
}>(), { decimals: 2, allowNegative: false, words: true, unit: '' })
const emit = defineEmits<{ 'update:modelValue': [string] }>()
const attrs = useAttrs()
const inputAttrs = computed(() => Object.fromEntries(Object.entries(attrs).filter(([k]) => k !== 'class' && k !== 'style')))

const shown = () => groupAmount(props.modelValue == null ? '' : String(props.modelValue), props.decimals, props.allowNegative)
const inWords = computed(() => (props.words ? amountTextInWords(shown(), props.unit) : ''))

function onInput(e: Event) {
  const el = e.target as HTMLInputElement
  const caret = el.selectionStart ?? el.value.length
  const significant = (s: string) => (s.match(/[0-9۰-۹.٫-]/g) ?? []).length
  const before = significant(el.value.slice(0, caret))
  const formatted = groupAmount(el.value, props.decimals, props.allowNegative)
  el.value = formatted
  let pos = 0
  for (let seen = 0; pos < formatted.length && seen < before; pos++) if (/[0-9.-]/.test(formatted[pos]!)) seen++
  el.setSelectionRange(pos, pos)
  emit('update:modelValue', formatted)
}
</script>

<template>
  <span class="tr-amount" :class="attrs.class" :style="(attrs.style as string | undefined)">
    <input v-bind="inputAttrs" :value="shown()" dir="ltr" :inputmode="decimals ? 'decimal' : 'numeric'" autocomplete="off" @input="onInput" />
    <small v-if="inWords" class="tr-amount-words" dir="rtl" aria-live="polite">{{ inWords }}</small>
  </span>
</template>

<!-- Not scoped: a screen marks a required field with `.req` on the label or cell around the box. -->
<style>
.tr-amount { display: flex; flex-direction: column; gap: .15rem; min-width: 0; }
.tr-amount > input { width: 100%; box-sizing: border-box; padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; background: #fff; }
.tr-amount > input:disabled { background: #f5f7fa; color: #52606d; }
.req > .tr-amount > input { border-color: #d64545; box-shadow: inset 0 0 0 1px #f5c2c2; }
.tr-amount-words { color: #52606d; font-size: .78rem; line-height: 1.5; font-weight: 400; white-space: normal; }
/* The words make this field taller than its neighbours: line the row of a form up at the top, so the boxes stay on one line and the
   words hang under their own box (the screens line their fields up at the bottom otherwise). */
fieldset:has(.tr-amount > .tr-amount-words) { align-items: start; }
</style>
