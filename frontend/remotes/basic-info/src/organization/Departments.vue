<script setup lang="ts">
/**
 * Departments (بخش‌ها / واحدهای سازمانی), new-version manual item 12: number, title and the cost centres (تفصیل marked as cost
 * centres in the accounting system) the department consists of. A cost centre belongs to one department only.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '../components/SearchSelect.vue'
import { searchKey } from '../text'

interface Department { id: string; number: number; name: string; costCenterCodes: string[]; isActive: boolean; version: number }
interface Detail { code: string; name: string; isActive: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US', { useGrouping: false })
const base = '/api/reference/departments'

const departments = ref<Department[]>([])
const centers = ref<Detail[]>([])
const search = ref('')
const editing = ref<Department | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const form = reactive({ number: '', name: '', costCenterCodes: [] as string[] })
const toAdd = ref('')

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const centerName = (code: string) => centers.value.find(c => c.code === code)?.name ?? ''
const owner = (code: string) => departments.value.find(d => d.costCenterCodes.includes(code) && d.id !== editing.value?.id)
const centerChoices = computed<Choice[]>(() => centers.value
  .filter(c => !form.costCenterCodes.includes(c.code))
  .map(c => ({ value: c.code, label: c.name, hint: c.code, muted: !c.isActive || !!owner(c.code) })))
const shown = computed(() => {
  const q = searchKey(search.value)
  return q ? departments.value.filter(d => searchKey(d.name).includes(q) || String(d.number) === q || d.costCenterCodes.some(c => c.includes(q))) : departments.value
})

async function load() {
  const [d, c] = await Promise.all([
    api.get<Department[]>(`${base}?includeInactive=true`),
    api.get<Detail[]>('/api/reference/ledger/details?costCentersOnly=true&includeInactive=true&take=1000'),
  ])
  if (d.ok) departments.value = d.value
  else fail(d.code)
  if (c.ok) centers.value = c.value
}

function startNew() {
  const next = Math.max(0, ...departments.value.map(d => d.number)) + 1
  Object.assign(form, { number: String(next), name: '', costCenterCodes: [] })
  editing.value = null
  showForm.value = true
  message.value = null
}

function startEdit(d: Department) {
  Object.assign(form, { number: String(d.number), name: d.name, costCenterCodes: [...d.costCenterCodes] })
  editing.value = d
  showForm.value = true
  message.value = null
}

function addCenter(code: string) {
  if (code && !form.costCenterCodes.includes(code)) form.costCenterCodes.push(code)
  toAdd.value = ''
}

async function save() {
  if (busy.value) return
  const body = { number: Number(searchKey(form.number)) || 0, name: form.name, costCenterCodes: form.costCenterCodes }
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { form: body, version: editing.value.version })
    : await api.post<string>(base, body)
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t(`بخش «${form.name.trim()}» ثبت شد.`, `Department "${form.name.trim()}" saved.`) }
  showForm.value = false
  await load()
}

async function change(d: Department, what: 'activate' | 'deactivate' | 'delete') {
  if (what === 'delete' && !window.confirm(t(`بخش «${d.name}» حذف شود؟`, `Delete department "${d.name}"?`))) return
  const r = await api.post<string>(`${base}/${d.id}/${what}`, { version: d.version })
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: what === 'delete' ? t('بخش حذف شد.', 'Department deleted.') : what === 'activate' ? t('فعال شد.', 'Switched on.') : t('غیرفعال شد.', 'Switched off.') }
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <h3>{{ t('بخش‌ها (واحدهای سازمانی)', 'Departments') }}</h3>
    <p v-if="!centers.length" class="muted">{{ t('مراکز هزینه از «حساب‌های تفصیل» (تفصیل‌های علامت‌خورده به عنوان مرکز هزینه) خوانده می‌شوند؛ ابتدا آن‌ها را وارد کنید.', 'Cost centres come from the detail accounts marked as cost centres; import them first.') }}</p>
    <button v-if="!showForm" type="button" @click="startNew">{{ t('بخش جدید', 'New department') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t('ویرایش بخش', 'Edit department') : t('بخش جدید', 'New department') }}</legend>
        <label class="req"><span>{{ t('شماره بخش', 'Number') }} <span class="star">*</span></span><input v-model="form.number" dir="ltr" inputmode="numeric" /></label>
        <label class="req wide"><span>{{ t('عنوان بخش (واحد سازمانی)', 'Title') }} <span class="star">*</span></span><input v-model="form.name" maxlength="100" autocomplete="off" /></label>
        <div class="wide">
          <label>{{ t('افزودن مرکز هزینه', 'Add a cost centre') }}
            <SearchSelect :model-value="toAdd" :options="centerChoices" :aria-label="t('مرکز هزینه', 'Cost centre')" :none-text="t('مرکز هزینه‌ای پیدا نشد', 'No cost centre found')"
                          @update:model-value="addCenter" />
          </label>
          <ul class="tr-chips">
            <li v-for="c in form.costCenterCodes" :key="c">
              <span dir="ltr">{{ c }}</span> {{ centerName(c) }}
              <small v-if="owner(c)" class="tr-warn">{{ t(`(در بخش «${owner(c)!.name}»)`, `(in "${owner(c)!.name}")`) }}</small>
              <button type="button" :aria-label="t('حذف', 'Remove')" @click="form.costCenterCodes = form.costCenterCodes.filter(x => x !== c)">×</button>
            </li>
            <li v-if="!form.costCenterCodes.length" class="muted">{{ t('مرکز هزینه‌ای انتخاب نشده است.', 'No cost centre chosen.') }}</li>
          </ul>
        </div>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <template v-if="departments.length">
      <div class="tr-search">
        <label>{{ t('جستجو', 'Search') }}<input v-model="search" type="search" autocomplete="off" /></label>
        <button type="button" class="tr-secondary" :disabled="!search" @click="search = ''">{{ t('نمایش همه', 'Show all') }}</button>
      </div>
      <table class="tr-table">
        <thead><tr><th>{{ t('شماره', 'No.') }}</th><th>{{ t('عنوان بخش', 'Title') }}</th><th>{{ t('مراکز هزینه', 'Cost centres') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th /></tr></thead>
        <tbody>
          <tr v-for="d in shown" :key="d.id" :class="{ inactive: !d.isActive }">
            <td>{{ num(d.number) }}</td>
            <td>{{ d.name }}</td>
            <td><span v-for="c in d.costCenterCodes" :key="c" class="tr-tag"><span dir="ltr">{{ c }}</span> {{ centerName(c) }}</span></td>
            <td>{{ d.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="startEdit(d)">{{ t('ویرایش', 'Edit') }}</button>
              <button type="button" class="tr-link" @click="change(d, d.isActive ? 'deactivate' : 'activate')">{{ d.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
              <button type="button" class="tr-link danger" @click="change(d, 'delete')">{{ t('حذف', 'Delete') }}</button>
            </td>
          </tr>
        </tbody>
      </table>
    </template>
    <p v-else-if="!showForm" class="muted">{{ t('هنوز بخشی تعریف نشده است.', 'No department yet.') }}</p>
  </div>
</template>

<style scoped src="./organization.css"></style>
