<script setup lang="ts">
/**
 * A drop-down list you can type into (combo box). Opening it shows the whole list; typing narrows it by name or code,
 * comparing Persian letters the way the server does (ي/ك = ی/ک, spaces ignored). Keyboard: ↑ ↓ Enter Esc.
 * Owner's report of 11 Nov 2026, items 3, 11 and 13: country, bank and ledger-account boxes must show the full list
 * and let the user find an entry by typing.
 *
 * For long lists kept on the server (تفصیل), the parent listens to `search` and replaces `options` with what it found.
 */
import { computed, nextTick, ref, watch } from 'vue'
import { searchKey } from '../text'

export interface Choice { value: string; label: string; hint?: string; muted?: boolean }

const props = withDefaults(defineProps<{
  modelValue: string
  options: Choice[]
  /** Text of the first entry, which clears the choice (e.g. "همه"); omitted: no such entry. */
  emptyLabel?: string
  placeholder?: string
  disabled?: boolean
  /** Required field: drawn with a red frame (owner, item 12). */
  required?: boolean
  /** The parent filters on the server; the list shows `options` as given. */
  remote?: boolean
  ariaLabel?: string
  /** Shown when nothing matches the typed text. */
  noneText?: string
  dir?: 'rtl' | 'ltr' | 'auto'
}>(), { emptyLabel: undefined, placeholder: '', disabled: false, required: false, remote: false, ariaLabel: undefined, noneText: '—', dir: 'auto' })

const emit = defineEmits<{ 'update:modelValue': [string]; search: [string] }>()

const open = ref(false)
const typed = ref<string | null>(null) // null: show the chosen entry's text
const active = ref(0)
const listEl = ref<HTMLElement | null>(null)
const id = `ss-${Math.random().toString(36).slice(2, 9)}`

const chosen = computed(() => props.options.find(o => o.value === props.modelValue))
const text = computed(() => typed.value ?? (chosen.value ? display(chosen.value) : ''))
/** The code first, so a long name cannot push it out of the box. */
const display = (o: Choice) => (o.hint ? `${o.hint} – ${o.label}` : o.label)

const shown = computed<Choice[]>(() => {
  const q = searchKey(typed.value ?? '')
  const list = props.remote || !q ? props.options : props.options.filter(o => searchKey(display(o)).includes(q))
  return props.emptyLabel !== undefined && !typed.value ? [{ value: '', label: props.emptyLabel }, ...list] : list
})

watch(shown, () => { active.value = Math.max(0, shown.value.findIndex(o => o.value === props.modelValue)) })

function show() {
  if (props.disabled) return
  open.value = true
  active.value = Math.max(0, shown.value.findIndex(o => o.value === props.modelValue))
  void nextTick(scrollToActive)
}

function onInput(e: Event) {
  typed.value = (e.target as HTMLInputElement).value
  open.value = true
  active.value = 0
  emit('search', typed.value)
}

function pick(o: Choice) {
  emit('update:modelValue', o.value)
  typed.value = null
  open.value = false
}

function close() {
  // Typed text that matches nothing is dropped; the previous choice stays.
  typed.value = null
  open.value = false
}

function onKey(e: KeyboardEvent) {
  if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
    e.preventDefault()
    if (!open.value) return show()
    const n = shown.value.length
    if (n) active.value = (active.value + (e.key === 'ArrowDown' ? 1 : n - 1)) % n
    void nextTick(scrollToActive)
  } else if (e.key === 'Enter' && open.value) {
    e.preventDefault()
    const o = shown.value[active.value]
    if (o) pick(o)
  } else if (e.key === 'Escape' && open.value) {
    e.preventDefault()
    close()
  }
}

function scrollToActive() {
  listEl.value?.querySelector<HTMLElement>(`[data-index="${active.value}"]`)?.scrollIntoView({ block: 'nearest' })
}
</script>

<template>
  <div class="ss" :class="{ open, required, disabled }" @focusout="(e: FocusEvent) => { if (!(e.currentTarget as HTMLElement).contains(e.relatedTarget as Node)) close() }">
    <input
      :id="id"
      role="combobox"
      :aria-expanded="open"
      :aria-controls="`${id}-list`"
      :aria-label="ariaLabel"
      :aria-required="required || undefined"
      :value="text"
      :placeholder="placeholder"
      :disabled="disabled"
      :dir="dir"
      autocomplete="off"
      @focus="(e: FocusEvent) => { (e.target as HTMLInputElement).select(); show() }"
      @click="show"
      @input="onInput"
      @keydown="onKey"
    />
    <button type="button" class="ss-arrow" tabindex="-1" :disabled="disabled" :aria-label="ariaLabel" @mousedown.prevent="open ? close() : show()">▾</button>
    <ul v-if="open" :id="`${id}-list`" ref="listEl" role="listbox" class="ss-list">
      <li
        v-for="(o, i) in shown"
        :key="o.value + i"
        role="option"
        :data-index="i"
        :aria-selected="o.value === modelValue"
        :class="{ active: i === active, muted: o.muted, empty: o.value === '' }"
        @mousedown.prevent="pick(o)"
        @mousemove="active = i"
      >
        <span>{{ o.label }}</span><small v-if="o.hint" dir="ltr">{{ o.hint }}</small>
      </li>
      <li v-if="!shown.length" class="ss-none">{{ noneText }}</li>
    </ul>
  </div>
</template>

<style scoped>
.ss { position: relative; display: flex; }
.ss input { flex: 1; min-width: 0; padding: .45rem .5rem; padding-inline-end: 1.8rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; background: #fff; }
.ss.required input { border-color: #d64545; box-shadow: inset 0 0 0 1px #f5c2c2; }
.ss input:focus { outline: 2px solid #2563eb; outline-offset: 1px; }
.ss-arrow { position: absolute; inset-inline-end: 2px; top: 2px; bottom: 2px; width: 1.6rem; border: 0; background: transparent; color: #52606d; cursor: pointer; padding: 0; }
.ss-list { position: absolute; z-index: 20; inset-inline: 0; top: calc(100% + 2px); max-height: 18rem; overflow-y: auto; margin: 0; padding: .25rem 0; list-style: none; background: #fff; border: 1px solid #cbd2d9; border-radius: 6px; box-shadow: 0 6px 18px rgba(31, 41, 51, .15); }
.ss-list li { display: flex; justify-content: space-between; gap: .75rem; padding: .35rem .6rem; cursor: pointer; }
.ss-list li small { color: #7b8794; font-family: ui-monospace, monospace; }
.ss-list li.active { background: #e8effc; }
.ss-list li[aria-selected="true"] { font-weight: 600; }
.ss-list li.muted { color: #9aa5b1; }
.ss-list li.empty { color: #52606d; border-bottom: 1px solid #eef1f4; }
.ss-none { color: #9aa5b1; cursor: default; }
</style>
