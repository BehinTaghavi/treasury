<script setup lang="ts">
/**
 * «دارندگان امضای مجاز (اسناد بهادار)» (owner, 16 Nov 2026, answers to Q41 and Q45; redesigned by his report no. 8 of 21 Nov 2026).
 * A signatory has a name, the staff تفصیل when there is one (its name is then taken), a position chosen from the owner's list – with
 * «سایر» a box opens in which it is typed –, national code, mobile and landline numbers, the extension inside the organisation, an
 * e-mail address, the scan of the specimen signature, and three ticks in any combination (owner's answer to Q88, 22 Nov 2026):
 * «چک», «سفته», «قراردادها و سایر اسناد تعهدی».
 * The signature rule per kind of document of 16 Nov 2026 is gone: who signs the cheques of a bank account is set on the bank account
 * (Basic Information → حساب‌های بانکی), and who confirms or approves forms on the page «دارندگان مجوزها (فرم‌ها)». A signatory named by
 * a bank account is switched off instead of deleted.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, toLatinDigits, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { searchKey } from '../text'
import SignatureBox from './SignatureBox.vue'
import { POSITIONS, positionTitle } from './persons'
import { uploadSignature } from './signatureScan'

interface Signatory {
  id: string; fullName: string; positionKind: string; position: string; detailCode: string | null; nationalCode: string | null; mobile: string | null
  phone: string | null; extension: string | null; email: string | null; signsCheques: boolean; signsNotes: boolean; signsContracts: boolean; hasSignature: boolean
  isActive: boolean; isInUse: boolean; version: number
}
interface Detail { code: string; name: string; isActive: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const access = { apiBase: props.apiBase, token: props.token }
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const base = '/api/reference/signatories'

const signatories = ref<Signatory[]>([])
const search = ref('')
const editing = ref<Signatory | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const empty = () => ({
  fullName: '', position: '', otherPosition: '', detailCode: '', nationalCode: '', mobile: '', phone: '', extension: '', email: '', signsCheques: true, signsNotes: false, signsContracts: false,
})
const form = reactive(empty())
/** A scan chosen on the form, sent after the form is saved. */
const chosenScan = ref<File | null>(null)
const details = ref<Detail[]>([])
const detailChoices = computed<Choice[]>(() => details.value.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive })))
const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const storedScan = computed(() => (editing.value?.hasSignature ? `${base}/${editing.value.id}/signature?v=${editing.value.version}` : ''))

/** تفصیل found on the server by name or code; the chosen one stays in the list. */
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
let timer: ReturnType<typeof setTimeout> | undefined
function onDetailSearch(text: string) {
  clearTimeout(timer)
  timer = setTimeout(() => searchDetails(text), 250)
}
/** Choosing a تفصیل fills the name when none is typed yet. */
function onDetail(code: string) {
  form.detailCode = code
  const d = details.value.find(x => x.code === code)
  if (d && !form.fullName.trim()) form.fullName = d.name
}

const signs = (s: Signatory) => [s.signsCheques ? t('چک', 'Cheques') : '', s.signsNotes ? t('سفته', 'Notes') : '', s.signsContracts ? t('قراردادها', 'Contracts') : ''].filter(Boolean)
const shown = computed(() => {
  const q = searchKey(search.value)
  return signatories.value.filter(s => !q || searchKey(`${s.fullName} ${s.position} ${s.nationalCode ?? ''} ${s.mobile ?? ''} ${s.detailCode ?? ''} ${s.email ?? ''}`).includes(q))
})

async function load() {
  const s = await api.get<Signatory[]>(`${base}?includeInactive=true`)
  if (s.ok) signatories.value = s.value
  else fail(s.code)
}

function open(s: Signatory | null) {
  Object.assign(form, s
    ? {
        fullName: s.fullName, position: s.positionKind, otherPosition: s.positionKind === 'Other' ? s.position : '', detailCode: s.detailCode ?? '', nationalCode: s.nationalCode ?? '',
        mobile: s.mobile ?? '', phone: s.phone ?? '', extension: s.extension ?? '', email: s.email ?? '', signsCheques: s.signsCheques, signsNotes: s.signsNotes, signsContracts: s.signsContracts,
      }
    : empty())
  editing.value = s
  chosenScan.value = null
  showForm.value = true
  message.value = null
  void searchDetails('')
}

