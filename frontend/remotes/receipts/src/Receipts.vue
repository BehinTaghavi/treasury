<script setup lang="ts">
/**
 * Receipts (دریافت‌ها), owner 14 Nov 2026: "different types of receipt forms for cheques, current or post-dated, in Rial and foreign
 * currency, promissory notes, cash receipts etc." One tab per kind, as the old program had one form per kind, plus the list.
 * Owner's report no. 6 (19 Nov 2026), item 12: the tabs come in the order he gave, by how often each is used — cheque receipt, cheque
 * steps, cash, «واریز به بانک», «واریز به کارت بانک», note receipt, note steps, legal action — and the list, now «فهرست جامع دریافت‌ها»,
 * stands last in a coloured box. Item 21: «چاپ رسید» of any saved receipt is shown and printed here, under the screen in use.
 * «واریز به کارت بانک» is a receipt form of its own kind since the owner's answers to Q68–Q70 (20 Nov 2026).
 * Owner, 20 Nov 2026 (the design he approved): the kinds are no longer a row of tabs but a column beside the main menu (receiptsMenu.ts,
 * ui-kit SectionMenu), in the same order; each screen has its name as heading, one line of explanation, and the rest of the
 * explanation behind «راهنمای این فرم».
 */
import { computed, ref } from 'vue'
import { createApi, menuItems, translate, type Locale } from '@treasury/ui-kit'
import SectionMenu from '@treasury/ui-kit/SectionMenu.vue'
import '@treasury/ui-kit/required.css'
import { receiptsMenu, type ReceiptScreen } from './receiptsMenu'
import type { SavedReceipt } from './savedReceipt'
import ReceiptForm from './ReceiptForm.vue'
import NoteReceiptForm from './NoteReceiptForm.vue'
import ReceiptList from './ReceiptList.vue'
import ChequeOperations from './ChequeOperations.vue'
import LegalAction from './LegalAction.vue'
import ReceiptsPanel from './ReceiptsPanel.vue'
import ReceiptPrint from './ReceiptPrint.vue'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')

type Tab = ReceiptScreen
const tab = ref<Tab>('Instruments')
const title = computed(() => { const x = menuItems(receiptsMenu).find(i => i.id === tab.value)!.title; return fa.value ? x[0] : x[1] })
/** The cheque the legal-action screen opens with, when the user comes from a cheque's row on the steps screen. */
const legalCheque = ref<string | undefined>(undefined)
function openTab(id: Tab) { legalCheque.value = undefined; editing.value = null; notice.value = null; printId.value = ''; helpOpen.value = false; tab.value = id }
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
const panelKind = computed(() => (tab.value === 'Cash' || tab.value === 'BankDeposit' || tab.value === 'CardDeposit' ? tab.value
  : tab.value === 'Notes' || tab.value === 'noteSteps' ? 'Notes' : 'Instruments'))
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
/**
 * One line under the heading of each screen and, where there is more to say, the rest behind «راهنمای این فرم» (owner, 20 Nov 2026:
 * the form looked crowded).
 */
