<script setup lang="ts">
/**
 * One voucher of a transfer between the company's bank accounts (owner's report no. 7, 20 Nov 2026), shown under the list for the
 * treasurer's «تایید سند»: step 0 is the voucher of an electronic transfer or of the payment side of a cheque transfer («انتقال دهنده»:
 * بدهکار وجوه در راه، بستانکار بانک انتقال دهنده), step 1 the voucher of its receipts side («انتقال گیرنده»: بدهکار اسناد نزد صندوق،
 * بستانکار وجوه در راه). Once a voucher is confirmed the transfer is no longer corrected or deleted.
 */
import { onMounted, ref, watch } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import VoucherTable, { type Voucher } from './VoucherTable.vue'

const props = withDefaults(defineProps<{
  transferId: string; step: 0 | 1; apiBase?: string; locale?: Locale; token?: () => string | undefined
  accountName?: (code: string) => string | undefined
}>(), { apiBase: '', locale: 'fa', token: undefined, accountName: undefined })
const emit = defineEmits<{ confirmed: []; close: [] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)

const voucher = ref<Voucher | null>(null)
const error = ref('')
const loaded = ref(false)
const busy = ref(false)
async function load() {
  const id = props.transferId
  const r = await api.get<Voucher[]>(`/api/cashflow/transfers/${id}/vouchers`)
  if (id !== props.transferId) return
  loaded.value = true
  if (!r.ok) { voucher.value = null; error.value = translate(r.code, props.locale); return }
  error.value = ''
  voucher.value = r.value.find(v => v.step === props.step) ?? null
}
async function confirm() {
  if (busy.value) return
  busy.value = true
  const r = await api.post<string>(`/api/cashflow/transfers/${props.transferId}/vouchers/${props.step}/confirm`, {})
  busy.value = false
  if (!r.ok) { error.value = translate(r.code, props.locale); return }
  emit('confirmed')
  await load()
}
watch(() => [props.transferId, props.step], () => void load())
onMounted(load)
</script>

<template>
  <section class="tr-card tr-tv">
    <div class="tr-buttons">
      <button type="button" class="tr-link" @click="emit('close')">{{ t('بستن سند', 'Close the voucher') }}</button>
    </div>
    <p v-if="error" class="tr-msg error" role="status">{{ error }}</p>
    <VoucherTable v-if="voucher" :voucher="voucher" :locale="locale" :account-name="accountName" can-confirm :busy="busy" @confirm="confirm" />
    <p v-else-if="loaded && !error" class="muted small">{{ t('این نقل و انتقال چنین سندی ندارد.', 'This transfer has no such voucher.') }}</p>
  </section>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
.tr-tv { margin-top: .75rem; }
.tr-tv :deep(.tr-voucher) { border: 0; padding: 0; margin: 0; background: transparent; }
</style>
