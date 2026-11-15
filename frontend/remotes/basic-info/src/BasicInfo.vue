<script setup lang="ts">
/**
 * Basic Information (اطلاعات پایه), in the order of the new-version manual.
 * The simple lists share one editor (CatalogueList); sections still to be built are shown as "coming next".
 */
import { computed, ref } from 'vue'
import type { Locale } from '@treasury/ui-kit'
import CatalogueList from './CatalogueList.vue'
import type { ListSpec } from './types'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), {
  apiBase: '',
  locale: 'fa',
  token: undefined,
})

const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)

const lists: ListSpec[] = [
  { list: 'countries', title: ['کشور', 'Countries'], singular: ['کشور', 'country'] },
  { list: 'provinces', title: ['استان / ایالت', 'Provinces'], singular: ['استان', 'province'], parent: { list: 'countries', label: ['کشور', 'Country'] } },
  { list: 'cities', title: ['شهر', 'Cities'], singular: ['شهر', 'city'], parent: { list: 'provinces', label: ['استان', 'Province'] } },
  { list: 'currencies', title: ['نوع ارز', 'Currencies'], singular: ['ارز', 'currency'], hasCode: true },
  { list: 'banks', title: ['نام بانک‌ها', 'Bank names'], singular: ['بانک', 'bank'] },
]

// Next pull requests (docs/plan.md, Phase B). Shown so the owner sees the whole menu.
const comingNext: [string, string][] = [
  ['حساب‌های بانکی', 'Bank accounts'],
  ['دسته چک‌ها', 'Cheque books'],
  ['عطف‌ها', 'References'],
  ['گروه بابت‌ها و بابت‌ها', 'Purpose groups and purposes'],
  ['صندوق‌ها', 'Cash desks'],
]

const selected = ref(lists[0]!.list)
const current = computed(() => lists.find(l => l.list === selected.value)!)
</script>

<template>
  <section class="tr-basic" :dir="fa ? 'rtl' : 'ltr'">
    <h2>{{ t('اطلاعات پایه', 'Basic information') }}</h2>
    <div class="tr-layout">
      <nav class="tr-sections" :aria-label="t('بخش‌های اطلاعات پایه', 'Basic information sections')">
        <button
          v-for="l in lists"
          :key="l.list"
          type="button"
          :class="{ active: l.list === selected }"
          :aria-current="l.list === selected ? 'page' : undefined"
          @click="selected = l.list"
        >{{ fa ? l.title[0] : l.title[1] }}</button>
        <span v-for="c in comingNext" :key="c[0]" class="tr-soon">{{ fa ? c[0] : c[1] }} <small>{{ t('(به‌زودی)', '(next)') }}</small></span>
      </nav>
      <CatalogueList :key="current.list + locale" :spec="current" :api-base="apiBase" :locale="locale" :token="token" />
    </div>
  </section>
</template>

<style scoped>
.tr-basic { font-family: Vazirmatn, Tahoma, sans-serif; color: #1f2933; }
.tr-layout { display: grid; grid-template-columns: 13rem 1fr; gap: 1.25rem; align-items: start; }
.tr-sections { display: flex; flex-direction: column; gap: .25rem; }
.tr-sections button { text-align: start; padding: .5rem .75rem; border: 0; border-radius: 6px; background: transparent; color: #1f2933; font: inherit; cursor: pointer; }
.tr-sections button.active { background: #e8effc; color: #1d4ed8; font-weight: 600; }
.tr-sections button:hover { background: #f0f3f8; }
.tr-soon { padding: .5rem .75rem; color: #9aa5b1; }
.tr-soon small { font-size: .75rem; }
@media (max-width: 720px) { .tr-layout { grid-template-columns: 1fr; } }
</style>
