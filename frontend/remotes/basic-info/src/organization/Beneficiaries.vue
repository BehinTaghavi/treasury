<script setup lang="ts">
/**
 * «ذینفعان / متعهدین / ضامنین» (new-version manual item 14 «مشخصات ذینفعان»; owner, 19 Nov 2026): one form for the three groups, told
 * apart by the ticks at the top (one person may be in more than one group). Beneficiaries are offered on payment requests, obligors
 * and guarantors on the receipt of promissory notes. Each entry: the تفصیل (the person must exist in the accounting system first), the
 * registered legal title printed on cheques (filled from the تفصیل title, can be corrected), national code or ID, postal address, bank
 * and branch, account number, Sheba, phone with its area code in a box of its own, and mobile number.
 * Owner, 19 Nov 2026:
 * - the تفصیل is a person (staff, customers, persons and companies); the list box no longer opens on the bank accounts — the type of
 *   تفصیل is chosen first, as on the receipt forms;
 * - «IR» is fixed in front of the Sheba number and the 24 digits are typed after it;
 * - the list of the entries is always on the screen, with a filter for each group;
 * - a refused save says why beside the save button and frames the field in red (a made-up Sheba of the right length used to be
 *   refused with a text asking for "IR and 24 digits").
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, toLatinDigits, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { searchKey } from '../text'

interface Beneficiary {
  id: string; detailCode: string; detailName: string | null; legalName: string; bankId: string | null; branchName: string | null
  accountNumber: string | null; sheba: string | null; phone: string | null; isActive: boolean; isInUse: boolean; version: number
  kind: 'Natural' | 'Legal' | null; nationalId: string | null; postalAddress: string | null; postalCode: string | null
  isBeneficiary: boolean; isObligor: boolean; isGuarantor: boolean; mobile: string | null; phoneAreaCode: string | null
}
interface Detail { code: string; name: string; type: string | null; isActive: boolean }
interface DetailType { type: string; activeCount: number; role: string | null }
interface Entry { id: string; name: string; isActive: boolean }
type Group = 'isBeneficiary' | 'isObligor' | 'isGuarantor'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US')
const base = '/api/reference/beneficiaries'
/** Owner, 19 Nov 2026: the تفصیل of these persons is of a person type — staff, customers, persons and companies. */
const PersonRoles = 'Staff,Customer,PersonOrCompany'
const groups: { key: Group; one: [string, string]; many: [string, string] }[] = [
  { key: 'isBeneficiary', one: ['ذینفع', 'Beneficiary'], many: ['ذینفعان', 'Beneficiaries'] },
  { key: 'isObligor', one: ['متعهد', 'Obligor'], many: ['متعهدین', 'Obligors'] },
  { key: 'isGuarantor', one: ['ضامن', 'Guarantor'], many: ['ضامنین', 'Guarantors'] },
]

const beneficiaries = ref<Beneficiary[]>([])
const banks = ref<Entry[]>([])
const details = ref<Detail[]>([])
const detailTypes = ref<DetailType[]>([])
const detailType = ref('')
const search = ref('')
const groupFilter = ref<'' | Group>('')
const editing = ref<Beneficiary | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
/** The field a refused save is about; framed in red. */
const wrong = ref('')
/** Owner's report no. 2, item 7: real person (کد ملی, 10 digits) or legal person (شناسه ملی, 11 digits), postal address and code. */
const empty = () => ({
  isBeneficiary: true, isObligor: false, isGuarantor: false,
  detailCode: '', legalName: '', bankId: '', branchName: '', accountNumber: '', sheba: '', areaCode: '', phone: '', mobile: '',
  kind: 'Natural' as 'Natural' | 'Legal', nationalId: '', postalAddress: '', postalCode: '',
})
const form = reactive(empty())

