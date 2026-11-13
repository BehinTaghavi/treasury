<script setup lang="ts">
/**
 * Petty cash holders (دارندگان تنخواه گردان), new-version manual item 11: the holder's تفصیل (name from it), the معین, the fixed
 * amount, the settlement limit (حد واریز), the payment ceiling (سقف پرداخت), the payment purpose groups that may be paid from it
 * (or all of them) and how it is topped up (cash, cheque or bank card). Saved by accounts-svc.
 * Owner's report no. 2: payment ceiling ≤ settlement limit < amount (item 4), creation date (item 11), how the holder keeps the
 * money – bank account, cash and/or bank card (item 12) – and top-up onto a bank card (item 13).
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, normaliseJalali, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '../components/SearchSelect.vue'
import { searchKey } from '../text'

type Recovery = 'Cash' | 'Cheque' | 'BankCard'
interface Keeping { bank: boolean; bankAccountName: string | null; bankAccountNumber: string | null; cash: boolean; card: boolean; cardHolderName: string | null; cardNumber: string | null }
interface Fund {
  id: string; holderDetailCode: string; holderName: string | null; ledgerAccountCode: string; amount: number; settleLimit: number; payCeiling: number
  allowedPurposeGroupIds: string[]; recovery: Recovery; isActive: boolean; version: number; createdOn: string | null; keeping: Keeping | null
}
const RECOVERY: Record<Recovery, [string, string]> = { Cash: ['نقدی', 'Cash'], Cheque: ['چک', 'Cheque'], BankCard: ['کارت عابر بانک', 'Bank card'] }
const noKeeping = (): Keeping => ({ bank: false, bankAccountName: '', bankAccountNumber: '', cash: false, card: false, cardHolderName: '', cardNumber: '' })
interface LedgerAccount { code: string; name: string; isPostable: boolean; isActive: boolean; detail1Types: string[] }
interface Detail { code: string; name: string; isActive: boolean }
interface Group { id: string; number: number; name: string; isActive: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const money = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US')
const base = '/api/accounts/petty-cash'

const funds = ref<Fund[]>([])
const ledger = ref<LedgerAccount[]>([])
const groups = ref<Group[]>([])
const holders = ref<Detail[]>([])
const editing = ref<Fund | null>(null)
const showForm = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const form = reactive({
  holderDetailCode: '', ledgerAccountCode: '', amount: '', settleLimit: '', payCeiling: '', allGroups: true, groupIds: [] as string[], recovery: 'Cash' as Recovery,
  createdOn: '', keeping: noKeeping(),
})
const toAdd = ref('')

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
/** Persian digits and thousands separators are accepted. */
const amount = (s: string) => Number(searchKey(s).replace(/[^0-9]/g, '')) || 0
const ledgerChoices = computed<Choice[]>(() => ledger.value.filter(a => a.isPostable && a.isActive).map(a => ({ value: a.code, label: a.name, hint: a.code })))
const holderChoices = computed<Choice[]>(() => holders.value.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive })))
const groupChoices = computed<Choice[]>(() => groups.value.filter(g => g.isActive && !form.groupIds.includes(g.id)).map(g => ({ value: g.id, label: g.name, hint: String(g.number) })))
const groupName = (id: string) => groups.value.find(g => g.id === id)?.name ?? '—'
const ledgerName = (code: string) => ledger.value.find(a => a.code === code)?.name ?? ''
const chosenLedger = computed(() => ledger.value.find(a => a.code === form.ledgerAccountCode))

async function load() {
  const r = await api.get<Fund[]>(`${base}?includeInactive=true`)
  if (r.ok) funds.value = r.value
  else fail(r.code)
}

async function loadLists() {
  const [l, g] = await Promise.all([
    api.get<LedgerAccount[]>('/api/reference/ledger/accounts?postableOnly=true'),
    api.get<Group[]>('/api/reference/purpose-groups?direction=Payment&includeInactive=true'),
  ])
  if (l.ok) ledger.value = l.value
  if (g.ok) groups.value = g.value
}

