<script setup lang="ts">
/**
 * The payment request form (new-version manual «ب-صدور درخواست وجه»). Fields marked * in the manual are required (red frame).
 * The requester's department and cost centre come from Basic Information; the amount is also written in words; the receiving
 * account (transfer) or card number (card to card) appears only for its payment method. Opened with `editId`, it corrects a
 * request that is not yet approved. Ctrl+Enter saves.
 * Owner, 19 Nov 2026: with the payment method «ثبت انتقال (خرج چک)» the received cheque is chosen in place of «پرداخت از حساب بانکی» —
 * only cheques whose latest status is «ثبت صیادی شد» are offered — and its number, due date and the name of who gave it to the company
 * are shown; the amount of the request is the amount of the cheque.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { amountInWords, cardNumberHint, createApi, formatAmount, normaliseJalali, parseAmount, searchKey, toLatinDigits, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import AmountInput from '@treasury/ui-kit/AmountInput.vue'
import ComboInput from '@treasury/ui-kit/ComboInput.vue'
import ShebaInput from '@treasury/ui-kit/ShebaInput.vue'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { methods, useLookups, type Purpose, type TransferableCheque } from './lookups'
import { ACCEPT, openFile, sizeText, uploadFile, type RequestFile } from './files'

interface View {
  id: string; number: number; date: string; requesterId: string; beneficiaryId: string; purposeGroupId: string; purpose: string
  reference1: string | null; reference2: string | null; amount: number; currency: string; method: string; priority: number
  transferAccount: string | null; cardNumber: string | null; payFromId: string | null; dueDate: string | null; departmentId: string | null
  hasMainBeneficiaries: boolean; attachments: number; note: string | null; version: number; files: RequestFile[] | null
  cheque: { instrumentId: string; number: string; dueDate: string | null; payerName: string; bankName: string | null } | null
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
  dueDate: '', departmentId: '', attachments: '0', note: '', chequeId: '',
})
const form = reactive(empty())
const editing = ref<{ number: number; version: number } | null>(null)
/** Scans (report no. 4, item 12): already stored ones when correcting, chosen ones waiting to be sent with a new request. */
const storedFiles = ref<RequestFile[]>([])
const queued = ref<File[]>([])
function chooseFiles(e: Event) {
  const input = e.target as HTMLInputElement
  const chosen = Array.from(input.files ?? [])
  input.value = ''
  if (editing.value && props.editId) void sendFiles(props.editId, chosen)
  else queued.value.push(...chosen)
}
/** Sends scans one by one; each one changes the request's version. Returns the reasons of the refused ones. */
async function sendFiles(id: string, files: File[]): Promise<string[]> {
  const refused: string[] = []
  for (const f of files) {
    const version = editing.value?.version ?? 1
    const reason = await uploadFile(props.apiBase, props.token, id, version, f, props.locale)
    if (reason) refused.push(`${f.name}: ${reason}`)
    if (editing.value) await load(id)
    else if (!reason) currentVersion++
  }
  if (refused.length) show('error', refused.join('؛ '))
  return refused
}
let currentVersion = 1
/** A new request: its id is known only once it is saved; versions then count up from 1 with each scan. */
async function sendFilesForNew(id: string): Promise<string[]> {
  const refused: string[] = []
  for (const f of queued.value) {
    const reason = await uploadFile(props.apiBase, props.token, id, currentVersion, f, props.locale)
    if (reason) refused.push(`${f.name}: ${reason}`)
    else currentVersion++
  }
  return refused
}
async function removeStored(f: RequestFile) {
  if (!editing.value || !props.editId) return
  const r = await api.post<string>(`/api/requests/payment-requests/${props.editId}/files/${f.id}/delete`, { version: editing.value.version })
  if (!r.ok) return show('error', translate(r.code, props.locale))
  await load(props.editId)
}
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

