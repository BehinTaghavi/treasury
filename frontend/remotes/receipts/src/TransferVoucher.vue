<script setup lang="ts">
/**
 * The vouchers of a transfer between the company's bank accounts (owner's report no. 7 of 20 Nov 2026 and his answers of 21 Nov
 * 2026), shown under the list for the treasurer's «تایید سند», each under its own title: step 0 the voucher of an electronic
 * transfer or of the payment side of a cheque transfer («سند انتقال دهنده»: بدهکار وجوه در راه، بستانکار بانک انتقال دهنده — for a
 * post-dated cheque بستانکار اسناد پرداختنی, Q81), step 1 its receipts side («سند انتقال گیرنده»: بدهکار اسناد نزد صندوق، بستانکار
 * وجوه در راه), step 2 «سند سررسید چک پرداختنی» (بدهکار اسناد پرداختنی، بستانکار بانک), steps 3 and 4 the two sides of a cancelled
 * cheque (Q79). `steps` names the vouchers of this screen: the payment side shows 0, 2 and 3, the receipts side 1 and 4. Once the
 * payer's voucher is confirmed the transfer is no longer corrected or deleted.
 */
import { onMounted, ref, watch } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import VoucherTable, { type Voucher } from './VoucherTable.vue'
import { voucherTitle } from './transfers'

const props = withDefaults(defineProps<{
  transferId: string; steps: readonly number[]; cheque?: boolean; apiBase?: string; locale?: Locale; token?: () => string | undefined
  accountName?: (code: string) => string | undefined
  /** Changes when the transfer took a step on its screen, so a new voucher (maturity, cancellation) is read. */
  refreshKey?: number
}>(), { cheque: false, apiBase: '', locale: 'fa', token: undefined, accountName: undefined, refreshKey: 0 })
const emit = defineEmits<{ confirmed: []; close: [] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)

const vouchers = ref<Voucher[]>([])
const error = ref('')
const loaded = ref(false)
const busy = ref(false)
async function load() {
  const id = props.transferId
  const r = await api.get<Voucher[]>(`/api/cashflow/transfers/${id}/vouchers`)
  if (id !== props.transferId) return
  loaded.value = true
  if (!r.ok) { vouchers.value = []; error.value = translate(r.code, props.locale); return }
  error.value = ''
  vouchers.value = r.value.filter(v => props.steps.includes(v.step)).sort((a, b) => a.step - b.step)
}
async function confirm(step: number) {
  if (busy.value) return
  busy.value = true
  const r = await api.post<string>(`/api/cashflow/transfers/${props.transferId}/vouchers/${step}/confirm`, {})
  busy.value = false
  if (!r.ok) { error.value = translate(r.code, props.locale); return }
  emit('confirmed')
  await load()
}
watch(() => [props.transferId, props.steps.join(','), props.refreshKey], () => void load())
onMounted(load)
</script>

<template>
  <section class="tr-card tr-tv">
    <div class="tr-buttons">
      <button type="button" class="tr-link" @click="emit('close')">{{ t('بستن سند', 'Close the voucher') }}</button>
    </div>
    <p v-if="error" class="tr-msg error" role="status">{{ error }}</p>
    <div v-for="v in vouchers" :key="v.step" class="tr-tv-one">
      <h4>{{ voucherTitle(v.step, cheque, locale) }}</h4>
      <VoucherTable :voucher="v" :locale="locale" :account-name="accountName" can-confirm :busy="busy" @confirm="confirm(v.step)" />
    </div>
    <p v-if="loaded && !error && !vouchers.length" class="muted small">{{ t('این نقل و انتقال چنین سندی ندارد.', 'This transfer has no such voucher.') }}</p>
  </section>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
.tr-tv { margin-top: .75rem; }
.tr-tv-one + .tr-tv-one { margin-top: 1rem; padding-top: 1rem; border-top: 1px solid #dfe4ec; }
.tr-tv-one h4 { margin: 0 0 .4rem; font-size: .92rem; color: #1b2f55; }
.tr-tv :deep(.tr-voucher) { border: 0; padding: 0; margin: 0; background: transparent; }
</style>
