<script setup lang="ts">
/**
 * One receipt form for the three kinds (owner, 14 Nov 2026): cash into a cash desk (old D_Naghd), money paid into a bank account (old
 * D_variz) by one of the kinds the owner listed on 18 Nov 2026 — Paya or Satna transfer with its Sheba, internal transfer, cash at a
 * branch, card to card (only into an account that has a bank card), payment gateway — or POS, and cheques and promissory notes from one payer (old D_Check with
 * «ثبت چک بعدی»). Current or post-dated is decided by the due date; Rial or foreign currency by the account or by each cheque.
 * Ctrl+Enter saves. Errors are shown from the API's codes, translated.
 * The voucher the receipt will produce is shown at the bottom while the form is filled (owner's report no. 4, item 8); the treasurer
 * confirms it from the receipts list after saving.
 * With `replaces` the form is the replacement of a received cheque (owner's item 14 (d)): the payer is the old cheque's, the new cheques
 * may differ in amount and number (answer to Q31), and the receipt is saved with the old cheque marked «تعویض شده».
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, formatAmount, normaliseJalali, parseAmount, toLatinDigits, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import AmountInput from '@treasury/ui-kit/AmountInput.vue'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups, type Detail, type Purpose } from './lookups'
import VoucherTable, { type Voucher } from './VoucherTable.vue'
import { depositMethods, forForeignCurrency, needsSheba } from './depositMethods'

type Kind = 'Cash' | 'BankDeposit' | 'Instruments'
type LineKind = 'OrdinaryCheque' | 'BankCheque' | 'GuaranteeCheque'
interface Line {
  key: number; kind: LineKind; number: string; dueDate: string; currency: string; amount: string; rate: string
  bankId: string; branchName: string; branchCode: string; accountNumber: string; accountHolder: string; city: string; outOfTown: boolean; sayadId: string
  g1Name: string; g1Code: string; g2Name: string; g2Code: string
}

const props = withDefaults(defineProps<{
  kind: Kind; apiBase?: string; locale?: Locale; token?: () => string | undefined
  replaces?: { id: string; number: string; amount: number; payerCode: string }
}>(), { apiBase: '', locale: 'fa', token: undefined, replaces: undefined })
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
  // Promissory notes get forms of their own later (owner's report no. 4, item 13).
]
const isCheque = (k: LineKind) => k.endsWith('Cheque')
/**
 * The kinds of deposit the chosen account can take: a foreign-currency account takes cash deposits and bank transfers only (owner's
 * report no. 4, item 7); card to card needs an account with a bank card (owner, 18 Nov 2026).
 */
const accountHasCard = computed(() => (L.bankAccounts.value.find(a => a.id === money1.accountId)?.cards?.length ?? 0) > 0)
const methodChoices = computed<Choice[]>(() => depositMethods
  .filter(m => !m.retired && (accountCurrency.value === 'IRR' || forForeignCurrency(m.value)) && (m.value !== 'CardToCard' || accountHasCard.value))
  .map(m => ({ value: m.value, label: fa.value ? m.fa : m.en })))
const isGuarantee = (k: LineKind) => k.startsWith('Guarantee')

// ---- header (every kind) ----
const header = reactive({ date: todayJalali(), payer: props.replaces?.payerCode ?? '', groupId: '', purpose: props.replaces ? `تعویض چک ${props.replaces.number}` : '',
  ref1: '', ref2: '', departmentId: '', salesProceeds: false, note: '' })
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
const money1 = reactive({ accountId: '', method: '', tracking: '', amount: '', rate: '', sheba: '' })
const deskChoices = computed<Choice[]>(() => L.desks.value.filter(d => d.isActive).map(d => ({ value: d.id, label: d.name, hint: d.currencyCode })))
const bankAccountChoices = computed<Choice[]>(() => L.bankAccounts.value.filter(a => a.isActive).map(a => ({ value: a.id, label: L.accountTitle(a), hint: a.currencyCode })))
const accountCurrency = computed(() =>
  (props.kind === 'Cash' ? L.desks.value.find(d => d.id === money1.accountId)?.currencyCode : L.bankAccounts.value.find(a => a.id === money1.accountId)?.currencyCode) ?? 'IRR')
// The kind of deposit is chosen by the user (no default); a kind the newly chosen account cannot take is cleared.
watch(methodChoices, () => { if (money1.method && !methodChoices.value.some(c => c.value === money1.method)) money1.method = '' })
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
const kindChoices = computed<Choice[]>(() => lineKinds.filter(k => !props.replaces || !isGuarantee(k.value)).map(k => ({ value: k.value, label: fa.value ? k.fa : k.en })))

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