// ---- «ثبت انتقال (خرج چک)»: the received cheques that are «ثبت صیادی شد» ----
const isTransfer = computed(() => form.method === 'ChequeTransfer')
const cheques = ref<TransferableCheque[]>([])
const chequesLoaded = ref(false)
async function loadCheques() {
  const r = await api.get<TransferableCheque[]>('/api/cashflow/instruments/received?state=SayadRegistered')
  // A cheque of a transfer between the company's own bank accounts (owner's report no. 7) is not handed on to a beneficiary: it goes
  // to its destination bank. cashflow-svc does not offer «ثبت انتقال» on it; it is left out here by its mark too.
  if (r.ok) cheques.value = r.value.filter(c => c.kind.endsWith('Cheque') && c.allowedActions.includes('Endorse') && !c.details?.transferId)
  else show('error', translate(r.code, props.locale))
  chequesLoaded.value = true
}
const cheque = computed(() => cheques.value.find(c => c.id === form.chequeId))
/** The cheque number first (it is what the list box shows once chosen), then bank, giver, amount and due date. */
const chequeChoices = computed<Choice[]>(() => cheques.value.map(c => ({
  value: c.id, hint: c.number ?? '',
  label: `${c.bankName ?? ''} – ${c.counterparty} – ${formatAmount(c.amount, props.locale)}${c.currency === 'IRR' ? '' : ' ' + c.currency} – ${t('سررسید', 'due')} ${c.dueDate ?? ''}`,
})))
watch(isTransfer, on => { if (on && !chequesLoaded.value) void loadCheques() }, { immediate: true })
// "The amount of the cheque is the amount of the request": the amount and currency follow the chosen cheque.
watch(cheque, c => { if (c && isTransfer.value) Object.assign(form, { amount: String(c.amount), currency: c.currency }) })

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
    departmentId: v.departmentId ?? '', attachments: String(v.attachments), note: v.note ?? '', chequeId: v.cheque?.instrumentId ?? '',
  })
  editing.value = { number: v.number, version: v.version }
  storedFiles.value = v.files ?? []
}

