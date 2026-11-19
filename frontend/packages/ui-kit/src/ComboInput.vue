<script setup lang="ts">
/**
 * A text box with a list beside it: the ▾ arrow (or ↓) opens the whole list, choosing an entry writes it into the box, and the text
 * stays free to change or to type from scratch. Used for «بابت» (owner's report no. 6, item 4): a purpose is chosen from the purposes
 * of its group, may be edited after it was chosen, or typed outside the list; a text that is not in the list is marked as new, and
 * the form adds it to the list when it is saved (decision of 10 Nov 2026).
 * The model is the text itself, never an id — the receipt keeps the purpose as it was written.
 */
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { searchKey } from './format'

const props = withDefaults(defineProps<{
  modelValue: string
  options: string[]
  placeholder?: string
  disabled?: boolean
  /** Required field: drawn with a red frame. */
  required?: boolean
  ariaLabel?: string
  maxlength?: number
  /** Shown under the box while its text is not one of the options, e.g. «بابت تازه: با ثبت به فهرست اضافه می‌شود». */
  newHint?: string
  /** Shown in the open list when it has no entry. */
  noneText?: string
}>(), { placeholder: '', disabled: false, required: false, ariaLabel: undefined, maxlength: undefined, newHint: '', noneText: '—' })
const emit = defineEmits<{ 'update:modelValue': [string] }>()

const open = ref(false)
/** Typing narrows the list; the arrow shows all of it again. */
const narrowed = ref(false)
const active = ref(-1)
const rootEl = ref<HTMLElement | null>(null)
const inputEl = ref<HTMLInputElement | null>(null)
const listEl = ref<HTMLElement | null>(null)
const id = `ci-${Math.random().toString(36).slice(2, 9)}`

const key = computed(() => searchKey(props.modelValue))
const shown = computed(() => (narrowed.value && key.value ? props.options.filter(o => searchKey(o).includes(key.value)) : props.options))
const isNew = computed(() => key.value !== '' && !props.options.some(o => searchKey(o) === key.value))

function onOutside(e: PointerEvent) {
  if (open.value && rootEl.value && !rootEl.value.contains(e.target as Node)) open.value = false
}
onMounted(() => document.addEventListener('pointerdown', onOutside, true))
onBeforeUnmount(() => document.removeEventListener('pointerdown', onOutside, true))

function showAll() {
  if (props.disabled) return
  narrowed.value = false
  active.value = props.options.findIndex(o => searchKey(o) === key.value)
  open.value = true
}
function toggle() {
  if (open.value) { open.value = false; return }
  inputEl.value?.focus()
  showAll()
}
function onInput(e: Event) {
  emit('update:modelValue', (e.target as HTMLInputElement).value)
  narrowed.value = true
  active.value = -1
  open.value = true
}
function pick(option: string) {
  emit('update:modelValue', option)
  open.value = false
  inputEl.value?.focus()
}
function onKey(e: KeyboardEvent) {
  if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
    e.preventDefault()
    if (!open.value) return showAll()
    const n = shown.value.length
    if (n) active.value = (active.value + (e.key === 'ArrowDown' ? 1 : -1) + n) % n
    listEl.value?.querySelector<HTMLElement>(`[data-index="${active.value}"]`)?.scrollIntoView({ block: 'nearest' })
  } else if (e.key === 'Enter' && open.value && active.value >= 0 && shown.value[active.value] !== undefined) {
    // Enter chooses the marked entry; with no entry marked it is left to the form (Ctrl+Enter saves).
    e.preventDefault()
    e.stopPropagation()
    pick(shown.value[active.value]!)
  } else if (e.key === 'Escape' && open.value) {
    e.preventDefault()
    open.value = false
  }
}
</script>

<template>
  <span ref="rootEl" class="ci" :class="{ required, disabled }" @focusout="(e: FocusEvent) => { if (!(e.currentTarget as HTMLElement).contains(e.relatedTarget as Node)) open = false }">
    <span class="ci-box">
      <input ref="inputEl" :id="id" role="combobox" :aria-expanded="open" :aria-controls="`${id}-list`" :aria-label="ariaLabel" :aria-required="required || undefined"
             :value="modelValue" :placeholder="placeholder" :disabled="disabled" :maxlength="maxlength" autocomplete="off" @input="onInput" @keydown="onKey" />
      <button type="button" class="ci-arrow" tabindex="-1" :disabled="disabled" :aria-label="ariaLabel" @mousedown.prevent="toggle">▾</button>
      <ul v-if="open" :id="`${id}-list`" ref="listEl" role="listbox" class="ci-list">
        <li v-for="(o, i) in shown" :key="o" role="option" :data-index="i" :aria-selected="searchKey(o) === key" :class="{ active: i === active }"
            @mousedown.prevent="pick(o)" @mousemove="active = i">{{ o }}</li>
        <li v-if="!shown.length" class="ci-none">{{ noneText }}</li>
      </ul>
    </span>
    <small v-if="newHint && isNew" class="ci-new">{{ newHint }}</small>
  </span>
</template>

<style scoped>
.ci { display: flex; flex-direction: column; gap: .15rem; min-width: 0; }
.ci-box { position: relative; display: flex; }
.ci input { flex: 1; min-width: 0; padding: .45rem .5rem; padding-inline-end: 1.8rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; background: #fff; }
.ci.required input { border-color: #d64545; box-shadow: inset 0 0 0 1px #f5c2c2; }
.ci input:focus { outline: 2px solid #2563eb; outline-offset: 1px; }
.ci-arrow { position: absolute; inset-inline-end: 2px; top: 2px; bottom: 2px; width: 1.6rem; border: 0; background: transparent; color: #52606d; cursor: pointer; padding: 0; }
.ci-list { position: absolute; z-index: 20; inset-inline: 0; top: calc(100% + 2px); max-height: 16rem; overflow-y: auto; margin: 0; padding: .25rem 0; list-style: none; background: #fff;
  border: 1px solid #cbd2d9; border-radius: 6px; box-shadow: 0 6px 18px rgba(15, 23, 42, .12); }
.ci-list li { padding: .35rem .6rem; cursor: pointer; }
.ci-list li.active { background: #e8effc; }
.ci-list li[aria-selected="true"] { font-weight: 600; }
.ci-none { color: #9aa5b1; cursor: default; }
.ci-new { color: #7a5200; font-size: .78rem; line-height: 1.5; }
</style>
