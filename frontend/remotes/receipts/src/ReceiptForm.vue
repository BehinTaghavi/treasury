<script setup lang="ts">
/**
 * One receipt form for the three kinds (owner, 14 Nov 2026): cash into a cash desk (old D_Naghd), money paid into a bank account by
 * deposit slip, transfer, POS or card-to-card (old D_variz), and cheques and promissory notes from one payer (old D_Check with
 * «ثبت چک بعدی»). Current or post-dated is decided by the due date; Rial or foreign currency by the account or by each cheque.
 * Ctrl+Enter saves. Errors are shown from the API's codes, translated.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, formatAmount, normaliseJalali, parseAmount, toLatinDigits, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups, type Detail, type Purpose } from './lookups'

type Kind = 'Cash' | 'BankDeposit' | 'Instruments'
type LineKind = 'OrdinaryCheque' | 'BankCheque' | 'GuaranteeCheque' | 'PromissoryNote' | 'GuaranteeNote'
interface Line {
  key: number; kind: LineKind; number: string; dueDate: string; currency: string; amount: string; rate: string
  bankId: string; branchName: string; branchCode: string; accountNumber: string; accountHolder: string; city: string; outOfTown: boolean; sayadId: string
  g1Name: string; g1Code: string; g2Name: string; g2Code: string
}

const props = withDefaults(defineProps<{ kind: Kind; apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const emit = defineEmits<{ saved: [id: string] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const L = useLookups(api)

const lineKinds: { value: LineKind; fa: string; en: string }[] = [
  { value: 'OrdinaryCheque', fa: 'چک', en: 'Cheque' },
  { value: 'BankCheque', fa: 'چک بانکی (رمزدار)', en: 'Bank cheque' },
  { value: 'GuaranteeCheque', fa: 'چک تضمینی (انتظامی)', en: 'Guarantee cheque' },
  { value: 'PromissoryNote', fa: 'سفته', en: 'Promissory note' },
  { value: 'GuaranteeNote', fa: 'سفته تضمینی (انتظامی)', en: 'Guarantee note' },
]
const methods = [
  { value: 'DepositSlip', fa: 'فیش واریزی', en: 'Deposit slip' },
  { value: 'Transfer', fa: 'حواله / انتقال (ساتنا، پایا)', en: 'Transfer (Satna, Paya)' },
  { value: 'Pos', fa: 'کارتخوان (POS)', en: 'Card reader (POS)' },
  { value: 'CardToCard', fa: 'کارت به کارت', en: 'Card to card' },
]
const isCheque = (k: LineKind) => k.endsWith('Cheque')
const isGuarantee = (k: LineKind) => k.startsWith('Guarantee')

// ---- header (every kind) ----
const header = reactive({ date: todayJalali(), payer: '', groupId: '', purpose: '', ref1: '', ref2: '', departmentId: '', salesProceeds: false, note: '' })
const payers = ref<Detail[]>([])
/** Owner, 14 Nov 2026 (answer to Q22): payers are customers and persons or companies. */
const PayerRoles = 'Customer,PersonOrCompany'
const purposes = ref<Purpose[]>([])
const group = computed(() => L.groups.value.find(g => g.id === header.groupId))
const refTitle = (id: string | null | undefined) => L.references.value.find(r => r.id === id)?.name
const groupChoices = computed<Choice[]>(() => L.groups.value.filter(g => g.direction === 'Receipt' && g.isActive).map(g => ({ value: g.id, label: g.name, hint: String(g.number) })))
const payerChoices = computed<Choice[]>(() => payers.value.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive })))
const departmentChoices = computed<Choice[]>(() => L.departments.value.filter(d => d.isActive).map(d => ({ value: d.id, label: d.name })))
let searchTimer: ReturnType<typeof setTimeout> | undefined
function onPayerSearch(text: string) {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(async () => { payers.value = await L.searchDetails(text, header.payer, PayerRoles) }, 250)
}
watch(() => header.groupId, async id => {
  purposes.value = []
  if (!id) return
  const r = await api.get<Purpose[]>(`/api/reference/purposes?groupId=${id}`)
  if (r.ok) purposes.value = r.value
})

// ---- cash and bank deposit ----
const money1 = reactive({ accountId: '', method: 'DepositSlip', tracking: '', amount: '', rate: '' })
const deskChoices = computed<Choice[]>(() => L.desks.value.filter(d => d.isActive).map(d => ({ value: d.id, label: d.name, hint: d.currencyCode })))
const bankAccountChoices = computed<Choice[]>(() => L.bankAccounts.value.filter(a => a.isActive).map(a => ({ value: a.id, label: L.accountTitle(a), hint: a.currencyCode })))
const accountCurrency = computed(() =>
  (props.kind === 'Cash' ? L.desks.value.find(d => d.id === money1.accountId)?.currencyCode : L.bankAccounts.value.find(a => a.id === money1.accountId)?.currencyCode) ?? 'IRR')
