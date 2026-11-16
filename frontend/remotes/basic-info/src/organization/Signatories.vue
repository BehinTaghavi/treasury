<script setup lang="ts">
/**
 * «دارندگان امضای مجاز» and the signature rules (owner, 16 Nov 2026, answers to Q41 and Q45–Q47). A signatory has a name, a position,
 * the staff تفصیل when there is one (its name is then taken), national code, mobile and landline numbers. Each kind of valuable
 * document — cheques and notes the company issues, the receipts of cheques and of notes, the payment sheets of cheques and of notes —
 * has one rule for every account and amount: how many signatures it needs, whose signature is compulsory and who may give the rest.
 * Signatures are given on paper; the forms ask who signs and print the names. A signatory named by a rule is taken out of the rule
 * before it is switched off or deleted.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, toLatinDigits, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { searchKey } from '../text'

interface Signatory {
  id: string; fullName: string; position: string; detailCode: string | null; nationalCode: string | null; mobile: string | null; phone: string | null
  isActive: boolean; isInRule: boolean; version: number
}
interface RuleSignatory { id: string; fullName: string; position: string; isActive: boolean }
interface Rule { document: string; required: number; compulsory: RuleSignatory[]; optional: RuleSignatory[]; version: number }
interface Detail { code: string; name: string; isActive: boolean }
type Part = '' | 'compulsory' | 'optional'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US', { useGrouping: false })
const base = '/api/reference/signatories'

/** The documents the owner named (answer to Q46), in the order the service lists them. */
const documentNames: Record<string, [string, string]> = {
  IssuedCheque: ['چک صادره', 'Cheque issued'], IssuedNote: ['سفتهٔ صادره', 'Note issued'], ChequeReceipt: ['رسید دریافت چک', 'Receipt of cheques'],
  NoteReceipt: ['رسید دریافت سفته', 'Receipt of notes'], ChequePaymentSheet: ['برگ پرداخت چک', 'Cheque payment sheet'], NotePaymentSheet: ['برگ پرداخت سفته', 'Note payment sheet'],
}
const documentName = (d: string) => (documentNames[d] ? (fa.value ? documentNames[d][0] : documentNames[d][1]) : d)

const signatories = ref<Signatory[]>([])
const rules = ref<Rule[]>([])
const search = ref('')
const editing = ref<Signatory | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const form = reactive({ fullName: '', position: '', detailCode: '', nationalCode: '', mobile: '', phone: '' })
const details = ref<Detail[]>([])
const detailChoices = computed<Choice[]>(() => details.value.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive })))
const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }

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

const shown = computed(() => {
  const q = searchKey(search.value)
  return signatories.value.filter(s => !q || searchKey(`${s.fullName} ${s.position} ${s.nationalCode ?? ''} ${s.mobile ?? ''} ${s.detailCode ?? ''}`).includes(q))
})

async function load() {
  const [s, r] = await Promise.all([api.get<Signatory[]>(`${base}?includeInactive=true`), api.get<Rule[]>('/api/reference/signature-rules')])
  if (s.ok) signatories.value = s.value
  else fail(s.code)
  if (r.ok) rules.value = r.value
}

function startNew() {
  Object.assign(form, { fullName: '', position: '', detailCode: '', nationalCode: '', mobile: '', phone: '' })
  editing.value = null
  showForm.value = true
  message.value = null
  void searchDetails('')
}

function startEdit(s: Signatory) {
  Object.assign(form, { fullName: s.fullName, position: s.position, detailCode: s.detailCode ?? '', nationalCode: s.nationalCode ?? '', mobile: s.mobile ?? '', phone: s.phone ?? '' })
  editing.value = s
  showForm.value = true
  message.value = null
  void searchDetails('')
}

async function save() {
  if (busy.value) return
  const text = (v: string) => v.trim() || null
  const body = {
    fullName: text(form.fullName), position: text(form.position), detailCode: form.detailCode || null, nationalCode: text(toLatinDigits(form.nationalCode)),
    mobile: text(toLatinDigits(form.mobile)), phone: text(toLatinDigits(form.phone)),
  }
  busy.value = true
  const r = editing.value ? await api.put<string>(`${base}/${editing.value.id}`, { form: body, version: editing.value.version }) : await api.post<string>(base, body)
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t(`«${form.fullName.trim()}» ثبت شد.`, `"${form.fullName.trim()}" saved.`) }
  showForm.value = false
  await load()
}

async function change(s: Signatory, what: 'activate' | 'deactivate' | 'delete') {
  if (what === 'delete' && !window.confirm(t(`«${s.fullName}» حذف شود؟`, `Delete "${s.fullName}"?`))) return
  const res = await api.post<string>(`${base}/${s.id}/${what}`, { version: s.version })
  if (!res.ok) return fail(res.code)
  message.value = { kind: 'ok', text: what === 'delete' ? t('حذف شد.', 'Deleted.') : what === 'activate' ? t('فعال شد.', 'Switched on.') : t('غیرفعال شد.', 'Switched off.') }
  await load()
}

