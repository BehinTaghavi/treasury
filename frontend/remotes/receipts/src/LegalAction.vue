<script setup lang="ts">
/**
 * «اقدام حقوقی» (owner, 18 Nov 2026): the legal action on a bounced cheque has its own screen. The cheque is chosen from a list box of
 * the bounced cheques and notes and of the guarantee cheques and notes (owner, 18 Nov 2026: they may get legal action too); its particulars are shown; the date of the action and its description are entered, with a place for the scans
 * of the action's papers. Recording it puts the cheque in the status «اقدام حقوقی». Below, the cheques already in legal action are
 * listed with their action and scans; scans can still be added or removed there.
 * Owner's answers of 18 Nov 2026 (Q38): the action has its voucher (Dr «اسناد واخواست شده حقوقی شده» / Cr «اسناد واخواست شده»), shown
 * with «تایید سند» on the opened cheque; and, "unlikely as it is", the action can be taken on a cheque still in the cash desk (then
 * Cr «اسناد نزد صندوق»): a tick box adds those cheques to the list box.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { chequeStateName, createApi, formatAmount, normaliseJalali, searchKey, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'
import { useLookups } from './lookups'
import { ACCEPT, MAX_BYTES, openLegalFile, sizeText, uploadLegalFile, type LegalFile } from './legalFiles'
import VoucherTable, { type Voucher } from './VoucherTable.vue'

interface Step { action: string; to: string; date: string; userId: string; reference: string | null; note: string | null; details: { accountId: string | null } | null }
interface Cheque {
  id: string; kind: string; number: string | null; bankName: string | null; amount: number; currency: string; amountInRial: number; issueDate: string; dueDate: string | null
  counterparty: string; counterpartyDetailCode: string | null; purpose: string; state: string; history: Step[]; allowedActions: string[]
  details: { branchName: string | null; accountNumber: string | null; accountHolder: string | null; sayadId: string | null } | null
}

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; chequeId?: string }>(),
  { apiBase: '', locale: 'fa', token: undefined, chequeId: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => (fa.value ? n.toLocaleString('fa-IR') : String(n))
const L = useLookups(api)

const bounced = ref<Cheque[]>([])
/** In the cash desk and allowed a legal action: guarantee cheques and notes, and (the rare case of the answer to Q38) ordinary cheques. */
const inDesk = ref<Cheque[]>([])
const withDesk = ref(false)
const guarantee = (c: Cheque) => c.kind === 'GuaranteeCheque' || c.kind === 'GuaranteeNote'
const kindNames: Record<string, [string, string]> = {
  OrdinaryCheque: ['چک', 'Cheque'], BankCheque: ['چک بانکی', 'Bank cheque'], GuaranteeCheque: ['چک تضمینی', 'Guarantee cheque'],
  PromissoryNote: ['سفته', 'Note'], GuaranteeNote: ['سفته تضمینی', 'Guarantee note'], Instalment: ['اقساط', 'Instalment'], Other: ['سایر', 'Other'],
}
const kindName = (c: Cheque) => (kindNames[c.kind] ? kindNames[c.kind]![fa.value ? 0 : 1] : c.kind)
const inLegal = ref<Cheque[]>([])
const busy = ref(false)
const message = ref<{ kind: 'error' | 'ok' | 'warning'; text: string } | null>(null)
const show = (kind: 'error' | 'ok' | 'warning', text: string) => { message.value = { kind, text } }

async function load() {
  const [b, l, d, s] = await Promise.all([
    api.get<Cheque[]>('/api/cashflow/instruments/received?state=Bounced'),
    api.get<Cheque[]>('/api/cashflow/instruments/received?state=InLegal'),
    api.get<Cheque[]>('/api/cashflow/instruments/received?state=InCashDesk'),
    api.get<Cheque[]>('/api/cashflow/instruments/received?state=SayadRegistered'),
  ])
  if (b.ok) bounced.value = b.value
  if (l.ok) inLegal.value = l.value
  inDesk.value = [...(d.ok ? d.value : []), ...(s.ok ? s.value : [])].filter(c => c.allowedActions.includes('SendToLegal'))
}