const rialOf = (amount: string, currency: string, rate: string) => {
  const a = parseAmount(amount)
  if (a === null) return null
  if (currency === 'IRR') return a
  const r = parseAmount(rate)
  return r === null ? null : Math.round(a * r)
}

// ---- cheques and notes ----
let nextKey = 1
const blankLine = (): Line => ({
  key: nextKey++, kind: 'OrdinaryCheque', number: '', dueDate: '', currency: 'IRR', amount: '', rate: '', bankId: '', branchName: '', branchCode: '',
  accountNumber: '', accountHolder: '', city: '', outOfTown: false, sayadId: '', g1Name: '', g1Code: '', g2Name: '', g2Code: '',
})
const lines = ref<Line[]>([blankLine()])
const bankChoices = computed<Choice[]>(() => L.banks.value.filter(b => b.isActive !== false).map(b => ({ value: b.id, label: b.name })))
const currencyChoices = computed<Choice[]>(() => L.currencies.value.filter(c => c.code).map(c => ({ value: c.code!, label: c.name, hint: c.code! })))
const cityChoices = computed<Choice[]>(() => L.cities.value.map(c => ({ value: c.name, label: c.name })))
const kindChoices = computed<Choice[]>(() => lineKinds.map(k => ({ value: k.value, label: fa.value ? k.fa : k.en })))

/** «ثبت چک بعدی»: the next line starts as a copy of the last one (same bank, account, due date…), with the next cheque number. */
function nextLine() {
  const last = lines.value[lines.value.length - 1]
  if (!last) return void lines.value.push(blankLine())
  const digits = toLatinDigits(last.number)
  const next = /^\d+$/.test(digits) ? String(Number(digits) + 1).padStart(digits.length, '0') : ''
  lines.value.push({ ...last, key: nextKey++, number: next, sayadId: '' })
}
function removeLine(i: number) {
  lines.value.splice(i, 1)
  if (!lines.value.length) lines.value.push(blankLine())
}
/** Current (روز) when due on or before the receipt date; post-dated (مدت‌دار) after it. Manual: «چک روز … به صورت اتوماتیک تیک می خورد». */
function timing(l: Line): 'current' | 'later' | null {
  const due = normaliseJalali(l.dueDate)
  const date = normaliseJalali(header.date)
  if (!due || !date) return null
  return due > date ? 'later' : 'current'
}
const linesRial = computed(() => lines.value.map(l => rialOf(l.amount, l.currency, l.rate)))
const total = computed(() => props.kind === 'Instruments'
  ? linesRial.value.reduce<number>((s, v) => s + (v ?? 0), 0)
  : rialOf(money1.amount, accountCurrency.value, money1.rate) ?? 0)

// ---- save ----
const saving = ref(false)
const message = ref<{ kind: 'error' | 'warning' | 'ok'; text: string } | null>(null)
const show = (kind: 'error' | 'warning' | 'ok', text: string) => { message.value = { kind, text } }

/** A purpose typed here that the group does not list yet is added to it (decision of 10 Nov 2026). */
async function addPurposeIfNew(): Promise<boolean> {
  const name = header.purpose.trim()
  if (!name || !header.groupId || purposes.value.some(p => p.name === name)) return true
  const r = await api.post<string>('/api/reference/purposes', { groupId: header.groupId, name })
  if (!r.ok) { show('error', translate(r.code, props.locale)); return false }
  purposes.value.push({ id: r.value, groupId: header.groupId, name, isActive: true })
  return true
}

function headerBody() {
  return {
    date: normaliseJalali(header.date) ?? header.date, payerDetailCode: header.payer, purposeGroupId: header.groupId || '00000000-0000-0000-0000-000000000000',
    purpose: header.purpose.trim(), reference1: header.ref1 || null, reference2: header.ref2 || null, departmentId: header.departmentId || null,
    salesProceeds: header.salesProceeds, note: header.note || null,
  }
}