/** Which field each refusal is about. */
const fieldOf: Record<string, string> = {
  'Beneficiary.RoleRequired': 'groups', 'Beneficiary.RoleInUse': 'groups',
  'Beneficiary.DetailRequired': 'detail', 'Beneficiary.DetailNotFound': 'detail', 'Beneficiary.DetailInactive': 'detail', 'Beneficiary.DetailTaken': 'detail',
  'Beneficiary.DetailNotPerson': 'detail', 'Beneficiary.NameRequired': 'name', 'Beneficiary.NameTooLong': 'name',
  'Beneficiary.NationalCodeRequired': 'national', 'Beneficiary.NationalCodeInvalid': 'national', 'Beneficiary.LegalIdRequired': 'national', 'Beneficiary.LegalIdInvalid': 'national',
  'Beneficiary.AddressTooLong': 'address', 'Beneficiary.PostalCodeInvalid': 'postal',
  'Beneficiary.BankRequired': 'bank', 'Beneficiary.BankNotFound': 'bank', 'Beneficiary.BankInactive': 'bank', 'Beneficiary.AccountNumberInvalid': 'account',
  'Beneficiary.ShebaInvalid': 'sheba', 'Beneficiary.ShebaCheckDigits': 'sheba',
  'Beneficiary.AreaCodeInvalid': 'area', 'Beneficiary.PhoneInvalid': 'phone', 'Beneficiary.MobileInvalid': 'mobile',
}
function fail(code: string) {
  message.value = { kind: 'error', text: translate(code, props.locale) }
  wrong.value = fieldOf[code] ?? ''
}
const bankName = (id: string | null) => banks.value.find(b => b.id === id)?.name ?? ''
const bankChoices = computed<Choice[]>(() => banks.value.filter(b => b.isActive || b.id === form.bankId).map(b => ({ value: b.id, label: b.name })))
const detailChoices = computed<Choice[]>(() => details.value.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive })))
/** The types of تفصیل that are persons, with how many active تفصیل each has. */
const typeChoices = computed<Choice[]>(() => detailTypes.value
  .filter(x => x.role !== null && PersonRoles.split(',').includes(x.role))
  .map(x => ({ value: x.type, label: x.type, hint: num(x.activeCount) })))
const groupNames = (b: Beneficiary) => groups.filter(g => b[g.key]).map(g => (fa.value ? g.one[0] : g.one[1]))
const countOf = (g: Group) => beneficiaries.value.filter(b => b[g]).length
const shown = computed(() => {
  const q = searchKey(search.value)
  const inGroup = groupFilter.value ? beneficiaries.value.filter(b => b[groupFilter.value as Group]) : beneficiaries.value
  return q
    ? inGroup.filter(b => [b.legalName, b.detailName ?? '', b.detailCode, b.accountNumber ?? '', b.sheba ?? '', bankName(b.bankId), b.nationalId ?? '', b.mobile ?? ''].some(f => searchKey(f).includes(q)))
    : inGroup
})
/** IR + 24 digits written in groups of four, as on bank papers. */
const shebaText = (s: string | null) => (s ? s.replace(/(.{4})/g, '$1 ').trim() : '')
const phoneText = (b: Beneficiary) => [b.phoneAreaCode, b.phone].filter(Boolean).join('-')
/** The 24 digits after the fixed «IR»: a pasted "IR…", spaces and dashes are taken off. */
const shebaDigits = computed(() => toLatinDigits(form.sheba).replace(/^\s*ir/i, '').replace(/[\s-]/g, ''))

async function load() {
  const [b, k, y] = await Promise.all([
    api.get<Beneficiary[]>(`${base}?includeInactive=true`), api.get<Entry[]>('/api/reference/banks?includeInactive=true'),
    api.get<DetailType[]>('/api/reference/ledger/detail-types'),
  ])
  if (b.ok) beneficiaries.value = b.value
  else fail(b.code)
  if (k.ok) banks.value = k.value
  if (y.ok) detailTypes.value = y.value
}

