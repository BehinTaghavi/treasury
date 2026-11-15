<script setup lang="ts">
/**
 * An amount box that shows thousand separators while the user types (owner's report no. 4, item 10). The model is the text as
 * shown ("12,500,000"); parseAmount reads it. The cursor stays after the same digit when a separator is added or removed.
 */
import { groupAmount } from './format'

const props = withDefaults(defineProps<{ modelValue: string | number | null | undefined; decimals?: number; allowNegative?: boolean }>(),
  { decimals: 2, allowNegative: false })
const emit = defineEmits<{ 'update:modelValue': [string] }>()

const shown = () => groupAmount(props.modelValue == null ? '' : String(props.modelValue), props.decimals, props.allowNegative)

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
  <input :value="shown()" dir="ltr" :inputmode="decimals ? 'decimal' : 'numeric'" autocomplete="off" @input="onInput" />
</template>
