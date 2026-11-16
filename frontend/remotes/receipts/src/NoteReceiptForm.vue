<script setup lang="ts">
/**
 * The receipt of promissory notes (owner's form «فرم دریافت سفته», 16 Nov 2026; report no. 4, item 13: notes have a form of their
 * own). Notes of one issuer are saved on one receipt, numbered in their own series. The form follows the owner's sheet: the receipt,
 * the persons (issuer and guarantors with their تفصیل), the notes, the supplementary information, and the checks of each note.
 * «عندالمطالبه» switches the due-date column off; «دریافت شد» asks for the date the original was received; «موضوع تعهد» is for a
 * guarantee only. Owner's answers of 16 Nov 2026: up to three guarantors for a note (Q39); every item of article 308 is ticked for
 * each note before the receipt is saved (Q40), except the payment date of a note payable on demand, which has none. The voucher the receipt will produce is shown at the bottom while the form is filled; Ctrl+Enter saves.
 * After saving, the receipt can be printed for the three signatures.
 */
import { computed, nextTick, onMounted, reactive, ref, watch } from 'vue'
import { amountInWords, createApi, formatAmount, normaliseJalali, parseAmount, toLatinDigits, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import AmountInput from '@treasury/ui-kit/AmountInput.vue'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups, type Detail, type Purpose } from './lookups'
import NoteReceiptPrint from './NoteReceiptPrint.vue'
import { attachmentNames, checkNames, confirmationText, formNames, maturityNames, reasonNames, type NoteAttachments, type NoteChecks, type NoteReceipt } from './notes'
import VoucherTable, { type Voucher } from './VoucherTable.vue'

interface Line { key: number; number: string; issueDate: string; dueDate: string; amount: string; checks: NoteChecks }
interface Guarantor { key: number; detailCode: string; name: string; nationalCode: string; phone: string; noteNumber: string; found: Detail[] }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const emit = defineEmits<{ saved: [id: string] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (fa.value ? f : e)
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => (fa.value ? n.toLocaleString('fa-IR') : String(n))
const pick = (m: Record<string, [string, string]>, k: string) => (fa.value ? m[k]![0] : m[k]![1])
const L = useLookups(api)

// ---- the receipt ----
const header = reactive({ date: todayJalali(), issuer: '', groupId: '', purpose: '', ref1: '', ref2: '', departmentId: '', note: '' })
const noAttachments = (): NoteAttachments => ({ noteImage: false, contract: false, representation: false, other: false })
const terms = reactive({
  reason: 'Debt', form: 'Paper', maturity: 'Fixed', issuerNationalCode: '', issuerPhone: '', commitmentSubject: '', returnTerms: '', custodyPlace: '',
  originalReceived: false, originalReceivedOn: '', discrepancy: '', attachments: noAttachments(), deliveredBy: '', approvedBy: '',
})
const issuers = ref<Detail[]>([])
/** As on the other receipts (answer to Q22): customers and persons or companies. */
const IssuerRoles = 'Customer,PersonOrCompany'
const purposes = ref<Purpose[]>([])
const group = computed(() => L.groups.value.find(g => g.id === header.groupId))
const refTitle = (id: string | null | undefined) => L.references.value.find(r => r.id === id)?.name
const groupChoices = computed<Choice[]>(() => L.groups.value.filter(g => g.direction === 'Receipt' && g.isActive).map(g => ({ value: g.id, label: g.name, hint: String(g.number) })))
const issuerChoices = computed<Choice[]>(() => issuers.value.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive })))
const departmentChoices = computed<Choice[]>(() => L.departments.value.filter(d => d.isActive).map(d => ({ value: d.id, label: d.name })))
/** The group's معین is the other side of the voucher of a note that pays a debt (owner's table «دریافت سفته عادی»). */
const groupAccount = computed(() => accountOf(group.value?.ledgerAccountCode))
let issuerTimer: ReturnType<typeof setTimeout> | undefined
function onIssuerSearch(text: string) {
  clearTimeout(issuerTimer)
  issuerTimer = setTimeout(async () => { issuers.value = await L.searchDetails(text, header.issuer, IssuerRoles) }, 250)
}
watch(() => header.groupId, async id => {
  purposes.value = []
  if (!id) return
  const r = await api.get<Purpose[]>(`/api/reference/purposes?groupId=${id}`)
  if (r.ok) purposes.value = r.value
})
const onDemand = computed(() => terms.maturity === 'OnDemand')
/** A note payable on demand has no payment date written on it, so that item of article 308 is not asked. */
const notAsked = (k: keyof NoteChecks) => k === 'paymentDate' && onDemand.value
const guarantee = computed(() => terms.reason === 'Guarantee')