/** The request the form makes, or the code of the first thing missing (checked again by the server). */
function request(): { path: string; body: Record<string, unknown> } | { error: string } {
  if (!normaliseJalali(header.date)) return { error: 'FiscalDate.Invalid' }
  if (!header.payer) return { error: 'Receipt.PayerRequired' }
  if (!header.groupId) return { error: 'Receipt.PurposeGroupRequired' }
  if (!header.purpose.trim()) return { error: 'Receipt.PurposeRequired' }
  if (props.kind === 'Instruments') {
    for (const l of lines.value) {
      if (parseAmount(l.amount) === null) return { error: 'Money.NotPositive' }
      if (l.dueDate && !normaliseJalali(l.dueDate)) return { error: 'FiscalDate.Invalid' }
      if (l.currency !== 'IRR' && parseAmount(l.rate) === null) return { error: 'Money.InvalidRate' }
      if (isCheque(l.kind) && !l.sayadId.trim()) return { error: 'Receipt.SayadIdRequired' }
    }
    const path = props.replaces ? `/api/cashflow/instruments/${props.replaces.id}/replace` : '/api/cashflow/receipts/instruments'
    return { path, body: { header: headerBody(), instruments: lines.value.map(lineBody) } }
  }
  if (!money1.accountId) return { error: 'Receipt.AccountNotFound' }
  const amount = parseAmount(money1.amount)
  if (amount === null) return { error: 'Money.NotPositive' }
  const rate = accountCurrency.value === 'IRR' ? null : parseAmount(money1.rate)
  if (accountCurrency.value !== 'IRR' && rate === null) return { error: 'Money.InvalidRate' }
  if (props.kind === 'Cash') return { path: '/api/cashflow/receipts/cash', body: { header: headerBody(), cashDeskId: money1.accountId, amount, rateToRial: rate } }
  if (!money1.method) return { error: 'Receipt.DepositMethodRequired' }
  if (needsSheba(money1.method) && !money1.sheba.trim()) return { error: 'Receipt.ShebaRequired' }
  return { path: '/api/cashflow/receipts/bank', body: {
    header: headerBody(), bankAccountId: money1.accountId, method: money1.method, trackingNumber: money1.tracking || null, amount, rateToRial: rate,
    payerSheba: needsSheba(money1.method) ? toLatinDigits(money1.sheba.trim()) : null } }
}

// ---- the voucher at the bottom of the form (nothing is saved) ----
const preview = ref<Voucher | null>(null)
const previewHint = ref('')
let previewTimer: ReturnType<typeof setTimeout> | undefined
let previewRun = 0
watch([header, money1, lines], () => {
  clearTimeout(previewTimer)
  previewTimer = setTimeout(refreshPreview, 400)
}, { deep: true })
async function refreshPreview() {
  const run = ++previewRun
  const req = request()
  if ('error' in req) { preview.value = null; previewHint.value = translate(req.error, props.locale); return }
  const r = await api.post<Voucher>(`${req.path}/voucher-preview`, req.body)
  if (run !== previewRun) return
  preview.value = r.ok ? r.value : null
  previewHint.value = r.ok ? '' : translate(r.code, props.locale)
}

