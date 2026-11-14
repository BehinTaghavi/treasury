<script setup lang="ts">
/**
 * The payment request form (new-version manual «ب-صدور درخواست وجه»). Fields marked * in the manual are required (red frame).
 * The requester's department and cost centre come from Basic Information; the amount is also written in words; the receiving
 * account (transfer) or card number (card to card) appears only for its payment method. Opened with `editId`, it corrects a
 * request that is not yet approved. Ctrl+Enter saves.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { amountInWords, createApi, formatAmount, normaliseJalali, parseAmount, toLatinDigits, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { methods, useLookups, type Purpose } from './lookups'

interface View {
  id: string; number: number; date: string; requesterId: string; beneficiaryId: string; purposeGroupId: string; purpose: string
  reference1: string | null; reference2: string | null; amount: number; currency: string; method: string; priority: number
  transferAccount: string | null; cardNumber: string | null; payFromId: string | null; dueDate: string | null; departmentId: string | null
  hasMainBeneficiaries: boolean; attachments: number; note: string | null; version: number
}

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; editId?: string | null }>(),
  { apiBase: '', locale: 'fa', token: undefined, editId: null })
const emit = defineEmits<{ saved: [id: string]; closed: [] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const L = useLookups(api)

const empty = () => ({
  date: todayJalali(), requesterId: '', beneficiaryId: '', hasMainBeneficiaries: false, priority: '1', amount: '', currency: 'IRR',
  purposeGroupId: '', purpose: '', ref1: '', ref2: '', method: 'CurrentCheque', transferAccount: '', cardNumber: '', payFromId: '',
  dueDate: '', departmentId: '', attachments: '0', note: '',
})
const form = reactive(empty())
const editing = ref<{ number: number; version: number } | null>(null)
const purposes = ref<Purpose[]>([])

const requester = computed(() => L.requesters.value.find(r => r.id === form.requesterId))
const beneficiary = computed(() => L.beneficiaries.value.find(b => b.id === form.beneficiaryId))
const group = computed(() => L.groups.value.find(g => g.id === form.purposeGroupId))
const refTitle = (id: string | null | undefined) => L.references.value.find(r => r.id === id)?.name
const departmentName = (id: string | null | undefined) => L.departments.value.find(d => d.id === id)?.name ?? ''

const requesterChoices = computed<Choice[]>(() => L.requesters.value.filter(r => r.isActive || r.id === form.requesterId).map(r => ({ value: r.id, label: r.fullName, hint: String(r.number) })))
const beneficiaryChoices = computed<Choice[]>(() => L.beneficiaries.value.filter(b => b.isActive || b.id === form.beneficiaryId).map(b => ({ value: b.id, label: b.legalName, hint: b.detailCode })))
const groupChoices = computed<Choice[]>(() => L.groups.value.filter(g => g.direction === 'Payment' && (g.isActive || g.id === form.purposeGroupId)).map(g => ({ value: g.id, label: g.name, hint: String(g.number) })))
const currencyChoices = computed<Choice[]>(() => L.currencies.value.filter(c => c.code).map(c => ({ value: c.code!, label: c.name, hint: c.code! })))
const methodChoices = computed<Choice[]>(() => methods.map(m => ({ value: m.value, label: fa.value ? m.fa : m.en })))
const priorityChoices = computed<Choice[]>(() => [['1', 'عادی', 'normal'], ['2', 'فوری', 'urgent'], ['3', 'خیلی فوری', 'very urgent']].map(([v, f, e]) => ({ value: v!, label: `${fa.value ? Number(v).toLocaleString('fa-IR') : v} – ${fa.value ? f : e}` })))
const departmentChoices = computed<Choice[]>(() => L.departments.value.filter(d => d.isActive).map(d => ({ value: d.id, label: d.name })))
/** Cash is paid from a cash desk, everything else from a bank account, in the request's currency. */
const payFromChoices = computed<Choice[]>(() => form.method === 'Cash'
  ? L.desks.value.filter(d => d.isActive && d.currencyCode === form.currency).map(d => ({ value: d.id, label: d.name }))
  : L.bankAccounts.value.filter(a => a.isActive && a.currencyCode === form.currency).map(a => ({ value: a.id, label: L.accountTitle(a) })))

