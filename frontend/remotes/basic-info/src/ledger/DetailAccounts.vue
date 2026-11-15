<script setup lang="ts">
/** تفصیل accounts copied from the accounting system: import from Excel, search by code or name, filter by type. */
import { onMounted, ref, watch } from 'vue'
import { createApi, type Locale } from '@treasury/ui-kit'
import ImportBox from './ImportBox.vue'

interface Detail { code: string; name: string; type: string | null; isCostCenter: boolean; isActive: boolean }
interface DetailType { type: string; activeCount: number }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const base = '/api/reference/ledger'
const take = 100

const details = ref<Detail[]>([])
const types = ref<DetailType[]>([])
const search = ref('')
const type = ref('')
const showInactive = ref(false)
const hasFormat = ref(false)

async function load() {
  const q = new URLSearchParams({ take: String(take), includeInactive: String(showInactive.value) })
  if (search.value.trim()) q.set('search', search.value.trim())
  if (type.value) q.set('type', type.value)
  const r = await api.get<Detail[]>(`${base}/details?${q}`)
  if (r.ok) details.value = r.value
}

async function loadAll() {
  const f = await api.get<unknown>(`${base}/format`)
  hasFormat.value = f.ok && f.value !== null
  const ty = await api.get<DetailType[]>(`${base}/detail-types`)
  if (ty.ok) types.value = ty.value
  await load()
}

let timer: ReturnType<typeof setTimeout> | undefined
watch(search, () => { clearTimeout(timer); timer = setTimeout(load, 250) })
watch([type, showInactive], load)
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
        <select v-model="type">
          <option value="">{{ t('همه', 'All') }}</option>
          <option v-for="ty in types" :key="ty.type" :value="ty.type">{{ ty.type }} ({{ ty.activeCount }})</option>
        </select>
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
    <p v-if="details.length === take" class="muted">{{ t(`فقط ${take} ردیف اول نشان داده می‌شود؛ برای یافتن بقیه جستجو کنید.`, `Only the first ${take} rows are shown; search to find others.`) }}</p>
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
</style>