const hints: Partial<Record<Tab, { line: [string, string]; more?: [string, string] }>> = {
  Instruments: {
    line: ['چک‌های یک پرداخت‌کننده با یک شمارهٔ رسید ثبت می‌شوند؛ روز یا مدت‌دار بودن هر چک از روی سررسید آن تعیین می‌شود.',
      'Cheques of one payer are saved on one receipt; current or post-dated follows each cheque\'s due date.'],
    more: ['سفته‌ها در «دریافت سفته» ثبت می‌شوند. برای چک ارزی، ارز و نرخ را وارد کنید. چکی که وارد شد در یک سطر خلاصه می‌شود؛ برای دیدن یا تغییر آن روی «ویرایش» همان سطر بزنید. «ثبت چک بعدی» ردیف تازه‌ای با مشخصات بانک و حساب چک قبلی و شمارهٔ بعدی باز می‌کند.',
      'Promissory notes are entered under "Note receipt". A foreign-currency cheque needs its currency and rate. A cheque that was entered folds into one line; press "Edit" on the line to see or change it. "Next cheque" opens a new line with the bank and account of the last one and the next number.'],
  },
  Notes: {
    line: ['سفته‌های یک متعهد با یک شمارهٔ رسید ثبت می‌شوند.', 'Promissory notes of one issuer are saved on one receipt.'],
    more: ['این رسید فقط تحویل سفته را نشان می‌دهد و به‌تنهایی تأیید دریافت وجه یا تسویهٔ بدهی نیست.', 'The receipt only records that the notes were handed over; it is not a receipt of money.'],
  },
  Cash: { line: ['وجه نقد دریافتی به صندوق؛ ارز همان ارز صندوق است.', 'Cash received into a cash desk, in the cash desk\'s currency.'] },
  BankDeposit: { line: ['واریز مستقیم مشتری به حساب بانکی شرکت: فیش، حواله (ساتنا/پایا)، کارتخوان یا کارت به کارت.',
    'Money the payer paid straight into a company bank account: slip, transfer, POS or card-to-card.'] },
  CardDeposit: { line: ['واریز مشتری به کارت بانکِ تنخواه‌گردان‌های شرکت، یا به کارت بانک شرکت که حساب بانکی ندارد: کارت به کارت، با فیش واریزی یا از درگاه پرداخت شرکت.',
    'Money a payer paid onto the bank card of one of the company\'s petty-cash holders, or onto a company bank card without a bank account: card to card, with a deposit slip or through the company\'s gateway.'] },
  legal: {
    line: ['چک برگشتی را از فهرست انتخاب کنید؛ مشخصات آن نمایش داده می‌شود.', 'Choose the bounced cheque from the list; its particulars are shown.'],
    more: ['سپس تاریخ و شرح اقدام حقوقی را وارد و اسکن ضمائم را اضافه کنید. با ثبت، آخرین وضعیت چک «اقدام حقوقی» می‌شود.',
      'Then enter the date and description of the legal action and add the scans. Recording it sets the cheque\'s latest status to legal action.'],
  },
}
const hint = computed(() => hints[tab.value])
const helpOpen = ref(false)
</script>

