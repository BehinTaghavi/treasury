<script setup lang="ts">
/**
 * Receipts (دریافت‌ها), owner 14 Nov 2026: "different types of receipt forms for cheques, current or post-dated, in Rial and foreign
 * currency, promissory notes, cash receipts etc." One tab per kind, as the old program had one form per kind, plus the list.
 * Owner's report no. 6 (19 Nov 2026), item 12: the tabs come in the order he gave, by how often each is used — cheque receipt, cheque
 * steps, cash, «واریز به بانک», «واریز به کارت بانک», note receipt, note steps, legal action — and the list, now «فهرست جامع دریافت‌ها»,
 * stands last in a coloured box. Item 21: «چاپ رسید» of any saved receipt is shown and printed here, under the screen in use.
 */
import { computed, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import type { SavedReceipt } from './savedReceipt'
import ReceiptForm from './ReceiptForm.vue'
import NoteReceiptForm from './NoteReceiptForm.vue'
import ReceiptList from './ReceiptList.vue'
import ChequeOperations from './ChequeOperations.vue'
import LegalAction from './LegalAction.vue'
import ReceiptsPanel from './ReceiptsPanel.vue'
import ReceiptPrint from './ReceiptPrint.vue'
import CardDeposit from './CardDeposit.vue'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')

// The order is the owner's (report no. 6, item 12): the most used and most important first.
const tabs = [
  { id: 'Instruments', title: ['دریافت چک', 'Cheques'] },
  // Owner's report no. 4, item 9, and item 14: every step of a received cheque after receipt.
  { id: 'collection', title: ['عملیات چک‌های دریافتی', 'Received cheque steps'] },
  { id: 'Cash', title: ['دریافت نقدی', 'Cash'] },
  // «واریز به بانک» replaces «واریز به حساب / کارتخوان» (report no. 6, item 12).
  { id: 'BankDeposit', title: ['واریز به بانک', 'Bank deposit'] },
  // Money paid onto the bank card of a petty-cash holder, or onto a company card without a bank account (report no. 6, item 12).
  { id: 'cardDeposit', title: ['واریز به کارت بانک', 'Deposit onto a bank card'] },
  // Owner's report no. 4, item 13, and his form «فرم دریافت سفته» (16 Nov 2026): promissory notes have a form of their own.
  { id: 'Notes', title: ['دریافت سفته', 'Promissory notes'] },
  // Owner, 16 Nov 2026: the steps of a note after receipt (to the bank, collected, handed back, endorsed…).
  { id: 'noteSteps', title: ['عملیات سفته‌های دریافتی', 'Received note steps'] },
  // Owner, 18 Nov 2026: the legal action on a bounced cheque has its own screen.
  { id: 'legal', title: ['اقدام حقوقی', 'Legal action'] },
  // «جامع» was added and the tab is drawn in a coloured box (report no. 6, item 12).
  { id: 'list', title: ['فهرست جامع دریافت‌ها', 'All receipts'], boxed: true },
] as const
type Tab = (typeof tabs)[number]['id']
const tab = ref<Tab>('Instruments')
/** The cheque the legal-action screen opens with, when the user comes from a cheque's row on the steps screen. */
const legalCheque = ref<string | undefined>(undefined)
function openTab(id: Tab) { legalCheque.value = undefined; editing.value = null; notice.value = null; printId.value = ''; tab.value = id }
/** «چاپ رسید» (report no. 6, item 21): the receipt whose sheet is shown under the screen and handed to the printer. */
const printId = ref('')
/** The same receipt asked for again is printed again. */
const printRun = ref(0)
function printReceipt(id: string) { printId.value = id; printRun.value++ }
function startLegal(chequeId: string) { legalCheque.value = chequeId; tab.value = 'legal' }
/**
 * Owner, 18 Nov 2026: on every receipt screen a button «فهرست دریافت‌ها» shows the receipts of that screen's kind under the form. The
 * cheque screens (steps, legal action) show the cheque receipts.
 */
const panelKind = computed(() => (tab.value === 'Cash' || tab.value === 'BankDeposit' ? tab.value
  : tab.value === 'cardDeposit' ? 'BankDeposit' : tab.value === 'Notes' || tab.value === 'noteSteps' ? 'Notes' : 'Instruments'))
/** Counts saved receipts and recorded cheque steps, so an open list shows the new receipt or the cheque's new status at once. */
const savedCount = ref(0)

/**
 * «ویرایش» (owner's report no. 6, items 2 and 6): a saved receipt whose voucher is not confirmed is opened in the form of its kind,
 * from the receipts list or from the list under the form, and saved back under its number. The server says why a receipt can no
 * longer be corrected (voucher confirmed, cancelled). A cheque on which a step was recorded stays locked in the form; the other
 * cheques of its receipt are corrected and removed (owner's answer to Q60). «حذف» of a receipt is offered beside «ویرایش» (Q59).
 */
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const editing = ref<SavedReceipt | null>(null)
const notice = ref<{ kind: 'error' | 'ok'; text: string } | null>(null)
async function startEdit(id: string) {
  notice.value = null
  const r = await api.get<SavedReceipt>(`/api/cashflow/receipts/${id}`)
  if (!r.ok) return void (notice.value = { kind: 'error', text: translate(r.code, props.locale) })
  if (r.value.notCorrectable) return void (notice.value = { kind: 'error', text: translate(r.value.notCorrectable, props.locale) })
  legalCheque.value = undefined
  printId.value = ''
  editing.value = r.value
  tab.value = r.value.kind
  window.scrollTo({ top: 0, behavior: 'smooth' })
}
/** A step was recorded on a cheque screen: the list under it reads the statuses again, and an old message is cleared. */
function stepRecorded() { notice.value = null; savedCount.value++ }
/** A receipt was deleted from the list under the form: if it was open for «ویرایش», the form is closed. */
function receiptDeleted(id: string) {
  if (editing.value?.id === id) editing.value = null
  notice.value = { kind: 'ok', text: t('رسید حذف شد و با عبارت «حذف شده» در فهرست مانده است.', 'The receipt was deleted; it stays in the list marked as deleted.') }
}
function editSaved() {
  const r = editing.value
  editing.value = null
  savedCount.value++
  if (r) notice.value = { kind: 'ok', text: t(`ویرایش رسید شمارهٔ ${r.number.toLocaleString('fa-IR', { useGrouping: false })} ثبت شد.`, `Receipt no. ${r.number} corrected.`) }
}
const hints: Record<string, [string, string]> = {
  Instruments: ['چک‌های یک پرداخت‌کننده با یک شمارهٔ رسید ثبت می‌شوند (سفته‌ها در زبانهٔ «دریافت سفته» ثبت می‌شوند). چک روز یا مدت‌دار از روی سررسید تعیین می‌شود؛ برای چک ارزی ارز و نرخ را وارد کنید.',
    'Cheques of one payer are saved on one receipt (promissory notes have their own tab). Current or post-dated follows the due date; a foreign-currency cheque needs its currency and rate.'],
  Notes: ['سفته‌های یک متعهد با یک شمارهٔ رسید ثبت می‌شوند. این رسید فقط تحویل سفته را نشان می‌دهد و به‌تنهایی تأیید دریافت وجه یا تسویهٔ بدهی نیست.',
    'Promissory notes of one issuer are saved on one receipt. The receipt only records that the notes were handed over; it is not a receipt of money.'],
  Cash: ['وجه نقد دریافتی به صندوق؛ ارز همان ارز صندوق است.', 'Cash received into a cash desk, in the cash desk\'s currency.'],
  BankDeposit: ['واریز مستقیم مشتری به حساب بانکی شرکت: فیش، حواله (ساتنا/پایا)، کارتخوان یا کارت به کارت.',
    'Money the payer paid straight into a company bank account: slip, transfer, POS or card-to-card.'],
  cardDeposit: ['واریز مشتری به کارت بانکِ تنخواه‌گردان‌های شرکت، یا به کارت بانک شرکت که حساب بانکی ندارد.',
    'Money a payer paid onto the bank card of one of the company\'s petty-cash holders, or onto a company bank card without a bank account.'],
  legal: ['چک برگشتی را از فهرست انتخاب کنید؛ مشخصات آن نمایش داده می‌شود. سپس تاریخ و شرح اقدام حقوقی را وارد و اسکن ضمائم را اضافه کنید. با ثبت، آخرین وضعیت چک «اقدام حقوقی» می‌شود.',
    'Choose the bounced cheque from the list; its particulars are shown. Enter the date and description of the legal action and add the scans. Recording it sets the cheque\'s latest status to legal action.'],
}
</script>

<template>
  <section class="tr-receipts" :dir="fa ? 'rtl' : 'ltr'">
    <h2>{{ t('دریافت‌ها', 'Receipts') }}</h2>
    <nav class="tr-tabs" role="tablist">
      <button v-for="x in tabs" :key="x.id" type="button" role="tab" :aria-selected="tab === x.id" :class="{ active: tab === x.id, boxed: 'boxed' in x }" @click="openTab(x.id)">
        {{ fa ? x.title[0] : x.title[1] }}
      </button>
    </nav>
    <p v-if="hints[tab]" class="hint">{{ fa ? hints[tab]![0] : hints[tab]![1] }}</p>
    <p v-if="notice" :class="['notice', notice.kind]" role="status">{{ notice.text }}</p>
    <p v-if="editing" class="notice editing">{{ t(
      `در حال ویرایش رسید شمارهٔ ${editing.number.toLocaleString('fa-IR', { useGrouping: false })} هستید. تا زمانی که «تایید سند» این رسید زده نشده است می‌توانید آن را ویرایش یا حذف کنید؛ شمارهٔ رسید تغییر نمی‌کند. چک یا سفته‌ای که مرحله‌ای (مانند ثبت صیادی) روی آن ثبت شده است قفل می‌ماند.`,
      `You are correcting receipt no. ${editing.number}. It can be corrected or deleted until its voucher is confirmed; its number stays. A cheque or note with a step recorded on it stays locked.`) }}</p>
    <ChequeOperations v-if="tab === 'collection'" :key="'collection' + locale" :api-base="apiBase" :locale="locale" :token="token" @legal="startLegal" @changed="stepRecorded" @print="printReceipt" />
    <ChequeOperations v-else-if="tab === 'noteSteps'" :key="'noteSteps' + locale" notes :api-base="apiBase" :locale="locale" :token="token" @legal="startLegal" @changed="stepRecorded" />
    <CardDeposit v-else-if="tab === 'cardDeposit'" :key="'card' + locale" :api-base="apiBase" :locale="locale" :token="token" @bank="openTab('BankDeposit')" />
    <LegalAction v-else-if="tab === 'legal'" :key="'legal' + locale + (legalCheque ?? '')" :api-base="apiBase" :locale="locale" :token="token" :cheque-id="legalCheque" @changed="stepRecorded" />
    <ReceiptList v-else-if="tab === 'list'" :api-base="apiBase" :locale="locale" :token="token" :fiscal-year="fiscalYear" @edit="startEdit" @print="printReceipt" />
    <NoteReceiptForm v-else-if="tab === 'Notes'" :key="'notes' + locale + (editing?.id ?? '')" :api-base="apiBase" :locale="locale" :token="token" :edit="editing ?? undefined"
                     @saved="editing ? editSaved() : savedCount++" @cancelled="editing = null" />
    <ReceiptForm v-else :key="tab + locale + (editing?.id ?? '')" :kind="tab" :api-base="apiBase" :locale="locale" :token="token" :edit="editing ?? undefined"
                 @saved="editing ? editSaved() : savedCount++" @cancelled="editing = null" @print="printReceipt" />
    <ReceiptsPanel v-if="tab !== 'list' && tab !== 'cardDeposit'" :key="'panel' + panelKind + locale" :kind="panelKind" :api-base="apiBase" :locale="locale" :token="token"
                   :fiscal-year="fiscalYear" :refresh-key="savedCount" :editing-id="editing?.id" @edit="startEdit" @deleted="receiptDeleted" @print="printReceipt" />
    <!-- report no. 6, item 21: the printed receipt of the saved receipt chosen with «چاپ رسید» -->
    <ReceiptPrint v-if="printId" :key="printId + printRun" :receipt-id="printId" :api-base="apiBase" :locale="locale" :token="token" @close="printId = ''" />
  </section>
</template>

<style scoped>
.tr-receipts { font-family: Vazirmatn, Tahoma, sans-serif; color: #1f2933; }
.tr-tabs { display: flex; flex-wrap: wrap; gap: .25rem; border-bottom: 1px solid #e4e7eb; margin-bottom: .75rem; }
.tr-tabs button { padding: .55rem .9rem; border: 0; border-bottom: 3px solid transparent; background: transparent; color: #1f2933; font: inherit; cursor: pointer; }
.tr-tabs button.active { border-bottom-color: #2563eb; color: #1d4ed8; font-weight: 600; }
/* «فهرست جامع دریافت‌ها» stands in a coloured box (owner's report no. 6, item 12). */
.tr-tabs button.boxed { margin-inline-start: .5rem; margin-bottom: .3rem; padding: .45rem 1rem; border: 1px solid #0f766e; border-radius: 8px; background: #ccfbf1; color: #115e59; font-weight: 700; }
.tr-tabs button.boxed.active { background: #0f766e; color: #fff; border-color: #0f766e; }
.hint { color: #52606d; font-size: .85rem; margin: .25rem 0 0; }
.notice { padding: .5rem .75rem; border-radius: 6px; margin: .5rem 0; }
.notice.error { background: #fdecea; color: #8a1c1c; }
.notice.ok { background: #e6f4ea; color: #1e5631; }
.notice.editing { background: #fff7e0; color: #7a5200; }
</style>
