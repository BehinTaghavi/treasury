<script setup lang="ts">
/**
 * «دارندگان مجوزها (فرم‌ها)» (owner's report no. 8, 21 Nov 2026): the company's staff, managers and officers included, who may or may
 * not be users of the system and who have the right to confirm («تایید») or approve («تصویب») some operations of the treasury system.
 * The upper part is the form of «دارندگان امضای مجاز» with «سمت سازمانی / شغل» in place of «سمت» and the scan of the signature. Under
 * «نقش» there are five tick boxes – first and second confirmer, first and second approver, and «سایر» with a typed title; ticking a
 * role opens «فهرست عملیات خزانه داری» with a tick box beside each operation. A person may have several roles, each in several
 * operations. Owner, 22 Nov 2026: in the approval flow the roles follow each other naturally – first confirmer, second confirmer, first
 * approver, second approver; a step nobody holds is left out – and a role under «سایر» is given its place among them («جایگاه در
 * گردش کار»). The tag beside a ticked role counts the operations ticked under it («… عملیات انتخاب شده»); it is not the order.
 * The forms of an operation list its holders as the persons who sign, in that order; the approval workflow itself is proposed in
 * docs/domain/permissions-and-approvals.md and not built yet.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, toLatinDigits, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { searchKey } from '../text'
import SignatureBox from './SignatureBox.vue'
import { OPERATION_GROUPS, OTHER_PLACES, ROLES, inFlowOrder, operationTitle, placeTitle, roleTitle, rolesToEdit, rolesToSend, type RoleDraft } from './persons'
import { uploadSignature } from './signatureScan'

/** `place`: «سایر» only, where the role stands in the approval flow; `step`: the role's order in the flow (owner, 22 Nov 2026). */
interface Role { kind: string; title: string; operations: string[]; place: string | null; step: number }
interface Holder {
  id: string; fullName: string; jobTitle: string; roles: Role[]; detailCode: string | null; nationalCode: string | null; mobile: string | null; phone: string | null
  extension: string | null; email: string | null; hasSignature: boolean; isActive: boolean; version: number
}
interface Detail { code: string; name: string; isActive: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const access = { apiBase: props.apiBase, token: props.token }
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US', { useGrouping: false })
const base = '/api/reference/permission-holders'

const holders = ref<Holder[]>([])
const search = ref('')
const editing = ref<Holder | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const empty = () => ({ fullName: '', jobTitle: '', detailCode: '', nationalCode: '', mobile: '', phone: '', extension: '', email: '' })
const form = reactive(empty())
const roles = reactive<Record<string, RoleDraft>>(rolesToEdit([]))
const chosenScan = ref<File | null>(null)
const details = ref<Detail[]>([])
const detailChoices = computed<Choice[]>(() => details.value.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive })))
const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const storedScan = computed(() => (editing.value?.hasSignature ? `${base}/${editing.value.id}/signature?v=${editing.value.version}` : ''))

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
function onDetail(code: string) {
  form.detailCode = code
  const d = details.value.find(x => x.code === code)
  if (d && !form.fullName.trim()) form.fullName = d.name
}

const roleName = (r: Role) => (r.kind === 'Other' ? r.title : roleTitle(r.kind, fa.value))
const shown = computed(() => {
  const q = searchKey(search.value)
  return holders.value.filter(h => !q || searchKey(
    `${h.fullName} ${h.jobTitle} ${h.nationalCode ?? ''} ${h.mobile ?? ''} ${h.roles.map(r => `${roleName(r)} ${r.operations.map(o => operationTitle(o, fa.value)).join(' ')}`).join(' ')}`).includes(q))
})

async function load() {
  const r = await api.get<Holder[]>(`${base}?includeInactive=true`)
  if (r.ok) holders.value = r.value
  else fail(r.code)
}

