<script setup lang="ts">
/**
 * Requesters of funds (درخواست کنندگان وجوه), new-version manual item 13: number (may differ from the person's تفصیل), full name,
 * department and cost centre. When the department lists cost centres, the requester's is one of them.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '../components/SearchSelect.vue'
import { searchKey } from '../text'

interface Department { id: string; number: number; name: string; costCenterCodes: string[]; isActive: boolean }
interface Requester { id: string; number: number; fullName: string; departmentId: string; costCenterCode: string; isActive: boolean; isInUse: boolean; version: number }
interface Detail { code: string; name: string; isActive: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US', { useGrouping: false })
const base = '/api/reference/requesters'

const requesters = ref<Requester[]>([])
const departments = ref<Department[]>([])
const centers = ref<Detail[]>([])
const departmentFilter = ref('')
const search = ref('')
const editing = ref<Requester | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const form = reactive({ number: '', fullName: '', departmentId: '', costCenterCode: '' })

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const department = (id: string) => departments.value.find(d => d.id === id)
const centerName = (code: string) => centers.value.find(c => c.code === code)?.name ?? ''
const departmentChoices = computed<Choice[]>(() => departments.value.filter(d => d.isActive || d.id === form.departmentId).map(d => ({ value: d.id, label: d.name, hint: num(d.number) })))
const filterChoices = computed<Choice[]>(() => departments.value.map(d => ({ value: d.id, label: d.name, hint: num(d.number) })))
/** The chosen department's cost centres, or every cost centre when it lists none. */
const centerChoices = computed<Choice[]>(() => {
  const listed = department(form.departmentId)?.costCenterCodes ?? []
  return centers.value.filter(c => (listed.length ? listed.includes(c.code) : c.isActive) || c.code === form.costCenterCode)
    .map(c => ({ value: c.code, label: c.name, hint: c.code, muted: !c.isActive }))
})
const shown = computed(() => {
  const q = searchKey(search.value)
  return requesters.value.filter(r => (!departmentFilter.value || r.departmentId === departmentFilter.value)
    && (!q || searchKey(r.fullName).includes(q) || String(r.number) === q || r.costCenterCode.includes(q)))
})

async function load() {
  const [r, d, c] = await Promise.all([
    api.get<Requester[]>(`${base}?includeInactive=true`),
    api.get<Department[]>('/api/reference/departments?includeInactive=true'),
    api.get<Detail[]>('/api/reference/ledger/details?costCentersOnly=true&includeInactive=true&take=1000'),
  ])
  if (r.ok) requesters.value = r.value
  else fail(r.code)
  if (d.ok) departments.value = d.value
  if (c.ok) centers.value = c.value
}

function onDepartment(id: string) {
  form.departmentId = id
  const listed = department(id)?.costCenterCodes ?? []
  if (listed.length === 1) form.costCenterCode = listed[0]!
  else if (listed.length && !listed.includes(form.costCenterCode)) form.costCenterCode = ''
}

function startNew() {
  const next = Math.max(0, ...requesters.value.map(r => r.number)) + 1
  Object.assign(form, { number: String(next), fullName: '', departmentId: '', costCenterCode: '' })
  if (departmentFilter.value) onDepartment(departmentFilter.value)
  editing.value = null
  showForm.value = true
  message.value = null
}

function startEdit(r: Requester) {
  Object.assign(form, { number: String(r.number), fullName: r.fullName, departmentId: r.departmentId, costCenterCode: r.costCenterCode })
  editing.value = r
  showForm.value = true
  message.value = null
}

async function save() {
  if (busy.value) return
  const body = { number: Number(searchKey(form.number)) || 0, fullName: form.fullName, departmentId: form.departmentId || '00000000-0000-0000-0000-000000000000', costCenterCode: form.costCenterCode }
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { form: body, version: editing.value.version })
    : await api.post<string>(base, body)
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t(`«${form.fullName.trim()}» ثبت شد.`, `"${form.fullName.trim()}" saved.`) }
  showForm.value = false
  await load()
}