/** A purpose typed here that the group does not list yet is added to it (decision of 13 Nov 2026). */
async function addPurposeIfNew(): Promise<boolean> {
  const name = form.purpose.trim()
  if (!name || !form.purposeGroupId || purposes.value.some(p => searchKey(p.name) === searchKey(name))) return true
  const r = await api.post<string>('/api/reference/purposes', { groupId: form.purposeGroupId, name })
  if (!r.ok && r.code === 'Purpose.NameTaken') return true
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
  if (isTransfer.value && !form.chequeId) return show('error', translate('PaymentRequest.ChequeRequired', props.locale))
  const input = {
    date, requesterId: form.requesterId, beneficiaryId: form.beneficiaryId, purposeGroupId: form.purposeGroupId, purpose: form.purpose.trim(),
    amount: amount.value, method: form.method, currency: form.currency, priority: Number(form.priority), reference1: form.ref1 || null,
    reference2: form.ref2 || null, transferAccount: toLatinDigits(form.transferAccount.trim()) || null, cardNumber: toLatinDigits(form.cardNumber.trim()) || null,
    payFromId: isTransfer.value ? null : form.payFromId || null, dueDate: !isTransfer.value && form.dueDate ? normaliseJalali(form.dueDate) : null,
    chequeId: isTransfer.value ? form.chequeId : null, departmentId: form.departmentId || null,
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
      currentVersion = 1
      const sent = queued.value.length ? await sendFilesForNew(r.value) : []
      const v = await api.get<View>(`/api/requests/payment-requests/${r.value}`)
      const no = v.ok ? (fa.value ? v.value.number.toLocaleString('fa-IR') : String(v.value.number)) : ''
      const saved = t(`درخواست وجه شمارهٔ ${no} ثبت شد و منتظر تایید است.`, `Payment request no. ${no} saved; it waits for approval.`)
      if (sent.length) show('error', `${saved} ${t('الصاق نشد', 'Not attached')}: ${sent.join('؛ ')}`)
      else show('ok', saved + (v.ok && v.value.files?.length ? t(` ${v.value.files.length.toLocaleString('fa-IR')} تصویر ضمیمه الصاق شد.`, ` ${v.value.files.length} scan(s) attached.`) : ''))
      queued.value = []
      Object.assign(form, { ...empty(), requesterId: form.requesterId, date: form.date })
      chequesLoaded.value = false
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
      <!-- «ثبت انتقال (خرج چک)»: the amount and currency are the cheque's (owner, 19 Nov 2026). -->
      <label v-if="isTransfer" class="req">{{ t('مبلغ (برابر مبلغ چک)', 'Amount (the cheque\'s)') }}
        <input class="ro" :value="amount ? formatAmount(amount, locale) : ''" readonly dir="ltr" :placeholder="t('چک را در «نحوهٔ پرداخت» انتخاب کنید', 'Choose the cheque below')" />
        <small v-if="words" class="muted">{{ words }}</small>
      </label>
      <label v-else class="req">{{ t('مبلغ', 'Amount') }} <AmountInput v-model="form.amount" :decimals="form.currency === 'IRR' ? 0 : 2" :unit="currencyName" /></label>
      <label v-if="isTransfer">{{ t('ارز', 'Currency') }} <input class="ro" :value="currencyName" readonly /></label>
      <label v-else>{{ t('ارز', 'Currency') }}
        <SearchSelect v-model="form.currency" :options="currencyChoices" required :aria-label="t('ارز', 'Currency')" />
      </label>
    </fieldset>

    <fieldset>
      <legend>{{ t('بابت', 'Purpose') }}</legend>
      <label>{{ t('گروه بابت', 'Purpose group') }}
        <SearchSelect v-model="form.purposeGroupId" :options="groupChoices" required :aria-label="t('گروه بابت', 'Purpose group')"
                      :none-text="t('گروه بابت پرداختی تعریف نشده است', 'No payment purpose group')" />
      </label>
      <label class="wide req">{{ t('بابت', 'Purpose') }}
        <ComboInput v-model="form.purpose" :options="purposes.filter(x => x.isActive).map(x => x.name)" required :maxlength="150" :aria-label="t('بابت', 'Purpose')"
                    :placeholder="t('از فهرست انتخاب کنید، آن را تغییر دهید یا بنویسید', 'Choose from the list, change it, or type')"
                    :none-text="form.purposeGroupId ? t('این گروه هنوز بابتی ندارد؛ بابت را بنویسید', 'No purpose in this group yet; type one') : t('ابتدا گروه بابت را انتخاب کنید', 'Choose the purpose group first')"
                    :new-hint="form.purposeGroupId ? t('بابت تازه: با ثبت درخواست به فهرست بابت‌های این گروه اضافه می‌شود.', 'New purpose: saving the request adds it to this group\'s list.') : ''" />
      </label>
      <label v-if="group?.reference1Id">{{ refTitle(group.reference1Id) ?? t('عطف ۱', 'Reference 1') }} <input v-model="form.ref1" /></label>
      <label v-if="group?.reference2Id">{{ refTitle(group.reference2Id) ?? t('عطف ۲', 'Reference 2') }} <input v-model="form.ref2" /></label>
      <!-- Owner's report no. 4, item 11: titled «واحد سازمانی درخواست کننده». -->
      <label v-if="departmentChoices.length">{{ t('واحد سازمانی درخواست کننده', 'Requesting department') }}
        <SearchSelect v-model="form.departmentId" :options="departmentChoices" :empty-label="t('— ندارد —', '— none —')" :aria-label="t('واحد سازمانی درخواست کننده', 'Requesting department')" />
      </label>
    </fieldset>

    <fieldset>
      <legend>{{ t('نحوهٔ پرداخت', 'How to pay') }}</legend>
      <label>{{ t('نوع پرداخت', 'Payment method') }}
        <SearchSelect v-model="form.method" :options="methodChoices" required :aria-label="t('نوع پرداخت', 'Payment method')" />
      </label>
      <label v-if="form.method === 'Transfer'" class="wide req">{{ t('انتقال به حساب بانکی (شبا)', 'Transfer to account (Sheba)') }}
        <!-- Owner's report no. 6, item 16: «IR» is fixed in the box; the 24 digits are typed after it. -->
        <ShebaInput v-model="form.transferAccount" :locale="locale" :aria-label="t('شماره شبا', 'Sheba')" />
      </label>
      <label v-if="form.method === 'CardToCard'" class="req">{{ t('انتقال به شماره کارت', 'To card number') }}
        <input v-model="form.cardNumber" dir="ltr" maxlength="19" inputmode="numeric" />
        <small v-if="cardNumberHint(form.cardNumber, locale)" :class="cardNumberHint(form.cardNumber, locale)!.bad ? 'tr-card-bad' : 'muted'">{{ cardNumberHint(form.cardNumber, locale)!.text }}</small>
      </label>
      <!-- Owner, 19 Nov 2026: «ثبت انتقال (خرج چک)» shows the cheque's number, due date and who gave it, in place of «پرداخت از حساب بانکی». -->
      <template v-if="isTransfer">
        <label class="wide">{{ t('چک دریافتی (فقط چک‌های «ثبت صیادی شد»)', 'Received cheque (only «ثبت صیادی شد»)') }}
          <SearchSelect v-model="form.chequeId" :options="chequeChoices" required :aria-label="t('چک دریافتی', 'Received cheque')"
                        :none-text="t('چکی با وضعیت «ثبت صیادی شد» نزد صندوق نیست؛ ابتدا در «دریافت‌ها ← عملیات چک‌های دریافتی» چک را ثبت صیادی کنید',
                          'No cheque with the status «ثبت صیادی شد»; register the cheque in Sayad on the cheque operations screen first')" />
        </label>
        <label>{{ t('شماره چک', 'Cheque no.') }} <input class="ro" :value="cheque?.number ?? ''" readonly dir="ltr" /></label>
        <label>{{ t('تاریخ سررسید چک', 'Cheque due date') }} <input class="ro" :value="cheque?.dueDate ?? ''" readonly dir="ltr" /></label>
        <label class="wide">{{ t('نام واگذارنده چک', 'Cheque given by') }} <input class="ro" :value="cheque?.counterparty ?? ''" readonly /></label>
        <label v-if="cheque?.details?.sayadId">{{ t('شماره صیادی چک', 'Sayad number') }} <input class="ro" :value="cheque.details.sayadId" readonly dir="ltr" /></label>
      </template>
      <template v-else>
        <label class="wide">{{ form.method === 'Cash' ? t('پرداخت از صندوق', 'Pay from cash desk') : t('پرداخت از حساب بانکی', 'Pay from bank account') }}
          <SearchSelect v-model="form.payFromId" :options="payFromChoices" :empty-label="t('— خزانه تعیین می‌کند —', '— treasury decides —')"
                        :aria-label="t('پرداخت از', 'Pay from')" />
        </label>
        <label :class="{ req: form.method === 'PostDatedCheque' }">{{ t('سررسید', 'Due date') }} <input v-model="form.dueDate" placeholder="1405/09/30" dir="ltr" /></label>
      </template>
      <label>{{ t('تعداد ضمائم', 'Attachments') }} <input v-model="form.attachments" inputmode="numeric" dir="ltr" /></label>
      <label class="full">{{ t('توضیحات', 'Note') }} <input v-model="form.note" maxlength="500" /></label>
    </fieldset>

    <fieldset>
      <legend>{{ t('تصویر اسکن ضمائم', 'Scanned attachments') }}</legend>
      <label class="full">{{ t('افزودن فایل (PDF، JPG یا PNG، حداکثر ۵ مگابایت)', 'Add files (PDF, JPG or PNG, up to 5 MB)') }}
        <input type="file" :accept="ACCEPT" multiple @change="chooseFiles" />
      </label>
      <ul v-if="storedFiles.length || queued.length" class="tr-files full">
        <li v-for="f in storedFiles" :key="f.id">
          <button type="button" class="tr-link" @click="openFile(apiBase, token, editId!, f.id)">{{ f.fileName }}</button>
          <span class="muted small">{{ sizeText(f.size, fa) }}</span>
          <button type="button" class="tr-link danger" @click="removeStored(f)">{{ t('حذف', 'Remove') }}</button>
        </li>
        <li v-for="(f, i) in queued" :key="'q' + i">
          {{ f.name }} <span class="muted small">{{ sizeText(f.size, fa) }} – {{ t('پس از ثبت الصاق می‌شود', 'sent when the request is saved') }}</span>
          <button type="button" class="tr-link danger" @click="queued.splice(i, 1)">{{ t('حذف', 'Remove') }}</button>
        </li>
      </ul>
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
.tr-card-bad { color: #b42318; font-size: .78rem; }
.full { grid-column: 1 / -1; }
.tr-files { list-style: none; margin: 0; padding: 0; display: grid; gap: .25rem; }
.tr-files li { display: flex; gap: .5rem; align-items: center; }
</style>
