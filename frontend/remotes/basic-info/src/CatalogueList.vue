<script setup lang="ts">
/**
 * One Basic Information list: add, edit, delete, switch on/off, search.
 * Owner's report of 11 Nov 2026:
 * - the search box used to hide new entries without saying so (items 4, 5, 7): adding clears the search, the new row is
 *   highlighted, the screen says how many rows the search shows and "نمایش همه" brings the whole list back;
 * - country and province are chosen from lists you can type into, in alphabetical order, Iran chosen first (items 1, 3);
 *   the city screen asks for the country, then the province, and shows that province's cities (item 1);
 * - an entry can be corrected: name, the province of a city, the country of a province, the code of an unused currency
 *   (items 2, 6), or deleted while nothing uses it (items 6, 9).
 * Entries in use (e.g. by a bank account) can only be switched off; a switched-off entry stays here (greyed).
 * Errors are shown from the API's error codes, translated; never raw server text.
 */
import { computed, nextTick, onMounted, reactive, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from './components/SearchSelect.vue'
import { searchKey } from './text'
import type { ListSpec } from './types'

interface Item { id: string; parentId: string | null; name: string; code: string | null; isActive: boolean; version: number; isInUse: boolean }

const props = withDefaults(defineProps<{ spec: ListSpec; apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), {
  apiBase: '',
  locale: 'fa',
  token: undefined,
})

const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const label = (pair: [string, string]) => (fa.value ? pair[0] : pair[1])
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US')
const base = `/api/reference/${props.spec.list}`
/** In more than 90% of cases the country is Iran (owner, item 3): it is chosen when the screen opens. */
const HOME_COUNTRY = 'ایران'

const items = ref<Item[]>([])
const parents = ref<Item[]>([])
const tops = ref<Item[]>([]) // countries, on the city screen
const top = ref('') // chosen country (city screen)
const parent = ref('') // chosen country (province screen) or province (city screen)
const search = ref('')
const form = reactive({ name: '', code: '' })
const editing = ref<{ id: string; name: string; code: string; parentId: string } | null>(null)
const highlight = ref<string | null>(null)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const busy = ref(false)

const show = (kind: 'ok' | 'error', text: string) => { message.value = { kind, text } }
const fail = (code: string) => show('error', translate(code, props.locale))

const byId = (list: Item[], id: string | null) => list.find(p => p.id === id)
const choices = (list: Item[], hint?: (i: Item) => string | undefined): Choice[] =>
  list.map(i => ({ value: i.id, label: i.name, hint: hint?.(i), muted: !i.isActive }))

/** Parents offered for adding and filtering: active ones, on the city screen only the provinces of the chosen country. */
const parentChoices = computed(() => choices(parents.value.filter(p => p.isActive && (!top.value || p.parentId === top.value))))
/** Parents offered when correcting an entry: every active parent, with its country on the city screen. */
const editParentChoices = computed(() =>
  choices(parents.value.filter(p => p.isActive), p => (props.spec.grandparent ? byId(tops.value, p.parentId)?.name : undefined)))
const topChoices = computed(() => choices(tops.value.filter(c => c.isActive)))

const inScope = (i: Item) =>
  (!parent.value || i.parentId === parent.value) && (!top.value || byId(parents.value, i.parentId)?.parentId === top.value)
const scoped = computed(() => items.value.filter(inScope))
const shown = computed(() => {
  const q = searchKey(search.value)
  return q ? scoped.value.filter(i => searchKey(i.name).includes(q) || (i.code ?? '').toLowerCase().includes(q)) : scoped.value
})
const filtered = computed(() => shown.value.length !== items.value.length)
const columns = computed(() => 3 + (props.spec.hasCode ? 1 : 0) + (props.spec.parent ? 1 : 0))

async function load() {
  const r = await api.get<Item[]>(`${base}?includeInactive=true`)
  if (r.ok) items.value = r.value
  else fail(r.code)
}

async function loadList(list: string) {
  const r = await api.get<Item[]>(`/api/reference/${list}?includeInactive=true`)
  if (r.ok) return r.value
  fail(r.code)
  return []
}

/**
 * The country opened first: «ایران», and of several entries with that name the one holding the provinces. If Iran's provinces
 * hang under a country with another name (renamed), that one. Owner's report no. 2, item 1: with a second, empty «ایران» the
 * province list stayed empty.
 */
function homeCountry(countries: Item[], provinces: Item[]) {
  const count = (c: Item) => provinces.filter(p => p.isActive && p.parentId === c.id).length
  const active = countries.filter(c => c.isActive).sort((a, b) => count(b) - count(a))
  const named = active.filter(c => searchKey(c.name) === searchKey(HOME_COUNTRY))
  if (named[0] && count(named[0]) > 0) return named[0].id
  if (active[0] && count(active[0]) > 0) return active[0].id
  return named[0]?.id ?? ''
}

function showAll() {
  search.value = ''
  parent.value = ''
  top.value = ''
}

function onTopChange(id: string) {
  top.value = id
  if (parent.value && byId(parents.value, parent.value)?.parentId !== id) parent.value = ''
}

async function add() {
  if (busy.value) return
  if (props.spec.parent && !parent.value) return fail('Catalogue.ParentRequired')
  busy.value = true
  const r = await api.post<string>(base, {
    name: form.name,
    parentId: parent.value || null,
    code: props.spec.hasCode ? form.code : null,
  })
  busy.value = false
  if (!r.ok) return fail(r.code)
  const name = form.name.trim()
  form.name = ''
  form.code = ''
  search.value = '' // a new entry must never be hidden by an old search (items 4, 7)
  show('ok', t(`«${name}» ثبت شد.`, `"${name}" saved.`))
  await load()
  await reveal(r.value)
}

async function reveal(id: string | null) {
  highlight.value = id
  await nextTick()
  if (id) document.querySelector(`[data-row="${id}"]`)?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
}

function startEdit(i: Item) {
  editing.value = { id: i.id, name: i.name, code: i.code ?? '', parentId: i.parentId ?? '' }
  message.value = null
}

async function saveEdit(i: Item) {
  const e = editing.value
  if (!e) return
  const r = await api.put<unknown>(`${base}/${i.id}`, {
    name: e.name,
    version: i.version,
    parentId: props.spec.parent && e.parentId !== i.parentId ? e.parentId : null,
    code: props.spec.hasCode && e.code.trim().toUpperCase() !== i.code ? e.code : null,
  })
  if (!r.ok) return fail(r.code)
  editing.value = null
  show('ok', t('تغییر ثبت شد.', 'Change saved.'))
  await load()
  await reveal(i.id)
}

async function toggle(i: Item) {
  const r = await api.post<unknown>(`${base}/${i.id}/${i.isActive ? 'deactivate' : 'activate'}`, { version: i.version })
  if (!r.ok) return fail(r.code)
  show('ok', i.isActive ? t('غیرفعال شد.', 'Switched off.') : t('فعال شد.', 'Switched on.'))
  await load()
}

async function remove(i: Item) {
  if (!window.confirm(t(`«${i.name}» حذف شود؟`, `Delete "${i.name}"?`))) return
  const r = await api.post<unknown>(`${base}/${i.id}/delete`, { version: i.version })
  if (!r.ok) return fail(r.code)
  show('ok', t(`«${i.name}» حذف شد.`, `"${i.name}" deleted.`))
  await load()
}

onMounted(async () => {
  if (props.spec.grandparent) tops.value = await loadList(props.spec.grandparent.list)
  if (props.spec.parent) parents.value = await loadList(props.spec.parent.list)
  await load()
  if (props.spec.grandparent) top.value = homeCountry(tops.value, parents.value) // city screen: provinces are the parents
  if (props.spec.parent?.list === 'countries') parent.value = homeCountry(parents.value, items.value) // province screen
})
</script>

<template>
  <div class="tr-list">
    <h3>{{ label(spec.title) }}</h3>

    <form class="tr-form" @submit.prevent="add">
      <label v-if="spec.grandparent">{{ label(spec.grandparent.label) }}
        <SearchSelect
          :model-value="top"
          :options="topChoices"
          :empty-label="t('همه', 'All')"
          :aria-label="label(spec.grandparent.label)"
          :none-text="t('موردی پیدا نشد', 'No match')"
          @update:model-value="onTopChange"
        />
      </label>
      <label v-if="spec.parent">{{ label(spec.parent.label) }}
        <SearchSelect
          v-model="parent"
          :options="parentChoices"
          :empty-label="t(`همه (برای افزودن، یک ${label(spec.parent.label)} را انتخاب کنید)`, `All (choose a ${label(spec.parent.label).toLowerCase()} to add)`)"
          :aria-label="label(spec.parent.label)"
          :none-text="t('موردی پیدا نشد', 'No match')"
        />
      </label>
      <label v-if="spec.hasCode">{{ t('کد ارز (مثل USD)', 'Currency code (e.g. USD)') }}
        <input v-model="form.code" maxlength="3" dir="ltr" autocomplete="off" />
      </label>
      <label>{{ t('نام', 'Name') }} {{ label(spec.singular) }}
        <input v-model="form.name" maxlength="100" autocomplete="off" />
      </label>
      <button type="submit" :disabled="busy">{{ t('افزودن', 'Add') }}</button>
    </form>

    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <div class="tr-search">
      <label>{{ t('جستجو', 'Search') }}<input v-model="search" type="search" autocomplete="off" /></label>
      <button type="button" class="tr-secondary" :disabled="!filtered" @click="showAll">{{ t('نمایش همه', 'Show all') }}</button>
      <span class="tr-count" role="status">
        <template v-if="filtered">{{ t(`${num(shown.length)} از ${num(items.length)} مورد نمایش داده می‌شود`, `Showing ${shown.length} of ${items.length}`) }}</template>
        <template v-else>{{ t(`${num(items.length)} مورد`, `${items.length} entries`) }}</template>
      </span>
    </div>

    <div class="tr-scroll">
      <table class="tr-table">
        <thead>
          <tr>
            <th v-if="spec.hasCode">{{ t('کد', 'Code') }}</th>
            <th>{{ t('نام', 'Name') }}</th>
            <th v-if="spec.parent">{{ label(spec.parent.label) }}</th>
            <th>{{ t('وضعیت', 'Status') }}</th>
            <th />
          </tr>
        </thead>
        <tbody>
          <tr v-for="i in shown" :key="i.id" :data-row="i.id" :class="{ inactive: !i.isActive, fresh: i.id === highlight }">
            <template v-if="editing?.id === i.id">
              <td v-if="spec.hasCode">
                <input v-model="editing.code" maxlength="3" dir="ltr" class="code" :disabled="i.isInUse || i.code === 'IRR'"
                       :title="i.isInUse ? t('این ارز استفاده شده و کد آن ثابت است.', 'This currency is used; its code is fixed.') : undefined"
                       :aria-label="t('کد ارز', 'Currency code')" />
              </td>
              <td><input v-model="editing.name" maxlength="100" :aria-label="t('نام جدید', 'New name')" @keydown.enter.prevent="saveEdit(i)" /></td>
              <td v-if="spec.parent">
                <SearchSelect v-model="editing.parentId" :options="editParentChoices" :aria-label="label(spec.parent.label)" />
              </td>
              <td />
              <td class="tr-actions">
                <button type="button" @click="saveEdit(i)">{{ t('ذخیره', 'Save') }}</button>
                <button type="button" class="tr-link" @click="editing = null">{{ t('انصراف', 'Cancel') }}</button>
              </td>
            </template>
            <template v-else>
              <td v-if="spec.hasCode" dir="ltr" class="code">{{ i.code }}</td>
              <td>{{ i.name }}</td>
              <td v-if="spec.parent">{{ byId(parents, i.parentId)?.name }}</td>
              <td>
                {{ i.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}
                <span v-if="i.isInUse" class="tr-tag" :title="t('در حساب‌ها استفاده شده؛ حذف نمی‌شود.', 'Used elsewhere; cannot be deleted.')">{{ t('در حال استفاده', 'in use') }}</span>
              </td>
              <td class="tr-actions">
                <button type="button" class="tr-link" @click="startEdit(i)">{{ t('ویرایش', 'Edit') }}</button>
                <button type="button" class="tr-link" @click="toggle(i)">{{ i.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
                <button v-if="!i.isInUse && i.code !== 'IRR'" type="button" class="tr-link danger" @click="remove(i)">{{ t('حذف', 'Delete') }}</button>
              </td>
            </template>
          </tr>
          <tr v-if="!shown.length">
            <td :colspan="columns" class="muted">
              <template v-if="items.length">{{ t('با این جستجو یا انتخاب موردی نیست.', 'Nothing matches this search or choice.') }}
                <button type="button" class="tr-link" @click="showAll">{{ t('نمایش همه', 'Show all') }}</button></template>
              <template v-else>{{ t('موردی نیست.', 'Nothing here yet.') }}</template>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
h3 { margin-top: 0; }
.tr-form { display: grid; grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr)); gap: .75rem; align-items: end; }
label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
input, select { padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; }
input:focus, select:focus { outline: 2px solid #2563eb; outline-offset: 1px; }
button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .5; cursor: default; }
.tr-secondary { background: #fff; color: #1d4ed8; }
.tr-link { background: transparent; color: #1d4ed8; border-color: transparent; padding: .2rem .4rem; }
.tr-link.danger { color: #b42318; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-search { display: flex; flex-wrap: wrap; gap: .75rem; align-items: end; margin-top: 1rem; }
.tr-search label { width: 18rem; max-width: 100%; }
.tr-count { color: #52606d; font-size: .85rem; padding-bottom: .5rem; }
.tr-scroll { overflow-x: auto; }
.tr-table { width: 100%; border-collapse: collapse; margin-top: .75rem; }
.tr-table th, .tr-table td { padding: .45rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; vertical-align: middle; }
.tr-table tr.inactive td { color: #9aa5b1; }
.tr-table tr.fresh td { background: #fff7d6; transition: background 2s; }
.tr-table td input { width: 100%; box-sizing: border-box; }
.tr-tag { display: inline-block; margin-inline-start: .4rem; padding: 0 .4rem; border-radius: 4px; background: #eef2f7; color: #52606d; font-size: .75rem; }
.tr-actions { white-space: nowrap; }
.code { font-family: ui-monospace, monospace; }
.muted { color: #7b8794; }
</style>