// ---- guarantors and endorsers ----
let nextKey = 1
const guarantors = ref<Guarantor[]>([])
const addGuarantor = () => guarantors.value.push({ key: nextKey++, detailCode: '', name: '', nationalCode: '', phone: '', noteNumber: '', found: [] })
const guarantorChoices = (g: Guarantor): Choice[] => g.found.map(d => ({ value: d.code, label: d.name, hint: d.code, muted: !d.isActive }))
const guarantorTimers: Record<number, ReturnType<typeof setTimeout>> = {}
function onGuarantorSearch(g: Guarantor, text: string) {
  clearTimeout(guarantorTimers[g.key])
  guarantorTimers[g.key] = setTimeout(async () => { g.found = await L.searchDetails(text, g.detailCode || undefined) }, 250)
}
/** Choosing a تفصیل fills the name. */
function onGuarantorDetail(g: Guarantor, code: string) {
  g.detailCode = code
  const d = g.found.find(x => x.code === code)
  if (d) g.name = d.name
}

// ---- the notes ----
const noChecks = (): NoteChecks => ({ signature: false, date: false, amountInWords: false, payee: false, paymentDate: false })
const blankLine = (): Line => ({ key: nextKey++, number: '', issueDate: '', dueDate: '', amount: '', checks: noChecks() })
const lines = ref<Line[]>([blankLine()])
/** «ثبت سفتهٔ بعدی»: the next note starts as a copy of the last one (same dates and amount) with the next number. */
function nextLine() {
  const last = lines.value[lines.value.length - 1]
  if (!last) return void lines.value.push(blankLine())
  const digits = toLatinDigits(last.number)
  const next = /^\d+$/.test(digits) ? String(BigInt(digits) + 1n).padStart(digits.length, '0') : ''
  lines.value.push({ ...last, key: nextKey++, number: next, checks: noChecks() })
}
function removeLine(i: number) {
  lines.value.splice(i, 1)
  if (!lines.value.length) lines.value.push(blankLine())
}
function tickAll(l: Line) { for (const [k] of checkNames) l.checks[k] = true }
const total = computed(() => lines.value.reduce((s, l) => s + (parseAmount(l.amount) ?? 0), 0))
const noteChoices = computed<Choice[]>(() => lines.value.map(l => toLatinDigits(l.number.trim())).filter(Boolean).map(n => ({ value: n, label: n })))
const lineTitle = (l: Line, i: number) => t(`ردیف ${num(i + 1)}`, `Line ${i + 1}`) + (l.number.trim() ? ` – ${l.number.trim()}` : '')

// ---- save ----
const saving = ref(false)
const message = ref<{ kind: 'error' | 'ok'; text: string } | null>(null)
const show = (kind: 'error' | 'ok', text: string) => { message.value = { kind, text } }
const saved = ref<NoteReceipt | null>(null)
const printing = ref(false)
const savedGroup = computed(() => L.groups.value.find(g => g.id === saved.value?.purposeGroupId))
const accountOf = (code: string | null | undefined) => (code ? `${code} – ${L.accountName(code) ?? ''}` : '')

/** A purpose typed here that the group does not list yet is added to it (decision of 10 Nov 2026). */
async function addPurposeIfNew(): Promise<boolean> {
  const name = header.purpose.trim()
  if (!name || !header.groupId || purposes.value.some(p => p.name === name)) return true
  const r = await api.post<string>('/api/reference/purposes', { groupId: header.groupId, name })
  if (!r.ok) { show('error', translate(r.code, props.locale)); return false }
  purposes.value.push({ id: r.value, groupId: header.groupId, name, isActive: true })
  return true
}

