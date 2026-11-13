<script setup lang="ts">
/**
 * Beneficiaries (ذینفعان), new-version manual item 14: the تفصیل (the beneficiary must exist in the accounting system first), the
 * registered legal title printed on cheques (filled from the تفصیل title, can be corrected), bank and branch, account number, Sheba
 * (شبا, needed when the paying bank differs) and a phone number for emergencies.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '../components/SearchSelect.vue'
import { searchKey } from '../text'

interface Beneficiary {
  id: string; detailCode: string; detailName: string | null; legalName: string; bankId: string | null; branchName: string | null
  accountNumber: string | null; sheba: string | null; phone: string | null; isActive: boolean; isInUse: boolean; version: number
  kind: 'Natural' | 'Legal' | null; nationalId: string | null; postalAddress: string | null; postalCode: string | null
}
interface Detail { code: string; name: string; type: string | null; isActive: boolean }
interface Entry { id: string; name: string; isActive: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US')
const base = '/api/reference/beneficiaries'

const beneficiaries = ref<Beneficiary[]>([])
const banks = ref<Entry[]>([])
const details = ref<Detail[]>([])
const search = ref('')
const editing = ref<Beneficiary | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
/** Owner's report no. 2, item 7: real person (کد ملی, 10 digits) or legal person (شناسه ملی, 11 digits), postal address and code. */
const empty = () => ({
  detailCode: '', legalName: '', bankId: '', branchName: '', accountNumber: '', sheba: '', phone: '',
  kind: 'Natural' as 'Natural' | 'Legal', nationalId: '', postalAddress: '', postalCode: '',
})
const form = reactive(empty())

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const bankName = (id: string | null) => banks.value.find(b => b.id === id)?.name ?? ''
const bankChoices = computed<Choice[]>(() => banks.value.filter(b => b.isActive || b.id === form.bankId).map(b => ({ value: b.id, label: b.name })))
const detailChoices = computed<Choice[]>(() => details.value.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive })))
const shown = computed(() => {
  const q = searchKey(search.value)
  return q ? beneficiaries.value.filter(b => [b.legalName, b.detailName ?? '', b.detailCode, b.accountNumber ?? '', b.sheba ?? '', bankName(b.bankId)].some(f => searchKey(f).includes(q))) : beneficiaries.value
})
/** IR + 24 digits written in groups of four, as on bank papers. */
const shebaText = (s: string | null) => (s ? s.replace(/(.{4})/g, '$1 ').trim() : '')

async function load() {
  const [b, k] = await Promise.all([api.get<Beneficiary[]>(`${base}?includeInactive=true`), api.get<Entry[]>('/api/reference/banks?includeInactive=true')])
  if (b.ok) beneficiaries.value = b.value
  else fail(b.code)
  if (k.ok) banks.value = k.value
}

let timer: ReturnType<typeof setTimeout> | undefined
async function searchDetails(text: string) {
  const q = new URLSearchParams({ take: '50' })
  if (text.trim()) q.set('search', text.trim())
  const r = await api.get<Detail[]>(`/api/reference/ledger/details?${q}`)
  const found = r.ok ? r.value : []
  if (form.detailCode && !found.some(d => d.code === form.detailCode)) {
    const one = await api.get<Detail>(`/api/reference/ledger/details/${encodeURIComponent(form.detailCode)}`)
    if (one.ok) found.unshift(one.value)
  }
  details.value = found
}
function onDetailSearch(text: string) {
  clearTimeout(timer)
  timer = setTimeout(() => searchDetails(text), 250)
}

/** Manual: the legal title is taken from the تفصیل title by default and can be corrected. */
function onDetail(code: string) {
  const before = details.value.find(d => d.code === form.detailCode)?.name
  form.detailCode = code
  const name = details.value.find(d => d.code === code)?.name ?? ''
  if (!form.legalName || form.legalName === before) form.legalName = name
}

function startNew() {
  Object.assign(form, empty())
  editing.value = null
  showForm.value = true
  message.value = null
  void searchDetails('')
}

