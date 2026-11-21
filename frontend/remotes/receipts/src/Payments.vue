<script setup lang="ts">
/**
 * Payments (پرداخت‌ها). Owner's report no. 7 (20 Nov 2026): a transfer between two bank accounts of the company is a payment of the
 * source bank and a receipt of the destination bank, so it stands in both areas — here «پرداخت چک – نقل و انتقال (انتقال دهنده)»,
 * where the cheque is issued and from where one button records it on the receipts side, and the electronic transfer (انتقال داخلی،
 * پل، پایا، ساتنا). The other payment screens (issuing and printing cheques for beneficiaries, bank transfers, cash payments) come
 * with Phase E. The screens are part of the receipts micro-frontend because both areas belong to cashflow-svc; the area is exposed
 * to the shell as `receipts/Payments`. The look is the one the owner approved on 20 Nov 2026 (side menu, heading, one line of help).
 */
import { computed, ref } from 'vue'
import { menuItems, type Locale } from '@treasury/ui-kit'
import SectionMenu from '@treasury/ui-kit/SectionMenu.vue'
import '@treasury/ui-kit/required.css'
import { paymentsMenu, type PaymentScreen } from './paymentsMenu'
import ElectronicTransfer from './ElectronicTransfer.vue'
import TransferChequePay from './TransferChequePay.vue'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')

const tab = ref<PaymentScreen>('transferCheque')
const title = computed(() => { const x = menuItems(paymentsMenu).find(i => i.id === tab.value)!.title; return fa.value ? x[0] : x[1] })
const hints: Record<PaymentScreen, [string, string]> = {
  transferCheque: ['چکی که شرکت از یک حساب خود برای انتقال به حساب دیگرش صادر و در صیاد ثبت می‌کند؛ با یک دگمه در بخش دریافت (انتقال گیرنده) هم ثبت می‌شود.',
    'A cheque the company issues from one of its accounts to move money to another, registered in Sayad; one button records it on the receipts side too.'],
  electronicTransfer: ['جابجایی وجه بین دو حساب بانکی شرکت بدون چک: انتقال داخلی، پل، پایا یا ساتنا، در سقف‌هایی که در اطلاعات پایه تعیین شده است.',
    'Money moved between two bank accounts of the company without a cheque: internal, Pol, Paya or Satna, within the limits kept in Basic Information.'],
}
</script>

<template>
  <SectionMenu class="tr-payments" :dir="fa ? 'rtl' : 'ltr'" :title="t('پرداخت‌ها', 'Payments')" :groups="paymentsMenu" :model-value="tab" :locale="locale" storage-key="payments"
               @update:model-value="tab = $event as PaymentScreen">
    <template #default="{ closed, path }">
      <header class="tr-head">
        <div class="tr-head-text">
          <div v-if="!closed && path.length > 2" class="tr-crumb">{{ path.slice(0, -1).join(' ‹ ') }}</div>
          <h2>{{ title }}</h2>
          <p class="hint">{{ fa ? hints[tab][0] : hints[tab][1] }}</p>
        </div>
      </header>
      <TransferChequePay v-if="tab === 'transferCheque'" :key="'cheque' + locale" :api-base="apiBase" :locale="locale" :token="token" :fiscal-year="fiscalYear" />
      <ElectronicTransfer v-else :key="'electronic' + locale" :api-base="apiBase" :locale="locale" :token="token" :fiscal-year="fiscalYear" />
      <p class="muted">{{ t('سایر صفحه‌های پرداخت (صدور و چاپ چک برای ذینفعان، حواله و پرداخت نقدی) در فاز بعدی به این قسمت اضافه می‌شوند.',
        'The other payment screens (issuing and printing cheques for beneficiaries, bank transfers, cash payments) are added to this area in a later phase.') }}</p>
    </template>
  </SectionMenu>
</template>

<style scoped>
/* The measures of the boxes in this area, as in «دریافت‌ها». */
.tr-payments { --tr-field-pad: .5rem .65rem; --tr-field-radius: 8px; --tr-field-border: #c9d1dd; --tr-field-h: 2.5rem; }
.tr-head { display: flex; align-items: flex-end; flex-wrap: wrap; gap: .75rem 1rem; margin-bottom: 1rem; }
.tr-head-text { flex: 1 1 20rem; min-width: 0; }
.tr-crumb { font-size: .78rem; color: #5b6779; }
.tr-head h2 { margin: .1rem 0 .2rem; font-size: 1.5rem; line-height: 1.4; }
.hint { color: #5b6779; font-size: .85rem; margin: 0; }
.muted { color: #7b8794; font-size: .85rem; margin: 1.25rem 0 0; }
</style>
