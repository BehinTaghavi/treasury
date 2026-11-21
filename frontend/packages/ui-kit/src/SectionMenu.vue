<script setup lang="ts">
/**
 * An area with its sub-menu (owner, 20 Nov 2026, the design he approved): a white column beside the main menu lists the screens of
 * the area under small group titles, and the chosen screen is drawn beside it. The column can be closed, so a wide form has the
 * whole width; then a button «منوی …» and the path to the screen stand above the screen. Whether it is closed is remembered on this
 * computer. On a narrow screen the items wrap above the screen instead of standing beside it.
 *
 * The shell gives its page a padding through `--shell-pad`; this layout reaches over it, so the column touches the main menu.
 */
import { computed, onMounted, ref } from 'vue'
import { menuPath, type MenuGroup } from './menu'

const props = withDefaults(defineProps<{
  /** The area's name: the heading of the column. */
  title: string
  groups: readonly MenuGroup[]
  /** The id of the chosen item. */
  modelValue: string
  locale?: 'fa' | 'en'
  /** Names this menu when its closed state is remembered. */
  storageKey?: string
}>(), { locale: 'fa', storageKey: '' })
const emit = defineEmits<{ 'update:modelValue': [id: string] }>()

const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (fa.value ? f : e)
const closed = ref(false)
const key = computed(() => (props.storageKey ? `treasury.menu.${props.storageKey}.closed` : ''))
onMounted(() => {
  try { if (key.value) closed.value = localStorage.getItem(key.value) === '1' } catch { /* private window: the menu simply opens */ }
})
function setClosed(value: boolean) {
  closed.value = value
  try { if (key.value) localStorage.setItem(key.value, value ? '1' : '0') } catch { /* not remembered */ }
}
const path = computed(() => menuPath(props.title, props.groups, props.modelValue, fa.value))
</script>

<template>
  <div class="tr-sm" :class="{ closed }">
    <nav v-if="!closed" class="tr-sm-nav" :aria-label="t(`منوی ${title}`, `${title} menu`)">
      <div class="tr-sm-in">
        <div class="tr-sm-head">
          <strong>{{ title }}</strong>
          <button type="button" class="tr-sm-toggle" :aria-label="t('بستن منو', 'Close the menu')" :title="t('بستن منو', 'Close the menu')" @click="setClosed(true)">
            <svg aria-hidden="true" viewBox="0 0 24 24"><path :d="fa ? 'M13 6l6 6-6 6M5 6l6 6-6 6' : 'M11 6l-6 6 6 6M19 6l-6 6 6 6'" /></svg>
          </button>
        </div>
        <template v-for="(g, gi) in groups" :key="gi">
          <span v-if="g.title" class="tr-sm-cap">{{ fa ? g.title[0] : g.title[1] }}</span>
          <button v-for="i in g.items" :key="i.id" type="button" :class="{ active: i.id === modelValue, boxed: i.boxed }"
                  :aria-current="i.id === modelValue ? 'page' : undefined" @click="emit('update:modelValue', i.id)">
            <svg v-if="i.boxed" aria-hidden="true" viewBox="0 0 24 24"><path d="M8 6h12M8 12h12M8 18h12M4 6h.01M4 12h.01M4 18h.01" /></svg>
            <span>{{ fa ? i.title[0] : i.title[1] }}</span>
          </button>
        </template>
      </div>
    </nav>
    <div class="tr-sm-main">
      <div v-if="closed" class="tr-sm-bar">
        <button type="button" class="tr-sm-open" @click="setClosed(false)">
          <svg aria-hidden="true" viewBox="0 0 24 24"><path d="M4 6h16M4 12h16M4 18h16" /></svg>
          <span>{{ t(`منوی ${title}`, `${title} menu`) }}</span>
        </button>
        <span class="tr-sm-path">{{ path.join(' ‹ ') }}</span>
      </div>
      <slot :closed="closed" :path="path" />
    </div>
  </div>
</template>