function lineBody(l: Line) {
  const guarantors = [[l.g1Name, l.g1Code], [l.g2Name, l.g2Code]].filter(([n, c]) => n!.trim() || c!.trim())
    .map(([n, c]) => ({ name: n!.trim(), nationalCode: toLatinDigits(c!.trim()) || null }))
  return {
    kind: l.kind, number: toLatinDigits(l.number.trim()), dueDate: isGuarantee(l.kind) && !l.dueDate ? null : normaliseJalali(l.dueDate) ?? l.dueDate,
    amount: { amount: parseAmount(l.amount) ?? 0, currency: l.currency, rateToRial: l.currency === 'IRR' ? null : parseAmount(l.rate) },
    bankId: isCheque(l.kind) && l.bankId ? l.bankId : null, branchName: l.branchName || null, branchCode: l.branchCode || null,
    accountNumber: toLatinDigits(l.accountNumber) || null, accountHolder: l.accountHolder || null, city: l.city || null, outOfTown: l.outOfTown,
    sayadId: toLatinDigits(l.sayadId.trim()) || null, guarantors: isCheque(l.kind) ? [] : guarantors,
  }
}

async function save() {
  message.value = null
  if (!normaliseJalali(header.date)) return show('error', translate('FiscalDate.Invalid', props.locale))
  if (!header.payer) return show('error', translate('Receipt.PayerRequired', props.locale))
  if (!header.groupId) return show('error', translate('Receipt.PurposeGroupRequired', props.locale))
  if (!header.purpose.trim()) return show('error', translate('Receipt.PurposeRequired', props.locale))
  let path: string
  let body: Record<string, unknown>
  if (props.kind === 'Instruments') {
    for (const l of lines.value) {
      if (parseAmount(l.amount) === null) return show('error', translate('Money.NotPositive', props.locale))
      if (l.dueDate && !normaliseJalali(l.dueDate)) return show('error', translate('FiscalDate.Invalid', props.locale))
      if (l.currency !== 'IRR' && parseAmount(l.rate) === null) return show('error', translate('Money.InvalidRate', props.locale))
      if (isCheque(l.kind) && !l.sayadId.trim()) return show('error', translate('Receipt.SayadIdRequired', props.locale))
    }
    path = '/api/cashflow/receipts/instruments'
    body = { header: headerBody(), instruments: lines.value.map(lineBody) }
  } else {
    if (!money1.accountId) return show('error', translate('Receipt.AccountNotFound', props.locale))
    const amount = parseAmount(money1.amount)
    if (amount === null) return show('error', translate('Money.NotPositive', props.locale))
    const rate = accountCurrency.value === 'IRR' ? null : parseAmount(money1.rate)
    if (accountCurrency.value !== 'IRR' && rate === null) return show('error', translate('Money.InvalidRate', props.locale))
    path = props.kind === 'Cash' ? '/api/cashflow/receipts/cash' : '/api/cashflow/receipts/bank'
    body = props.kind === 'Cash'
      ? { header: headerBody(), cashDeskId: money1.accountId, amount, rateToRial: rate }
      : { header: headerBody(), bankAccountId: money1.accountId, method: money1.method, trackingNumber: money1.tracking || null, amount, rateToRial: rate }
  }

  saving.value = true
  try {
    if (!(await addPurposeIfNew())) return
    const r = await api.post<string>(path, body)
    if (!r.ok) return show('error', translate(r.code, props.locale))
    const saved = await api.get<{ number: number; fiscalYear: number }>(`/api/cashflow/receipts/${r.value}`)
    const no = saved.ok ? (fa.value ? saved.value.number.toLocaleString('fa-IR') : String(saved.value.number)) : ''
    const text = t(`رسید شمارهٔ ${no} ثبت شد.`, `Receipt no. ${no} saved.`)
    if (r.warnings.length) show('warning', `${text} ${r.warnings.map(w => translate(w, props.locale)).join(' ')}`)
    else show('ok', text)
    Object.assign(money1, { amount: '', rate: '', tracking: '' })
    Object.assign(header, { purpose: '', ref1: '', ref2: '', note: '' })
    lines.value = [blankLine()]
    emit('saved', r.value)
  } finally {
    saving.value = false
  }
}

function onKey(e: KeyboardEvent) {
  if (e.key === 'Enter' && e.ctrlKey) { e.preventDefault(); void save() }
}

onMounted(async () => { await L.load(); payers.value = await L.searchDetails('', undefined, PayerRoles) })
</script>