async function searchHolders(text: string) {
  const types = chosenLedger.value?.detail1Types ?? []
  const found = (await Promise.all((types.length ? types : [null]).map(type => {
    // The holder is staff, a cost centre or an organisational unit (owner's report no. 2, item 3).
    const q = new URLSearchParams({ take: '50', roles: 'Staff,CostCentre,OrgUnit' })
    if (text.trim()) q.set('search', text.trim())
    if (type) q.set('type', type)
    return api.get<Detail[]>(`/api/reference/ledger/details?${q}`)
  }))).flatMap(r => (r.ok ? r.value : []))
  if (form.holderDetailCode && !found.some(d => d.code === form.holderDetailCode)) {
    const r = await api.get<Detail>(`/api/reference/ledger/details/${encodeURIComponent(form.holderDetailCode)}`)
    if (r.ok) found.unshift(r.value)
  }
  holders.value = found
}
watch(() => form.ledgerAccountCode, () => void searchHolders(''))
let timer: ReturnType<typeof setTimeout> | undefined
function onSearch(text: string) {
  clearTimeout(timer)
  timer = setTimeout(() => searchHolders(text), 250)
}

function addGroup(id: string) {
  if (id && !form.groupIds.includes(id)) form.groupIds.push(id)
  toAdd.value = ''
}

function startNew() {
  Object.assign(form, {
    holderDetailCode: '', ledgerAccountCode: '', amount: '', settleLimit: '', payCeiling: '', allGroups: true, groupIds: [], recovery: 'Cash',
    createdOn: todayJalali(), keeping: noKeeping(),
  })
  editing.value = null
  showForm.value = true
  message.value = null
  void searchHolders('')
}

function startEdit(f: Fund) {
  Object.assign(form, {
    holderDetailCode: f.holderDetailCode, ledgerAccountCode: f.ledgerAccountCode, amount: money(f.amount), settleLimit: money(f.settleLimit), payCeiling: money(f.payCeiling),
    allGroups: f.allowedPurposeGroupIds.length === 0, groupIds: [...f.allowedPurposeGroupIds], recovery: f.recovery,
    createdOn: f.createdOn ?? '', keeping: { ...noKeeping(), ...(f.keeping ?? {}) },
  })
  editing.value = f
  showForm.value = true
  message.value = null
  void searchHolders('')
}

async function save() {
  if (busy.value) return
  const body = {
    holderDetailCode: form.holderDetailCode, ledgerAccountCode: form.ledgerAccountCode, amount: amount(form.amount), settleLimit: amount(form.settleLimit),
    payCeiling: amount(form.payCeiling), allowedPurposeGroupIds: form.allGroups ? [] : form.groupIds, recovery: form.recovery,
    createdOn: form.createdOn ? normaliseJalali(form.createdOn) ?? form.createdOn : null, keeping: form.keeping,
  }
  busy.value = true
  const r = editing.value
    ? await api.put<string>(`${base}/${editing.value.id}`, { form: body, version: editing.value.version })
    : await api.post<string>(base, body)
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t('تنخواه گردان ثبت شد.', 'Petty cash saved.') }
  showForm.value = false
  await load()
}

async function toggle(f: Fund) {
  const r = await api.post<string>(`${base}/${f.id}/${f.isActive ? 'deactivate' : 'activate'}`, { version: f.version })
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: f.isActive ? t('غیرفعال شد.', 'Switched off.') : t('فعال شد.', 'Switched on.') }
  await load()
}

onMounted(async () => {
  await loadLists()
  await load()
})
</script>