async function change(r: Requester, what: 'activate' | 'deactivate' | 'delete') {
  if (what === 'delete' && !window.confirm(t(`«${r.fullName}» حذف شود؟`, `Delete "${r.fullName}"?`))) return
  const res = await api.post<string>(`${base}/${r.id}/${what}`, { version: r.version })
  if (!res.ok) return fail(res.code)
  message.value = { kind: 'ok', text: what === 'delete' ? t('حذف شد.', 'Deleted.') : what === 'activate' ? t('فعال شد.', 'Switched on.') : t('غیرفعال شد.', 'Switched off.') }
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <h3>{{ t('درخواست کنندگان وجوه', 'Requesters of funds') }}</h3>
    <p v-if="!departments.length" class="muted">{{ t('ابتدا در «بخش‌ها» بخش‌های شرکت را تعریف کنید.', 'Define the departments first.') }}</p>
    <button v-if="!showForm && departments.length" type="button" @click="startNew">{{ t('درخواست کنندهٔ جدید', 'New requester') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t('ویرایش درخواست کننده', 'Edit requester') : t('درخواست کنندهٔ جدید', 'New requester') }}</legend>
        <label class="req"><span>{{ t('شماره درخواست کننده', 'Number') }} <span class="star">*</span></span><input v-model="form.number" dir="ltr" inputmode="numeric" /></label>
        <label class="req"><span>{{ t('نام و نام خانوادگی', 'Full name') }} <span class="star">*</span></span><input v-model="form.fullName" maxlength="100" autocomplete="off" /></label>
        <label class="req"><span>{{ t('بخش (واحد سازمانی)', 'Department') }} <span class="star">*</span></span>
          <SearchSelect :model-value="form.departmentId" :options="departmentChoices" required :aria-label="t('بخش', 'Department')" @update:model-value="onDepartment" />
        </label>
        <label class="req"><span>{{ t('مرکز هزینه', 'Cost centre') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.costCenterCode" :options="centerChoices" required :aria-label="t('مرکز هزینه', 'Cost centre')" :none-text="t('مرکز هزینه‌ای پیدا نشد', 'No cost centre found')" />
        </label>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <template v-if="requesters.length">
      <div class="tr-search">
        <label>{{ t('بخش', 'Department') }}<SearchSelect v-model="departmentFilter" :options="filterChoices" :empty-label="t('همه', 'All')" :aria-label="t('بخش', 'Department')" /></label>
        <label>{{ t('جستجو', 'Search') }}<input v-model="search" type="search" autocomplete="off" /></label>
        <button type="button" class="tr-secondary" :disabled="!search && !departmentFilter" @click="search = ''; departmentFilter = ''">{{ t('نمایش همه', 'Show all') }}</button>
      </div>
      <table class="tr-table">
        <thead><tr><th>{{ t('شماره', 'No.') }}</th><th>{{ t('نام و نام خانوادگی', 'Name') }}</th><th>{{ t('بخش', 'Department') }}</th><th>{{ t('مرکز هزینه', 'Cost centre') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th /></tr></thead>
        <tbody>
          <tr v-for="r in shown" :key="r.id" :class="{ inactive: !r.isActive }">
            <td>{{ num(r.number) }}</td>
            <td>{{ r.fullName }}</td>
            <td>{{ department(r.departmentId)?.name }}</td>
            <td><span dir="ltr">{{ r.costCenterCode }}</span> <small class="muted">{{ centerName(r.costCenterCode) }}</small></td>
            <td>{{ r.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }} <span v-if="r.isInUse" class="tr-tag">{{ t('در حال استفاده', 'in use') }}</span></td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="startEdit(r)">{{ t('ویرایش', 'Edit') }}</button>
              <button type="button" class="tr-link" @click="change(r, r.isActive ? 'deactivate' : 'activate')">{{ r.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
              <button v-if="!r.isInUse" type="button" class="tr-link danger" @click="change(r, 'delete')">{{ t('حذف', 'Delete') }}</button>
            </td>
          </tr>
          <tr v-if="!shown.length"><td colspan="6" class="muted">{{ t('موردی پیدا نشد.', 'Nothing found.') }}</td></tr>
        </tbody>
      </table>
    </template>
  </div>
</template>

<style scoped src="./organization.css"></style>