async function save() {
  message.value = null
  const req = request()
  if ('error' in req) return show('error', translate(req.error, props.locale))
  const { path, body } = req

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
    Object.assign(money1, { amount: '', rate: '', tracking: '', sheba: '' })
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

onMounted(async () => { await L.load(); payers.value = await L.searchDetails('', header.payer || undefined, PayerRoles) })
</script>

<template>
  <form class="tr-form" @keydown="onKey" @submit.prevent="save">
    <fieldset>
      <legend>{{ t('مشخصات دریافت', 'Receipt') }}</legend>
      <label class="req">{{ t('تاریخ دریافت', 'Date') }} <input v-model="header.date" placeholder="1405/08/23" dir="ltr" /></label>
      <label class="wide">{{ t('پرداخت کننده (حساب تفصیلی)', 'Payer (detail account)') }}
        <SearchSelect v-model="header.payer" :options="payerChoices" remote required :disabled="!!replaces" :aria-label="t('پرداخت کننده', 'Payer')"
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
      <!-- «واحد سازمانی دریافت کننده» (owner, 15 Nov 2026: keep it, with this title): the department that received the money (manual, form «دریافت چک و اسناد دریافتنی»). Shown only when departments are
           defined in Basic Information; Iran Nazho Co. defines none (owner, 13 Nov 2026). -->
      <label v-if="departmentChoices.length">{{ t('واحد سازمانی دریافت کننده', 'Receiving department') }}
        <SearchSelect v-model="header.departmentId" :options="departmentChoices" :empty-label="t('— ندارد —', '— none —')" :aria-label="t('واحد سازمانی دریافت کننده', 'Receiving department')" />
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
        <label class="wide">{{ t('نوع واریز', 'Kind of deposit') }}
          <SearchSelect v-model="money1.method" :options="methodChoices" required :aria-label="t('نوع واریز', 'Kind of deposit')" />
        </label>
        <label v-if="needsSheba(money1.method)" class="wide req">{{ t('شماره شبای واریزکننده (حساب مبدأ حواله)', "Payer's Sheba (the transfer's source account)") }}
          <input v-model="money1.sheba" dir="ltr" placeholder="IR00 0000 0000 0000 0000 0000 00" />
        </label>
        <label>{{ money1.method === 'DepositSlip' ? t('شماره فیش', 'Slip no.') : t('شماره پیگیری / مرجع', 'Tracking / reference no.') }}
          <input v-model="money1.tracking" dir="ltr" />
        </label>
        <p v-if="money1.accountId && accountCurrency === 'IRR' && !accountHasCard" class="muted small wide">
          {{ t('این حساب کارت بانک ندارد؛ برای ثبت «کارت به کارت» ابتدا کارت بانک را در اطلاعات پایه ← حساب‌های بانکی تعریف کنید.',
            'This account has no bank card; define one in Basic Information → Bank accounts to record card-to-card deposits.') }}
        </p>
      </template>
      <label class="req">{{ t('مبلغ', 'Amount') }} ({{ accountCurrency }}) <AmountInput v-model="money1.amount" :decimals="accountCurrency === 'IRR' ? 0 : 2" /></label>
      <label v-if="accountCurrency !== 'IRR'" class="req">{{ t('نرخ ارز (ریال)', 'Rate (Rial)') }} <AmountInput v-model="money1.rate" :decimals="4" /></label>
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
        <label class="req">{{ t('شماره چک', 'Cheque no.') }} <input v-model="l.number" dir="ltr" /></label>
        <label :class="{ req: !isGuarantee(l.kind) }">{{ t('سررسید', 'Due date') }} <input v-model="l.dueDate" placeholder="1405/09/30" dir="ltr" /></label>
        <label>{{ t('ارز', 'Currency') }}
          <SearchSelect v-model="l.currency" :options="currencyChoices" required :aria-label="t('ارز', 'Currency')" />
        </label>
        <label class="req">{{ t('مبلغ', 'Amount') }} ({{ l.currency }}) <AmountInput v-model="l.amount" :decimals="l.currency === 'IRR' ? 0 : 2" /></label>
        <template v-if="l.currency !== 'IRR'">
          <label class="req">{{ t('نرخ ارز (ریال)', 'Rate (Rial)') }} <AmountInput v-model="l.rate" :decimals="4" /></label>
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
      </fieldset>
      <div class="tr-buttons">
        <button type="button" class="tr-secondary" @click="nextLine">{{ t('+ ثبت چک بعدی', '+ Next cheque') }}</button>
        <span class="muted">{{ t(`${lines.length.toLocaleString('fa-IR')} ردیف`, `${lines.length} lines`) }}</span>
      </div>
    </template>

    <div class="tr-buttons">
      <strong>{{ t('جمع (ریال):', 'Total (Rial):') }} <span class="num">{{ money(total) }}</span></strong>
      <span v-if="replaces" class="muted">{{ t(`چک قبلی: ${money(replaces.amount)} – تفاوت: ${money(total - replaces.amount)}`,
        `Old cheque: ${money(replaces.amount)} – difference: ${money(total - replaces.amount)}`) }}</span>
      <button type="submit" :disabled="saving">{{ t('ثبت رسید (Ctrl+Enter)', 'Save receipt (Ctrl+Enter)') }}</button>
    </div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
    <VoucherTable v-if="preview" :voucher="preview" :locale="locale" :account-name="L.accountName" />
    <p v-else class="muted small">{{ t('سند حسابداری این رسید پس از تکمیل فرم اینجا نمایش داده می‌شود.', 'The voucher of this receipt is shown here once the form is filled in.') }}
      <template v-if="previewHint"> ({{ previewHint }})</template></p>
  </form>
</template>

<style scoped src="./receipts.css"></style>