function startEdit(b: Beneficiary) {
  Object.assign(form, {
    detailCode: b.detailCode, legalName: b.legalName, bankId: b.bankId ?? '', branchName: b.branchName ?? '', accountNumber: b.accountNumber ?? '',
    sheba: b.sheba ?? '', phone: b.phone ?? '',
    kind: b.kind ?? 'Natural', nationalId: b.nationalId ?? '', postalAddress: b.postalAddress ?? '', postalCode: b.postalCode ?? '',
  })
  editing.value = b
  showForm.value = true
  message.value = null
  void searchDetails('')
}

async function save() {
  if (busy.value) return
  const body = {
    detailCode: form.detailCode, legalName: form.legalName || null, bankId: form.bankId || null, branchName: form.branchName || null,
    accountNumber: form.accountNumber || null, sheba: form.sheba || null, phone: form.phone || null,
    kind: form.kind, nationalId: form.nationalId || null, postalAddress: form.postalAddress || null, postalCode: form.postalCode || null,
  }
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { form: body, version: editing.value.version })
    : await api.post<string>(base, body)
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t('ذینفع ثبت شد.', 'Beneficiary saved.') }
  showForm.value = false
  await load()
}

async function change(b: Beneficiary, what: 'activate' | 'deactivate' | 'delete') {
  if (what === 'delete' && !window.confirm(t(`«${b.legalName}» حذف شود؟`, `Delete "${b.legalName}"?`))) return
  const r = await api.post<string>(`${base}/${b.id}/${what}`, { version: b.version })
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: what === 'delete' ? t('حذف شد.', 'Deleted.') : what === 'activate' ? t('فعال شد.', 'Switched on.') : t('غیرفعال شد.', 'Switched off.') }
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <h3>{{ t('مشخصات ذینفعان', 'Beneficiaries') }}</h3>
    <button type="button" class="tr-new" @click="startNew">{{ t('ذینفع جدید', 'New beneficiary') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t('ویرایش ذینفع', 'Edit beneficiary') : t('ذینفع جدید', 'New beneficiary') }}</legend>
        <label class="req wide"><span>{{ t('حساب تفصیل ذینفع', 'Detail account') }} <span class="star">*</span></span>
          <SearchSelect :model-value="form.detailCode" :options="detailChoices" remote required :aria-label="t('حساب تفصیل', 'Detail account')"
                        :none-text="t('تفصیلی پیدا نشد؛ ذینفع جدید ابتدا در سیستم مالی تعریف شود', 'Not found; define the beneficiary in the accounting system first')"
                        @search="onDetailSearch" @update:model-value="onDetail" />
        </label>
        <label class="req wide"><span>{{ t('عنوان حقوقی ثبت‌شده (روی چک)', 'Registered legal title (printed on cheques)') }} <span class="star">*</span></span>
          <input v-model="form.legalName" maxlength="150" autocomplete="off" />
        </label>
        <div class="kind">
          <label class="tr-check"><input v-model="form.kind" type="radio" value="Natural" /> {{ t('شخص حقیقی', 'Person') }}</label>
          <label class="tr-check"><input v-model="form.kind" type="radio" value="Legal" /> {{ t('شخص حقوقی', 'Company / organisation') }}</label>
        </div>
        <label class="req"><span>{{ form.kind === 'Natural' ? t('کد ملی (۱۰ رقم)', 'National code (10 digits)') : t('شناسه ملی (۱۱ رقم)', 'National ID (11 digits)') }} <span class="star">*</span></span>
          <input v-model="form.nationalId" dir="ltr" inputmode="numeric" :maxlength="form.kind === 'Natural' ? 10 : 11" autocomplete="off" />
        </label>
        <label class="wide">{{ t('نشانی پستی', 'Postal address') }}<input v-model="form.postalAddress" maxlength="300" /></label>
        <label>{{ t('کد پستی (۱۰ رقم)', 'Postal code (10 digits)') }}<input v-model="form.postalCode" dir="ltr" inputmode="numeric" maxlength="11" /></label>
        <label>{{ t('نام بانک', 'Bank') }}<SearchSelect v-model="form.bankId" :options="bankChoices" :empty-label="t('ندارد', 'None')" :aria-label="t('نام بانک', 'Bank')" /></label>
        <label>{{ t('نام شعبه', 'Branch') }}<input v-model="form.branchName" maxlength="100" /></label>
        <label>{{ t('شماره حساب', 'Account number') }}<input v-model="form.accountNumber" dir="ltr" maxlength="40" /></label>
        <label class="wide">{{ t('شماره شبا', 'Sheba (IBAN)') }}<input v-model="form.sheba" dir="ltr" placeholder="IR00 0000 0000 0000 0000 0000 00" maxlength="40" />
          <small class="muted">{{ t('وقتی بانک صادرکنندهٔ چک با بانک ذینفع متفاوت است، شبا روی حواله‌کرد چک می‌آید.', 'Used on the cheque when the paying bank differs from the beneficiary\'s bank.') }}</small>
        </label>
        <label>{{ t('شماره تلفن تماس', 'Phone') }}<input v-model="form.phone" dir="ltr" maxlength="20" /></label>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <template v-if="beneficiaries.length">
      <div class="tr-search">
        <label>{{ t('جستجو (نام، تفصیل، حساب، شبا، بانک)', 'Search (name, detail, account, Sheba, bank)') }}<input v-model="search" type="search" autocomplete="off" /></label>
        <button type="button" class="tr-secondary" :disabled="!search" @click="search = ''">{{ t('نمایش همه', 'Show all') }}</button>
        <span class="muted small">{{ t(`${num(shown.length)} از ${num(beneficiaries.length)} ذینفع`, `${shown.length} of ${beneficiaries.length}`) }}</span>
      </div>
      <div style="overflow-x:auto">
        <table class="tr-table">
          <thead><tr><th>{{ t('تفصیل', 'Detail') }}</th><th>{{ t('عنوان حقوقی', 'Legal title') }}</th><th>{{ t('کد / شناسه ملی', 'National code / ID') }}</th><th>{{ t('بانک / شعبه', 'Bank / branch') }}</th><th>{{ t('شماره حساب', 'Account') }}</th><th>{{ t('شبا', 'Sheba') }}</th><th>{{ t('تلفن', 'Phone') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th /></tr></thead>
          <tbody>
            <tr v-for="b in shown" :key="b.id" :class="{ inactive: !b.isActive }">
              <td><span dir="ltr">{{ b.detailCode }}</span> <small class="muted">{{ b.detailName }}</small></td>
              <td>{{ b.legalName }}</td>
              <td dir="ltr">{{ b.nationalId }}</td>
              <td>{{ bankName(b.bankId) }} <small v-if="b.branchName" class="muted">{{ b.branchName }}</small></td>
              <td dir="ltr">{{ b.accountNumber }}</td>
              <td dir="ltr" class="small">{{ shebaText(b.sheba) }}</td>
              <td dir="ltr">{{ b.phone }}</td>
              <td>{{ b.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }} <span v-if="b.isInUse" class="tr-tag">{{ t('در حال استفاده', 'in use') }}</span></td>
              <td class="tr-actions">
                <button type="button" class="tr-link" @click="startEdit(b)">{{ t('ویرایش', 'Edit') }}</button>
                <button type="button" class="tr-link" @click="change(b, b.isActive ? 'deactivate' : 'activate')">{{ b.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
                <button v-if="!b.isInUse" type="button" class="tr-link danger" @click="change(b, 'delete')">{{ t('حذف', 'Delete') }}</button>
              </td>
            </tr>
            <tr v-if="!shown.length"><td colspan="9" class="muted">{{ t('موردی پیدا نشد.', 'Nothing found.') }}</td></tr>
          </tbody>
        </table>
      </div>
    </template>
    <p v-else-if="!showForm" class="muted">{{ t('هنوز ذینفعی تعریف نشده است.', 'No beneficiary yet.') }}</p>
  </div>
</template>

<style scoped src="./organization.css"></style>
<style scoped>
.kind { display: flex; gap: 1.25rem; align-items: center; align-self: end; }
</style>