<template>
  <form class="tr-form" @keydown="onKey" @submit.prevent="save">
    <fieldset>
      <legend>{{ t('مشخصات دریافت', 'Receipt') }}</legend>
      <label class="req">{{ t('تاریخ دریافت', 'Date') }} <input v-model="header.date" placeholder="1405/08/23" dir="ltr" /></label>
      <label class="wide">{{ t('پرداخت کننده (حساب تفصیلی)', 'Payer (detail account)') }}
        <SearchSelect v-model="header.payer" :options="payerChoices" remote required :aria-label="t('پرداخت کننده', 'Payer')"
                      :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @search="onPayerSearch" />
      </label>
      <label>{{ t('گروه بابت', 'Purpose group') }}
        <SearchSelect v-model="header.groupId" :options="groupChoices" required :aria-label="t('گروه بابت', 'Purpose group')"
                      :none-text="t('گروه بابت دریافتی تعریف نشده است', 'No receipt purpose group')" />
      </label>
      <label class="wide req">{{ t('بابت', 'Purpose') }}
        <input v-model="header.purpose" list="tr-purposes" :placeholder="t('از فهرست انتخاب کنید یا بنویسید', 'Choose or type')" />
        <datalist id="tr-purposes"><option v-for="p in purposes.filter(x => x.isActive)" :key="p.id" :value="p.name" /></datalist>
      </label>
      <label v-if="group?.reference1Id">{{ refTitle(group.reference1Id) ?? t('عطف ۱', 'Reference 1') }} <input v-model="header.ref1" /></label>
      <label v-if="group?.reference2Id">{{ refTitle(group.reference2Id) ?? t('عطف ۲', 'Reference 2') }} <input v-model="header.ref2" /></label>
      <!-- بخش: the department that received the money (manual, form «دریافت چک و اسناد دریافتنی»). Shown only when departments are
           defined in Basic Information; Iran Nazho Co. defines none (owner, 13 Nov 2026). -->
      <label v-if="departmentChoices.length">{{ t('بخش', 'Department') }}
        <SearchSelect v-model="header.departmentId" :options="departmentChoices" :empty-label="t('— ندارد —', '— none —')" :aria-label="t('بخش', 'Department')" />
      </label>
      <label class="tr-check"><input v-model="header.salesProceeds" type="checkbox" /> {{ t('وجوه حاصل فروش', 'Sales proceeds') }}</label>
      <label class="wide">{{ t('توضیحات', 'Note') }} <input v-model="header.note" maxlength="500" /></label>
    </fieldset>

    <fieldset v-if="kind !== 'Instruments'">
      <legend>{{ kind === 'Cash' ? t('دریافت نقدی', 'Cash') : t('واریز به حساب', 'Paid into a bank account') }}</legend>
      <label v-if="kind === 'Cash'" class="wide">{{ t('صندوق', 'Cash desk') }}
        <SearchSelect v-model="money1.accountId" :options="deskChoices" required :aria-label="t('صندوق', 'Cash desk')" :none-text="t('صندوقی تعریف نشده است', 'No cash desk')" />
      </label>
      <template v-else>
        <label class="wide">{{ t('حساب بانکی', 'Bank account') }}
          <SearchSelect v-model="money1.accountId" :options="bankAccountChoices" required :aria-label="t('حساب بانکی', 'Bank account')" />
        </label>
        <label>{{ t('نوع واریز', 'Paid in by') }}
          <SearchSelect v-model="money1.method" :options="methods.map(m => ({ value: m.value, label: fa ? m.fa : m.en }))" required :aria-label="t('نوع واریز', 'Paid in by')" />
        </label>
        <label>{{ money1.method === 'Pos' ? t('شماره پیگیری / مرجع', 'Tracking / reference no.') : money1.method === 'DepositSlip' ? t('شماره فیش', 'Slip no.') : t('شماره پیگیری', 'Tracking no.') }}
          <input v-model="money1.tracking" dir="ltr" />
        </label>
      </template>
      <label class="req">{{ t('مبلغ', 'Amount') }} ({{ accountCurrency }}) <input v-model="money1.amount" inputmode="decimal" dir="ltr" /></label>
      <label v-if="accountCurrency !== 'IRR'" class="req">{{ t('نرخ ارز (ریال)', 'Rate (Rial)') }} <input v-model="money1.rate" inputmode="decimal" dir="ltr" /></label>
      <label v-if="accountCurrency !== 'IRR'">{{ t('هم‌ارز ریالی', 'Rial equivalent') }} <input class="ro num" :value="money(total)" readonly /></label>
    </fieldset>

    <template v-else>
      <fieldset v-for="(l, i) in lines" :key="l.key">
        <legend>
          {{ t(`ردیف ${(i + 1).toLocaleString('fa-IR')}`, `Line ${i + 1}`) }}
          <span v-if="timing(l) === 'current'" class="tr-tag current">{{ t('روز', 'Current') }}</span>
          <span v-else-if="timing(l) === 'later'" class="tr-tag later">{{ t('مدت‌دار', 'Post-dated') }}</span>
          <button type="button" class="tr-link danger" @click="removeLine(i)">{{ t('حذف ردیف', 'Remove') }}</button>
        </legend>
        <label>{{ t('نوع', 'Kind') }}
          <SearchSelect v-model="l.kind" :options="kindChoices" required :aria-label="t('نوع', 'Kind')" />
        </label>
        <label class="req">{{ isCheque(l.kind) ? t('شماره چک', 'Cheque no.') : t('شماره سفته', 'Note no.') }} <input v-model="l.number" dir="ltr" /></label>
        <label :class="{ req: !isGuarantee(l.kind) }">{{ t('سررسید', 'Due date') }} <input v-model="l.dueDate" placeholder="1405/09/30" dir="ltr" /></label>
        <label>{{ t('ارز', 'Currency') }}
          <SearchSelect v-model="l.currency" :options="currencyChoices" required :aria-label="t('ارز', 'Currency')" />
        </label>
        <label class="req">{{ t('مبلغ', 'Amount') }} ({{ l.currency }}) <input v-model="l.amount" inputmode="decimal" dir="ltr" /></label>
        <template v-if="l.currency !== 'IRR'">
          <label class="req">{{ t('نرخ ارز (ریال)', 'Rate (Rial)') }} <input v-model="l.rate" inputmode="decimal" dir="ltr" /></label>
          <label>{{ t('هم‌ارز ریالی', 'Rial equivalent') }} <input class="ro num" :value="linesRial[i] === null ? '' : money(linesRial[i]!)" readonly /></label>
        </template>
        <template v-if="isCheque(l.kind)">
          <label>{{ t('بانک', 'Bank') }}
            <SearchSelect v-model="l.bankId" :options="bankChoices" required :aria-label="t('بانک', 'Bank')" />
          </label>
          <label>{{ t('شعبه', 'Branch') }} <input v-model="l.branchName" /></label>
          <label>{{ t('کد شعبه', 'Branch code') }} <input v-model="l.branchCode" dir="ltr" /></label>
          <label>{{ t('شماره حساب', 'Account no.') }} <input v-model="l.accountNumber" dir="ltr" /></label>
          <label class="req">{{ t('شناسهٔ صیاد (۱۶ رقم)', 'Sayad ID (16 digits)') }} <input v-model="l.sayadId" maxlength="16" dir="ltr" inputmode="numeric" /></label>
          <label>{{ t('نام دارندهٔ حساب', 'Account holder') }} <input v-model="l.accountHolder" :placeholder="t('اگر چک شخص دیگری است', 'If someone else\'s cheque')" /></label>
          <label>{{ t('شهر', 'City') }}
            <SearchSelect v-model="l.city" :options="cityChoices" :empty-label="t('—', '—')" :aria-label="t('شهر', 'City')" />
          </label>
          <label class="tr-check"><input v-model="l.outOfTown" type="checkbox" /> {{ t('خارج از مرکز', 'Out of town') }}</label>
        </template>
        <template v-else>
          <label>{{ t('ضامن اول', 'First guarantor') }} <input v-model="l.g1Name" /></label>
          <label>{{ t('کد / شناسهٔ ملی ضامن اول', 'National code') }} <input v-model="l.g1Code" maxlength="11" dir="ltr" inputmode="numeric" /></label>
          <label>{{ t('ضامن دوم', 'Second guarantor') }} <input v-model="l.g2Name" /></label>
          <label>{{ t('کد / شناسهٔ ملی ضامن دوم', 'National code') }} <input v-model="l.g2Code" maxlength="11" dir="ltr" inputmode="numeric" /></label>
        </template>
      </fieldset>
      <div class="tr-buttons">
        <button type="button" class="tr-secondary" @click="nextLine">{{ t('+ ثبت چک بعدی', '+ Next cheque') }}</button>
        <span class="muted">{{ t(`${lines.length.toLocaleString('fa-IR')} ردیف`, `${lines.length} lines`) }}</span>
      </div>
    </template>

    <div class="tr-buttons">
      <strong>{{ t('جمع (ریال):', 'Total (Rial):') }} <span class="num">{{ money(total) }}</span></strong>
      <button type="submit" :disabled="saving">{{ t('ثبت رسید (Ctrl+Enter)', 'Save receipt (Ctrl+Enter)') }}</button>
    </div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
  </form>
</template>

<style scoped src="./receipts.css"></style>
