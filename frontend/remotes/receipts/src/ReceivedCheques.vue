<script setup lang="ts">
/**
 * Received cheques and notes (دریافت چک و سفته).
 * - Keyboard first: Enter moves to the next field, Ctrl+Enter saves (like the old Access forms).
 * - Only the actions the server allows for the instrument's current state are offered (doc 6.2).
 * - Errors are shown from the API's error codes, translated; never raw server text.
 */
import { computed, reactive, ref } from 'vue'
import { createApi, formatAmount, normaliseJalali, parseAmount, todayJalali, translate, type Locale } from '@treasury/ui-kit'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), {
  apiBase: '',
  locale: 'fa',
  token: undefined,
})

interface InstrumentView {
  id: string
  kind: string
  number: string | null
  bankName: string | null
  amount: number
  counterparty: string
  dueDate: string | null
  state: string
  allowedActions: string[]
  history: { action: string; date: string; userId: string }[]
}

const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (fa: string, en: string) => (props.locale === 'fa' ? fa : en)

const kinds = [
  { value: 'OrdinaryCheque', fa: 'چک عادی', en: 'Ordinary cheque' },
  { value: 'BankCheque', fa: 'چک بانکی', en: 'Bank cheque' },
  { value: 'GuaranteeCheque', fa: 'چک انتظامی', en: 'Guarantee cheque' },
  { value: 'PromissoryNote', fa: 'سفته', en: 'Promissory note' },
  { value: 'GuaranteeNote', fa: 'سفته انتظامی', en: 'Guarantee note' },
  { value: 'Instalment', fa: 'اقساط', en: 'Instalment' },
  { value: 'Other', fa: 'سایر', en: 'Other' },
]
const stateNames: Record<string, [string, string]> = {
  InCashDesk: ['نزد صندوق', 'In cash desk'],
  DepositedForCollection: ['واگذار جهت وصول', 'Sent for collection'],
  Cleared: ['وصول شده', 'Collected'],
  Bounced: ['برگشت شده', 'Bounced'],
  ReturnedToIssuer: ['عودت شده', 'Returned'],
  Endorsed: ['واگذار به سایرین', 'Endorsed'],
}
const actionNames: Record<string, [string, string]> = {
  DepositForCollection: ['واگذاری جهت وصول', 'Send for collection'],
  RecordCleared: ['ثبت وصول', 'Record collected'],
  RecordBounced: ['ثبت برگشت', 'Record bounced'],
  ReturnToIssuer: ['عودت', 'Return to issuer'],
  Endorse: ['خرج (واگذاری به سایرین)', 'Endorse'],
}
const needsDestination = new Set(['DepositForCollection', 'Endorse'])

const form = reactive({ kind: 'OrdinaryCheque', number: '', bankName: '', amount: '', receivedOn: todayJalali(), dueDate: '', drawerName: '', purpose: '' })
const saving = ref(false)
const message = ref<{ kind: 'error' | 'warning' | 'ok'; text: string } | null>(null)
const items = ref<InstrumentView[]>([])
interface PendingAction { id: string; action: string; date: string; destination: string; note: string }
const pending = ref<PendingAction | null>(null)

function show(kind: 'error' | 'warning' | 'ok', text: string) {
  message.value = { kind, text }
}

/** Enter moves to the next field; Ctrl+Enter saves. */
function onKey(e: KeyboardEvent) {
  if (e.key !== 'Enter') return
  e.preventDefault()
  if (e.ctrlKey) {
    void save()
    return
  }
  const fields = Array.from((e.currentTarget as HTMLElement).querySelectorAll<HTMLElement>('input, select, button[type=submit]'))
  const i = fields.indexOf(e.target as HTMLElement)
  fields[i + 1]?.focus()
}

async function refresh(id: string) {
  const r = await api.get<InstrumentView>(`/api/cashflow/instruments/${id}`)
  if (!r.ok) return
  const i = items.value.findIndex(x => x.id === id)
  if (i >= 0) items.value[i] = r.value
  else items.value.unshift(r.value)
}

async function save() {
  const amount = parseAmount(form.amount)
  const receivedOn = normaliseJalali(form.receivedOn)
  const dueDate = form.dueDate ? normaliseJalali(form.dueDate) : null
  if (amount === null) return show('error', translate('Money.NotPositive', props.locale))
  if (!receivedOn || (form.dueDate && !dueDate)) return show('error', translate('FiscalDate.Invalid', props.locale))

  saving.value = true
  const r = await api.post<string>('/api/cashflow/instruments/received', {
    kind: form.kind,
    number: form.number,
    bankName: form.bankName,
    amount: { amount },
    receivedOn,
    dueDate,
    drawerId: crypto.randomUUID(), // until the Counterparties screen exists
    drawerName: form.drawerName,
    purpose: form.purpose,
  })
  saving.value = false
  if (!r.ok) return show('error', translate(r.code, props.locale))
  await refresh(r.value)
  if (r.warnings.length) show('warning', r.warnings.map(w => translate(w, props.locale)).join(' '))
  else show('ok', t('ثبت شد.', 'Saved.'))
  Object.assign(form, { number: '', amount: '', dueDate: '', drawerName: '', purpose: '' })
}