const amount = computed(() => parseAmount(form.amount))
const currencyName = computed(() => L.currencies.value.find(c => c.code === form.currency)?.name ?? form.currency)
const words = computed(() => (amount.value ? `${amountInWords(amount.value)} ${currencyName.value}` : ''))

watch(() => form.purposeGroupId, async id => {
  purposes.value = []
  if (!id) return
  const r = await api.get<Purpose[]>(`/api/reference/purposes?groupId=${id}`)
  if (r.ok) purposes.value = r.value
})
// Transfer: the beneficiary's Sheba is offered (manual: «انتقال به حساب بانکی»); the account paid from must fit the new method.
watch(() => [form.method, form.beneficiaryId], () => {
  if (form.method === 'Transfer' && !form.transferAccount && beneficiary.value?.sheba) form.transferAccount = beneficiary.value.sheba
})
watch(() => [form.method, form.currency], () => { if (form.payFromId && !payFromChoices.value.some(c => c.value === form.payFromId)) form.payFromId = '' })

const saving = ref(false)
const message = ref<{ kind: 'error' | 'ok'; text: string } | null>(null)
const show = (kind: 'error' | 'ok', text: string) => { message.value = { kind, text } }

async function load(id: string) {
  const r = await api.get<View>(`/api/requests/payment-requests/${id}`)
  if (!r.ok) return show('error', translate(r.code, props.locale))
  const v = r.value
  Object.assign(form, {
    date: v.date, requesterId: v.requesterId, beneficiaryId: v.beneficiaryId, hasMainBeneficiaries: v.hasMainBeneficiaries, priority: String(v.priority),
    amount: String(v.amount), currency: v.currency, purposeGroupId: v.purposeGroupId, purpose: v.purpose, ref1: v.reference1 ?? '', ref2: v.reference2 ?? '',
    method: v.method, transferAccount: v.transferAccount ?? '', cardNumber: v.cardNumber ?? '', payFromId: v.payFromId ?? '', dueDate: v.dueDate ?? '',
    departmentId: v.departmentId ?? '', attachments: String(v.attachments), note: v.note ?? '',
  })
  editing.value = { number: v.number, version: v.version }
}

/** A purpose typed here that the group does not list yet is added to it (decision of 13 Nov 2026). */
async function addPurposeIfNew(): Promise<boolean> {
  const name = form.purpose.trim()
  if (!name || !form.purposeGroupId || purposes.value.some(p => p.name === name)) return true
  const r = await api.post<string>('/api/reference/purposes', { groupId: form.purposeGroupId, name })
  if (!r.ok) { show('error', translate(r.code, props.locale)); return false }
  purposes.value.push({ id: r.value, groupId: form.purposeGroupId, name, isActive: true })
  return true
}

