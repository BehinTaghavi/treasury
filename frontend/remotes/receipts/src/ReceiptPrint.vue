<script setup lang="ts">
/**
 * «چاپ رسید» of a saved receipt of any kind (owner's report no. 6, item 21): reads the receipt, shows its sheet on the screen and
 * opens the browser's print window; a copy of the sheet placed directly under <body> is the only thing printed (as the receipt of
 * promissory notes and the payment sheet do). A receipt of cheques, cash or a bank deposit is printed on ReceiptSheet; a receipt of
 * promissory notes on the form the owner designed on 16 Nov 2026 (NoteReceiptSheet).
 */
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { COMPANY_LOGO_PATH, COMPANY_PATH, createApi, translate, type CompanyInfo, type Locale } from '@treasury/ui-kit'
import { useLookups } from './lookups'
import ReceiptSheet, { type PrintedReceipt } from './ReceiptSheet.vue'
import NoteReceiptSheet from './NoteReceiptSheet.vue'
import type { NoteReceipt } from './notes'

const props = withDefaults(defineProps<{ receiptId: string; apiBase?: string; locale?: Locale; token?: () => string | undefined }>(),
  { apiBase: '', locale: 'fa', token: undefined })
const emit = defineEmits<{ close: [] }>()
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const L = useLookups(api)

const receipt = ref<PrintedReceipt | null>(null)
const company = ref<{ name: string; logoUrl: string | null }>({ name: '', logoUrl: null })
const error = ref('')
const root = ref<HTMLElement | null>(null)
const section = ref<HTMLElement | null>(null)
let logoUrl: string | null = null

const group = computed(() => L.groups.value.find(g => g.id === receipt.value?.purposeGroupId))
const refTitle = (id: string | null | undefined) => L.references.value.find(x => x.id === id)?.name ?? ''
const departmentName = computed(() => L.departments.value.find(d => d.id === receipt.value?.departmentId)?.name ?? '')
const ledgerAccount = computed(() => { const code = group.value?.ledgerAccountCode; return code ? `${code} – ${L.accountName(code) ?? ''}` : '' })
const accountTitle = computed(() => (receipt.value ? L.receiptAccountTitle(receipt.value, props.locale === 'fa') : ''))
const currencyName = (code: string) => L.currencies.value.find(c => c.code === code)?.name ?? code

/** The company's logo as the shell reads it (with the login token once there is one). */
async function loadLogo(): Promise<string | null> {
  try {
    const token = props.token?.()
    const res = await fetch(`${props.apiBase}${COMPANY_LOGO_PATH}`, { cache: 'no-store', headers: token ? { Authorization: `Bearer ${token}` } : {} })
    return res.ok ? URL.createObjectURL(await res.blob()) : null
  } catch { return null }
}

async function load() {
  const id = props.receiptId
  error.value = ''
  const [r, c] = await Promise.all([api.get<PrintedReceipt>(`/api/cashflow/receipts/${id}`), api.get<CompanyInfo>(COMPANY_PATH), L.load()])
  if (id !== props.receiptId) return
  if (!r.ok) { receipt.value = null; error.value = translate(r.code, props.locale); return }
  if (c.ok && c.value.hasLogo && !logoUrl) logoUrl = await loadLogo()
  company.value = { name: c.ok ? c.value.name : '', logoUrl }
  receipt.value = r.value
  await nextTick()
  section.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  await print()
}

/** Opens the browser's print window with this sheet only: another sheet left open on the screen (a payment sheet) is left out. */
async function print() {
  if (!receipt.value) return
  await nextTick()
  // The logo is fetched for the sheet: give it a moment so the print shows it.
  const img = root.value?.querySelector<HTMLImageElement>('.logo')
  if (img && !img.complete) await new Promise(done => { img.onload = img.onerror = () => done(null); setTimeout(() => done(null), 1500) })
  const others = [...document.querySelectorAll<HTMLElement>('.tr-print-root')].filter(el => el !== root.value)
  others.forEach(el => el.setAttribute('data-print-skip', ''))
  const restore = () => { others.forEach(el => el.removeAttribute('data-print-skip')); window.removeEventListener('afterprint', restore) }
  window.addEventListener('afterprint', restore)
  window.print()
  // Browsers that do not fire «afterprint» still get the other sheets back.
  setTimeout(restore, 60_000)
}