const text = (s: string) => s.trim() || null
const date = (s: string) => (s.trim() ? normaliseJalali(s) ?? s : null)

/** The request the form makes, or the code of the first thing missing (checked again by the server). */
function request(): { body: Record<string, unknown> } | { error: string } {
  if (!normaliseJalali(header.date)) return { error: 'FiscalDate.Invalid' }
  if (!header.issuer) return { error: 'Receipt.PayerRequired' }
  if (!header.groupId) return { error: 'Receipt.PurposeGroupRequired' }
  if (!header.purpose.trim()) return { error: 'Receipt.PurposeRequired' }
  for (const l of lines.value) {
    if (!l.number.trim()) return { error: 'Instrument.NumberRequired' }
    if (l.issueDate.trim() && !normaliseJalali(l.issueDate)) return { error: 'FiscalDate.Invalid' }
    if (!onDemand.value && !l.dueDate.trim()) return { error: 'Instrument.DueDateRequired' }
    if (!onDemand.value && !normaliseJalali(l.dueDate)) return { error: 'FiscalDate.Invalid' }
    if (parseAmount(l.amount) === null) return { error: 'Money.NotPositive' }
  }
  if (lines.value.some(l => !checkNames.every(([k]) => l.checks[k] || notAsked(k)))) return { error: 'Receipt.NoteChecksIncomplete' }
  if (terms.originalReceived && !terms.originalReceivedOn.trim()) return { error: 'Receipt.OriginalDateRequired' }
  if (terms.originalReceived && !normaliseJalali(terms.originalReceivedOn)) return { error: 'FiscalDate.Invalid' }
  if (guarantors.value.some(g => !g.name.trim() && !g.detailCode)) return { error: 'Instrument.GuarantorNameRequired' }
  return {
    body: {
      header: {
        date: normaliseJalali(header.date), payerDetailCode: header.issuer, purposeGroupId: header.groupId, purpose: header.purpose.trim(),
        reference1: text(header.ref1), reference2: text(header.ref2), departmentId: header.departmentId || null, salesProceeds: false, note: text(header.note),
      },
      terms: {
        reason: terms.reason, form: terms.form, maturity: terms.maturity, issuerNationalCode: text(toLatinDigits(terms.issuerNationalCode)), issuerPhone: text(terms.issuerPhone),
        commitmentSubject: guarantee.value ? text(terms.commitmentSubject) : null, returnTerms: terms.reason === 'Debt' ? null : text(terms.returnTerms),
        custodyPlace: text(terms.custodyPlace), originalReceived: terms.originalReceived, originalReceivedOn: terms.originalReceived ? date(terms.originalReceivedOn) : null,
        discrepancy: text(terms.discrepancy), attachments: terms.attachments, deliveredBy: text(terms.deliveredBy), approvedBy: text(terms.approvedBy),
      },
      notes: lines.value.map(l => ({
        number: toLatinDigits(l.number.trim()), issueDate: date(l.issueDate), dueDate: onDemand.value ? null : date(l.dueDate), amount: parseAmount(l.amount) ?? 0,
        checks: { ...l.checks, paymentDate: !onDemand.value && l.checks.paymentDate },
      })),
      guarantors: guarantors.value.map(g => ({
        name: text(g.name), nationalCode: text(toLatinDigits(g.nationalCode)), detailCode: g.detailCode || null, phone: text(g.phone), noteNumber: g.noteNumber || null,
      })),
    },
  }
}

// ---- the voucher at the bottom of the form (nothing is saved) ----
const preview = ref<Voucher | null>(null)
const previewHint = ref('')
let previewTimer: ReturnType<typeof setTimeout> | undefined
let previewRun = 0
watch([header, terms, lines], () => {
  clearTimeout(previewTimer)
  previewTimer = setTimeout(refreshPreview, 400)
}, { deep: true })
async function refreshPreview() {
  const run = ++previewRun
  const req = request()
  if ('error' in req) { preview.value = null; previewHint.value = translate(req.error, props.locale); return }
  const r = await api.post<Voucher>('/api/cashflow/receipts/notes/voucher-preview', req.body)
  if (run !== previewRun) return
  preview.value = r.ok ? r.value : null
  previewHint.value = r.ok ? '' : translate(r.code, props.locale)
}