async function save() {
  message.value = null
  const date = normaliseJalali(form.date)
  if (!date || (form.dueDate && !normaliseJalali(form.dueDate))) return show('error', translate('FiscalDate.Invalid', props.locale))
  if (!form.requesterId) return show('error', translate('PaymentRequest.RequesterRequired', props.locale))
  if (!form.beneficiaryId) return show('error', translate('PaymentRequest.BeneficiaryRequired', props.locale))
  if (!amount.value) return show('error', translate('PaymentRequest.AmountRequired', props.locale))
  if (!form.purposeGroupId) return show('error', translate('PaymentRequest.PurposeGroupRequired', props.locale))
  if (!form.purpose.trim()) return show('error', translate('PaymentRequest.PurposeRequired', props.locale))
  const input = {
    date, requesterId: form.requesterId, beneficiaryId: form.beneficiaryId, purposeGroupId: form.purposeGroupId, purpose: form.purpose.trim(),
    amount: amount.value, method: form.method, currency: form.currency, priority: Number(form.priority), reference1: form.ref1 || null,
    reference2: form.ref2 || null, transferAccount: toLatinDigits(form.transferAccount.trim()) || null, cardNumber: toLatinDigits(form.cardNumber.trim()) || null,
    payFromId: form.payFromId || null, dueDate: form.dueDate ? normaliseJalali(form.dueDate) : null, departmentId: form.departmentId || null,
    hasMainBeneficiaries: form.hasMainBeneficiaries, attachments: Number(toLatinDigits(form.attachments) || 0), note: form.note || null,
  }
  saving.value = true
  try {
    if (!(await addPurposeIfNew())) return
    const r = editing.value && props.editId
      ? await api.put<string>(`/api/requests/payment-requests/${props.editId}`, { input, version: editing.value.version })
      : await api.post<string>('/api/requests/payment-requests', input)
    if (!r.ok) return show('error', translate(r.code, props.locale))
    if (editing.value) {
      show('ok', t(`درخواست شمارهٔ ${editing.value.number.toLocaleString('fa-IR')} اصلاح شد.`, `Request no. ${editing.value.number} corrected.`))
      await load(r.value)
    } else {
      const v = await api.get<View>(`/api/requests/payment-requests/${r.value}`)
      const no = v.ok ? (fa.value ? v.value.number.toLocaleString('fa-IR') : String(v.value.number)) : ''
      show('ok', t(`درخواست وجه شمارهٔ ${no} ثبت شد و منتظر تایید است.`, `Payment request no. ${no} saved; it waits for approval.`))
      Object.assign(form, { ...empty(), requesterId: form.requesterId, date: form.date })
    }
    emit('saved', r.value)
  } finally {
    saving.value = false
  }
}

function onKey(e: KeyboardEvent) {
  if (e.key === 'Enter' && e.ctrlKey) { e.preventDefault(); void save() }
}

onMounted(async () => {
  await L.load()
  if (props.editId) await load(props.editId)
})
</script>