// ---- the form: choose a bounced cheque, enter the action ----
const form = reactive({ chequeId: '', date: todayJalali(), description: '' })
const queued = ref<File[]>([])
const fileInput = ref<HTMLInputElement | null>(null)
const candidates = computed(() => [...bounced.value, ...inDesk.value.filter(c => guarantee(c) || withDesk.value)])
const choices = computed<Choice[]>(() => candidates.value.map(c => ({
  value: c.id,
  label: `${c.number ?? ''} – ${c.bankName ?? kindName(c)} – ${c.counterparty}`,
  hint: `${kindName(c)} · ${money(c.amount)}${c.currency !== 'IRR' ? ' ' + c.currency : ''}${c.dueDate ? ` · ${t('سررسید', 'due')} ${c.dueDate}` : ''} · ${chequeStateName(c.state, props.locale)}`,
})))
const chosen = computed(() => candidates.value.find(c => c.id === form.chequeId) ?? null)

/** The bounce of a cheque: the bank's notice, its date, the reason and the account it was deposited at. */
function bounceOf(c: Cheque) {
  const s = [...c.history].reverse().find(h => h.action === 'RecordBounced' || h.action === 'RecordEndorsementReturned')
  const a = L.bankAccounts.value.find(x => x.id === s?.details?.accountId)
  return { notice: s?.reference ?? '', date: s?.date ?? '', reason: s?.note ?? '', account: a ? L.accountTitle(a) : '' }
}
/** The legal action of a cheque in legal action: its date, description and who recorded it. */
const actionOf = (c: Cheque) => [...c.history].reverse().find(h => h.action === 'SendToLegal')

function tooLarge(files: File[]) {
  const big = files.filter(f => f.size > MAX_BYTES)
  if (big.length) show('error', `${translate('Instrument.AttachmentTooLarge', props.locale)} (${big.map(f => f.name).join('، ')})`)
  return files.filter(f => f.size <= MAX_BYTES)
}
function chooseFiles(e: Event) {
  const input = e.target as HTMLInputElement
  queued.value.push(...tooLarge(Array.from(input.files ?? [])))
  input.value = ''
}

async function upload(chequeId: string, files: File[]): Promise<string[]> {
  const failed: string[] = []
  for (const f of files) {
    const reason = await uploadLegalFile(props.apiBase, props.token, chequeId, f, props.locale)
    if (reason) failed.push(`${f.name}: ${reason}`)
  }
  return failed
}

async function record() {
  message.value = null
  const c = chosen.value
  if (!c) return show('error', t('چک را از فهرست انتخاب کنید.', 'Choose the cheque from the list.'))
  const date = normaliseJalali(form.date)
  if (!date) return show('error', translate('FiscalDate.Invalid', props.locale))
  if (!form.description.trim()) return show('error', translate('Instrument.LegalActionDescriptionRequired', props.locale))
  busy.value = true
  const r = await api.post<string>(`/api/cashflow/instruments/${c.id}/actions/SendToLegal`, { date, note: form.description, destination: null, reference: null })
  if (!r.ok) { busy.value = false; return show('error', translate(r.code, props.locale)) }
  const files = queued.value
  const failed = await upload(c.id, files)
  busy.value = false
  const done = t(`اقدام حقوقی ${kindName(c)} ${c.number} ثبت شد؛ آخرین وضعیت آن «${chequeStateName('InLegal', props.locale)}» است.${guarantee(c) ? '' : ' سند آن را در پایین همین صفحه تایید کنید.'}`,
    `Legal action recorded for ${kindName(c)} ${c.number}; its latest status is "${chequeStateName('InLegal', props.locale)}".${guarantee(c) ? '' : ' Confirm its voucher below.'}`)
  const sent = files.length - failed.length
  const scans = sent ? t(` ${num(sent)} فایل ضمیمه شد.`, ` ${sent} scan(s) attached.`) : ''
  if (failed.length) show('warning', `${done}${scans} ${t('این فایل‌ها ضمیمه نشدند', 'Not attached')} – ${failed.join('؛ ')}`)
  else show('ok', done + scans)
  form.chequeId = ''
  form.description = ''
  form.date = todayJalali()
  queued.value = []
  await load()
  await open(c.id)
}

