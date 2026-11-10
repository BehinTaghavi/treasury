<script setup lang="ts">
/**
 * Import of an accounting-system export: choose the file → the server's preview (what would be added, renamed, switched off,
 * and the rows it cannot take, with their Excel row number) → confirm. Nothing changes before the confirmation.
 */
import { computed, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'

interface Rejected { rowNumber: number; code: string; reason: string }
interface Report { added: number; renamed: number; reactivated: number; deactivated: number; unchanged: number; rejected: Rejected[] }

const props = withDefaults(defineProps<{ endpoint: string; hint: [string, string]; apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), {
  apiBase: '', locale: 'fa', token: undefined,
})
const emit = defineEmits<{ imported: [] }>()

const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const file = ref<File | null>(null)
const preview = ref<Report | null>(null)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const busy = ref(false)
const input = ref<HTMLInputElement | null>(null)

const shownRejected = computed(() => preview.value?.rejected.slice(0, 30) ?? [])

async function choose(e: Event) {
  file.value = (e.target as HTMLInputElement).files?.[0] ?? null
  preview.value = null
  message.value = null
  if (!file.value) return
  busy.value = true
  const r = await api.upload<Report>(`${props.endpoint}?preview=true`, file.value, file.value.name)
  busy.value = false
  if (r.ok) preview.value = r.value
  else message.value = { kind: 'error', text: translate(r.code, props.locale) }
}

async function confirmImport() {
  if (!file.value) return
  busy.value = true
  const r = await api.upload<Report>(props.endpoint, file.value, file.value.name)
  busy.value = false
  if (!r.ok) {
    message.value = { kind: 'error', text: translate(r.code, props.locale) }
    return
  }
  message.value = { kind: 'ok', text: t(`ورود انجام شد: ${r.value.added} حساب جدید، ${r.value.renamed} تغییر نام، ${r.value.deactivated} غیرفعال.`, `Imported: ${r.value.added} new, ${r.value.renamed} renamed, ${r.value.deactivated} switched off.`) }
  preview.value = null
  file.value = null
  if (input.value) input.value.value = ''
  emit('imported')
}
</script>

<template>
  <div class="tr-import">
    <label>{{ t('ورود از اکسل', 'Import from Excel') }} <small>({{ locale === 'fa' ? hint[0] : hint[1] }})</small>
      <input ref="input" type="file" accept=".xls,.xlsx" :disabled="busy" @change="choose" />
    </label>
    <p v-if="busy" class="muted">{{ t('در حال خواندن فایل…', 'Reading the file…') }}</p>

    <div v-if="preview" class="tr-preview" role="status">
      <strong>{{ t('پیش‌نمایش (هنوز چیزی ثبت نشده است):', 'Preview (nothing saved yet):') }}</strong>
      <ul>
        <li>{{ t('جدید', 'New') }}: {{ preview.added }}</li>
        <li>{{ t('تغییر نام', 'Renamed') }}: {{ preview.renamed }}</li>
        <li>{{ t('فعال‌شدن دوباره', 'Switched on again') }}: {{ preview.reactivated }}</li>
        <li :class="{ warn: preview.deactivated > 0 }">{{ t('غیرفعال می‌شود (در فایل نیست)', 'Switched off (not in the file)') }}: {{ preview.deactivated }}</li>
        <li>{{ t('بدون تغییر', 'Unchanged') }}: {{ preview.unchanged }}</li>
        <li :class="{ warn: preview.rejected.length > 0 }">{{ t('ردیف‌های پذیرفته‌نشده', 'Rows not taken') }}: {{ preview.rejected.length }}</li>
      </ul>
      <table v-if="shownRejected.length" class="tr-table">
        <thead><tr><th>{{ t('ردیف اکسل', 'Excel row') }}</th><th>{{ t('کد', 'Code') }}</th><th>{{ t('علت', 'Reason') }}</th></tr></thead>
        <tbody>
          <tr v-for="r in shownRejected" :key="r.rowNumber"><td>{{ r.rowNumber }}</td><td dir="ltr">{{ r.code }}</td><td>{{ translate(r.reason, locale) }}</td></tr>
        </tbody>
      </table>
      <button type="button" :disabled="busy" @click="confirmImport">{{ t('تأیید و ورود', 'Confirm and import') }}</button>
    </div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
  </div>
</template>

<style scoped>
.tr-import { padding: .75rem; background: #f5f7fa; border-radius: 8px; margin-bottom: 1rem; }
label { display: flex; flex-direction: column; gap: .35rem; font-size: .85rem; }
small { color: #7b8794; }
.tr-preview ul { margin: .5rem 0; padding-inline-start: 1.25rem; }
.warn { color: #8a4b00; font-weight: 600; }
button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .6; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-table { border-collapse: collapse; margin: .5rem 0; }
.tr-table th, .tr-table td { padding: .3rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; }
.muted { color: #7b8794; }
</style>
