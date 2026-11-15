<script setup lang="ts">
/**
 * Receipts (دریافت‌ها), owner 14 Nov 2026: "different types of receipt forms for cheques, current or post-dated, in Rial and foreign
 * currency, promissory notes, cash receipts etc." One tab per kind, as the old program had one form per kind, plus the list.
 */
import { computed, ref } from 'vue'
import type { Locale } from '@treasury/ui-kit'
import ReceiptForm from './ReceiptForm.vue'
import ReceiptList from './ReceiptList.vue'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')

const tabs = [
  { id: 'Instruments', title: ['دریافت چک', 'Cheques'] },
  { id: 'Cash', title: ['دریافت نقدی', 'Cash'] },
  { id: 'BankDeposit', title: ['واریز به حساب / کارتخوان', 'Bank deposit / POS'] },
  { id: 'list', title: ['فهرست دریافت‌ها', 'Receipts list'] },
] as const
type Tab = (typeof tabs)[number]['id']
const tab = ref<Tab>('Instruments')
const hints: Record<string, [string, string]> = {
  Instruments: ['چک‌های یک پرداخت‌کننده با یک شمارهٔ رسید ثبت می‌شوند (سفته‌ها فرم جداگانه خواهند داشت). چک روز یا مدت‌دار از روی سررسید تعیین می‌شود؛ برای چک ارزی ارز و نرخ را وارد کنید.',
    'Cheques and notes of one payer are saved on one receipt. Current or post-dated follows the due date; a foreign-currency cheque needs its currency and rate.'],
  Cash: ['وجه نقد دریافتی به صندوق؛ ارز همان ارز صندوق است.', 'Cash received into a cash desk, in the cash desk\'s currency.'],
  BankDeposit: ['واریز مستقیم مشتری به حساب بانکی شرکت: فیش، حواله (ساتنا/پایا)، کارتخوان یا کارت به کارت.',
    'Money the payer paid straight into a company bank account: slip, transfer, POS or card-to-card.'],
}
</script>

<template>
  <section class="tr-receipts" :dir="fa ? 'rtl' : 'ltr'">
    <h2>{{ t('دریافت‌ها', 'Receipts') }}</h2>
    <nav class="tr-tabs" role="tablist">
      <button v-for="x in tabs" :key="x.id" type="button" role="tab" :aria-selected="tab === x.id" :class="{ active: tab === x.id }" @click="tab = x.id">
        {{ fa ? x.title[0] : x.title[1] }}
      </button>
    </nav>
    <p v-if="tab !== 'list'" class="hint">{{ fa ? hints[tab]![0] : hints[tab]![1] }}</p>
    <ReceiptList v-if="tab === 'list'" :api-base="apiBase" :locale="locale" :token="token" :fiscal-year="fiscalYear" />
    <ReceiptForm v-else :key="tab + locale" :kind="tab" :api-base="apiBase" :locale="locale" :token="token" />
  </section>
</template>

<style scoped>
.tr-receipts { font-family: Vazirmatn, Tahoma, sans-serif; color: #1f2933; }
.tr-tabs { display: flex; flex-wrap: wrap; gap: .25rem; border-bottom: 1px solid #e4e7eb; margin-bottom: .75rem; }
.tr-tabs button { padding: .55rem .9rem; border: 0; border-bottom: 3px solid transparent; background: transparent; color: #1f2933; font: inherit; cursor: pointer; }
.tr-tabs button.active { border-bottom-color: #2563eb; color: #1d4ed8; font-weight: 600; }
.hint { color: #52606d; font-size: .85rem; margin: .25rem 0 0; }
</style>