function startAction(id: string, action: string) {
  pending.value = { id, action, date: todayJalali(), destination: '', note: '' }
}

async function confirmAction() {
  const p = pending.value
  if (!p) return
  const date = normaliseJalali(p.date)
  if (!date) return show('error', translate('FiscalDate.Invalid', props.locale))
  const r = await api.post<string>(`/api/cashflow/instruments/${p.id}/actions/${p.action}`, {
    date,
    destination: p.destination || null,
    note: p.note || null,
  })
  if (!r.ok) return show('error', translate(r.code, props.locale))
  await refresh(p.id)
  pending.value = null
  show('ok', t('عملیات ثبت شد.', 'Done.'))
}
</script>

<template>
  <section class="tr-receipts" :dir="fa ? 'rtl' : 'ltr'">
    <h2>{{ t('دریافت چک و سفته', 'Received cheques and notes') }}</h2>

    <form class="tr-form" @keydown="onKey" @submit.prevent="save">
      <label>{{ t('نوع', 'Kind') }}
        <select v-model="form.kind">
          <option v-for="k in kinds" :key="k.value" :value="k.value">{{ fa ? k.fa : k.en }}</option>
        </select>
      </label>
      <label>{{ t('شماره', 'Number') }}<input v-model="form.number" autofocus inputmode="numeric" /></label>
      <label>{{ t('بانک', 'Bank') }}<input v-model="form.bankName" /></label>
      <label>{{ t('مبلغ (ریال)', 'Amount (Rial)') }}<input v-model="form.amount" inputmode="numeric" /></label>
      <label>{{ t('تاریخ دریافت', 'Received on') }}<input v-model="form.receivedOn" placeholder="1405/08/18" /></label>
      <label>{{ t('سررسید', 'Due date') }}<input v-model="form.dueDate" placeholder="1405/09/30" /></label>
      <label>{{ t('صادرکننده', 'Drawer') }}<input v-model="form.drawerName" /></label>
      <label>{{ t('بابت', 'Purpose') }}<input v-model="form.purpose" /></label>
      <button type="submit" :disabled="saving">{{ t('ثبت (Ctrl+Enter)', 'Save (Ctrl+Enter)') }}</button>
    </form>

    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <div v-if="pending" class="tr-action">
      <strong>{{ fa ? actionNames[pending.action]?.[0] : actionNames[pending.action]?.[1] }}</strong>
      <label>{{ t('تاریخ', 'Date') }}<input v-model="pending.date" /></label>
      <label v-if="needsDestination.has(pending.action)">{{ t('حساب واگذاری / گیرنده', 'Collection account / endorsee') }}<input v-model="pending.destination" /></label>
      <label v-if="pending.action === 'RecordBounced'">{{ t('علت', 'Reason') }}<input v-model="pending.note" /></label>
      <button type="button" @click="confirmAction">{{ t('تایید', 'Confirm') }}</button>
    </div>

    <table v-if="items.length" class="tr-table">
      <thead>
        <tr>
          <th>{{ t('شماره', 'Number') }}</th><th>{{ t('بانک', 'Bank') }}</th><th>{{ t('مبلغ', 'Amount') }}</th>
          <th>{{ t('سررسید', 'Due') }}</th><th>{{ t('صادرکننده', 'Drawer') }}</th><th>{{ t('وضعیت', 'State') }}</th><th />
        </tr>
      </thead>
      <tbody>
        <tr v-for="i in items" :key="i.id">
          <td>{{ i.number }}</td><td>{{ i.bankName }}</td><td class="num">{{ formatAmount(i.amount, locale) }}</td>
          <td>{{ i.dueDate }}</td><td>{{ i.counterparty }}</td>
          <td>{{ fa ? stateNames[i.state]?.[0] : stateNames[i.state]?.[1] }}</td>
          <td>
            <button v-for="a in i.allowedActions" :key="a" type="button" class="tr-link" @click="startAction(i.id, a)">
              {{ fa ? actionNames[a]?.[0] : actionNames[a]?.[1] }}
            </button>
          </td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<style scoped>
.tr-receipts { font-family: Vazirmatn, Tahoma, sans-serif; color: #1f2933; }
.tr-form { display: grid; grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr)); gap: .75rem; align-items: end; }
.tr-form label, .tr-action label { display: flex; flex-direction: column; gap: .25rem; font-size: .85rem; }
input, select { padding: .45rem .5rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; }
input:focus, select:focus { outline: 2px solid #2563eb; outline-offset: 1px; }
button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .6; }
.tr-link { background: transparent; color: #1d4ed8; border-color: transparent; padding: .2rem .4rem; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.warning { background: #fff7e0; color: #7a5200; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-action { display: flex; gap: .75rem; align-items: end; margin: 1rem 0; padding: .75rem; background: #f5f7fa; border-radius: 8px; }
.tr-table { width: 100%; border-collapse: collapse; margin-top: 1rem; }
.tr-table th, .tr-table td { padding: .45rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; }
.num { font-variant-numeric: tabular-nums; }
</style>