async function save() {
  if (busy.value) return
  const text = (v: string) => v.trim() || null
  const body = {
    fullName: text(form.fullName), position: form.position || null, otherPosition: form.position === 'Other' ? text(form.otherPosition) : null, detailCode: form.detailCode || null,
    nationalCode: text(toLatinDigits(form.nationalCode)), mobile: text(toLatinDigits(form.mobile)), phone: text(toLatinDigits(form.phone)),
    extension: text(toLatinDigits(form.extension)), email: text(form.email), signsCheques: form.signsCheques, signsNotes: form.signsNotes, signsContracts: form.signsContracts,
  }
  busy.value = true
  try {
    const r = editing.value ? await api.put<string>(`${base}/${editing.value.id}`, { form: body, version: editing.value.version }) : await api.post<string>(base, body)
    if (!r.ok) return fail(r.code)
    await load()
    const saved = signatories.value.find(s => s.id === r.value)
    // The form first (a scan belongs to a saved signatory), then the scan with the signatory's new version.
    if (chosenScan.value && saved) {
      editing.value = saved
      const problem = await uploadSignature(access, `${base}/${saved.id}/signature`, chosenScan.value, saved.version)
      if (problem) return fail(problem)
      await load()
    }
    message.value = { kind: 'ok', text: t(`«${form.fullName.trim() || saved?.fullName || ''}» ثبت شد.`, `"${form.fullName.trim() || saved?.fullName || ''}" saved.`) }
    showForm.value = false
  } finally {
    busy.value = false
  }
}

async function removeScan() {
  const s = editing.value
  if (!s || busy.value || !window.confirm(t('اسکن امضا حذف شود؟', 'Remove the scan of the signature?'))) return
  busy.value = true
  const r = await api.post<string>(`${base}/${s.id}/signature/remove`, { version: s.version })
  busy.value = false
  if (!r.ok) return fail(r.code)
  await load()
  // What is typed but not saved yet stays in the boxes; only the version and the scan change.
  editing.value = signatories.value.find(x => x.id === s.id) ?? null
  message.value = { kind: 'ok', text: t('اسکن امضا حذف شد.', 'Scan removed.') }
}

