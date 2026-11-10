<script setup lang="ts">
/**
 * Chart of accounts copied from the accounting system (کدینگ حساب‌ها): code lengths per level (set once), import from
 * the accounting system's Excel export, the tree, and the تفصیل types each account allows (several per position; owner,
 * 10 Nov 2026). Old accounting versions do not export those types, so they are chosen here.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import ImportBox from './ImportBox.vue'

interface Format { group: number; general: number; subsidiary: number; subSubsidiary: number; detail: number; locked: boolean }
interface Account { code: string; name: string; level: string; parentCode: string | null; isPostable: boolean; isActive: boolean; detail1Types: string[]; detail2Types: string[] }
interface DetailType { type: string; activeCount: number }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const base = '/api/reference/ledger'

const format = ref<Format | null>(null)
const lengths = reactive({ group: 1, general: 2, subsidiary: 4, subSubsidiary: 6, detail: 6 })
const accounts = ref<Account[]>([])
const types = ref<DetailType[]>([])
const search = ref('')
const postableOnly = ref(false)
const showInactive = ref(false)
const editing = ref<{ code: string; d1: string[]; d2: string[] } | null>(null)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)

const levelNames: Record<string, [string, string]> = {
  Group: ['گروه', 'Group'], General: ['کل', 'General'], Subsidiary: ['معین', 'Subsidiary'], SubSubsidiary: ['معین در معین', 'Sub-subsidiary'],
}
const indent: Record<string, number> = { Group: 0, General: 1, Subsidiary: 2, SubSubsidiary: 3 }
const key = (s: string) => s.replace(/[يى]/g, 'ی').replace(/ك/g, 'ک').replace(/[\s‌]/g, '').toLowerCase()

const shown = computed(() => {
  const q = key(search.value)
  return accounts.value.filter(a =>
    (!postableOnly.value || a.isPostable) && (showInactive.value || a.isActive) && (!q || a.code.startsWith(q) || key(a.name).includes(q)))
})

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }

async function load() {
  const f = await api.get<Format | null>(`${base}/format`)
  if (f.ok) {
    format.value = f.value
    if (f.value) Object.assign(lengths, f.value)
  }
  const a = await api.get<Account[]>(`${base}/accounts?includeInactive=true`)
  if (a.ok) accounts.value = a.value
  const ty = await api.get<DetailType[]>(`${base}/detail-types`)
  if (ty.ok) types.value = ty.value
}

async function saveFormat() {
  const r = await api.put<boolean>(`${base}/format`, { ...lengths })
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t('طول کدها ثبت شد.', 'Code lengths saved.') }
  await load()
}

async function saveTypes() {
  if (!editing.value) return
  const r = await api.put<boolean>(`${base}/accounts/${editing.value.code}/detail-types`, { detail1Types: editing.value.d1, detail2Types: editing.value.d2 })
  if (!r.ok) return fail(r.code)
  editing.value = null
  message.value = { kind: 'ok', text: t('انواع تفصیل ثبت شد.', 'Detail types saved.') }
  await load()
}

function toggle(list: string[], type: string) {
  const i = list.indexOf(type)
  if (i >= 0) list.splice(i, 1)
  else list.push(type)
}

onMounted(load)
</script>

<template>
  <div>
    <h3>{{ t('کدینگ حساب‌ها (گروه، کل، معین، معین در معین)', 'Chart of accounts') }}</h3>

    <form class="tr-format" @submit.prevent="saveFormat">
      <strong>{{ t('طول کد هر سطح (رقم)', 'Code length per level (digits)') }}</strong>
      <label>{{ t('گروه', 'Group') }}<input v-model.number="lengths.group" type="number" min="1" max="15" :disabled="format?.locked" /></label>
      <label>{{ t('کل', 'General') }}<input v-model.number="lengths.general" type="number" min="1" max="15" :disabled="format?.locked" /></label>
      <label>{{ t('معین', 'Subsidiary') }}<input v-model.number="lengths.subsidiary" type="number" min="1" max="15" :disabled="format?.locked" /></label>
      <label>{{ t('معین در معین', 'Sub-subsidiary') }}<input v-model.number="lengths.subSubsidiary" type="number" min="1" max="15" :disabled="format?.locked" /></label>
      <label>{{ t('تفصیل', 'Detail') }}<input v-model.number="lengths.detail" type="number" min="1" max="15" :disabled="format?.locked" /></label>
      <button v-if="!format?.locked" type="submit">{{ format ? t('ذخیره', 'Save') : t('تعیین طول کدها', 'Set code lengths') }}</button>
      <small v-else class="muted">{{ t('پس از ورود حساب‌ها قفل است.', 'Locked after the first import.') }}</small>
    </form>

    <ImportBox
      v-if="format"
      :endpoint="`${base}/chart/import`"
      :hint="['فایل «تجمیعی» کدینگ از سیستم حسابداری', 'the chart-of-accounts export of the accounting system']"
      :api-base="apiBase" :locale="locale" :token="token"
      @imported="load"
    />
    <p v-else class="muted">{{ t('برای ورود حساب‌ها ابتدا طول کدها را تعیین کنید.', 'Set the code lengths before importing accounts.') }}</p>

    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <div v-if="editing" class="tr-types">
      <strong>{{ t('انواع تفصیل مجاز برای حساب', 'Allowed detail types for account') }} {{ editing.code }}</strong>
      <div class="tr-cols">
        <fieldset>
          <legend>{{ t('تفصیل اول', 'First detail') }}</legend>
          <label v-for="ty in types" :key="ty.type"><input type="checkbox" :checked="editing.d1.includes(ty.type)" @change="toggle(editing.d1, ty.type)" /> {{ ty.type }} <small>({{ ty.activeCount }})</small></label>
        </fieldset>
        <fieldset :disabled="editing.d1.length === 0">
          <legend>{{ t('تفصیل دوم', 'Second detail') }}</legend>
          <label v-for="ty in types" :key="ty.type"><input type="checkbox" :checked="editing.d2.includes(ty.type)" @change="toggle(editing.d2, ty.type)" /> {{ ty.type }}</label>
        </fieldset>
      </div>
      <button type="button" @click="saveTypes">{{ t('ذخیره', 'Save') }}</button>
      <button type="button" class="tr-link" @click="editing = null">{{ t('انصراف', 'Cancel') }}</button>
    </div>

    <div v-if="accounts.length" class="tr-filters">
      <label>{{ t('جستجو (کد یا نام)', 'Search (code or name)') }}<input v-model="search" autocomplete="off" /></label>
      <label class="tr-check"><input v-model="postableOnly" type="checkbox" /> {{ t('فقط حساب‌های قابل ثبت', 'Only accounts that take entries') }}</label>
      <label class="tr-check"><input v-model="showInactive" type="checkbox" /> {{ t('نمایش غیرفعال‌ها', 'Show inactive') }}</label>
      <span class="muted">{{ shown.length }} / {{ accounts.length }}</span>
    </div>

    <table v-if="accounts.length" class="tr-table">
      <thead>
        <tr><th>{{ t('کد', 'Code') }}</th><th>{{ t('نام', 'Name') }}</th><th>{{ t('سطح', 'Level') }}</th><th>{{ t('تفصیل اول', 'Detail 1') }}</th><th>{{ t('تفصیل دوم', 'Detail 2') }}</th><th /></tr>
      </thead>
      <tbody>
        <tr v-for="a in shown" :key="a.code" :class="{ inactive: !a.isActive, heading: !a.isPostable }">
          <td dir="ltr" class="code">{{ a.code }}</td>
          <td :style="{ paddingInlineStart: `${0.5 + 1.25 * (indent[a.level] ?? 0)}rem` }">{{ a.name }}</td>
          <td>{{ locale === 'fa' ? levelNames[a.level]?.[0] : levelNames[a.level]?.[1] }}</td>
          <td>{{ a.detail1Types.join('، ') }}</td>
          <td>{{ a.detail2Types.join('، ') }}</td>
          <td><button v-if="a.isPostable && a.isActive" type="button" class="tr-link" @click="editing = { code: a.code, d1: [...a.detail1Types], d2: [...a.detail2Types] }">{{ t('انواع تفصیل', 'Detail types') }}</button></td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
h3 { margin-top: 0; }
.tr-format { display: flex; flex-wrap: wrap; gap: .75rem; align-items: end; margin-bottom: 1rem; }
.tr-format label, .tr-filters label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
.tr-format input { width: 5rem; }
input { padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; }
button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
.tr-link { background: transparent; color: #1d4ed8; border-color: transparent; padding: .2rem .4rem; }
.tr-filters { display: flex; flex-wrap: wrap; gap: 1rem; align-items: end; margin: .5rem 0; }
.tr-check { flex-direction: row !important; align-items: center; }
.tr-types { padding: .75rem; background: #f5f7fa; border-radius: 8px; margin: .75rem 0; }
.tr-cols { display: flex; gap: 1rem; flex-wrap: wrap; margin: .5rem 0; }
fieldset { border: 1px solid #e4e7eb; border-radius: 6px; display: flex; flex-direction: column; gap: .2rem; min-width: 14rem; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-table { width: 100%; border-collapse: collapse; }
.tr-table th, .tr-table td { padding: .35rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; }
.tr-table tr.heading td { font-weight: 600; }
.tr-table tr.inactive td { color: #9aa5b1; }
.code { font-family: ui-monospace, monospace; }
.muted { color: #7b8794; }
small { color: #7b8794; }
</style>