// ---- cheques in legal action: their action and scans ----
const search = ref('')
const shown = computed(() => {
  const key = searchKey(search.value)
  return inLegal.value.filter(c => !key || searchKey(`${c.number} ${c.counterparty} ${c.bankName ?? ''} ${actionOf(c)?.note ?? ''}`).includes(key))
})
const openId = ref('')
const opened = computed(() => inLegal.value.find(c => c.id === openId.value) ?? null)
const files = ref<LegalFile[]>([])
/** The voucher of the legal action (the step SendToLegal of the opened cheque). */
const voucher = ref<Voucher | null>(null)
const confirming = ref(false)
async function open(id: string) {
  openId.value = id
  const [r, v] = await Promise.all([api.get<LegalFile[]>(`/api/cashflow/instruments/${id}/legal-files`), api.get<Voucher[]>(`/api/cashflow/instruments/${id}/vouchers`)])
  files.value = r.ok ? r.value : []
  const c = inLegal.value.find(x => x.id === id)
  const step = c ? c.history.map(h => h.action).lastIndexOf('SendToLegal') + 1 : 0
  voucher.value = (v.ok ? v.value : []).find(x => x.step === step) ?? null
}
async function confirmVoucher() {
  if (!opened.value || !voucher.value || confirming.value) return
  confirming.value = true
  const r = await api.post<string>(`/api/cashflow/instruments/${opened.value.id}/vouchers/${voucher.value.step}/confirm`, {})
  confirming.value = false
  if (!r.ok) return show('error', translate(r.code, props.locale))
  show('ok', t('سند اقدام حقوقی تایید شد.', 'The voucher of the legal action is confirmed.'))
  await open(opened.value.id)
}
async function addFiles(e: Event) {
  const input = e.target as HTMLInputElement
  const chosenFiles = tooLarge(Array.from(input.files ?? []))
  input.value = ''
  if (!opened.value || !chosenFiles.length) return
  busy.value = true
  const failed = await upload(opened.value.id, chosenFiles)
  busy.value = false
  if (failed.length) show('error', `${t('ضمیمه نشد', 'Not attached')} – ${failed.join('؛ ')}`)
  else show('ok', t(`${num(chosenFiles.length)} فایل ضمیمه شد.`, `${chosenFiles.length} scan(s) attached.`))
  await open(opened.value.id)
}
async function removeFile(f: LegalFile) {
  if (!opened.value) return
  const r = await api.post<string>(`/api/cashflow/instruments/${opened.value.id}/legal-files/${f.id}/delete`, {})
  if (!r.ok) return show('error', translate(r.code, props.locale))
  await open(opened.value.id)
}

onMounted(async () => {
  await L.load()
  await load()
  if (props.chequeId && inDesk.value.some(c => c.id === props.chequeId && !guarantee(c))) withDesk.value = true
  if (props.chequeId && candidates.value.some(c => c.id === props.chequeId)) form.chequeId = props.chequeId
  else if (props.chequeId && inLegal.value.some(c => c.id === props.chequeId)) await open(props.chequeId)
})
</script>

