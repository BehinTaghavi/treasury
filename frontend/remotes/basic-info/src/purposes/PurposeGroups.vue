<script setup lang="ts">
/**
 * Purpose groups (گروه بابت‌ها) and their purposes (بابت‌ها), new-version manual item 9.
 * A group is for receipts or payments, has a number, a description, the معین / معین در معین it is posted to and up to two
 * references (عطف) whose values receipts and payments of the group carry. Choosing a group shows its purposes below.
 * Same behaviour as the other Basic Information lists: search, edit, switch off, delete while unused.
 */
import { computed, nextTick, onMounted, reactive, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '../components/SearchSelect.vue'
import { searchKey } from '../text'

type Direction = 'Receipt' | 'Payment'
interface Group {
  id: string; number: number; direction: Direction; name: string; ledgerAccountCode: string
  reference1Id: string | null; reference2Id: string | null; isActive: boolean; isInUse: boolean; version: number
}
interface PurposeRow { id: string; groupId: string; name: string; isActive: boolean; isInUse: boolean; version: number }
interface Entry { id: string; name: string; isActive: boolean }
interface LedgerAccount { code: string; name: string; isPostable: boolean; isActive: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US')
const dirLabel = (d: Direction) => (d === 'Receipt' ? t('دریافت', 'Receipt') : t('پرداخت', 'Payment'))

const groups = ref<Group[]>([])
const purposes = ref<PurposeRow[]>([])
const references = ref<Entry[]>([])
const ledger = ref<LedgerAccount[]>([])
const filter = ref<'' | Direction>('')
const search = ref('')
const selected = ref<string | null>(null)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const busy = ref(false)

const emptyForm = () => ({ direction: 'Receipt' as Direction, name: '', ledgerAccountCode: '', reference1Id: '', reference2Id: '' })
const form = reactive(emptyForm())
const editingGroup = ref<Group | null>(null)
const showForm = ref(false)

const newPurpose = ref('')
const purposeSearch = ref('')
const editingPurpose = ref<{ id: string; name: string; groupId: string } | null>(null)

const show = (kind: 'ok' | 'error', text: string) => { message.value = { kind, text } }
const fail = (code: string) => show('error', translate(code, props.locale))

const ledgerChoices = computed<Choice[]>(() => ledger.value.filter(a => a.isPostable && a.isActive).map(a => ({ value: a.code, label: a.name, hint: a.code })))
const referenceChoices = computed<Choice[]>(() => references.value.filter(r => r.isActive).map(r => ({ value: r.id, label: r.name })))
const groupChoices = computed<Choice[]>(() => groups.value.filter(g => g.isActive).map(g => ({ value: g.id, label: g.name, hint: `${num(g.number)} · ${dirLabel(g.direction)}` })))
const ledgerName = (code: string) => ledger.value.find(a => a.code === code)?.name ?? ''
const referenceName = (id: string | null) => references.value.find(r => r.id === id)?.name ?? ''

const shownGroups = computed(() => {
  const q = searchKey(search.value)
  return groups.value.filter(g => (!filter.value || g.direction === filter.value)
    && (!q || searchKey(g.name).includes(q) || g.ledgerAccountCode.includes(q) || String(g.number) === q))
})
const current = computed(() => groups.value.find(g => g.id === selected.value) ?? null)
const shownPurposes = computed(() => {
  const q = searchKey(purposeSearch.value)
  return purposes.value.filter(p => p.groupId === selected.value && (!q || searchKey(p.name).includes(q)))
})
const purposeCount = (groupId: string) => purposes.value.filter(p => p.groupId === groupId).length

async function load() {
  const [g, p] = await Promise.all([
    api.get<Group[]>('/api/reference/purpose-groups?includeInactive=true'),
    api.get<PurposeRow[]>('/api/reference/purposes?includeInactive=true'),
  ])
  if (g.ok) groups.value = g.value
  else fail(g.code)
  if (p.ok) purposes.value = p.value
}

async function loadLists() {
  const [r, l] = await Promise.all([
    api.get<Entry[]>('/api/reference/references?includeInactive=true'),
    api.get<LedgerAccount[]>('/api/reference/ledger/accounts?postableOnly=true'),
  ])
  if (r.ok) references.value = r.value
  if (l.ok) ledger.value = l.value
}

function startNew() {
  Object.assign(form, emptyForm(), { direction: filter.value || 'Receipt' })
  editingGroup.value = null
  showForm.value = true
  message.value = null
}

function startEdit(g: Group) {
  Object.assign(form, { direction: g.direction, name: g.name, ledgerAccountCode: g.ledgerAccountCode, reference1Id: g.reference1Id ?? '', reference2Id: g.reference2Id ?? '' })
  editingGroup.value = g
  showForm.value = true
  message.value = null
}

async function saveGroup() {
  if (busy.value) return
  const body = { direction: form.direction, name: form.name, ledgerAccountCode: form.ledgerAccountCode, reference1Id: form.reference1Id || null, reference2Id: form.reference2Id || null }
  busy.value = true
  const r = editingGroup.value
    ? await api.put<string>(`/api/reference/purpose-groups/${editingGroup.value.id}`, { form: body, version: editingGroup.value.version })
    : await api.post<string>('/api/reference/purpose-groups', body)
  busy.value = false
  if (!r.ok) return fail(r.code)
  show('ok', t(`گروه «${form.name.trim()}» ثبت شد.`, `Group "${form.name.trim()}" saved.`))
  showForm.value = false
  search.value = ''
  await load()
  selected.value = r.value
  await loadLists() // a reference named by the group is now "in use"
}

async function changeGroup(g: Group, change: 'activate' | 'deactivate' | 'delete') {
  if (change === 'delete' && !window.confirm(t(`گروه «${g.name}» حذف شود؟`, `Delete group "${g.name}"?`))) return
  const r = await api.post<string>(`/api/reference/purpose-groups/${g.id}/${change}`, { version: g.version })
  if (!r.ok) return fail(r.code)
  show('ok', change === 'delete' ? t('گروه حذف شد.', 'Group deleted.') : change === 'activate' ? t('فعال شد.', 'Switched on.') : t('غیرفعال شد.', 'Switched off.'))
  if (change === 'delete' && selected.value === g.id) selected.value = null
  await load()
}

async function choose(g: Group) {
  selected.value = g.id
  purposeSearch.value = ''
  editingPurpose.value = null
  await nextTick()
  document.querySelector('.tr-purposes')?.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
}

async function addPurpose() {
  if (!current.value || busy.value) return
  busy.value = true
  const r = await api.post<string>('/api/reference/purposes', { groupId: current.value.id, name: newPurpose.value })
  busy.value = false
  if (!r.ok) return fail(r.code)
  show('ok', t(`بابت «${newPurpose.value.trim()}» ثبت شد.`, `Purpose "${newPurpose.value.trim()}" saved.`))
  newPurpose.value = ''
  purposeSearch.value = ''
  await load()
}

async function savePurpose(p: PurposeRow) {
  const e = editingPurpose.value
  if (!e) return
  const r = await api.put<string>(`/api/reference/purposes/${p.id}`, { groupId: e.groupId, name: e.name, version: p.version })
  if (!r.ok) return fail(r.code)
  editingPurpose.value = null
  show('ok', e.groupId !== p.groupId ? t('بابت به گروه دیگر منتقل شد.', 'Purpose moved to the other group.') : t('تغییر ثبت شد.', 'Change saved.'))
  await load()
}

async function changePurpose(p: PurposeRow, change: 'activate' | 'deactivate' | 'delete') {
  if (change === 'delete' && !window.confirm(t(`بابت «${p.name}» حذف شود؟`, `Delete purpose "${p.name}"?`))) return
  const r = await api.post<string>(`/api/reference/purposes/${p.id}/${change}`, { version: p.version })
  if (!r.ok) return fail(r.code)
  show('ok', change === 'delete' ? t('بابت حذف شد.', 'Purpose deleted.') : change === 'activate' ? t('فعال شد.', 'Switched on.') : t('غیرفعال شد.', 'Switched off.'))
  await load()
}

onMounted(async () => {
  await loadLists()
  await load()
})
</script>

<template>
  <div>
    <h3>{{ t('گروه بابت‌ها و بابت‌ها', 'Purpose groups and purposes') }}</h3>
    <p v-if="!ledger.length" class="muted">{{ t('ابتدا در «کدینگ حساب‌ها» حساب‌های سیستم حسابداری را وارد کنید؛ حساب معین هر گروه از آنجا انتخاب می‌شود.', 'Import the chart of accounts first; each group\'s ledger account is chosen from it.') }}</p>

    <div class="tr-bar">
      <div class="tr-tabs" role="tablist">
        <button v-for="d in ([['', t('همه', 'All')], ['Receipt', t('دریافت', 'Receipts')], ['Payment', t('پرداخت', 'Payments')]] as const)" :key="d[0]"
                type="button" role="tab" :aria-selected="filter === d[0]" :class="{ on: filter === d[0] }" @click="filter = d[0]">{{ d[1] }}</button>
      </div>
      <button v-if="!showForm" type="button" @click="startNew">{{ t('گروه بابت جدید', 'New purpose group') }}</button>
    </div>

    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="saveGroup">
      <fieldset>
        <legend>{{ editingGroup ? t(`ویرایش گروه ${num(editingGroup.number)}`, `Edit group ${editingGroup.number}`) : t('گروه بابت جدید', 'New purpose group') }}</legend>
        <div class="req tr-radio" role="radiogroup" :aria-label="t('نوع بابت', 'Kind')">
          <span>{{ t('نوع بابت', 'Kind') }} <span class="star">*</span></span>
          <label><input v-model="form.direction" type="radio" value="Receipt" :disabled="editingGroup?.isInUse" /> {{ t('دریافت', 'Receipt') }}</label>
          <label><input v-model="form.direction" type="radio" value="Payment" :disabled="editingGroup?.isInUse" /> {{ t('پرداخت', 'Payment') }}</label>
        </div>
        <label class="req"><span>{{ t('شرح گروه بابت', 'Description') }} <span class="star">*</span></span>
          <input v-model="form.name" maxlength="100" autocomplete="off" :placeholder="t('مثلاً پیش دریافت', 'e.g. Advance received')" />
        </label>
        <label class="req"><span>{{ t('معین / معین در معین', 'Ledger account') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.ledgerAccountCode" :options="ledgerChoices" required :aria-label="t('معین / معین در معین', 'Ledger account')" :none-text="t('حسابی پیدا نشد', 'No account found')" />
        </label>
        <label>{{ t('عطف ۱', 'Reference 1') }}
          <SearchSelect v-model="form.reference1Id" :options="referenceChoices" :empty-label="t('ندارد', 'None')" :aria-label="t('عطف ۱', 'Reference 1')" />
        </label>
        <label>{{ t('عطف ۲', 'Reference 2') }}
          <SearchSelect v-model="form.reference2Id" :options="referenceChoices" :empty-label="t('ندارد', 'None')" :disabled="!form.reference1Id" :aria-label="t('عطف ۲', 'Reference 2')" />
        </label>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <div class="tr-search">
      <label>{{ t('جستجوی گروه (شرح، شماره یا کد حساب)', 'Search groups (description, number or account)') }}<input v-model="search" type="search" autocomplete="off" /></label>
      <button type="button" class="tr-secondary" :disabled="!search && !filter" @click="search = ''; filter = ''">{{ t('نمایش همه', 'Show all') }}</button>
      <span class="muted small">{{ t(`${num(shownGroups.length)} از ${num(groups.length)} گروه`, `${shownGroups.length} of ${groups.length} groups`) }}</span>
    </div>

    <div class="tr-scroll">
      <table class="tr-table">
        <thead>
          <tr>
            <th>{{ t('شماره', 'No.') }}</th><th>{{ t('نوع', 'Kind') }}</th><th>{{ t('شرح گروه', 'Description') }}</th><th>{{ t('معین', 'Ledger account') }}</th>
            <th>{{ t('عطف ۱', 'Ref. 1') }}</th><th>{{ t('عطف ۲', 'Ref. 2') }}</th><th>{{ t('بابت‌ها', 'Purposes') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th />
          </tr>
        </thead>
        <tbody>
          <tr v-for="g in shownGroups" :key="g.id" :class="{ inactive: !g.isActive, chosen: g.id === selected }">
            <td>{{ num(g.number) }}</td>
            <td>{{ dirLabel(g.direction) }}</td>
            <td><button type="button" class="tr-link strong" @click="choose(g)">{{ g.name }}</button></td>
            <td><span dir="ltr">{{ g.ledgerAccountCode }}</span> <small class="muted">{{ ledgerName(g.ledgerAccountCode) }}</small></td>
            <td>{{ referenceName(g.reference1Id) }}</td>
            <td>{{ referenceName(g.reference2Id) }}</td>
            <td>{{ num(purposeCount(g.id)) }}</td>
            <td>
              {{ g.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}
              <span v-if="g.isInUse" class="tr-tag">{{ t('در حال استفاده', 'in use') }}</span>
            </td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="choose(g)">{{ t('بابت‌ها', 'Purposes') }}</button>
              <button type="button" class="tr-link" @click="startEdit(g)">{{ t('ویرایش', 'Edit') }}</button>
              <button type="button" class="tr-link" @click="changeGroup(g, g.isActive ? 'deactivate' : 'activate')">{{ g.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
              <button v-if="!g.isInUse" type="button" class="tr-link danger" @click="changeGroup(g, 'delete')">{{ t('حذف', 'Delete') }}</button>
            </td>
          </tr>
          <tr v-if="!shownGroups.length"><td colspan="9" class="muted">{{ groups.length ? t('با این جستجو گروهی پیدا نشد.', 'No group matches.') : t('هنوز گروه بابتی تعریف نشده است.', 'No purpose group yet.') }}</td></tr>
        </tbody>
      </table>
    </div>

    <section v-if="current" class="tr-purposes">
      <h4>{{ t(`بابت‌های گروه «${current.name}»`, `Purposes of "${current.name}"`) }} <small class="muted">({{ dirLabel(current.direction) }})</small></h4>
      <form class="tr-inline-form" @submit.prevent="addPurpose">
        <label>{{ t('شرح بابت جدید', 'New purpose') }}<input v-model="newPurpose" maxlength="150" autocomplete="off" :disabled="!current.isActive" /></label>
        <button type="submit" :disabled="busy || !current.isActive">{{ t('افزودن', 'Add') }}</button>
        <label>{{ t('جستجو', 'Search') }}<input v-model="purposeSearch" type="search" autocomplete="off" /></label>
      </form>
      <p v-if="!current.isActive" class="muted small">{{ t('این گروه غیرفعال است؛ بابت جدید نمی‌گیرد.', 'This group is switched off and takes no new purposes.') }}</p>
      <table class="tr-table">
        <thead><tr><th>{{ t('شرح بابت', 'Purpose') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th /></tr></thead>
        <tbody>
          <tr v-for="p in shownPurposes" :key="p.id" :class="{ inactive: !p.isActive }">
            <template v-if="editingPurpose?.id === p.id">
              <td class="tr-edit">
                <input v-model="editingPurpose.name" maxlength="150" :aria-label="t('شرح بابت', 'Purpose')" @keydown.enter.prevent="savePurpose(p)" />
                <SearchSelect v-model="editingPurpose.groupId" :options="groupChoices" :aria-label="t('گروه', 'Group')" />
              </td>
              <td />
              <td class="tr-actions">
                <button type="button" @click="savePurpose(p)">{{ t('ذخیره', 'Save') }}</button>
                <button type="button" class="tr-link" @click="editingPurpose = null">{{ t('انصراف', 'Cancel') }}</button>
              </td>
            </template>
            <template v-else>
              <td>{{ p.name }}</td>
              <td>{{ p.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }} <span v-if="p.isInUse" class="tr-tag">{{ t('در حال استفاده', 'in use') }}</span></td>
              <td class="tr-actions">
                <button type="button" class="tr-link" @click="editingPurpose = { id: p.id, name: p.name, groupId: p.groupId }">{{ t('ویرایش', 'Edit') }}</button>
                <button type="button" class="tr-link" @click="changePurpose(p, p.isActive ? 'deactivate' : 'activate')">{{ p.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
                <button v-if="!p.isInUse" type="button" class="tr-link danger" @click="changePurpose(p, 'delete')">{{ t('حذف', 'Delete') }}</button>
              </td>
            </template>
          </tr>
          <tr v-if="!shownPurposes.length"><td colspan="3" class="muted">{{ t('بابتی نیست.', 'No purpose here yet.') }}</td></tr>
        </tbody>
      </table>
    </section>
  </div>
</template>

<style scoped>
h3 { margin-top: 0; }
h4 { margin: 0 0 .5rem; }
.tr-bar { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; }
.tr-tabs { display: flex; gap: .25rem; }
.tr-tabs button { background: #fff; color: #1d4ed8; }
.tr-tabs button.on { background: #2563eb; color: #fff; }
.tr-form { display: grid; gap: .75rem; margin: 1rem 0; }
fieldset { border: 1px solid #e4e7eb; border-radius: 8px; display: grid; grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr)); gap: .75rem; align-items: start; padding: .75rem; }
legend { font-weight: 600; padding: 0 .25rem; }
label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
.tr-radio { display: flex; flex-direction: column; gap: .3rem; font-size: .85rem; }
.tr-radio label { flex-direction: row; align-items: center; gap: .35rem; }
input:not([type=radio]) { padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; background: #fff; }
.req > input { border-color: #d64545; box-shadow: inset 0 0 0 1px #f5c2c2; }
.star { color: #d64545; font-weight: 700; }
button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .5; cursor: default; }
.tr-secondary { background: #fff; color: #1d4ed8; }
.tr-link { background: transparent; color: #1d4ed8; border-color: transparent; padding: .2rem .4rem; }
.tr-link.strong { font-weight: 600; color: #1f2933; padding: 0; text-align: start; }
.tr-link.danger { color: #b42318; }
.tr-buttons { display: flex; gap: .5rem; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-search { display: flex; flex-wrap: wrap; gap: .75rem; align-items: end; margin-top: 1rem; }
.tr-search label { width: 22rem; max-width: 100%; }
.tr-scroll { overflow-x: auto; }
.tr-table { width: 100%; border-collapse: collapse; margin-top: .75rem; }
.tr-table th, .tr-table td { padding: .4rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; vertical-align: middle; }
.tr-table tr.inactive td { color: #9aa5b1; }
.tr-table tr.chosen td { background: #eef4ff; }
.tr-tag { display: inline-block; margin-inline-start: .4rem; padding: 0 .4rem; border-radius: 4px; background: #eef2f7; color: #52606d; font-size: .75rem; }
.tr-actions { white-space: nowrap; }
.tr-purposes { margin-top: 1.5rem; padding: 1rem; border: 1px solid #c7d7fe; border-radius: 8px; background: #fafcff; }
.tr-inline-form { display: flex; flex-wrap: wrap; gap: .75rem; align-items: end; }
.tr-inline-form label { width: 18rem; max-width: 100%; }
.tr-edit { display: grid; grid-template-columns: 1fr 1fr; gap: .5rem; }
.muted { color: #7b8794; }
.small { font-size: .8rem; }
</style>