<template>
  <form class="tr-form" @keydown="onKey" @submit.prevent="save">
    <p v-if="editing" class="tr-msg ok">{{ t(`اصلاح درخواست شمارهٔ ${editing.number.toLocaleString('fa-IR')}`, `Correcting request no. ${editing.number}`) }}</p>
    <fieldset>
      <legend>{{ t('درخواست کننده', 'Requester') }}</legend>
      <label class="wide">{{ t('نام درخواست کننده', 'Requester') }}
        <SearchSelect v-model="form.requesterId" :options="requesterChoices" required :aria-label="t('درخواست کننده', 'Requester')"
                      :none-text="t('درخواست کننده‌ای در اطلاعات پایه تعریف نشده است', 'No requester in Basic Information')" />
      </label>
      <label>{{ t('واحد درخواست کننده', 'Department') }} <input class="ro" :value="departmentName(requester?.departmentId)" readonly /></label>
      <label>{{ t('مرکز هزینه', 'Cost centre') }} <input class="ro" :value="requester?.costCenterCode ?? ''" readonly dir="ltr" /></label>
      <label class="req">{{ t('تاریخ درخواست', 'Date') }} <input v-model="form.date" placeholder="1405/08/24" dir="ltr" /></label>
      <label>{{ t('اولویت', 'Priority') }}
        <SearchSelect v-model="form.priority" :options="priorityChoices" required :aria-label="t('اولویت', 'Priority')" />
      </label>
    </fieldset>

    <fieldset>
      <legend>{{ t('ذینفع و مبلغ', 'Beneficiary and amount') }}</legend>
      <label class="wide">{{ t('ذینفع', 'Beneficiary') }}
        <SearchSelect v-model="form.beneficiaryId" :options="beneficiaryChoices" required :aria-label="t('ذینفع', 'Beneficiary')"
                      :none-text="t('ذینفعی پیدا نشد؛ ابتدا در اطلاعات پایه تعریف کنید', 'Not found; define it in Basic Information first')" />
      </label>
      <label class="tr-check"><input v-model="form.hasMainBeneficiaries" type="checkbox" /> {{ t('پرداخت جمعی دارد (ذینفعان اصلی دارد)', 'Paid on to main beneficiaries') }}</label>
      <label class="req">{{ t('مبلغ', 'Amount') }} <input v-model="form.amount" inputmode="decimal" dir="ltr" /></label>
      <label>{{ t('ارز', 'Currency') }}
        <SearchSelect v-model="form.currency" :options="currencyChoices" required :aria-label="t('ارز', 'Currency')" />
      </label>
      <label class="full">{{ t('مبلغ به حروف', 'Amount in words') }} <input class="ro" :value="words" readonly /></label>
    </fieldset>

    <fieldset>
      <legend>{{ t('بابت', 'Purpose') }}</legend>
      <label>{{ t('گروه بابت', 'Purpose group') }}
        <SearchSelect v-model="form.purposeGroupId" :options="groupChoices" required :aria-label="t('گروه بابت', 'Purpose group')"
                      :none-text="t('گروه بابت پرداختی تعریف نشده است', 'No payment purpose group')" />
      </label>
      <label class="wide req">{{ t('بابت', 'Purpose') }}
        <input v-model="form.purpose" list="tr-req-purposes" maxlength="300" :placeholder="t('از فهرست انتخاب کنید یا بنویسید', 'Choose or type')" />
        <datalist id="tr-req-purposes"><option v-for="p in purposes.filter(x => x.isActive)" :key="p.id" :value="p.name" /></datalist>
      </label>
      <label v-if="group?.reference1Id">{{ refTitle(group.reference1Id) ?? t('عطف ۱', 'Reference 1') }} <input v-model="form.ref1" /></label>
      <label v-if="group?.reference2Id">{{ refTitle(group.reference2Id) ?? t('عطف ۲', 'Reference 2') }} <input v-model="form.ref2" /></label>
      <label v-if="departmentChoices.length">{{ t('بخش', 'Department') }}
        <SearchSelect v-model="form.departmentId" :options="departmentChoices" :empty-label="t('— ندارد —', '— none —')" :aria-label="t('بخش', 'Department')" />
      </label>
    </fieldset>

    <fieldset>
      <legend>{{ t('نحوهٔ پرداخت', 'How to pay') }}</legend>
      <label>{{ t('نوع پرداخت', 'Payment method') }}
        <SearchSelect v-model="form.method" :options="methodChoices" required :aria-label="t('نوع پرداخت', 'Payment method')" />
      </label>
      <label v-if="form.method === 'Transfer'" class="wide req">{{ t('انتقال به حساب بانکی (شبا)', 'Transfer to account (Sheba)') }}
        <input v-model="form.transferAccount" dir="ltr" placeholder="IR..." />
      </label>
      <label v-if="form.method === 'CardToCard'" class="req">{{ t('انتقال به شماره کارت', 'To card number') }}
        <input v-model="form.cardNumber" dir="ltr" maxlength="19" inputmode="numeric" />
      </label>
      <label class="wide">{{ form.method === 'Cash' ? t('پرداخت از صندوق', 'Pay from cash desk') : t('پرداخت از حساب بانکی', 'Pay from bank account') }}
        <SearchSelect v-model="form.payFromId" :options="payFromChoices" :empty-label="t('— خزانه تعیین می‌کند —', '— treasury decides —')"
                      :aria-label="t('پرداخت از', 'Pay from')" />
      </label>
      <label :class="{ req: form.method === 'PostDatedCheque' }">{{ t('سررسید', 'Due date') }} <input v-model="form.dueDate" placeholder="1405/09/30" dir="ltr" /></label>
      <label>{{ t('تعداد ضمائم', 'Attachments') }} <input v-model="form.attachments" inputmode="numeric" dir="ltr" /></label>
      <label class="full">{{ t('توضیحات', 'Note') }} <input v-model="form.note" maxlength="500" /></label>
    </fieldset>

    <div class="tr-buttons">
      <button type="submit" :disabled="saving">{{ editing ? t('ذخیرهٔ اصلاح (Ctrl+Enter)', 'Save correction (Ctrl+Enter)') : t('ثبت درخواست (Ctrl+Enter)', 'Save request (Ctrl+Enter)') }}</button>
      <button v-if="editing" type="button" class="tr-secondary" @click="emit('closed')">{{ t('بازگشت به فهرست', 'Back to the list') }}</button>
    </div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
  </form>
</template>

<style scoped src="./requests.css"></style>
<style scoped>
.full { grid-column: 1 / -1; }
</style>