async function save(thenPrint = false) {
  message.value = null
  const req = request()
  if ('error' in req) return show('error', translate(req.error, props.locale))
  saving.value = true
  try {
    if (!(await addPurposeIfNew())) return
    const r = await api.post<string>('/api/cashflow/receipts/notes', req.body)
    if (!r.ok) return show('error', translate(r.code, props.locale))
    const got = await api.get<NoteReceipt>(`/api/cashflow/receipts/${r.value}`)
    saved.value = got.ok ? got.value : null
    show('ok', t(`رسید سفتهٔ شمارهٔ ${got.ok ? num(got.value.number) : ''} ثبت شد.`, `Note receipt no. ${got.ok ? got.value.number : ''} saved.`))
    Object.assign(header, { purpose: '', ref1: '', ref2: '', note: '' })
    Object.assign(terms, { issuerNationalCode: '', issuerPhone: '', commitmentSubject: '', returnTerms: '', custodyPlace: '', originalReceived: false, originalReceivedOn: '',
      discrepancy: '', attachments: noAttachments(), deliveredBy: '', approvedBy: '' })
    lines.value = [blankLine()]
    guarantors.value = []
    emit('saved', r.value)
    if (thenPrint && saved.value) await print()
  } finally {
    saving.value = false
  }
}

/** Shows the saved receipt as a sheet and opens the browser's print window; only the sheet is printed. */
async function print() {
  printing.value = true
  await nextTick()
  window.print()
}

function onKey(e: KeyboardEvent) {
  if (e.key === 'Enter' && e.ctrlKey) { e.preventDefault(); void save() }
}

onMounted(async () => { await L.load(); issuers.value = await L.searchDetails('', undefined, IssuerRoles) })
</script>

