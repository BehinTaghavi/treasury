<script setup lang="ts">
/**
 * One Basic Information list: add, rename, switch on/off, search.
 * Entries are never deleted; a switched-off entry stays here (greyed) but is no longer offered on forms.
 * Errors are shown from the API's error codes, translated; never raw server text.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import type { ListSpec } from './types'

interface Item { id: string; parentId: string | null; name: string; code: string | null; isActive: boolean; version: number }

const props = withDefaults(defineProps<{ spec: ListSpec; apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), {
  apiBase: '',
  locale: 'fa',
  token: undefined,
})

const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const base = `/api/reference/${props.spec.list}`

const items = ref<Item[]>([])
const parents = ref<Item[]>([])
const parentFilter = ref('')
const search = ref('')
const form = reactive({ name: '', code: '' })
const editing = ref<{ id: string; name: string } | null>(null)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const busy = ref(false)

const show = (kind: 'ok' | 'error', text: string) => { message.value = { kind, text } }
const fail = (code: string) => show('error', translate(code, props.locale))

/** Same letters as the server's comparison (PersianText.Key): ي/ك = ی/ک, spaces ignored. */
const key = (s: string) => s.replace(/[يى]/g, 'ی').replace(/ك/g, 'ک').replace(/[\s‌]/g, '').toLowerCase()

const parentName = (id: string | null) => parents.value.find(p => p.id === id)?.name ?? ''
const shown = computed(() => {
  const q = key(search.value)
  return q ? items.value.filter(i => key(i.name).includes(q) || (i.code ?? '').toLowerCase().includes(q)) : items.value
})
const activeParents = computed(() => parents.value.filter(p => p.isActive))

async function load() {
  const query = new URLSearchParams({ includeInactive: 'true' })
  if (parentFilter.value) query.set('parentId', parentFilter.value)
  const r = await api.get<Item[]>(`${base}?${query}`)
  if (r.ok) items.value = r.value
  else fail(r.code)
}

async function loadParents() {
  if (!props.spec.parent) return
  const r = await api.get<Item[]>(`/api/reference/${props.spec.parent.list}?includeInactive=true`)
  if (r.ok) parents.value = r.value
  else fail(r.code)
}

async function add() {
  if (busy.value) return
  if (props.spec.parent && !parentFilter.value) return fail('Catalogue.ParentRequired')
  busy.value = true
  const r = await api.post<string>(base, {
    name: form.name,
    parentId: parentFilter.value || null,
    code: props.spec.hasCode ? form.code : null,
  })
  busy.value = false
  if (!r.ok) return fail(r.code)
  form.name = ''
  form.code = ''
  show('ok', t('ثبت شد.', 'Saved.'))
  await load()
}

async function rename(item: Item) {
  if (!editing.value) return
  const r = await api.put<string>(`${base}/${item.id}`, { name: editing.value.name, version: item.version })
  if (!r.ok) return fail(r.code)
  editing.value = null
  show('ok', t('تغییر ثبت شد.', 'Change saved.'))
  await load()
}

async function toggle(item: Item) {
  const r = await api.post<string>(`${base}/${item.id}/${item.isActive ? 'deactivate' : 'activate'}`, { version: item.version })
  if (!r.ok) return fail(r.code)
  show('ok', item.isActive ? t('غیرفعال شد.', 'Switched off.') : t('فعال شد.', 'Switched on.'))
  await load()
}

watch(parentFilter, load)
onMounted(async () => {
  await loadParents()
  await load()
})
</script>

<template>
  <div class="tr-list">
    <h3>{{ fa ? spec.title[0] : spec.title[1] }}</h3>

    <form class="tr-form" @submit.prevent="add">
      <label v-if="spec.parent">{{ fa ? spec.parent.label[0] : spec.parent.label[1] }}
        <select v-model="parentFilter">
          <option value="">{{ t('همه (برای افزودن یکی را انتخاب کنید)', 'All (choose one to add)') }}</option>
          <option v-for="p in activeParents" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
      </label>
      <label v-if="spec.hasCode">{{ t('کد ارز (مثل USD)', 'Currency code (e.g. USD)') }}
        <input v-model="form.code" maxlength="3" dir="ltr" autocomplete="off" />
      </label>
      <label>{{ t('نام', 'Name') }} {{ fa ? spec.singular[0] : spec.singular[1] }}
        <input v-model="form.name" maxlength="100" autocomplete="off" />
      </label>
      <button type="submit" :disabled="busy">{{ t('افزودن', 'Add') }}</button>
    </form>

    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <label class="tr-search">{{ t('جستجو', 'Search') }}<input v-model="search" autocomplete="off" /></label>

    <table class="tr-table">
      <thead>
        <tr>
          <th v-if="spec.hasCode">{{ t('کد', 'Code') }}</th>
          <th>{{ t('نام', 'Name') }}</th>
          <th v-if="spec.parent">{{ fa ? spec.parent.label[0] : spec.parent.label[1] }}</th>
          <th>{{ t('وضعیت', 'Status') }}</th>
          <th />
        </tr>
      </thead>
      <tbody>
        <tr v-for="i in shown" :key="i.id" :class="{ inactive: !i.isActive }">
          <td v-if="spec.hasCode" dir="ltr" class="code">{{ i.code }}</td>
          <td>
            <form v-if="editing?.id === i.id" class="tr-inline" @submit.prevent="rename(i)">
              <input v-model="editing.name" maxlength="100" :aria-label="t('نام جدید', 'New name')" />
              <button type="submit">{{ t('ذخیره', 'Save') }}</button>
              <button type="button" class="tr-link" @click="editing = null">{{ t('انصراف', 'Cancel') }}</button>
            </form>
            <template v-else>{{ i.name }}</template>
          </td>
          <td v-if="spec.parent">{{ parentName(i.parentId) }}</td>
          <td>{{ i.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
          <td class="tr-actions">
            <button type="button" class="tr-link" @click="editing = { id: i.id, name: i.name }">{{ t('ویرایش', 'Edit') }}</button>
            <button type="button" class="tr-link" @click="toggle(i)">{{ i.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
          </td>
        </tr>
        <tr v-if="!shown.length"><td :colspan="5" class="muted">{{ t('موردی نیست.', 'Nothing here yet.') }}</td></tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
h3 { margin-top: 0; }
.tr-form { display: grid; grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr)); gap: .75rem; align-items: end; }
label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
input, select { padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; }
input:focus, select:focus { outline: 2px solid #2563eb; outline-offset: 1px; }
button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .6; }
.tr-link { background: transparent; color: #1d4ed8; border-color: transparent; padding: .2rem .4rem; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-search { max-width: 18rem; margin-top: 1rem; }
.tr-table { width: 100%; border-collapse: collapse; margin-top: .75rem; }
.tr-table th, .tr-table td { padding: .45rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; }
.tr-table tr.inactive td { color: #9aa5b1; }
.tr-inline { display: flex; gap: .5rem; align-items: center; }
.tr-actions { white-space: nowrap; }
.code { font-family: ui-monospace, monospace; }
.muted { color: #7b8794; }
</style>