<template>
  <SectionMenu class="tr-receipts" :dir="fa ? 'rtl' : 'ltr'" :title="t('دریافت‌ها', 'Receipts')" :groups="receiptsMenu" :model-value="tab" :locale="locale" storage-key="receipts"
               @update:model-value="openTab($event as Tab)">
    <template #default="{ closed, path }">
      <header class="tr-head">
        <div class="tr-head-text">
          <div v-if="!closed && path.length > 2" class="tr-crumb">{{ path.slice(0, -1).join(' ‹ ') }}</div>
          <h2>{{ title }}</h2>
          <p v-if="hint" class="hint">{{ fa ? hint.line[0] : hint.line[1] }}</p>
        </div>
        <button v-if="hint?.more" type="button" class="tr-help-btn" :aria-expanded="helpOpen" @click="helpOpen = !helpOpen">
          {{ helpOpen ? t('بستن راهنما', 'Hide the help') : t('راهنمای این فرم', 'Help for this form') }}
        </button>
      </header>
      <p v-if="helpOpen && hint?.more" class="tr-help">{{ fa ? hint.more[0] : hint.more[1] }}</p>
      <p v-if="notice" :class="['notice', notice.kind]" role="status">{{ notice.text }}</p>
      <p v-if="editing" class="notice editing">{{ t(
        `در حال ویرایش رسید شمارهٔ ${editing.number.toLocaleString('fa-IR', { useGrouping: false })} هستید. تا زمانی که «تایید سند» این رسید زده نشده است می‌توانید آن را ویرایش یا حذف کنید؛ شمارهٔ رسید تغییر نمی‌کند. چک یا سفته‌ای که مرحله‌ای (مانند ثبت صیادی) روی آن ثبت شده است قفل می‌ماند.`,
        `You are correcting receipt no. ${editing.number}. It can be corrected or deleted until its voucher is confirmed; its number stays. A cheque or note with a step recorded on it stays locked.`) }}</p>
      <ChequeOperations v-if="tab === 'collection'" :key="'collection' + locale" :api-base="apiBase" :locale="locale" :token="token" @legal="startLegal" @changed="stepRecorded" @print="printReceipt" />
      <ChequeOperations v-else-if="tab === 'noteSteps'" :key="'noteSteps' + locale" notes :api-base="apiBase" :locale="locale" :token="token" @legal="startLegal" @changed="stepRecorded" />
      <LegalAction v-else-if="tab === 'legal'" :key="'legal' + locale + (legalCheque ?? '')" :api-base="apiBase" :locale="locale" :token="token" :cheque-id="legalCheque" @changed="stepRecorded" />
      <ReceiptList v-else-if="tab === 'list'" :api-base="apiBase" :locale="locale" :token="token" :fiscal-year="fiscalYear" @edit="startEdit" @print="printReceipt" />
      <NoteReceiptForm v-else-if="tab === 'Notes'" :key="'notes' + locale + (editing?.id ?? '')" :api-base="apiBase" :locale="locale" :token="token" :edit="editing ?? undefined"
                       @saved="editing ? editSaved() : savedCount++" @cancelled="editing = null" />
      <ReceiptForm v-else :key="tab + locale + (editing?.id ?? '')" :kind="tab" :api-base="apiBase" :locale="locale" :token="token" :edit="editing ?? undefined"
                   @saved="editing ? editSaved() : savedCount++" @cancelled="editing = null" @print="printReceipt" />
      <ReceiptsPanel v-if="tab !== 'list'" :key="'panel' + panelKind + locale" :kind="panelKind" :api-base="apiBase" :locale="locale" :token="token"
                     :fiscal-year="fiscalYear" :refresh-key="savedCount" :editing-id="editing?.id" @edit="startEdit" @deleted="receiptDeleted" @print="printReceipt" />
      <!-- report no. 6, item 21: the printed receipt of the saved receipt chosen with «چاپ رسید» -->
      <ReceiptPrint v-if="printId" :key="printId + printRun" :receipt-id="printId" :api-base="apiBase" :locale="locale" :token="token" @close="printId = ''" />
    </template>
  </SectionMenu>
</template>

<style scoped>
/* The measures of the boxes in this area; the shared boxes of the ui-kit (lists, amounts, Sheba) follow them. */
.tr-receipts { --tr-field-pad: .5rem .65rem; --tr-field-radius: 8px; --tr-field-border: #c9d1dd; --tr-field-h: 2.5rem; }
.tr-head { display: flex; align-items: flex-end; flex-wrap: wrap; gap: .75rem 1rem; margin-bottom: 1rem; }
.tr-head-text { flex: 1 1 20rem; min-width: 0; }
.tr-crumb { font-size: .78rem; color: #5b6779; }
.tr-head h2 { margin: .1rem 0 .2rem; font-size: 1.5rem; line-height: 1.4; }
.hint { color: #5b6779; font-size: .85rem; margin: 0; }
.tr-help-btn { min-height: 2.5rem; padding: 0 .9rem; border: 1px solid #c9d1dd; border-radius: 8px; background: #fff; color: #1b2f55; font: inherit; font-size: .85rem; font-weight: 500; cursor: pointer; }
.tr-help-btn:hover { background: #f0f3f8; }
.tr-help { margin: 0 0 1rem; padding: .75rem 1rem; border: 1px solid #d5dff3; border-radius: 10px; background: #f1f5fd; color: #2b3850; font-size: .85rem; line-height: 1.9; }
.notice { padding: .6rem .9rem; border-radius: 8px; margin: 0 0 .75rem; }
.notice.error { background: #fdecea; color: #8a1c1c; }
.notice.ok { background: #e6f4ea; color: #1e5631; }
.notice.editing { background: #fff7e0; color: #7a5200; }
</style>
