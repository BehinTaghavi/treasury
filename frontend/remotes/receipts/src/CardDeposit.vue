<script setup lang="ts">
/**
 * «واریز به کارت بانک» (owner's report no. 6, item 12): money a payer pays onto the bank card of one of the company's petty-cash
 * holders («حساب کارت بانک‌های تنخواه‌گردان‌های وابسته به شرکت … یک یا چند») or onto a company bank card that has no bank account.
 * The tab is in its place in the order he gave. The receipt form itself waits for his answers to Q68–Q70 (docs/domain/open-questions.md):
 * which account the voucher debits, where the company's card without a bank account is defined, and how these receipts are numbered —
 * a voucher is not built on a guess. Until then the screen shows the bank cards already defined in Basic Information, which the form
 * will offer: the cards of the petty-cash holders, and the company's cards on its bank accounts (their deposits are recorded today on
 * «واریز به بانک» as «کارت به کارت»).
 */
import { computed, onMounted, ref } from 'vue'
import { createApi, toPersianDigits, type Locale } from '@treasury/ui-kit'
import { useLookups } from './lookups'

interface Fund { id: string; holderDetailCode: string; holderName: string | null; ledgerAccountCode: string; isActive: boolean
  keeping: { card: boolean; cardHolderName: string | null; cardNumber: string | null } | null }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const emit = defineEmits<{ bank: [] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (fa.value ? f : e)
const L = useLookups(api)
const funds = ref<Fund[]>([])
const loaded = ref(false)

const card = (n: string | null | undefined) => { const s = (n ?? '').replace(/(\d{4})(?=\d)/g, '$1-'); return fa.value ? toPersianDigits(s) : s }
const holderCards = computed(() => funds.value.filter(f => f.isActive && f.keeping?.card && f.keeping.cardNumber))
const companyCards = computed(() => L.bankAccounts.value.filter(a => a.isActive).flatMap(a => (a.cards ?? []).map(c => ({ ...c, account: L.accountTitle(a) }))))

onMounted(async () => {
  const [f] = await Promise.all([api.get<Fund[]>('/api/accounts/petty-cash'), L.load()])
  if (f.ok) funds.value = f.value
  loaded.value = true
})
</script>

<template>
  <div class="tr-card-deposit">
    <p class="tr-msg warning">
      {{ t('فرم ثبت «واریز به کارت بانک» در دست ساخت است. برای اینکه سند حسابداری آن درست صادر شود، سه پرسش کوتاه (Q68 تا Q70) از شما پرسیده شده است: حساب بدهکارِ سند، محل تعریف «کارت بانک شرکت که حساب بانکی ندارد» و شماره‌گذاری رسیدها. با پاسخ شما فرم در همین زبانه قرار می‌گیرد.',
        'The form for deposits onto a bank card is being built. Three short questions (Q68–Q70) were asked so that its voucher is right: the account it debits, where a company card without a bank account is defined, and how these receipts are numbered. The form appears on this tab once they are answered.') }}
    </p>
    <h3>{{ t('کارت بانک تنخواه‌گردان‌ها (اطلاعات پایه ← دارندگان تنخواه گردان)', 'Bank cards of the petty-cash holders (Basic Information → Petty cash)') }}</h3>
    <table class="tr-table">
      <thead><tr><th>{{ t('تنخواه‌گردان', 'Petty-cash holder') }}</th><th>{{ t('تفصیل', 'Detail') }}</th><th>{{ t('عنوان دارندهٔ کارت', 'Name on the card') }}</th><th>{{ t('شماره کارت', 'Card number') }}</th></tr></thead>
      <tbody>
        <tr v-for="f in holderCards" :key="f.id">
          <td>{{ f.holderName }}</td><td class="num">{{ f.holderDetailCode }}</td><td>{{ f.keeping!.cardHolderName }}</td><td class="num" dir="ltr">{{ card(f.keeping!.cardNumber) }}</td>
        </tr>
        <tr v-if="!holderCards.length"><td colspan="4" class="muted">{{ loaded
          ? t('برای هیچ تنخواه‌گردانی کارت بانک تعریف نشده است. در «اطلاعات پایه ← دارندگان تنخواه گردان»، در «نحوهٔ نگهداری وجوه»، «کارت بانک» را تیک بزنید و شماره کارت را وارد کنید.',
            'No petty-cash holder has a bank card. Tick «Bank card» under how the money is kept in Basic Information → Petty cash and enter the card number.')
          : t('در حال خواندن…', 'Loading…') }}</td></tr>
      </tbody>
    </table>
    <h3 style="margin-top: 1.25rem">{{ t('کارت بانک‌های شرکت روی حساب‌های بانکی (اطلاعات پایه ← حساب‌های بانکی)', "The company's bank cards on its bank accounts (Basic Information → Bank accounts)") }}</h3>
    <p class="muted small">{{ t('واریز به این کارت‌ها امروز در زبانهٔ «واریز به بانک» با نوع واریز «کارت به کارت» ثبت می‌شود.', 'Deposits onto these cards are recorded today on the «Bank deposit» tab as «card to card».') }}
      <button type="button" class="tr-link" @click="emit('bank')">{{ t('رفتن به «واریز به بانک»', 'Go to «Bank deposit»') }}</button></p>
    <table class="tr-table">
      <thead><tr><th>{{ t('حساب بانکی', 'Bank account') }}</th><th>{{ t('عنوان دارندهٔ کارت', 'Name on the card') }}</th><th>{{ t('شماره کارت', 'Card number') }}</th></tr></thead>
      <tbody>
        <tr v-for="c in companyCards" :key="c.number"><td>{{ c.account }}</td><td>{{ c.holderName }}</td><td class="num" dir="ltr">{{ card(c.number) }}</td></tr>
        <tr v-if="!companyCards.length"><td colspan="3" class="muted">{{ loaded ? t('برای هیچ حساب بانکی‌ای کارت بانک تعریف نشده است.', 'No bank account has a bank card.') : t('در حال خواندن…', 'Loading…') }}</td></tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped src="./receipts.css"></style>