// ---- the signature rule of one kind of document ----
const ruleEdit = reactive<{ document: string; version: number; required: string; parts: Record<string, Part> }>({ document: '', version: 0, required: '0', parts: {} })
/** Signatories that can be named: the active ones, and those the rule already names. */
const candidates = computed(() => signatories.value.filter(s => s.isActive || ruleEdit.parts[s.id]))
const compulsoryCount = computed(() => candidates.value.filter(s => ruleEdit.parts[s.id] === 'compulsory').length)
const namedCount = computed(() => candidates.value.filter(s => ruleEdit.parts[s.id]).length)
const names = (list: RuleSignatory[]) => list.map(s => `${s.fullName} (${s.position})`).join('، ')

function startRule(r: Rule) {
  const parts: Record<string, Part> = {}
  for (const s of r.compulsory) parts[s.id] = 'compulsory'
  for (const s of r.optional) parts[s.id] = 'optional'
  Object.assign(ruleEdit, { document: r.document, version: r.version, required: String(r.required), parts })
  message.value = null
}

async function saveRule() {
  if (busy.value) return
  const ids = (part: Part) => candidates.value.filter(s => ruleEdit.parts[s.id] === part).map(s => s.id)
  busy.value = true
  const r = await api.put<string>(`/api/reference/signature-rules/${ruleEdit.document}`,
    { required: Number(toLatinDigits(ruleEdit.required)) || 0, compulsoryIds: ids('compulsory'), optionalIds: ids('optional'), version: ruleEdit.version })
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t(`قاعدهٔ امضای «${documentName(ruleEdit.document)}» ثبت شد.`, `Signature rule of "${documentName(ruleEdit.document)}" saved.`) }
  ruleEdit.document = ''
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <h3>{{ t('دارندگان امضای مجاز', 'Authorised signatories') }}</h3>
    <button type="button" class="tr-new" @click="startNew">{{ t('صاحب امضای جدید', 'New signatory') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t('ویرایش صاحب امضا', 'Edit signatory') : t('صاحب امضای جدید', 'New signatory') }}</legend>
        <label class="wide">{{ t('حساب تفصیلی (اگر کارمند است)', 'Detail account (if staff)') }}
          <SearchSelect :model-value="form.detailCode" :options="detailChoices" remote :empty-label="t('— ندارد —', '— none —')" :aria-label="t('حساب تفصیلی', 'Detail account')"
                        :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @update:model-value="onDetail" @search="onDetailSearch" />
        </label>
        <label class="req wide"><span>{{ t('نام و نام خانوادگی', 'Name') }} <span class="star">*</span></span><input v-model="form.fullName" maxlength="100" /></label>
        <label class="req"><span>{{ t('سمت', 'Position') }} <span class="star">*</span></span><input v-model="form.position" maxlength="100" :placeholder="t('مثلاً مدیر عامل', 'e.g. Managing director')" /></label>
        <label>{{ t('کد ملی', 'National code') }} <input v-model="form.nationalCode" dir="ltr" inputmode="numeric" maxlength="10" /></label>
        <label>{{ t('تلفن همراه', 'Mobile') }} <input v-model="form.mobile" dir="ltr" inputmode="tel" maxlength="13" placeholder="09121234567" /></label>
        <label>{{ t('تلفن ثابت', 'Landline') }} <input v-model="form.phone" dir="ltr" inputmode="tel" maxlength="20" placeholder="021-88990011" /></label>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <template v-if="signatories.length">
      <div class="tr-search">
        <label>{{ t('جستجو', 'Search') }}<input v-model="search" type="search" autocomplete="off" /></label>
        <button type="button" class="tr-secondary" :disabled="!search" @click="search = ''">{{ t('نمایش همه', 'Show all') }}</button>
      </div>
      <table class="tr-table">
        <thead>
          <tr><th>{{ t('نام و نام خانوادگی', 'Name') }}</th><th>{{ t('سمت', 'Position') }}</th><th>{{ t('حساب تفصیلی', 'Detail account') }}</th><th>{{ t('کد ملی', 'National code') }}</th>
            <th>{{ t('تلفن همراه', 'Mobile') }}</th><th>{{ t('تلفن ثابت', 'Landline') }}</th><th>{{ t('وضعیت', 'State') }}</th><th /></tr>
        </thead>
        <tbody>
          <tr v-for="s in shown" :key="s.id" :class="{ inactive: !s.isActive }">
            <td>{{ s.fullName }}</td><td>{{ s.position }}</td><td dir="ltr">{{ s.detailCode }}</td><td dir="ltr">{{ s.nationalCode }}</td>
            <td dir="ltr">{{ s.mobile }}</td><td dir="ltr">{{ s.phone }}</td>
            <td>{{ s.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }} <span v-if="s.isInRule" class="tr-tag">{{ t('در قاعدهٔ امضا', 'in a rule') }}</span></td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="startEdit(s)">{{ t('ویرایش', 'Edit') }}</button>
              <template v-if="!s.isInRule">
                <button type="button" class="tr-link" @click="change(s, s.isActive ? 'deactivate' : 'activate')">{{ s.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
                <button type="button" class="tr-link danger" @click="change(s, 'delete')">{{ t('حذف', 'Delete') }}</button>
              </template>
            </td>
          </tr>
          <tr v-if="!shown.length"><td colspan="8" class="muted">{{ t('موردی پیدا نشد.', 'Nothing found.') }}</td></tr>
        </tbody>
      </table>
    </template>
    <p v-else class="muted">{{ t('هنوز صاحب امضایی تعریف نشده است.', 'No signatory yet.') }}</p>

    <h3 style="margin-top: 1.75rem">{{ t('قاعدهٔ امضای اسناد بهادار', 'Signature rules of valuable documents') }}</h3>
    <p class="muted small">{{ t('برای هر نوع سند: تعداد امضای لازم، امضاهای اجباری و امضاهای اختیاری (باقی‌ماندهٔ تعداد از میان اختیاری‌ها انتخاب می‌شود). قاعده برای همهٔ حساب‌ها و مبلغ‌ها یکسان است. فعلاً «رسید دریافت سفته» از قاعدهٔ خود استفاده می‌کند؛ سایر اسناد همراه با فرم‌هایشان.',
      'For each kind of document: the number of signatures needed, the compulsory and the optional signatories. The same for every account and amount. For now the receipt of notes follows its rule; the other documents will with their forms.') }}</p>
    <table class="tr-table">
      <thead><tr><th>{{ t('نوع سند', 'Document') }}</th><th>{{ t('تعداد امضای لازم', 'Signatures needed') }}</th><th>{{ t('امضای اجباری', 'Compulsory') }}</th><th>{{ t('امضای اختیاری', 'Optional') }}</th><th /></tr></thead>
      <tbody>
        <tr v-for="r in rules" :key="r.document">
          <td>{{ documentName(r.document) }}</td>
          <td>{{ r.required ? num(r.required) : t('امضای مجاز لازم ندارد', 'none needed') }}</td>
          <td>{{ names(r.compulsory) }}</td><td>{{ names(r.optional) }}</td>
          <td class="tr-actions"><button type="button" class="tr-link" @click="startRule(r)">{{ t('تعیین قاعده', 'Set rule') }}</button></td>
        </tr>
      </tbody>
    </table>

    <form v-if="ruleEdit.document" class="tr-form" @submit.prevent="saveRule">
      <fieldset>
        <legend>{{ t(`قاعدهٔ امضای «${documentName(ruleEdit.document)}»`, `Signature rule of "${documentName(ruleEdit.document)}"`) }}</legend>
        <label class="req"><span>{{ t('تعداد امضای لازم', 'Signatures needed') }} <span class="star">*</span></span><input v-model="ruleEdit.required" dir="ltr" inputmode="numeric" maxlength="2" /></label>
        <p class="wide muted small" style="margin: 0; align-self: end">{{ t(`اجباری: ${num(compulsoryCount)} نفر · نام‌برده: ${num(namedCount)} نفر. تعداد امضا باید بین این دو باشد.`,
          `Compulsory: ${compulsoryCount} · named: ${namedCount}. The number of signatures lies between them.`) }}</p>
        <label v-for="s in candidates" :key="s.id">{{ s.fullName }} <small class="muted">{{ s.position }}</small>
          <select v-model="ruleEdit.parts[s.id]" :aria-label="t(`امضای ${s.fullName}`, `Signature of ${s.fullName}`)">
            <option value="">{{ t('— امضا نمی‌کند —', '— does not sign —') }}</option>
            <option value="compulsory">{{ t('اجباری', 'Compulsory') }}</option>
            <option value="optional">{{ t('اختیاری', 'Optional') }}</option>
          </select>
        </label>
        <p v-if="!candidates.length" class="wide muted">{{ t('ابتدا صاحبان امضا را تعریف کنید.', 'Add the signatories first.') }}</p>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیرهٔ قاعده', 'Save rule') }}</button>
        <button type="button" class="tr-link" @click="ruleEdit.document = ''">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>
  </div>
</template>

<style scoped src="./organization.css"></style>