let timer: ReturnType<typeof setTimeout> | undefined
async function searchDetails(text: string) {
  const q = new URLSearchParams({ take: '50', roles: PersonRoles })
  if (text.trim()) q.set('search', text.trim())
  if (detailType.value) q.set('type', detailType.value)
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
function onType(type: string) {
  detailType.value = type
  void searchDetails('')
}

/** Manual: the legal title is taken from the تفصیل title by default and can be corrected. */
function onDetail(code: string) {
  const before = details.value.find(d => d.code === form.detailCode)?.name
  form.detailCode = code
  const name = details.value.find(d => d.code === code)?.name ?? ''
  if (!form.legalName || form.legalName === before) form.legalName = name
}

function open(b: Beneficiary | null) {
  Object.assign(form, empty())
  if (b) {
    Object.assign(form, {
      isBeneficiary: b.isBeneficiary, isObligor: b.isObligor, isGuarantor: b.isGuarantor,
      detailCode: b.detailCode, legalName: b.legalName, bankId: b.bankId ?? '', branchName: b.branchName ?? '', accountNumber: b.accountNumber ?? '',
      sheba: (b.sheba ?? '').replace(/^IR/, ''), areaCode: b.phoneAreaCode ?? '', phone: b.phone ?? '', mobile: b.mobile ?? '',
      kind: b.kind ?? 'Natural', nationalId: b.nationalId ?? '', postalAddress: b.postalAddress ?? '', postalCode: b.postalCode ?? '',
    })
  } else if (groupFilter.value) {
    // A new entry opened from one group's list starts in that group.
    Object.assign(form, { isBeneficiary: false, [groupFilter.value]: true })
  }
  editing.value = b
  showForm.value = true
  message.value = null
  wrong.value = ''
  detailType.value = ''
  void searchDetails('')
}

async function save() {
  if (busy.value) return
  const body = {
    isBeneficiary: form.isBeneficiary, isObligor: form.isObligor, isGuarantor: form.isGuarantor,
    detailCode: form.detailCode, legalName: form.legalName || null, bankId: form.bankId || null, branchName: form.branchName || null,
    accountNumber: form.accountNumber || null, sheba: shebaDigits.value ? `IR${shebaDigits.value}` : null,
    phoneAreaCode: form.areaCode || null, phone: form.phone || null, mobile: form.mobile || null,
    kind: form.kind, nationalId: form.nationalId || null, postalAddress: form.postalAddress || null, postalCode: form.postalCode || null,
  }
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { form: body, version: editing.value.version })
    : await api.post<string>(base, body)
  busy.value = false
  if (!r.ok) return fail(r.code)
  wrong.value = ''
  message.value = { kind: 'ok', text: t(`«${form.legalName}» ثبت شد و در فهرست پایین و در فهرست‌های انتخاب گروه خود دیده می‌شود.`, `"${form.legalName}" saved; it is in the list below and in its group's list boxes.`) }
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
    <h3>{{ t('ذینفعان / متعهدین / ضامنین', 'Beneficiaries / obligors / guarantors') }}</h3>
    <p class="muted small">{{ t('مشخصات هر سه گروه در همین فرم ثبت می‌شود و با تیک‌های بالای فرم از هم جدا می‌شوند: «ذینفعان» در درخواست وجه و «متعهدین» و «ضامنین» در دریافت سفته انتخاب می‌شوند.',
      'The three groups are entered on this form and told apart by the ticks at its top: beneficiaries are chosen on payment requests, obligors and guarantors on the receipt of promissory notes.') }}</p>
    <button type="button" class="tr-new" @click="open(null)">{{ t('جدید', 'New') }}</button>
    <p v-if="message && !showForm" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" novalidate @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t(`ویرایش «${editing.legalName}»`, `Edit "${editing.legalName}"`) : t('ثبت مشخصات جدید', 'New entry') }}</legend>
        <div :class="['groups', 'full', { bad: wrong === 'groups' }]" role="group" :aria-label="t('نوع', 'Group')">
          <strong>{{ t('نوع', 'Group') }} <span class="star">*</span></strong>
          <label v-for="g in groups" :key="g.key" class="tr-check"><input v-model="form[g.key]" type="checkbox" /> {{ fa ? g.one[0] : g.one[1] }}</label>
          <span class="muted small">{{ t('یک شخص می‌تواند در بیش از یک گروه باشد.', 'One person may be in more than one group.') }}</span>
        </div>
        <label v-if="typeChoices.length">{{ t('نوع حساب تفصیل', 'Type of detail account') }}
          <SearchSelect :model-value="detailType" :options="typeChoices" :empty-label="t('— همهٔ اشخاص —', '— all persons —')" :aria-label="t('نوع حساب تفصیل', 'Type of detail account')"
                        @update:model-value="onType" />
        </label>
        <label :class="['req', 'wide', { bad: wrong === 'detail' }]"><span>{{ t('حساب تفصیل (کارکنان، مشتریان، اشخاص و شرکت‌ها)', 'Detail account (staff, customers, persons and companies)') }} <span class="star">*</span></span>
          <SearchSelect :model-value="form.detailCode" :options="detailChoices" remote required :aria-label="t('حساب تفصیل', 'Detail account')"
                        :none-text="t('تفصیلی از نوع اشخاص پیدا نشد؛ شخص جدید ابتدا در سیستم مالی تعریف شود', 'No person found; define the person in the accounting system first')"
                        @search="onDetailSearch" @update:model-value="onDetail" />
        </label>
        <label :class="['req', 'wide', { bad: wrong === 'name' }]"><span>{{ t('عنوان حقوقی ثبت‌شده (روی چک)', 'Registered legal title (printed on cheques)') }} <span class="star">*</span></span>
          <input v-model="form.legalName" maxlength="150" autocomplete="off" />
        </label>
        <div class="kind">
          <label class="tr-check"><input v-model="form.kind" type="radio" value="Natural" /> {{ t('شخص حقیقی', 'Person') }}</label>
          <label class="tr-check"><input v-model="form.kind" type="radio" value="Legal" /> {{ t('شخص حقوقی', 'Company / organisation') }}</label>
        </div>
        <label :class="['req', { bad: wrong === 'national' }]"><span>{{ form.kind === 'Natural' ? t('کد ملی (۱۰ رقم)', 'National code (10 digits)') : t('شناسه ملی (۱۱ رقم)', 'National ID (11 digits)') }} <span class="star">*</span></span>
          <input v-model="form.nationalId" dir="ltr" inputmode="numeric" :maxlength="form.kind === 'Natural' ? 10 : 11" autocomplete="off" />
        </label>
        <label :class="['wide', { bad: wrong === 'address' }]">{{ t('نشانی پستی', 'Postal address') }}<input v-model="form.postalAddress" maxlength="300" /></label>
        <label :class="{ bad: wrong === 'postal' }">{{ t('کد پستی (۱۰ رقم)', 'Postal code (10 digits)') }}<input v-model="form.postalCode" dir="ltr" inputmode="numeric" maxlength="11" /></label>
        <label :class="{ bad: wrong === 'bank' }">{{ t('نام بانک', 'Bank') }}<SearchSelect v-model="form.bankId" :options="bankChoices" :empty-label="t('ندارد', 'None')" :aria-label="t('نام بانک', 'Bank')" /></label>
        <label>{{ t('نام شعبه', 'Branch') }}<input v-model="form.branchName" maxlength="100" /></label>
        <label :class="{ bad: wrong === 'account' }">{{ t('شماره حساب', 'Account number') }}<input v-model="form.accountNumber" dir="ltr" maxlength="40" /></label>
        <label :class="['wide', { bad: wrong === 'sheba' }]">{{ t('شماره شبا (۲۴ رقم پس از IR)', 'Sheba (24 digits after IR)') }}
          <span class="joined" dir="ltr">
            <span class="fixed" aria-hidden="true">IR</span>
            <input v-model="form.sheba" dir="ltr" inputmode="numeric" placeholder="00 0000 0000 0000 0000 0000 00" maxlength="34" :aria-label="t('شماره شبا', 'Sheba')" />
          </span>
          <small :class="shebaDigits.length && shebaDigits.length !== 24 ? 'tr-warn' : 'muted'">{{ shebaDigits.length
            ? t(`${num(shebaDigits.length)} رقم از ۲۴ رقم`, `${shebaDigits.length} of 24 digits`)
            : t('وقتی بانک صادرکنندهٔ چک با بانک ذینفع متفاوت است، شبا روی حواله‌کرد چک می‌آید.', "Used on the cheque when the paying bank differs from the beneficiary's bank.") }}</small>
        </label>
        <label :class="{ bad: wrong === 'area' || wrong === 'phone' }">{{ t('شماره تلفن و پیش‌شماره', 'Phone and area code') }}
          <span class="joined" dir="ltr">
            <input v-model="form.areaCode" class="area" dir="ltr" inputmode="numeric" maxlength="4" placeholder="021" :aria-label="t('پیش‌شماره', 'Area code')" />
            <input v-model="form.phone" dir="ltr" inputmode="numeric" maxlength="20" :aria-label="t('شماره تلفن', 'Phone number')" />
          </span>
        </label>
        <label :class="{ bad: wrong === 'mobile' }">{{ t('شماره موبایل', 'Mobile number') }}<input v-model="form.mobile" dir="ltr" inputmode="numeric" maxlength="15" placeholder="09121234567" /></label>
      </fieldset>
      <p v-if="message" :class="['tr-msg', message.kind]" role="alert">{{ message.text }}</p>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false; message = null">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <h4>{{ t('فهرست ذینفعان، متعهدین و ضامنین تعریف‌شده', 'Defined beneficiaries, obligors and guarantors') }}</h4>
    <div class="tr-search">
      <div class="filter" role="group" :aria-label="t('گروه', 'Group')">
        <button type="button" :class="{ active: groupFilter === '' }" @click="groupFilter = ''">{{ t('همه', 'All') }} ({{ num(beneficiaries.length) }})</button>
        <button v-for="g in groups" :key="g.key" type="button" :class="{ active: groupFilter === g.key }" @click="groupFilter = g.key">
          {{ fa ? g.many[0] : g.many[1] }} ({{ num(countOf(g.key)) }})</button>
      </div>
      <label>{{ t('جستجو (نام، تفصیل، کد ملی، حساب، شبا، بانک، موبایل)', 'Search (name, detail, national code, account, Sheba, bank, mobile)') }}<input v-model="search" type="search" autocomplete="off" /></label>
      <button type="button" class="tr-secondary" :disabled="!search" @click="search = ''">{{ t('نمایش همه', 'Show all') }}</button>
      <span class="muted small">{{ t(`${num(shown.length)} از ${num(beneficiaries.length)}`, `${shown.length} of ${beneficiaries.length}`) }}</span>
    </div>
    <div style="overflow-x:auto">
      <table class="tr-table">
        <thead><tr><th>{{ t('نوع', 'Group') }}</th><th>{{ t('تفصیل', 'Detail') }}</th><th>{{ t('عنوان حقوقی', 'Legal title') }}</th><th>{{ t('کد / شناسه ملی', 'National code / ID') }}</th><th>{{ t('بانک / شعبه', 'Bank / branch') }}</th><th>{{ t('شماره حساب', 'Account') }}</th><th>{{ t('شبا', 'Sheba') }}</th><th>{{ t('تلفن / موبایل', 'Phone / mobile') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th /></tr></thead>
        <tbody>
          <tr v-for="b in shown" :key="b.id" :class="{ inactive: !b.isActive }">
            <td><span v-for="n in groupNames(b)" :key="n" class="tr-tag">{{ n }}</span></td>
            <td><span dir="ltr">{{ b.detailCode }}</span> <small class="muted">{{ b.detailName }}</small></td>
            <td>{{ b.legalName }}</td>
            <td dir="ltr">{{ b.nationalId }}</td>
            <td>{{ bankName(b.bankId) }} <small v-if="b.branchName" class="muted">{{ b.branchName }}</small></td>
            <td dir="ltr">{{ b.accountNumber }}</td>
            <td dir="ltr" class="small nowrap">{{ shebaText(b.sheba) }}</td>
            <td dir="ltr" class="nowrap">{{ phoneText(b) }}<br v-if="phoneText(b) && b.mobile" />{{ b.mobile }}</td>
            <td>{{ b.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }} <span v-if="b.isInUse" class="tr-tag">{{ t('در حال استفاده', 'in use') }}</span></td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="open(b)">{{ t('ویرایش', 'Edit') }}</button>
              <button type="button" class="tr-link" @click="change(b, b.isActive ? 'deactivate' : 'activate')">{{ b.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
              <button v-if="!b.isInUse" type="button" class="tr-link danger" @click="change(b, 'delete')">{{ t('حذف', 'Delete') }}</button>
            </td>
          </tr>
          <tr v-if="!shown.length"><td colspan="10" class="muted">{{ beneficiaries.length ? t('موردی پیدا نشد.', 'Nothing found.') : t('هنوز کسی تعریف نشده است؛ با دکمهٔ «جدید» شروع کنید.', 'Nobody yet; start with «New».') }}</td></tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped src="./organization.css"></style>
<style scoped>
.kind { display: flex; gap: 1.25rem; align-items: center; align-self: end; }
.nowrap { white-space: nowrap; }
.full { grid-column: 1 / -1; }
h4 { margin: 1.25rem 0 0; }
.groups { display: flex; flex-wrap: wrap; gap: .5rem 1.5rem; align-items: center; padding: .5rem .75rem; border-radius: 6px; background: #f5f8ff; border: 1px solid #d9e2ec; }
.groups.bad { border-color: #d64545; background: #fdecea; }
/* The field a refused save is about. */
label.bad > input, label.bad .joined input, label.bad :deep(input) { border-color: #b42318; box-shadow: 0 0 0 2px #f5c2c2; }
label.bad { color: #8a1c1c; }
/* Two boxes that read as one number: «IR» + 24 digits, area code + phone number (always left to right). */
.joined { display: flex; gap: .25rem; align-items: stretch; }
.joined input { flex: 1; min-width: 0; }
.joined .area { flex: 0 0 4.2rem; text-align: center; }
.joined .fixed { display: flex; align-items: center; padding: 0 .6rem; border: 1px solid #cbd2d9; border-radius: 6px; background: #eef1f5; color: #52606d; font-weight: 600; letter-spacing: .05em; }
.filter { display: flex; flex-wrap: wrap; gap: .3rem; }
.filter button { background: #fff; color: #1f2933; border: 1px solid #cbd2d9; border-radius: 999px; padding: .3rem .8rem; }
.filter button.active { background: #e8effc; color: #1d4ed8; border-color: #93b4f5; font-weight: 600; }
</style>