<template>
  <div>
    <h3>{{ t('دارندگان تنخواه گردان', 'Petty cash holders') }}</h3>
    <button v-if="!showForm" type="button" @click="startNew">{{ t('تنخواه گردان جدید', 'New petty cash') }}</button>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="showForm" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ editing ? t('ویرایش تنخواه گردان', 'Edit petty cash') : t('تنخواه گردان جدید', 'New petty cash') }}</legend>
        <label class="req"><span>{{ t('معین / معین در معین تنخواه', 'Ledger account') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.ledgerAccountCode" :options="ledgerChoices" required :aria-label="t('معین تنخواه', 'Ledger account')" :none-text="t('حسابی پیدا نشد', 'No account found')" />
        </label>
        <label class="req wide"><span>{{ t('دارندهٔ تنخواه (حساب تفصیل)', 'Holder (detail account)') }} <span class="star">*</span></span>
          <SearchSelect v-model="form.holderDetailCode" :options="holderChoices" remote required :aria-label="t('دارندهٔ تنخواه', 'Holder')"
                        :none-text="t('تفصیلی پیدا نشد؛ دارنده باید ابتدا در سیستم مالی تعریف شود', 'Not found; define the holder in the accounting system first')" @search="onSearch" />
        </label>
        <label class="req"><span>{{ t('مبلغ تنخواه گردان (ریال)', 'Amount (Rial)') }} <span class="star">*</span></span><input v-model="form.amount" dir="ltr" inputmode="numeric" @blur="form.amount = amount(form.amount) ? money(amount(form.amount)) : ''" /></label>
        <label class="req"><span>{{ t('مبلغ حد واریز (ریال)', 'Settlement limit (Rial)') }} <span class="star">*</span></span><input v-model="form.settleLimit" dir="ltr" inputmode="numeric" @blur="form.settleLimit = amount(form.settleLimit) ? money(amount(form.settleLimit)) : ''" />
          <small class="muted">{{ t('کمتر از مبلغ تنخواه. وقتی جمع هزینه‌ها به این مبلغ رسید، تنخواه واریز و ترمیم می‌شود.', 'Below the amount. When expenses reach it, the petty cash is settled and topped up.') }}</small>
        </label>
        <label class="req"><span>{{ t('سقف پرداخت (ریال)', 'Payment ceiling (Rial)') }} <span class="star">*</span></span><input v-model="form.payCeiling" dir="ltr" inputmode="numeric" @blur="form.payCeiling = amount(form.payCeiling) ? money(amount(form.payCeiling)) : ''" />
          <small class="muted">{{ t('بیشترین مبلغ یک پرداخت؛ حداکثر برابر حد واریز.', 'Largest single payment; at most the settlement limit.') }}</small>
        </label>
        <label class="req"><span>{{ t('تاریخ ایجاد', 'Created on') }} <span class="star">*</span></span><input v-model="form.createdOn" dir="ltr" placeholder="1405/08/22" /></label>
        <div class="wide keeping">
          <span class="lbl req-lbl">{{ t('نحوهٔ نگهداری وجوه تنخواه توسط دارنده', 'How the holder keeps the money') }} <span class="star">*</span></span>
          <div class="keep-row">
            <label class="tr-check"><input v-model="form.keeping.bank" type="checkbox" /> {{ t('بانک', 'Bank') }}</label>
            <template v-if="form.keeping.bank">
              <label class="req"><span>{{ t('نام حساب', 'Account name') }}</span><input v-model="form.keeping.bankAccountName" /></label>
              <label class="req"><span>{{ t('شماره حساب', 'Account number') }}</span><input v-model="form.keeping.bankAccountNumber" dir="ltr" /></label>
            </template>
          </div>
          <div class="keep-row"><label class="tr-check"><input v-model="form.keeping.cash" type="checkbox" /> {{ t('نقد', 'Cash') }}</label></div>
          <div class="keep-row">
            <label class="tr-check"><input v-model="form.keeping.card" type="checkbox" /> {{ t('کارت بانک', 'Bank card') }}</label>
            <template v-if="form.keeping.card">
              <label class="req"><span>{{ t('عنوان دارندهٔ کارت', 'Card holder') }}</span><input v-model="form.keeping.cardHolderName" /></label>
              <label class="req"><span>{{ t('شماره کارت (۱۶ رقم)', 'Card number (16 digits)') }}</span><input v-model="form.keeping.cardNumber" dir="ltr" inputmode="numeric" maxlength="19" placeholder="6037-9900-0000-0000" /></label>
            </template>
          </div>
        </div>
        <div class="wide">
          <span class="lbl">{{ t('گروه بابت‌های مجاز', 'Allowed purpose groups') }}</span>
          <label class="tr-check"><input v-model="form.allGroups" type="radio" :value="true" /> {{ t('همهٔ گروه بابت‌های پرداخت', 'All payment purpose groups') }}</label>
          <label class="tr-check"><input v-model="form.allGroups" type="radio" :value="false" /> {{ t('فقط گروه‌های انتخاب‌شده', 'Only the chosen groups') }}</label>
          <template v-if="!form.allGroups">
            <SearchSelect :model-value="toAdd" :options="groupChoices" :aria-label="t('افزودن گروه بابت', 'Add a purpose group')" @update:model-value="addGroup" />
            <ul class="tr-chips">
              <li v-for="g in form.groupIds" :key="g">{{ groupName(g) }}<button type="button" :aria-label="t('حذف', 'Remove')" @click="form.groupIds = form.groupIds.filter(x => x !== g)">×</button></li>
            </ul>
          </template>
        </div>
        <div>
          <span class="lbl">{{ t('نحوهٔ ترمیم', 'Top-up') }}</span>
          <label class="tr-check"><input v-model="form.recovery" type="radio" value="Cash" /> {{ t('نقدی', 'Cash') }}</label>
          <label class="tr-check"><input v-model="form.recovery" type="radio" value="Cheque" /> {{ t('چک', 'Cheque') }}</label>
          <label class="tr-check"><input v-model="form.recovery" type="radio" value="BankCard" /> {{ t('کارت عابر بانک', 'Bank card') }}</label>
        </div>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy">{{ t('ذخیره', 'Save') }}</button>
        <button type="button" class="tr-link" @click="showForm = false">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <div v-if="funds.length" style="overflow-x:auto">
      <table class="tr-table">
        <thead><tr><th>{{ t('دارنده', 'Holder') }}</th><th>{{ t('معین', 'Account') }}</th><th>{{ t('مبلغ', 'Amount') }}</th><th>{{ t('حد واریز', 'Limit') }}</th><th>{{ t('سقف پرداخت', 'Ceiling') }}</th><th>{{ t('گروه بابت‌ها', 'Groups') }}</th><th>{{ t('ترمیم', 'Top-up') }}</th><th>{{ t('تاریخ ایجاد', 'Created') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th /></tr></thead>
        <tbody>
          <tr v-for="f in funds" :key="f.id" :class="{ inactive: !f.isActive }">
            <td>{{ f.holderName }} <small class="muted" dir="ltr">{{ f.holderDetailCode }}</small></td>
            <td><span dir="ltr">{{ f.ledgerAccountCode }}</span> <small class="muted">{{ ledgerName(f.ledgerAccountCode) }}</small></td>
            <td>{{ money(f.amount) }}</td><td>{{ money(f.settleLimit) }}</td><td>{{ money(f.payCeiling) }}</td>
            <td><template v-if="f.allowedPurposeGroupIds.length"><span v-for="g in f.allowedPurposeGroupIds" :key="g" class="tr-tag">{{ groupName(g) }}</span></template><template v-else>{{ t('همه', 'All') }}</template></td>
            <td>{{ t(...RECOVERY[f.recovery]) }}</td>
            <td dir="ltr">{{ f.createdOn ?? '' }}</td>
            <td>{{ f.isActive ? t('فعال', 'Active') : t('غیرفعال', 'Inactive') }}</td>
            <td class="tr-actions">
              <button type="button" class="tr-link" @click="startEdit(f)">{{ t('ویرایش', 'Edit') }}</button>
              <button type="button" class="tr-link" @click="toggle(f)">{{ f.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <p v-else-if="!showForm" class="muted">{{ t('هنوز تنخواه گردانی تعریف نشده است.', 'No petty cash yet.') }}</p>
  </div>
</template>

<style scoped src="../organization/organization.css"></style>
<style scoped>
.lbl { display: block; font-size: .85rem; margin-bottom: .25rem; }
.keeping { display: grid; gap: .5rem; }
.keep-row { display: flex; flex-wrap: wrap; gap: .5rem 1rem; align-items: end; }
.keep-row .tr-check { min-width: 7rem; }
</style>
