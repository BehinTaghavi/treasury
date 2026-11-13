<script setup lang="ts">
/**
 * تفصیل accounts copied from the accounting system: import from Excel, search by code or name, filter by type, and browse the
 * whole list page by page (owner's report no. 2, item 2: with «همه» only the first 100 by code – banks and facilities – showed).
 */
import { computed, onMounted, ref, watch } from 'vue'
import { createApi, type Locale } from '@treasury/ui-kit'
import ImportBox from './ImportBox.vue'
import SearchSelect from '../components/SearchSelect.vue'

interface Detail { code: string; name: string; type: string | null; isCostCenter: boolean; isActive: boolean }
interface DetailType { type: string; activeCount: number }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const base = '/api/reference/ledger'
const take = 100
const page = ref(0)
const total = ref(0)
const num = (n: number) => n.toLocaleString(props.locale === 'fa' ? 'fa-IR' : 'en-US')
const from = computed(() => (total.value ? page.value * take + 1 : 0))
const to = computed(() => Math.min(total.value, (page.value + 1) * take))
const pages = computed(() => Math.max(1, Math.ceil(total.value / take)))

const details = ref<Detail[]>([])
const types = ref<DetailType[]>([])
const search = ref('')
const type = ref('')
const showInactive = ref(false)
const hasFormat = ref(false)

function filters() {
  const q = new URLSearchParams({ includeInactive: String(showInactive.value) })
  if (search.value.trim()) q.set('search', search.value.trim())
  if (type.value) q.set('type', type.value)
  return q
}

async function load() {
  const q = filters()
  const c = await api.get<number>(`${base}/details-count?${q}`)
  if (c.ok) total.value = c.value
  if (page.value >= pages.value) page.value = pages.value - 1
  q.set('take', String(take))
  q.set('skip', String(page.value * take))
  const r = await api.get<Detail[]>(`${base}/details?${q}`)
  if (r.ok) details.value = r.value
}

function goTo(p: number) {
  page.value = Math.min(Math.max(0, p), pages.value - 1)
  void load()
}

async function loadAll() {
  const f = await api.get<unknown>(`${base}/format`)
  hasFormat.value = f.ok && f.value !== null
  const ty = await api.get<DetailType[]>(`${base}/detail-types`)
  if (ty.ok) types.value = ty.value
  await load()
}

let timer: ReturnType<typeof setTimeout> | undefined
watch(search, () => { clearTimeout(timer); timer = setTimeout(() => { page.value = 0; void load() }, 250) })
watch([type, showInactive], () => { page.value = 0; void load() })
onMounted(loadAll)
</script>

<template>
  <div>
    <h3>{{ t('حساب‌های تفصیل', 'Detail accounts') }}</h3>
    <ImportBox
      v-if="hasFormat"
      :endpoint="`${base}/details/import`"
      :hint="['فایل تفصیل «محدوده» با نوع و وضعیت، یا فایل «رقم»', 'the detail-account export with type and status, or the short one']"
      :api-base="apiBase" :locale="locale" :token="token"
      @imported="loadAll"
    />
    <p v-else class="muted">{{ t('ابتدا در بخش «کدینگ حساب‌ها» طول کدها را تعیین کنید.', 'Set the code lengths in the chart of accounts first.') }}</p>

    <div class="tr-filters">
      <label>{{ t('جستجو (کد یا نام)', 'Search (code or name)') }}<input v-model="search" autocomplete="off" /></label>
      <label>{{ t('نوع تفصیل', 'Detail type') }}
        <SearchSelect v-model="type" :options="types.map(ty => ({ value: ty.type, label: ty.type, hint: num(ty.activeCount) }))"
                      :empty-label="t('همه', 'All')" :aria-label="t('نوع تفصیل', 'Detail type')" :none-text="t('نوعی پیدا نشد', 'No type found')" />
      </label>
      <label class="tr-check"><input v-model="showInactive" type="checkbox" /> {{ t('نمایش غیرفعال‌ها', 'Show inactive') }}</label>
    </div>

    <table class="tr-table">
      <thead><tr><th>{{ t('کد', 'Code') }}</th><th>{{ t('نام', 'Name') }}</th><th>{{ t('نوع', 'Type') }}</th><th>{{ t('مرکز هزینه', 'Cost centre') }}</th><th>{{ t('وضعیت', 'Status') }}</th></tr></thead>
      <tbody>
        <tr v-for="d in details" :key="d.code" :class="{ inactive: !d.isActive }">
          <td dir="ltr" class="code">{{ d.code }}</td><td>{{ d.name }}</td><td>{{ d.type }}</td>
          <td>{{ d.isCostCenter ? t('است', 'Yes') : '' }}</td><td>{{ d.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
        </tr>
        <tr v-if="!details.length"><td colspan="5" class="muted">{{ t('موردی نیست.', 'Nothing here yet.') }}</td></tr>
      </tbody>
    </table>
    <div v-if="total > take" class="tr-pager">
      <button type="button" :disabled="page === 0" @click="goTo(0)">{{ t('اول', 'First') }}</button>
      <button type="button" :disabled="page === 0" @click="goTo(page - 1)">{{ t('قبلی', 'Previous') }}</button>
      <span role="status">{{ t(`ردیف ${num(from)} تا ${num(to)} از ${num(total)}`, `Rows ${from}–${to} of ${total}`) }}</span>
      <button type="button" :disabled="page >= pages - 1" @click="goTo(page + 1)">{{ t('بعدی', 'Next') }}</button>
      <button type="button" :disabled="page >= pages - 1" @click="goTo(pages - 1)">{{ t('آخر', 'Last') }}</button>
    </div>
    <p v-else-if="total" class="muted">{{ t(`${num(total)} مورد`, `${total} rows`) }}</p>
  </div>
</template>

<style scoped>
h3 { margin-top: 0; }
.tr-filters { display: flex; flex-wrap: wrap; gap: 1rem; align-items: end; margin: .5rem 0; }
.tr-filters label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
.tr-check { flex-direction: row !important; align-items: center; }
input, select { padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; }
.tr-table { width: 100%; border-collapse: collapse; }
.tr-table th, .tr-table td { padding: .35rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; }
.tr-table tr.inactive td { color: #9aa5b1; }
.code { font-family: ui-monospace, monospace; }
.muted { color: #7b8794; }
.tr-pager { display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; margin-top: .75rem; }
.tr-pager button { padding: .35rem .75rem; border: 1px solid #1d4ed8; border-radius: 6px; background: #fff; color: #1d4ed8; font: inherit; cursor: pointer; }
.tr-pager button:disabled { opacity: .45; cursor: default; }
</style>