watch(() => props.receiptId, () => void load())
onMounted(load)
onBeforeUnmount(() => { if (logoUrl) URL.revokeObjectURL(logoUrl) })
</script>

<template>
  <section ref="section" class="tr-receipt-print">
    <div class="bar">
      <strong>{{ t('چاپ رسید', 'Print receipt') }}<template v-if="receipt"> – {{ t(`شمارهٔ ${receipt.number.toLocaleString('fa-IR', { useGrouping: false })}`, `no. ${receipt.number}`) }}</template></strong>
      <button type="button" :disabled="!receipt" @click="print">{{ t('چاپ', 'Print') }}</button>
      <button type="button" class="link" @click="emit('close')">{{ t('بستن پیش‌نمایش چاپ', 'Close print preview') }}</button>
    </div>
    <p v-if="error" class="error" role="status">{{ error }}</p>
    <template v-if="receipt">
      <template v-if="receipt.kind === 'Notes'">
        <NoteReceiptSheet :receipt="(receipt as unknown as NoteReceipt)" :locale="locale" :department-name="departmentName" :ledger-account="ledgerAccount"
                          :reference1-title="refTitle(group?.reference1Id)" :reference2-title="refTitle(group?.reference2Id)" />
        <Teleport to="body">
          <div ref="root" class="tr-print-root">
            <NoteReceiptSheet :receipt="(receipt as unknown as NoteReceipt)" :locale="locale" :department-name="departmentName" :ledger-account="ledgerAccount"
                              :reference1-title="refTitle(group?.reference1Id)" :reference2-title="refTitle(group?.reference2Id)" />
          </div>
        </Teleport>
      </template>
      <template v-else>
        <ReceiptSheet :receipt="receipt" :company="company" :locale="locale" :account-title="accountTitle" :currency-name="currencyName" :department-name="departmentName"
                      :reference1-title="refTitle(group?.reference1Id)" :reference2-title="refTitle(group?.reference2Id)" />
        <Teleport to="body">
          <div ref="root" class="tr-print-root">
            <ReceiptSheet :receipt="receipt" :company="company" :locale="locale" :account-title="accountTitle" :currency-name="currencyName" :department-name="departmentName"
                          :reference1-title="refTitle(group?.reference1Id)" :reference2-title="refTitle(group?.reference2Id)" />
          </div>
        </Teleport>
      </template>
    </template>
    <p v-else-if="!error" class="muted">{{ t('در حال آماده‌سازی رسید…', 'Preparing the receipt…') }}</p>
  </section>
</template>

<style scoped>
.tr-receipt-print { margin-top: 1.25rem; padding: .75rem; border: 1px solid #d9e2ec; border-radius: 8px; background: #f8fafc; }
.bar { display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; }
.bar button { padding: .5rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
.bar button:disabled { opacity: .5; cursor: default; }
.bar button.link { background: transparent; color: #1d4ed8; border-color: transparent; }
.error { padding: .5rem .75rem; border-radius: 6px; background: #fdecea; color: #8a1c1c; }
.muted { color: #7b8794; font-size: .85rem; }
</style>

<!-- Not scoped: these rules reach <body> and its children while the browser prints (the same rules as NoteReceiptPrint). -->
<style>
.tr-print-root { display: none; }
@media print {
  html, body { background: #fff !important; }
  body > *:not(.tr-print-root) { display: none !important; }
  .tr-print-root { display: block; }
  .tr-print-root[data-print-skip] { display: none !important; }
  .tr-print-root .tr-print-sheet { width: auto; max-width: none; min-height: 0; margin: 0; padding: 0; border: 0; }
  .tr-print-root .tr-receipt-sheet { padding: 0 .4mm; }
  @page { size: A4; margin: 11mm 12mm; }
}
</style>