<template>
  <form class="tr-form" @keydown="onKey" @submit.prevent="save()">
    <fieldset>
      <legend>{{ t('مشخصات دریافت', 'Receipt') }}</legend>
      <label>{{ t('شماره رسید دریافت', 'Receipt no.') }} <span class="tr-box">{{ t('خودکار – پس از ثبت', 'Given when saved') }}</span></label>
      <label class="req">{{ t('تاریخ دریافت', 'Date received') }} <input v-model="header.date" placeholder="1405/08/25" dir="ltr" /></label>
      <label v-if="departmentChoices.length">{{ t('شعبه / واحد سازمانی', 'Department') }}
        <SearchSelect v-model="header.departmentId" :options="departmentChoices" :empty-label="t('— ندارد —', '— none —')" :aria-label="t('شعبه / واحد سازمانی', 'Department')" />
      </label>
      <label>{{ t('گروه بابت', 'Purpose group') }}
        <SearchSelect v-model="header.groupId" :options="groupChoices" required :aria-label="t('گروه بابت', 'Purpose group')"
                      :none-text="t('گروه بابت دریافتی تعریف نشده است', 'No receipt purpose group')" />
      </label>
      <label class="wide">{{ t('حساب معین گروه بابت', "The group's ledger account") }} <span class="tr-box">{{ groupAccount || t('با انتخاب گروه بابت مشخص می‌شود', 'Follows the purpose group') }}</span></label>
      <label class="wide req">{{ t('بابت', 'Purpose') }}
        <input v-model="header.purpose" list="tr-note-purposes" :placeholder="t('از فهرست انتخاب کنید یا بنویسید', 'Choose or type')" />
        <datalist id="tr-note-purposes"><option v-for="p in purposes.filter(x => x.isActive)" :key="p.id" :value="p.name" /></datalist>
      </label>
      <div class="tr-group wide">
        <span>{{ t('دلیل دریافت سفته', 'Why the notes are received') }}</span>
        <div class="tr-ticks">
          <label v-for="k in Object.keys(reasonNames)" :key="k"><input v-model="terms.reason" type="radio" name="tr-note-reason" :value="k" /> {{ pick(reasonNames, k) }}</label>
        </div>
      </div>
      <label v-if="group?.reference1Id">{{ refTitle(group.reference1Id) ?? t('عطف ۱', 'Reference 1') }} <input v-model="header.ref1" /></label>
      <label v-if="group?.reference2Id">{{ refTitle(group.reference2Id) ?? t('عطف ۲', 'Reference 2') }} <input v-model="header.ref2" /></label>
    </fieldset>

    <fieldset class="tr-block">
      <legend>{{ t('مشخصات اشخاص', 'Persons') }}</legend>
      <div class="tr-scroll">
        <table class="tr-table">
          <thead>
            <tr><th>{{ t('عنوان', 'Role') }}</th><th>{{ t('نام شخص / شرکت', 'Name') }}</th><th>{{ t('شماره حساب تفصیلی', 'Detail account') }}</th>
              <th>{{ t('کد ملی / شناسه ملی', 'National code / ID') }}</th><th>{{ t('شماره تماس', 'Phone') }}</th><th>{{ t('سفتهٔ مرتبط', 'For note') }}</th><th /></tr>
          </thead>
          <tbody>
            <tr>
              <td>{{ t('صادرکننده و متعهد سفته', 'Issuer and obligor') }}</td>
              <td style="min-width: 15rem">
                <SearchSelect v-model="header.issuer" :options="issuerChoices" remote required :aria-label="t('صادرکننده و متعهد سفته', 'Issuer and obligor')"
                              :none-text="t('تفصیلی پیدا نشد', 'No detail account found')" @search="onIssuerSearch" />
              </td>
              <td class="num">{{ header.issuer }}</td>
              <td><input v-model="terms.issuerNationalCode" dir="ltr" inputmode="numeric" maxlength="11" :aria-label="t('کد ملی یا شناسه ملی متعهد', 'National code of the obligor')" /></td>
              <td><input v-model="terms.issuerPhone" dir="ltr" maxlength="30" :aria-label="t('شماره تماس متعهد', 'Phone of the obligor')" /></td>
              <td class="muted">—</td><td />
            </tr>
            <tr v-for="(g, i) in guarantors" :key="g.key">
              <td>{{ t('ضامن / ظهرنویس', 'Guarantor / endorser') }}</td>
              <td class="req"><input v-model="g.name" :aria-label="t('نام ضامن', 'Name of the guarantor')" /></td>
              <td style="min-width: 13rem">
                <SearchSelect :model-value="g.detailCode" :options="guarantorChoices(g)" remote :empty-label="t('— بدون تفصیلی —', '— none —')"
                              :aria-label="t('حساب تفصیلی ضامن', 'Detail account of the guarantor')" :none-text="t('تفصیلی پیدا نشد', 'No detail account found')"
                              @update:model-value="onGuarantorDetail(g, $event)" @search="onGuarantorSearch(g, $event)" />
              </td>
              <td><input v-model="g.nationalCode" dir="ltr" inputmode="numeric" maxlength="11" :aria-label="t('کد ملی یا شناسه ملی ضامن', 'National code of the guarantor')" /></td>
              <td><input v-model="g.phone" dir="ltr" maxlength="30" :aria-label="t('شماره تماس ضامن', 'Phone of the guarantor')" /></td>
              <td style="min-width: 9rem">
                <SearchSelect v-model="g.noteNumber" :options="noteChoices" :empty-label="t('همهٔ سفته‌های رسید', 'Every note')" :aria-label="t('سفتهٔ مرتبط با ضامن', 'Note of the guarantor')" />
              </td>
              <td><button type="button" class="tr-link danger" @click="guarantors.splice(i, 1)">{{ t('حذف', 'Remove') }}</button></td>
            </tr>
            <tr>
              <td>{{ t('تحویل‌گیرنده سفته', 'Received by') }}</td><td>{{ t('خزانه‌داری (کاربر ثبت‌کننده)', 'Treasury (the user saving)') }}</td>
              <td class="muted">—</td><td class="muted">—</td><td class="muted">—</td><td class="muted">—</td><td />
            </tr>
            <tr>
              <td>{{ t('ذینفع سفته', 'Beneficiary') }}</td><td>{{ t('شرکت (دارندهٔ سیستم خزانه‌داری)', 'The company') }}</td>
              <td class="muted">—</td><td class="muted">—</td><td class="muted">—</td><td class="muted">—</td><td />
            </tr>
          </tbody>
        </table>
      </div>
      <div class="tr-buttons" style="margin-top: .6rem">
        <button type="button" class="tr-secondary" @click="addGuarantor">{{ t('+ افزودن ضامن / ظهرنویس', '+ Guarantor / endorser') }}</button>
        <span class="muted small">{{ t('هر سفته حداکثر سه ضامن دارد. ضامن از فهرست حساب‌های تفصیلی انتخاب یا نام او نوشته می‌شود.',
          'A note has at most three guarantors, chosen from the detail accounts or typed.') }}</span>
      </div>
    </fieldset>

    <fieldset class="tr-block">
      <legend>{{ t('مشخصات سفته‌های دریافتی', 'Notes received') }}</legend>
      <div class="tr-buttons" style="gap: .75rem 3rem; align-items: flex-start; margin-bottom: .6rem">
        <div class="tr-group">
          <span>{{ t('نوع سند', 'Document') }}</span>
          <div class="tr-ticks">
            <label v-for="k in Object.keys(formNames)" :key="k"><input v-model="terms.form" type="radio" name="tr-note-form" :value="k" /> {{ pick(formNames, k) }}</label>
          </div>
        </div>
        <div class="tr-group">
          <span>{{ t('نوع سررسید', 'Due') }}</span>
          <div class="tr-ticks">
            <label v-for="k in Object.keys(maturityNames)" :key="k"><input v-model="terms.maturity" type="radio" name="tr-note-maturity" :value="k" /> {{ pick(maturityNames, k) }}</label>
            <span class="muted small">{{ t('با انتخاب «عندالمطالبه» ستون تاریخ سررسید غیرفعال می‌شود.', 'With «on demand» the due-date column is switched off.') }}</span>
          </div>
        </div>
      </div>
      <div class="tr-scroll">
        <table class="tr-table">
          <thead>
            <tr><th>{{ t('ردیف', 'No.') }}</th><th>{{ t('شماره / شناسه سفته', 'Note number / ID') }}</th><th>{{ t('تاریخ صدور', 'Issue date') }}</th>
              <th>{{ t('تاریخ سررسید', 'Due date') }}</th><th>{{ t('مبلغ تعهد (ریال)', 'Amount (Rial)') }}</th><th /></tr>
          </thead>
          <tbody>
            <tr v-for="(l, i) in lines" :key="l.key">
              <td class="num">{{ num(i + 1) }}</td>
              <td class="req"><input v-model="l.number" dir="ltr" :aria-label="t(`شماره سفته ردیف ${num(i + 1)}`, `Number of note ${i + 1}`)" /></td>
              <td><input v-model="l.issueDate" dir="ltr" placeholder="1405/08/25" :aria-label="t(`تاریخ صدور ردیف ${num(i + 1)}`, `Issue date of note ${i + 1}`)" /></td>
              <td :class="{ req: !onDemand }">
                <input v-if="!onDemand" v-model="l.dueDate" dir="ltr" placeholder="1405/11/30" :aria-label="t(`تاریخ سررسید ردیف ${num(i + 1)}`, `Due date of note ${i + 1}`)" />
                <input v-else class="ro" disabled :value="t('عندالمطالبه', 'On demand')" :aria-label="t(`تاریخ سررسید ردیف ${num(i + 1)}`, `Due date of note ${i + 1}`)" />
              </td>
              <td class="req"><AmountInput v-model="l.amount" :decimals="0" :aria-label="t(`مبلغ تعهد ردیف ${num(i + 1)}`, `Amount of note ${i + 1}`)" /></td>
              <td><button type="button" class="tr-link danger" @click="removeLine(i)">{{ t('حذف', 'Remove') }}</button></td>
            </tr>
          </tbody>
        </table>
      </div>
      <div class="tr-buttons" style="margin-top: .6rem">
        <button type="button" class="tr-secondary" @click="nextLine">{{ t('+ ثبت سفتهٔ بعدی', '+ Next note') }}</button>
      </div>
      <div class="tr-totals">
        <span><span class="muted">{{ t('تعداد کل:', 'Count:') }}</span> <strong>{{ num(lines.length) }} {{ t('فقره', '') }}</strong></span>
        <span><span class="muted">{{ t('جمع مبلغ تعهد به عدد:', 'Total in figures:') }}</span> <strong class="num">{{ money(total) }} {{ t('ریال', 'Rial') }}</strong></span>
        <span class="wide3"><span class="muted">{{ t('جمع مبلغ تعهد به حروف:', 'Total in words:') }}</span> <strong>{{ amountInWords(total) }} {{ t('ریال', 'Rial') }}</strong></span>
      </div>
    </fieldset>

    <fieldset>
      <legend>{{ t('اطلاعات تکمیلی', 'Supplementary information') }}</legend>
      <label class="wide">{{ t('موضوع تعهد (در سفتهٔ تضمینی)', 'What the guarantee secures') }}
        <input v-model="terms.commitmentSubject" maxlength="500" :disabled="!guarantee" :placeholder="t('مثلاً اجرای تعهدات قرارداد یا انجام کار مشخص', 'e.g. performance of a contract')" />
      </label>
      <label class="wide">{{ t('شرایط و مرجع تأیید استرداد سفته به متعهد', 'Terms of returning the note to the obligor') }}
        <input v-model="terms.returnTerms" maxlength="500" :disabled="terms.reason === 'Debt'" :placeholder="t('مثلاً پس از تأیید انجام موضوع تعهد توسط …', 'e.g. after … confirms the commitment was met')" />
      </label>
      <label class="wide">{{ t('محل نگهداری اصل سند / مرجع الکترونیکی', 'Where the original is kept / electronic reference') }}
        <input v-model="terms.custodyPlace" maxlength="500" :placeholder="t('مثلاً صندوق شرکت، نزد واحد خزانه‌داری', 'e.g. the company safe, treasury')" />
      </label>
      <div class="tr-group">
        <span>{{ t('وضعیت دریافت اصل سند', 'Original note') }}</span>
        <div class="tr-ticks">
          <label><input v-model="terms.originalReceived" type="radio" name="tr-note-original" :value="true" /> {{ t('دریافت شد', 'Received') }}</label>
          <label><input v-model="terms.originalReceived" type="radio" name="tr-note-original" :value="false" /> {{ t('دریافت نشد', 'Not received') }}</label>
        </div>
      </div>
      <label v-if="terms.originalReceived" class="req">{{ t('تاریخ دریافت اصل سند', 'Date the original was received') }}
        <input v-model="terms.originalReceivedOn" dir="ltr" placeholder="1405/08/25" />
      </label>
      <label class="wide">{{ t('شرح نقص یا مغایرت احتمالی', 'Defect or discrepancy, if any') }} <input v-model="terms.discrepancy" maxlength="500" /></label>
      <div class="tr-group wide">
        <span>{{ t('پیوست‌ها', 'Attachments') }}</span>
        <div class="tr-ticks">
          <label v-for="[k, f, e] in attachmentNames" :key="k"><input v-model="terms.attachments[k]" type="checkbox" /> {{ t(f, e) }}</label>
        </div>
      </div>
      <label>{{ t('شماره سند حسابداری', 'Voucher no.') }} <span class="tr-box">{{ t('پس از تایید سند', 'After the voucher is confirmed') }}</span></label>
    </fieldset>

    <fieldset class="tr-block">
      <legend>{{ t('کنترل و تأیید دریافت', 'Checks and confirmation') }}</legend>
      <p class="small" style="margin: 0 0 .4rem">{{ t('کنترل مشخصات روی هر سفته', 'Checked on each note') }}
        <span class="muted">{{ t('(موارد مادهٔ ۳۰۸ قانون تجارت؛ تا همهٔ موارد هر سفته تیک نخورد رسید ثبت نمی‌شود)',
          '(the items of article 308 of the Commercial Code; the receipt is saved only when every item of each note is ticked)') }}</span></p>
      <div class="tr-scroll">
        <table class="tr-table">
          <thead><tr><th>{{ t('سفته', 'Note') }}</th><th v-for="[k, f, e] in checkNames" :key="k">{{ t(f, e) }}</th><th /></tr></thead>
          <tbody>
            <tr v-for="(l, i) in lines" :key="l.key">
              <td>{{ lineTitle(l, i) }}</td>
              <td v-for="[k, f, e] in checkNames" :key="k">
                <span v-if="notAsked(k)" class="muted small">{{ t('ندارد (عندالمطالبه)', 'none (on demand)') }}</span>
                <input v-else v-model="l.checks[k]" type="checkbox" :aria-label="`${t(f, e)} – ${lineTitle(l, i)}`" />
              </td>
              <td><button type="button" class="tr-link" @click="tickAll(l)">{{ t('همه کنترل شد', 'All checked') }}</button></td>
            </tr>
          </tbody>
        </table>
      </div>
      <p class="tr-msg warning">{{ t(confirmationText[0], confirmationText[1]) }}</p>
      <div class="tr-buttons" style="align-items: flex-end">
        <label style="flex: 1; min-width: 12rem">{{ t('تحویل‌دهنده', 'Handed over by') }} <input v-model="terms.deliveredBy" maxlength="500" :placeholder="t('نام تحویل‌دهندهٔ سفته', 'Name')" /></label>
        <label style="flex: 1; min-width: 12rem">{{ t('تحویل‌گیرندهٔ خزانه', 'Received by (treasury)') }} <span class="tr-box">{{ t('کاربر ثبت‌کننده', 'The user saving') }}</span></label>
        <label style="flex: 1; min-width: 12rem">{{ t('تأییدکنندهٔ مجاز', 'Authorised approver') }} <input v-model="terms.approvedBy" maxlength="500" :placeholder="t('نام و سمت', 'Name and position')" /></label>
      </div>
      <p class="muted small" style="margin: .4rem 0 0">{{ t('امضا و تاریخ هر سه نفر روی رسید چاپی می‌آید.', 'The three sign and date the printed receipt.') }}</p>
      <label style="margin-top: .6rem">{{ t('توضیحات', 'Remarks') }} <textarea v-model="header.note" rows="2" maxlength="500" /></label>
    </fieldset>

    <div class="tr-buttons">
      <strong>{{ t('جمع (ریال):', 'Total (Rial):') }} <span class="num">{{ money(total) }}</span></strong>
      <button type="submit" :disabled="saving">{{ t('ثبت رسید (Ctrl+Enter)', 'Save receipt (Ctrl+Enter)') }}</button>
      <button type="button" class="tr-secondary" :disabled="saving" @click="save(true)">{{ t('ثبت و چاپ رسید', 'Save and print') }}</button>
      <button v-if="saved" type="button" class="tr-secondary" @click="print">{{ t(`چاپ رسید شمارهٔ ${num(saved.number)}`, `Print receipt no. ${saved.number}`) }}</button>
    </div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
    <VoucherTable v-if="preview" :voucher="preview" :locale="locale" :account-name="L.accountName" />
    <p v-else class="muted small">{{ t('سند حسابداری این رسید پس از تکمیل فرم اینجا نمایش داده می‌شود.', 'The voucher of this receipt is shown here once the form is filled in.') }}
      <template v-if="previewHint"> ({{ previewHint }})</template></p>

    <NoteReceiptPrint v-if="saved && printing" :receipt="saved" :locale="locale" :ledger-account="accountOf(savedGroup?.ledgerAccountCode)"
                      :department-name="L.departments.value.find(d => d.id === saved!.departmentId)?.name ?? ''"
                      :reference1-title="refTitle(savedGroup?.reference1Id) ?? ''" :reference2-title="refTitle(savedGroup?.reference2Id) ?? ''" />
  </form>
</template>

<style scoped src="./receipts.css"></style>