<template>
  <div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form class="tr-form" @submit.prevent="record">
      <fieldset>
        <legend>{{ t('چک یا سفتهٔ مورد اقدام حقوقی', 'Cheque or note') }}</legend>
        <label class="full">{{ t('انتخاب از فهرست چک‌ها و سفته‌های برگشتی و چک‌ها و سفته‌های تضمینی (شماره، بانک، پرداخت‌کننده)', 'Choose from the bounced cheques and notes and the guarantee cheques and notes (number, bank, payer)') }}
          <SearchSelect v-model="form.chequeId" :options="choices" required :aria-label="t('چک برگشتی', 'Bounced cheque')"
                        :none-text="t('چک برگشتی یا تضمینی‌ای برای اقدام حقوقی نیست', 'No bounced or guarantee cheque')" />
        </label>
        <label class="tr-check full">
          <input v-model="withDesk" type="checkbox" @change="form.chequeId = ''" />
          {{ t('حالت استثنایی: چک‌های عادیِ نزد صندوق هم در فهرست باشند (اقدام حقوقی روی چکی که هنوز برگشت نخورده)', 'Exceptional case: also list ordinary cheques still in the cash desk (legal action on a cheque that has not bounced)') }}
        </label>
        <template v-if="chosen">
          <dl class="tr-facts full">
            <div><dt>{{ t('نوع', 'Kind') }}</dt><dd>{{ kindName(chosen) }}</dd></div>
            <div><dt>{{ t('شماره', 'No.') }}</dt><dd class="num">{{ chosen.number }}</dd></div>
            <div><dt>{{ t('شناسهٔ صیاد', 'Sayad ID') }}</dt><dd class="num">{{ chosen.details?.sayadId ?? '—' }}</dd></div>
            <div><dt>{{ t('بانک و شعبه', 'Bank and branch') }}</dt><dd>{{ chosen.bankName }}<template v-if="chosen.details?.branchName"> – {{ chosen.details.branchName }}</template></dd></div>
            <div><dt>{{ t('شماره حساب چک', 'Account no.') }}</dt><dd class="num">{{ chosen.details?.accountNumber ?? '—' }}</dd></div>
            <div><dt>{{ t('پرداخت‌کننده', 'Payer') }}</dt><dd>{{ chosen.counterparty }} <span class="muted small">{{ chosen.counterpartyDetailCode }}</span></dd></div>
            <div><dt>{{ t('صاحب حساب', 'Account holder') }}</dt><dd>{{ chosen.details?.accountHolder ?? '—' }}</dd></div>
            <div><dt>{{ t('مبلغ', 'Amount') }}</dt><dd class="num">{{ money(chosen.amount) }} {{ chosen.currency === 'IRR' ? t('ریال', 'Rial') : chosen.currency }}</dd></div>
            <div><dt>{{ t('بابت', 'Purpose') }}</dt><dd>{{ chosen.purpose }}</dd></div>
            <div><dt>{{ t('تاریخ دریافت', 'Received') }}</dt><dd class="num">{{ chosen.issueDate }}</dd></div>
            <div><dt>{{ t('سررسید', 'Due') }}</dt><dd class="num">{{ chosen.dueDate ?? '—' }}</dd></div>
            <div><dt>{{ t('وضعیت فعلی', 'Status now') }}</dt><dd>{{ chequeStateName(chosen.state, locale) }}</dd></div>
            <template v-if="chosen.state === 'Bounced'">
              <div><dt>{{ t('اعلامیهٔ برگشت', 'Bounce notice') }}</dt><dd class="num">{{ bounceOf(chosen).notice }} – {{ bounceOf(chosen).date }}</dd></div>
              <div><dt>{{ t('علت برگشت', 'Reason') }}</dt><dd>{{ bounceOf(chosen).reason || '—' }}</dd></div>
            </template>
            <div v-if="bounceOf(chosen).account"><dt>{{ t('حساب واگذاری', 'Deposited at') }}</dt><dd>{{ bounceOf(chosen).account }}</dd></div>
          </dl>
        </template>
      </fieldset>

      <fieldset>
        <legend>{{ t('اقدام حقوقی', 'Legal action') }}</legend>
        <label class="req">{{ t('تاریخ اقدام', 'Date of the action') }} <input v-model="form.date" dir="ltr" :disabled="!chosen" /></label>
        <label class="full req">{{ t('شرح اقدام حقوقی', 'Description of the legal action') }}
          <textarea v-model="form.description" rows="3" :disabled="!chosen" />
        </label>
        <label class="full">{{ t('اسکن ضمائم اقدام حقوقی (PDF، JPG یا PNG، هر فایل حداکثر ۵ مگابایت)', 'Scans of the action\'s papers (PDF, JPG or PNG, up to 5 MB each)') }}
          <input ref="fileInput" type="file" :accept="ACCEPT" multiple :disabled="!chosen" @change="chooseFiles" />
        </label>
        <ul v-if="queued.length" class="tr-files full">
          <li v-for="(f, i) in queued" :key="i">
            <span>{{ f.name }}</span> <span class="muted small num">{{ sizeText(f.size) }}</span>
            <button type="button" class="tr-link danger" @click="queued.splice(i, 1)">{{ t('حذف', 'Remove') }}</button>
          </li>
        </ul>
        <div class="tr-buttons full">
          <button type="submit" :disabled="busy || !chosen">{{ t('ثبت اقدام حقوقی', 'Record the legal action') }}</button>
        </div>
      </fieldset>
    </form>

    <h3>{{ t('چک‌های در وضعیت اقدام حقوقی', 'Cheques in legal action') }} <span class="muted small">({{ num(inLegal.length) }})</span></h3>
    <label style="max-width: 28rem">{{ t('جستجو (شماره، پرداخت‌کننده، بانک، شرح)', 'Search (number, payer, bank, description)') }} <input v-model="search" /></label>
    <div class="tr-scroll">
      <table class="tr-table">
        <thead>
          <tr>
            <th>{{ t('تاریخ اقدام', 'Action date') }}</th><th>{{ t('شماره چک', 'Cheque no.') }}</th><th>{{ t('بانک', 'Bank') }}</th><th>{{ t('پرداخت‌کننده', 'Payer') }}</th>
            <th>{{ t('مبلغ', 'Amount') }}</th><th>{{ t('سررسید', 'Due') }}</th><th>{{ t('شرح اقدام حقوقی', 'Description') }}</th><th>{{ t('وضعیت', 'Status') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="c in shown" :key="c.id" :class="['clickable', { selected: openId === c.id }]" @click="open(c.id)">
            <td class="num">{{ actionOf(c)?.date }}</td>
            <td class="num">{{ c.number }}</td>
            <td>{{ c.bankName }}</td>
            <td>{{ c.counterparty }}</td>
            <td class="num">{{ money(c.amount) }} <span v-if="c.currency !== 'IRR'">{{ c.currency }}</span></td>
            <td class="num">{{ c.dueDate }}</td>
            <td>{{ actionOf(c)?.note }}</td>
            <td>{{ chequeStateName(c.state, locale) }}</td>
          </tr>
          <tr v-if="!shown.length"><td colspan="8" class="muted">{{ t('چکی در وضعیت اقدام حقوقی نیست.', 'No cheque in legal action.') }}</td></tr>
        </tbody>
      </table>
    </div>

    <section v-if="opened" class="tr-card">
      <div class="tr-buttons">
        <h3 style="margin: 0">{{ t(`اقدام حقوقی چک ${opened.number}`, `Legal action on cheque ${opened.number}`) }}</h3>
        <span class="tr-tag">{{ t('آخرین وضعیت', 'Latest status') }}: {{ chequeStateName(opened.state, locale) }}</span>
        <button type="button" class="tr-link" @click="openId = ''">{{ t('بستن', 'Close') }}</button>
      </div>
      <p class="small">
        {{ opened.bankName }}<template v-if="opened.details?.branchName"> – {{ opened.details.branchName }}</template> · {{ opened.counterparty }} ·
        {{ t('مبلغ', 'Amount') }} <span class="num">{{ money(opened.amount) }}</span> {{ opened.currency === 'IRR' ? '' : opened.currency }} ·
        {{ t('سررسید', 'due') }} <span class="num">{{ opened.dueDate }}</span>
        <template v-if="bounceOf(opened).notice"> · {{ t('اعلامیهٔ برگشت', 'bounce notice') }} <span class="num">{{ bounceOf(opened).notice }} – {{ bounceOf(opened).date }}</span></template>
      </p>
      <p><strong>{{ t('تاریخ اقدام', 'Date of the action') }}:</strong> <span class="num">{{ actionOf(opened)?.date }}</span> ·
        <strong>{{ t('ثبت', 'By') }}:</strong> {{ actionOf(opened)?.userId }}</p>
      <p class="tr-text"><strong>{{ t('شرح اقدام حقوقی', 'Description') }}:</strong> {{ actionOf(opened)?.note }}</p>

      <h4>{{ t('اسکن ضمائم اقدام حقوقی', 'Scans of the action\'s papers') }} <span class="muted small">({{ num(files.length) }})</span></h4>
      <ul v-if="files.length" class="tr-files">
        <li v-for="f in files" :key="f.id">
          <button type="button" class="tr-link" @click="openLegalFile(apiBase, token, opened.id, f.id)">{{ f.fileName }}</button>
          <span class="muted small num">{{ sizeText(f.size) }}</span> <span class="muted small">{{ f.uploadedBy }}</span>
          <button type="button" class="tr-link danger" @click="removeFile(f)">{{ t('حذف', 'Remove') }}</button>
        </li>
      </ul>
      <p v-else class="muted small">{{ t('فایلی ضمیمه نشده است.', 'No scan attached.') }}</p>
      <label style="max-width: 32rem">{{ t('افزودن فایل (PDF، JPG یا PNG، حداکثر ۵ مگابایت)', 'Add files (PDF, JPG or PNG, up to 5 MB)') }}
        <input type="file" :accept="ACCEPT" multiple :disabled="busy" @change="addFiles" />
      </label>
      <VoucherTable v-if="voucher" :voucher="voucher" :locale="locale" :account-name="L.accountName" can-confirm :busy="confirming" @confirm="confirmVoucher" />
      <p v-else-if="guarantee(opened)" class="muted small">{{ t('چک و سفتهٔ تضمینی در حساب‌های انتظامی نگهداری می‌شوند؛ سند اقدام حقوقی آن‌ها هنوز تعیین نشده است (پرسش Q42).',
        'Guarantee cheques and notes are kept in the memorandum accounts; the voucher of their legal action is not decided yet (question Q42).') }}</p>
      <p class="muted small">{{ t('مراحل بعدی این چک (تعویض چک، ابطال) در زبانهٔ «عملیات چک‌های دریافتی» ثبت می‌شود.',
        'The next steps of this cheque (replacement, cancellation) are recorded on the «Received cheque steps» tab.') }}</p>
    </section>
  </div>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
.full { grid-column: 1 / -1; }
.tr-facts { display: grid; grid-template-columns: repeat(auto-fill, minmax(17rem, 1fr)); gap: .35rem 1.25rem; margin: 0; padding: .6rem .75rem; background: #f8fafc; border-radius: 6px; }
.tr-facts div { display: flex; gap: .5rem; align-items: baseline; }
.tr-facts dt { color: #52606d; font-size: .8rem; min-width: 6.5rem; }
.tr-facts dd { margin: 0; font-weight: 600; }
.tr-files { list-style: none; margin: 0 0 .5rem; padding: 0; display: grid; gap: .25rem; }
.tr-files li { display: flex; gap: .5rem; align-items: center; }
.tr-card { margin-top: 1.25rem; padding: .75rem; border: 1px solid #d9e2ec; border-radius: 8px; }
.tr-text { white-space: pre-wrap; }
textarea { resize: vertical; }
</style>