<style scoped>
.tr-sm { display: grid; grid-template-columns: 14.5rem minmax(0, 1fr); margin: calc(-1 * var(--shell-pad, 0px)); font-family: Vazirmatn, Tahoma, sans-serif; color: #18233a; }
.tr-sm.closed { grid-template-columns: minmax(0, 1fr); }
.tr-sm-nav { background: #fff; border-inline-end: 1px solid #dfe4ec; }
/* The column stays in view while a long form scrolls. */
.tr-sm-in { position: sticky; top: 0; max-height: 100vh; overflow-y: auto; box-sizing: border-box; display: flex; flex-direction: column; gap: .125rem; padding: .85rem .75rem 1.25rem; }
.tr-sm-head { display: flex; align-items: center; justify-content: space-between; padding-inline-start: .5rem; margin-bottom: .25rem; }
.tr-sm-head strong { font-size: .95rem; }
.tr-sm-cap { padding: .75rem .75rem .2rem; font-size: .75rem; font-weight: 600; color: #5b6779; }
.tr-sm-head + .tr-sm-cap { padding-top: .25rem; }
.tr-sm-in > button { display: flex; align-items: center; gap: .6rem; min-height: 2.5rem; padding: .3rem .75rem; border: 0; border-radius: 8px; background: transparent; color: #2b3850;
  font: inherit; font-size: .9rem; line-height: 1.5; text-align: start; cursor: pointer; }
.tr-sm-in > button:hover { background: #f0f3f8; }
.tr-sm-in > button.active { background: #e6edfb; color: #1b2f55; font-weight: 700; }
/* «فهرست جامع دریافت‌ها» stands in a coloured box (owner's report no. 6, item 12). */
.tr-sm-in > button.boxed { margin-top: .9rem; min-height: 2.9rem; border: 1px solid #0e7c6b; border-radius: 10px; background: #e3f4f0; color: #0b5d51; font-weight: 600; }
.tr-sm-in > button.boxed:hover { background: #d3ede7; }
.tr-sm-in > button.boxed.active { background: #0e7c6b; color: #fff; }
.tr-sm svg { width: 1.2rem; height: 1.2rem; flex: 0 0 auto; fill: none; stroke: currentColor; stroke-width: 1.8; stroke-linecap: round; stroke-linejoin: round; }
.tr-sm-toggle { width: 2.5rem; height: 2.5rem; display: flex; align-items: center; justify-content: center; border: 0; border-radius: 8px; background: transparent; color: #5b6779; cursor: pointer; }
.tr-sm-toggle:hover { background: #f0f3f8; color: #1b2f55; }
.tr-sm button:focus-visible { outline: 2px solid #2a5bd7; outline-offset: 1px; }
.tr-sm-main { min-width: 0; padding: var(--shell-pad, 1rem); }
.tr-sm-bar { display: flex; align-items: center; flex-wrap: wrap; gap: .75rem; margin-bottom: .9rem; }
.tr-sm-open { display: flex; align-items: center; gap: .5rem; min-height: 2.5rem; padding: 0 .85rem; border: 1px solid #c9d1dd; border-radius: 8px; background: #fff; color: #1b2f55; font: inherit; font-size: .85rem;
  font-weight: 500; cursor: pointer; }
.tr-sm-open:hover { background: #f0f3f8; }
.tr-sm-path { font-size: .85rem; color: #5b6779; }
/* Narrow screen: the items wrap above the screen, each group title on a line of its own. */
@media (max-width: 900px) {
  .tr-sm { grid-template-columns: minmax(0, 1fr); }
  .tr-sm-nav { border-inline-end: 0; border-bottom: 1px solid #dfe4ec; }
  .tr-sm-in { position: static; max-height: none; flex-direction: row; flex-wrap: wrap; align-items: center; gap: .25rem; }
  .tr-sm-head, .tr-sm-cap { flex: 0 0 100%; box-sizing: border-box; }
  .tr-sm-cap { padding: .4rem .5rem 0; }
  .tr-sm-in > button.boxed { margin-top: 0; }
}
@media print { .tr-sm { display: block; margin: 0; } .tr-sm-nav, .tr-sm-bar { display: none; } .tr-sm-main { padding: 0; } }
</style>