async function change(s: Signatory, what: 'activate' | 'deactivate' | 'delete') {
  if (what === 'delete' && !window.confirm(t(`«${s.fullName}» حذف شود؟`, `Delete "${s.fullName}"?`))) return
  const res = await api.post<string>(`${base}/${s.id}/${what}`, { version: s.version })
  if (!res.ok) return fail(res.code)
  message.value = { kind: 'ok', text: what === 'delete' ? t('حذف شد.', 'Deleted.') : what === 'activate' ? t('فعال شد.', 'Switched on.') : t('غیرفعال شد.', 'Switched off.') }
  if (editing.value?.id === s.id) showForm.value = false
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <h3>{{ t('دارندگان امضای مجاز (اسناد بهادار)', 'Authorised signatories (valuable documents)') }}</h3>
    <p class="muted small">{{ t('کسانی که چک و سفتهٔ شرکت را امضا می‌کنند. امضاکنندگان چک‌های هر حساب بانکی و تعداد امضای لازم آن، در «اطلاعات پایه ← حساب‌های بانکی» روی همان حساب تعریف می‌شود. تأییدکنندگان و تصویب‌کنندگان فرم‌ها در «دارندگان مجوزها (فرم‌ها)» هستند.',
      'The persons who sign the company\'s cheques and notes. Who signs the cheques of a bank account, and how many signatures it needs, is set on that account in Basic Information → Bank accounts. Who confirms or approves forms is on the page Permission holders (forms).') }}</p>
    <button type="button" class="tr-new" @click="open(null)">{{ t('صاحب امضای جدید', 'New signatory') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t('ویرایش صاحب امضا', 'Edit signatory') : t('صاحب امضای جدید', 'New signatory') }}</legend>
        <label class="wide">{{ t('حساب تفصیلی (اگر کارمند است)', 'Detail account (if staff)') }}
          <SearchSelect :model-value="form.detailCode" :options="detailChoices" remote :empty-label="t('— ندارد —', '— none —')" :aria-label="t('حساب تفصیلی', 'Detail account')"
                        :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @update:model-value="onDetail" @search="onDetailSearch" />
        </label>
        <label class="req wide"><span>{{ t('نام و نام خانوادگی', 'Name') }} <span class="star">*</span></span><input v-model="form.fullName" maxlength="100" /></label>
        <label class="req"><span>{{ t('سمت', 'Position') }} <span class="star">*</span></span>
          <select v-model="form.position" :aria-label="t('سمت', 'Position')">
            <option value="" disabled>{{ t('— انتخاب کنید —', '— choose —') }}</option>
            <option v-for="p in POSITIONS" :key="p.code" :value="p.code">{{ fa ? p.fa : p.en }}</option>
          </select>
        </label>
        <!-- Report no. 8, item 3-ب: with «سایر» a box in which the position inside the organisation is typed. -->
        <label v-if="form.position === 'Other'" class="req"><span>{{ t('سمت سازمانی', 'Position in the organisation') }} <span class="star">*</span></span>
          <input v-model="form.otherPosition" maxlength="100" :placeholder="t('مثلاً مدیر مالی', 'e.g. Finance manager')" />
        </label>
        <!-- Report no. 8, item 3-د, as the owner settled it (answer to Q88): three ticks beside the position, in any combination. -->
        <div class="wide tr-ticks">
          <span class="tr-ticks-title">{{ t('دارنده امضای مجاز برای', 'Authorised to sign') }} <span class="star">*</span></span>
          <label class="tr-check"><input v-model="form.signsCheques" type="checkbox" />{{ t('چک', 'Cheques') }}</label>
          <label class="tr-check"><input v-model="form.signsNotes" type="checkbox" />{{ t('سفته', 'Notes') }}</label>
          <label class="tr-check"><input v-model="form.signsContracts" type="checkbox" />{{ t('قراردادها و سایر اسناد تعهدی', 'Contracts and other documents of commitment') }}</label>
        </div>
        <label>{{ t('کد ملی', 'National code') }} <input v-model="form.nationalCode" dir="ltr" inputmode="numeric" maxlength="10" /></label>
        <label>{{ t('تلفن همراه', 'Mobile') }} <input v-model="form.mobile" dir="ltr" inputmode="tel" maxlength="13" placeholder="09121234567" /></label>
        <label>{{ t('تلفن ثابت', 'Landline') }} <input v-model="form.phone" dir="ltr" inputmode="tel" maxlength="20" placeholder="021-88990011" /></label>
        <label>{{ t('شماره تلفن داخل سازمانی', 'Extension') }} <input v-model="form.extension" dir="ltr" inputmode="numeric" maxlength="6" placeholder="214" /></label>
        <label>{{ t('آدرس ایمیل', 'E-mail') }} <input v-model="form.email" dir="ltr" inputmode="email" maxlength="100" placeholder="name@company.ir" /></label>
        <SignatureBox v-model:chosen="chosenScan" class="wide" :access="access" :locale="locale" :stored-path="storedScan" @problem="fail" @remove="removeScan" />
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false; message = null">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <template v-if="signatories.length">
      <div class="tr-search">
        <label>{{ t('جستجو', 'Search') }}<input v-model="search" type="search" autocomplete="off" /></label>
        <button type="button" class="tr-secondary" :disabled="!search" @click="search = ''">{{ t('نمایش همه', 'Show all') }}</button>
      </div>
      <table class="tr-table tr-cards">
        <thead>
          <tr><th>{{ t('نام و نام خانوادگی', 'Name') }}</th><th>{{ t('سمت', 'Position') }}</th><th>{{ t('امضا می‌کند', 'Signs') }}</th><th>{{ t('کد ملی', 'National code') }}</th>
            <th>{{ t('تلفن همراه', 'Mobile') }}</th><th>{{ t('داخلی', 'Ext.') }}</th><th>{{ t('ایمیل', 'E-mail') }}</th><th>{{ t('اسکن امضا', 'Scan') }}</th>
            <th>{{ t('وضعیت', 'State') }}</th><th /></tr>
        </thead>
        <tbody>
          <tr v-for="s in shown" :key="s.id" :class="{ inactive: !s.isActive }">
            <td :data-label="t('نام', 'Name')">{{ s.fullName }}</td>
            <td :data-label="t('سمت', 'Position')">{{ s.positionKind === 'Other' ? s.position : positionTitle(s.positionKind, fa) }}</td>
            <td :data-label="t('امضا می‌کند', 'Signs')"><span v-for="x in signs(s)" :key="x" class="tr-tag">{{ x }}</span></td>
            <td :data-label="t('کد ملی', 'National code')" dir="ltr">{{ s.nationalCode }}</td>
            <td :data-label="t('تلفن همراه', 'Mobile')" dir="ltr">{{ s.mobile }}</td>
            <td :data-label="t('داخلی', 'Ext.')" dir="ltr">{{ s.extension }}</td>
            <td :data-label="t('ایمیل', 'E-mail')" dir="ltr">{{ s.email }}</td>
            <td :data-label="t('اسکن امضا', 'Scan')">{{ s.hasSignature ? t('دارد', 'Yes') : t('ندارد', 'No') }}</td>
            <td :data-label="t('وضعیت', 'State')">{{ s.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}
              <span v-if="s.isInUse" class="tr-tag">{{ t('در حساب بانکی', 'on a bank account') }}</span></td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="open(s)">{{ t('ویرایش', 'Edit') }}</button>
              <button type="button" class="tr-link" @click="change(s, s.isActive ? 'deactivate' : 'activate')">{{ s.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
              <button v-if="!s.isInUse" type="button" class="tr-link danger" @click="change(s, 'delete')">{{ t('حذف', 'Delete') }}</button>
            </td>
          </tr>
          <tr v-if="!shown.length"><td colspan="10" class="muted">{{ t('موردی پیدا نشد.', 'Nothing found.') }}</td></tr>
        </tbody>
      </table>
    </template>
    <p v-else class="muted">{{ t('هنوز صاحب امضایی تعریف نشده است.', 'No signatory yet.') }}</p>
  </div>
</template>

<style scoped src="./organization.css"></style>