function open(h: Holder | null) {
  Object.assign(form, h
    ? {
        fullName: h.fullName, jobTitle: h.jobTitle, detailCode: h.detailCode ?? '', nationalCode: h.nationalCode ?? '', mobile: h.mobile ?? '', phone: h.phone ?? '',
        extension: h.extension ?? '', email: h.email ?? '',
      }
    : empty())
  Object.assign(roles, rolesToEdit(h?.roles ?? []))
  editing.value = h
  chosenScan.value = null
  showForm.value = true
  message.value = null
  void searchDetails('')
}

function toggleOperation(role: string, operation: string, on: boolean) {
  const list = roles[role]!.operations
  if (on && !list.includes(operation)) list.push(operation)
  if (!on) roles[role]!.operations = list.filter(o => o !== operation)
}
const allTicked = (role: string, group: (typeof OPERATION_GROUPS)[number]) => group.operations.every(o => roles[role]!.operations.includes(o.code))
function toggleGroup(role: string, group: (typeof OPERATION_GROUPS)[number], on: boolean) {
  for (const o of group.operations) toggleOperation(role, o.code, on)
}

async function save() {
  if (busy.value) return
  const text = (v: string) => v.trim() || null
  const body = {
    fullName: text(form.fullName), jobTitle: text(form.jobTitle), roles: rolesToSend(roles), detailCode: form.detailCode || null,
    nationalCode: text(toLatinDigits(form.nationalCode)), mobile: text(toLatinDigits(form.mobile)), phone: text(toLatinDigits(form.phone)),
    extension: text(toLatinDigits(form.extension)), email: text(form.email),
  }
  busy.value = true
  try {
    const r = editing.value ? await api.put<string>(`${base}/${editing.value.id}`, { form: body, version: editing.value.version }) : await api.post<string>(base, body)
    if (!r.ok) return fail(r.code)
    await load()
    const saved = holders.value.find(h => h.id === r.value)
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
  const h = editing.value
  if (!h || busy.value || !window.confirm(t('اسکن امضا حذف شود؟', 'Remove the scan of the signature?'))) return
  busy.value = true
  const r = await api.post<string>(`${base}/${h.id}/signature/remove`, { version: h.version })
  busy.value = false
  if (!r.ok) return fail(r.code)
  await load()
  editing.value = holders.value.find(x => x.id === h.id) ?? null
  message.value = { kind: 'ok', text: t('اسکن امضا حذف شد.', 'Scan removed.') }
}

async function change(h: Holder, what: 'activate' | 'deactivate' | 'delete') {
  if (what === 'delete' && !window.confirm(t(`«${h.fullName}» حذف شود؟`, `Delete "${h.fullName}"?`))) return
  const res = await api.post<string>(`${base}/${h.id}/${what}`, { version: h.version })
  if (!res.ok) return fail(res.code)
  message.value = { kind: 'ok', text: what === 'delete' ? t('حذف شد.', 'Deleted.') : what === 'activate' ? t('فعال شد.', 'Switched on.') : t('غیرفعال شد.', 'Switched off.') }
  if (editing.value?.id === h.id) showForm.value = false
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <h3>{{ t('دارندگان مجوزها (فرم‌ها)', 'Permission holders (forms)') }}</h3>
    <p class="muted small">{{ t('کارکنان شرکت (مدیران و مسئولین؛ چه کاربر سیستم باشند چه نباشند) که حق «تایید» یا «تصویب» برخی عملیات خزانه‌داری را دارند. برای هر نقش، عملیات مربوط را تیک بزنید. فعلاً فرم‌های «دریافت سفته» و «ثبت انتقال (خرج چک)» دارندگان مجوزِ عملیات خود را برای امضا پیشنهاد می‌کنند؛ گردش کار تایید و تصویب پس از تصمیم شما ساخته می‌شود.',
      'The company\'s staff (managers and officers, users of the system or not) who may confirm or approve some treasury operations. Tick the operations of each role. For now the forms of note receipts and of the transfer of a cheque offer the holders of their operation as signers; the approval workflow is built after your decision.') }}</p>
    <button type="button" class="tr-new" @click="open(null)">{{ t('دارنده مجوز جدید', 'New permission holder') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t('ویرایش دارنده مجوز', 'Edit permission holder') : t('دارنده مجوز جدید', 'New permission holder') }}</legend>
        <label class="wide">{{ t('حساب تفصیلی (اگر کارمند است)', 'Detail account (if staff)') }}
          <SearchSelect :model-value="form.detailCode" :options="detailChoices" remote :empty-label="t('— ندارد —', '— none —')" :aria-label="t('حساب تفصیلی', 'Detail account')"
                        :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @update:model-value="onDetail" @search="onDetailSearch" />
        </label>
        <label class="req wide"><span>{{ t('نام و نام خانوادگی', 'Name') }} <span class="star">*</span></span><input v-model="form.fullName" maxlength="100" /></label>
        <label class="req wide"><span>{{ t('سمت سازمانی / شغل', 'Position in the organisation / job') }} <span class="star">*</span></span>
          <input v-model="form.jobTitle" maxlength="100" :placeholder="t('مثلاً مدیر مالی', 'e.g. Finance manager')" /></label>
        <label>{{ t('کد ملی', 'National code') }} <input v-model="form.nationalCode" dir="ltr" inputmode="numeric" maxlength="10" /></label>
        <label>{{ t('تلفن همراه', 'Mobile') }} <input v-model="form.mobile" dir="ltr" inputmode="tel" maxlength="13" placeholder="09121234567" /></label>
        <label>{{ t('تلفن ثابت', 'Landline') }} <input v-model="form.phone" dir="ltr" inputmode="tel" maxlength="20" placeholder="021-88990011" /></label>
        <label>{{ t('شماره تلفن داخل سازمانی', 'Extension') }} <input v-model="form.extension" dir="ltr" inputmode="numeric" maxlength="6" placeholder="214" /></label>
        <label>{{ t('آدرس ایمیل', 'E-mail') }} <input v-model="form.email" dir="ltr" inputmode="email" maxlength="100" placeholder="name@company.ir" /></label>
        <SignatureBox v-model:chosen="chosenScan" class="wide" :access="access" :locale="locale" :stored-path="storedScan" @problem="fail" @remove="removeScan" />
      </fieldset>

      <fieldset class="tr-roles">
        <legend>{{ t('نقش', 'Role') }} <span class="star">*</span></legend>
        <p class="muted small tr-roles-hint">{{ t('هر نقش را که تیک بزنید، فهرست عملیات خزانه‌داری برای همان نقش باز می‌شود. یک شخص می‌تواند چند نقش داشته باشد.',
          'Ticking a role opens the list of treasury operations for that role. A person may have several roles.') }}
          <br />{{ t('ترتیب در گردش کار: تایید کننده (اول) ← تایید کننده (دوم) ← تصویب کننده (اول) ← تصویب کننده (دوم). مرحله‌ای که برای یک عملیات کسی ندارد حذف می‌شود؛ جایگاه نقش «سایر» را خودتان تعیین می‌کنید.',
            'Order in the flow: first confirmer → second confirmer → first approver → second approver. A step nobody holds in an operation is left out; you set the place of an "Other" role.') }}</p>
        <section v-for="r in ROLES" :key="r.code" :class="['tr-role', { on: roles[r.code]!.ticked }]">
          <label class="tr-check tr-role-name"><input v-model="roles[r.code]!.ticked" type="checkbox" />{{ fa ? r.fa : r.en }}
            <span v-if="roles[r.code]!.ticked" class="tr-tag">{{ t(`${num(roles[r.code]!.operations.length)} عملیات انتخاب شده`, `${roles[r.code]!.operations.length} operations ticked`) }}</span></label>
          <template v-if="roles[r.code]!.ticked">
            <div v-if="r.code === 'Other'" class="tr-other">
              <label class="req"><span>{{ t('عنوان نقش', 'Title of the role') }} <span class="star">*</span></span>
                <input v-model="roles[r.code]!.title" maxlength="60" :placeholder="t('مثلاً ناظر مالی', 'e.g. Financial supervisor')" /></label>
              <!-- Owner, 22 Nov 2026: a role that stands between the fixed steps is given its place in the flow. -->
              <label><span>{{ t('جایگاه در گردش کار', 'Place in the approval flow') }}</span>
                <select v-model="roles[r.code]!.place" :aria-label="t('جایگاه نقش سایر در گردش کار', 'Place of the Other role in the flow')">
                  <option v-for="p in OTHER_PLACES" :key="p.code" :value="p.code">{{ fa ? p.fa : p.en }}</option>
                </select></label>
            </div>
            <div class="tr-ops">
              <div v-for="g in OPERATION_GROUPS" :key="g.en" class="tr-ops-group">
                <label class="tr-check tr-ops-title"><input type="checkbox" :checked="allTicked(r.code, g)" :aria-label="t(`همهٔ ${g.fa}`, `All ${g.en}`)"
                                                           @change="toggleGroup(r.code, g, ($event.target as HTMLInputElement).checked)" />{{ fa ? g.fa : g.en }}</label>
                <label v-for="o in g.operations" :key="o.code" class="tr-check"><input type="checkbox" :checked="roles[r.code]!.operations.includes(o.code)"
                                                                                       @change="toggleOperation(r.code, o.code, ($event.target as HTMLInputElement).checked)" />{{ fa ? o.fa : o.en }}</label>
              </div>
            </div>
          </template>
        </section>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false; message = null">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <template v-if="holders.length">
      <div class="tr-search">
        <label>{{ t('جستجو', 'Search') }}<input v-model="search" type="search" autocomplete="off" /></label>
        <button type="button" class="tr-secondary" :disabled="!search" @click="search = ''">{{ t('نمایش همه', 'Show all') }}</button>
      </div>
      <table class="tr-table tr-cards tr-holders">
        <thead>
          <tr><th class="c-person">{{ t('نام و سمت', 'Name and job') }}</th><th>{{ t('نقش‌ها (به ترتیب گردش کار) و عملیات', 'Roles (in the order of the flow) and operations') }}</th>
            <th class="c-contact">{{ t('تماس', 'Contact') }}</th><th class="c-scan">{{ t('اسکن امضا', 'Scan') }}</th><th class="c-state">{{ t('وضعیت', 'State') }}</th><th class="c-actions" /></tr>
        </thead>
        <tbody>
          <tr v-for="h in shown" :key="h.id" :class="{ inactive: !h.isActive }">
            <td :data-label="t('نام و سمت', 'Name and job')"><div><strong class="tr-h-name">{{ h.fullName }}</strong><span class="tr-h-job">{{ h.jobTitle }}</span></div></td>
            <td :data-label="t('نقش‌ها و عملیات', 'Roles and operations')">
              <div class="tr-h-roles">
                <div v-for="r in inFlowOrder(h.roles)" :key="r.kind" class="tr-h-role">
                  <span class="tr-h-role-name">{{ roleName(r) }}<small v-if="r.kind === 'Other' && r.place">{{ placeTitle(r.place, fa) }}</small></span>
                  <span class="tr-h-ops"><span v-for="o in r.operations" :key="o" class="tr-chip">{{ operationTitle(o, fa) }}</span></span>
                </div>
              </div>
            </td>
            <td :data-label="t('تماس', 'Contact')"><div class="tr-h-contact"><span v-if="h.mobile" dir="ltr">{{ h.mobile }}</span>
              <span v-if="h.extension">{{ t('داخلی', 'Ext.') }} <span dir="ltr">{{ h.extension }}</span></span><span v-if="h.email" dir="ltr">{{ h.email }}</span></div></td>
            <td :data-label="t('اسکن امضا', 'Scan')">{{ h.hasSignature ? t('دارد', 'Yes') : t('ندارد', 'No') }}</td>
            <td :data-label="t('وضعیت', 'State')">{{ h.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="open(h)">{{ t('ویرایش', 'Edit') }}</button>
              <button type="button" class="tr-link" @click="change(h, h.isActive ? 'deactivate' : 'activate')">{{ h.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
              <button type="button" class="tr-link danger" @click="change(h, 'delete')">{{ t('حذف', 'Delete') }}</button>
            </td>
          </tr>
          <tr v-if="!shown.length"><td colspan="6" class="muted">{{ t('موردی پیدا نشد.', 'Nothing found.') }}</td></tr>
        </tbody>
      </table>
    </template>
    <p v-else class="muted">{{ t('هنوز دارنده مجوزی تعریف نشده است.', 'No permission holder yet.') }}</p>
  </div>
</template>

<style scoped src="./organization.css"></style>
<style scoped>
.tr-roles { display: block; }
.tr-roles-hint { margin: 0 0 .5rem; }
.tr-role { border: 1px solid #e4e7eb; border-radius: 8px; padding: .5rem .75rem; margin-bottom: .5rem; }
.tr-role.on { border-color: #93c5fd; background: #f8fbff; }
.tr-role-name { font-weight: 600; font-size: .9rem; }
.tr-other { display: grid; grid-template-columns: repeat(auto-fill, minmax(14rem, 20rem)); gap: .75rem; margin: .5rem 0; }
.tr-ops { display: grid; grid-template-columns: repeat(auto-fill, minmax(13rem, 1fr)); gap: .75rem; margin-top: .5rem; }
.tr-ops-group { display: flex; flex-direction: column; gap: .3rem; }
.tr-ops-title { font-weight: 600; color: #1f2933; border-bottom: 1px solid #e4e7eb; padding-bottom: .2rem; }
/* The list (owner, 22 Nov 2026: «فهرست دارندگان مجوز لازم است کمی مرتب‌تر شود»): the person in one column, each role on its own line with
   its operations as chips, the contact details together; columns keep their width so the rows line up. */
.tr-holders { table-layout: fixed; }
.tr-holders th { white-space: nowrap; font-size: .82rem; color: #52606d; font-weight: 600; background: #f8fafc; }
.tr-holders td { vertical-align: top; }
.tr-holders .c-person { width: 10rem; }
.tr-holders .c-contact { width: 8.5rem; }
.tr-holders .c-scan { width: 4.5rem; }
.tr-holders .c-state { width: 4.5rem; }
.tr-holders .c-actions { width: 7rem; }
.tr-holders .tr-actions { white-space: normal; }
.tr-holders .tr-actions .tr-link { display: block; padding: .1rem 0; text-align: start; }
.tr-h-name { display: block; }
.tr-h-job { display: block; color: #7b8794; font-size: .82rem; }
.tr-h-roles { display: grid; gap: .35rem; }
.tr-h-role { display: grid; grid-template-columns: 8.5rem 1fr; gap: .5rem; align-items: start; padding-bottom: .35rem; border-bottom: 1px dashed #e4e7eb; }
.tr-h-role:last-child { padding-bottom: 0; border-bottom: 0; }
.tr-h-role-name { font-weight: 600; font-size: .85rem; color: #1e3a8a; }
.tr-h-role-name small { display: block; font-weight: 400; color: #7b8794; font-size: .74rem; }
.tr-h-ops { display: flex; flex-wrap: wrap; gap: .25rem; }
.tr-chip { padding: .05rem .45rem; border-radius: 999px; background: #eef4ff; color: #1f2933; font-size: .78rem; white-space: nowrap; }
.tr-h-contact { display: grid; gap: .1rem; font-size: .85rem; overflow-wrap: anywhere; }
@media (max-width: 900px) {
  .tr-holders { table-layout: auto; }
  .tr-holders td { width: 100%; }
  .tr-h-role { grid-template-columns: 1fr; gap: .15rem; }
  .tr-holders .tr-actions .tr-link { display: inline-block; padding: .2rem .4rem; }
}
</style>
