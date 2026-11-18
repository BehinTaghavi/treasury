<script setup lang="ts">
/**
 * Receipts (دریافت‌ها), owner 14 Nov 2026: "different types of receipt forms for cheques, current or post-dated, in Rial and foreign
 * currency, promissory notes, cash receipts etc." One tab per kind, as the old program had one form per kind, plus the list.
 */
import { computed, ref } from 'vue'
import type { Locale } from '@treasury/ui-kit'
import ReceiptForm from './ReceiptForm.vue'
import NoteReceiptForm from './NoteReceiptForm.vue'
import ReceiptList from './ReceiptList.vue'
import ChequeOperations from './ChequeOperations.vue'
import LegalAction from './LegalAction.vue'
import ReceiptsPanel from './ReceiptsPanel.vue'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')

const tabs = [
  { id: 'Instruments', title: ['دریافت چک', 'Cheques'] },
  // Owner's report no. 4, item 13, and his form «فرم دریافت سفته» (16 Nov 2026): promissory notes have a form of their own.
  { id: 'Notes', title: ['دریافت سفته', 'Promissory notes'] },
  { id: 'Cash', title: ['دریافت نقدی', 'Cash'] },
  { id: 'BankDeposit', title: ['واریز به حساب / کارتخوان', 'Bank deposit / POS'] },
  { id: 'list', title: ['فهرست دریافت‌ها', 'Receipts list'] },
  // Owner's report no. 4, item 9, and item 14: every step of a received cheque after receipt.
  { id: 'collection', title: ['عملیات چک‌های دریافتی', 'Received cheque steps'] },
  // Owner, 16 Nov 2026: the steps of a note after receipt (to the bank, collected, handed back, endorsed…).
  { id: 'noteSteps', title: ['عملیات سفته‌های دریافتی', 'Received note steps'] },
  // Owner, 18 Nov 2026: the legal action on a bounced cheque has its own screen.
  { id: 'legal', title: ['اقدام حقوقی', 'Legal action'] },
] as const
type Tab = (typeof tabs)[number]['id']
const tab = ref<Tab>('Instruments')
/** The cheque the legal-action screen opens with, when the user comes from a cheque's row on the steps screen. */
const legalCheque = ref<string | undefined>(undefined)
function openTab(id: Tab) { legalCheque.value = undefined; tab.value = id }
function startLegal(chequeId: string) { legalCheque.value = chequeId; tab.value = 'legal' }
/**
 * Owner, 18 Nov 2026: on every receipt screen a button «فهرست دریافت‌ها» shows the receipts of that screen's kind under the form. The
 * cheque screens (steps, legal action) show the cheque receipts.
 */
const panelKind = computed(() => (tab.value === 'Cash' || tab.value === 'BankDeposit' ? tab.value
  : tab.value === 'Notes' || tab.value === 'noteSteps' ? 'Notes' : 'Instruments'))
/** Counts saved receipts, so an open list shows the new one. */
const savedCount = ref(0)
const hints: Record<string, [string, string]> = {
  Instruments: ['چک‌های یک پرداخت‌کننده با یک شمارهٔ رسید ثبت می‌شوند (سفته‌ها در زبانهٔ «دریافت سفته» ثبت می‌شوند). چک روز یا مدت‌دار از روی سررسید تعیین می‌شود؛ برای چک ارزی ارز و نرخ را وارد کنید.',
    'Cheques of one payer are saved on one receipt (promissory notes have their own tab). Current or post-dated follows the due date; a foreign-currency cheque needs its currency and rate.'],
  Notes: ['سفته‌های یک متعهد با یک شمارهٔ رسید ثبت می‌شوند. این رسید فقط تحویل سفته را نشان می‌دهد و به‌تنهایی تأیید دریافت وجه یا تسویهٔ بدهی نیست.',
    'Promissory notes of one issuer are saved on one receipt. The receipt only records that the notes were handed over; it is not a receipt of money.'],
  Cash: ['وجه نقد دریافتی به صندوق؛ ارز همان ارز صندوق است.', 'Cash received into a cash desk, in the cash desk\'s currency.'],
  BankDeposit: ['واریز مستقیم مشتری به حساب بانکی شرکت: فیش، حواله (ساتنا/پایا)، کارتخوان یا کارت به کارت.',
    'Money the payer paid straight into a company bank account: slip, transfer, POS or card-to-card.'],
  legal: ['چک برگشتی را از فهرست انتخاب کنید؛ مشخصات آن نمایش داده می‌شود. سپس تاریخ و شرح اقدام حقوقی را وارد و اسکن ضمائم را اضافه کنید. با ثبت، آخرین وضعیت چک «اقدام حقوقی» می‌شود.',
    'Choose the bounced cheque from the list; its particulars are shown. Enter the date and description of the legal action and add the scans. Recording it sets the cheque\'s latest status to legal action.'],
}
</script>

<template>
  <section class="tr-receipts" :dir="fa ? 'rtl' : 'ltr'">
    <h2>{{ t('دریافت‌ها', 'Receipts') }}</h2>
    <nav class="tr-tabs" role="tablist">
      <button v-for="x in tabs" :key="x.id" type="button" role="tab" :aria-selected="tab === x.id" :class="{ active: tab === x.id }" @click="openTab(x.id)">
        {{ fa ? x.title[0] : x.title[1] }}
      </button>
    </nav>
    <p v-if="hints[tab]" class="hint">{{ fa ? hints[tab]![0] : hints[tab]![1] }}</p>
    <ChequeOperations v-if="tab === 'collection'" :key="'collection' + locale" :api-base="apiBase" :locale="locale" :token="token" @legal="startLegal" />
    <ChequeOperations v-else-if="tab === 'noteSteps'" :key="'noteSteps' + locale" notes :api-base="apiBase" :locale="locale" :token="token" @legal="startLegal" />
    <LegalAction v-else-if="tab === 'legal'" :key="'legal' + locale + (legalCheque ?? '')" :api-base="apiBase" :locale="locale" :token="token" :cheque-id="legalCheque" />
    <ReceiptList v-else-if="tab === 'list'" :api-base="apiBase" :locale="locale" :token="token" :fiscal-year="fiscalYear" />
    <NoteReceiptForm v-else-if="tab === 'Notes'" :key="'notes' + locale" :api-base="apiBase" :locale="locale" :token="token" @saved="savedCount++" />
    <ReceiptForm v-else :key="tab + locale" :kind="tab" :api-base="apiBase" :locale="locale" :token="token" @saved="savedCount++" />
    <ReceiptsPanel v-if="tab !== 'list'" :key="'panel' + panelKind + locale" :kind="panelKind" :api-base="apiBase" :locale="locale" :token="token"
                   :fiscal-year="fiscalYear" :refresh-key="savedCount" />
  </section>
</template>

<style scoped>
.tr-receipts { font-family: Vazirmatn, Tahoma, sans-serif; color: #1f2933; }
.tr-tabs { display: flex; flex-wrap: wrap; gap: .25rem; border-bottom: 1px solid #e4e7eb; margin-bottom: .75rem; }
.tr-tabs button { padding: .55rem .9rem; border: 0; border-bottom: 3px solid transparent; background: transparent; color: #1f2933; font: inherit; cursor: pointer; }
.tr-tabs button.active { border-bottom-color: #2563eb; color: #1d4ed8; font-weight: 600; }
.hint { color: #52606d; font-size: .85rem; margin: .25rem 0 0; }
</style>
