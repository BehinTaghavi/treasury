<script setup lang="ts">
/**
 * Cheque types and their print layout (معرفی انواع چک و چاپ آنها), new-version manual item 7: per bank account, the print types
 * the bank's cheques come in (new / newest / old, large / small) with a scan of each cheque, used when cheques are printed.
 * Cheque books then name their type. Saved by cashflow-svc. The positions of the printed fields come with cheque printing.
 */
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '../components/SearchSelect.vue'

type PrintType = 'NewLarge' | 'NewestLarge' | 'OldLarge' | 'NewSmall' | 'NewestSmall' | 'OldSmall'
interface Format { id: string; number: number; bankAccountId: string; printType: PrintType; hasImage: boolean; isActive: boolean; version: number }
interface BankAccount { id: string; accountNumber: string; bankId: string; branchName: string | null; isActive: boolean }
interface Entry { id: string; name: string }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US')
const base = '/api/cashflow/cheque-formats'

/** The manual's six print types, in its order. */
const PRINT_TYPES: { value: PrintType; fa: string; en: string }[] = [
  { value: 'NewLarge', fa: 'جدید – قطع بزرگ', en: 'New – large' },
  { value: 'NewestLarge', fa: 'جدیدترین – قطع بزرگ', en: 'Newest – large' },
  { value: 'OldLarge', fa: 'قدیمی – قطع بزرگ', en: 'Old – large' },
  { value: 'NewSmall', fa: 'جدید – قطع کوچک', en: 'New – small' },
  { value: 'NewestSmall', fa: 'جدیدترین – قطع کوچک', en: 'Newest – small' },
  { value: 'OldSmall', fa: 'قدیمی – قطع کوچک', en: 'Old – small' },
]
const typeLabel = (p: PrintType) => { const x = PRINT_TYPES.find(y => y.value === p)!; return fa.value ? x.fa : x.en }

const formats = ref<Format[]>([])
const accounts = ref<BankAccount[]>([])
const banks = ref<Entry[]>([])
const account = ref('')
const newType = ref<PrintType>('NewLarge')
const images = ref<Record<string, string>>({})
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)

const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }
const bankName = (id: string | undefined) => banks.value.find(b => b.id === id)?.name ?? ''
const accountChoices = computed<Choice[]>(() => accounts.value.map(a => ({ value: a.id, label: `${bankName(a.bankId)} · ${a.branchName ?? ''}`, hint: a.accountNumber, muted: !a.isActive })))
const shown = computed(() => formats.value.filter(f => f.bankAccountId === account.value))
const freeTypes = computed(() => PRINT_TYPES.filter(p => !shown.value.some(f => f.printType === p.value)))

function headers(): Record<string, string> {
  const h: Record<string, string> = { 'Idempotency-Key': crypto.randomUUID() }
  const token = props.token?.()
  if (token) h.Authorization = `Bearer ${token}`
  return h
}

/** Scans are fetched with the same credentials as the API (an <img> cannot send them) and shown from memory. */
async function loadImage(f: Format) {
  if (!f.hasImage) return
  const res = await fetch(`${props.apiBase}${base}/${f.id}/image`, { headers: headers() })
  if (!res.ok) return
  if (images.value[f.id]) URL.revokeObjectURL(images.value[f.id]!)
  images.value[f.id] = URL.createObjectURL(await res.blob())
}

async function load() {
  const r = await api.get<Format[]>(`${base}?includeInactive=true`)
  if (r.ok) formats.value = r.value
  else fail(r.code)
  await Promise.all(shown.value.map(loadImage))
}

async function loadLists() {
  const [a, b] = await Promise.all([api.get<BankAccount[]>('/api/accounts/bank-accounts?includeInactive=true'), api.get<Entry[]>('/api/reference/banks?includeInactive=true')])
  if (a.ok) accounts.value = a.value
  if (b.ok) banks.value = b.value
  if (!account.value && accounts.value.length) account.value = accounts.value.find(x => x.isActive)?.id ?? accounts.value[0]!.id
}

async function add() {
  if (busy.value || !account.value) return
  busy.value = true
  const r = await api.post<string>(base, { bankAccountId: account.value, printType: newType.value })
  busy.value = false
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: t('نوع چک ثبت شد؛ اکنون تصویر اسکن آن را بارگذاری کنید.', 'Cheque type saved; now upload its scan.') }
  await load()
  newType.value = freeTypes.value[0]?.value ?? 'NewLarge'
}

async function upload(f: Format, e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  if (file.size > 2 * 1024 * 1024) return fail('ChequeFormat.ImageTooLarge')
  const body = new FormData()
  body.append('file', file)
  body.append('version', String(f.version))
  const res = await fetch(`${props.apiBase}${base}/${f.id}/image`, { method: 'POST', headers: headers(), body })
  if (!res.ok) {
    const problem = await res.json().catch(() => ({}))
    return fail((problem as { code?: string }).code ?? 'Network.Error')
  }
  message.value = { kind: 'ok', text: t('تصویر چک ذخیره شد.', 'Scan saved.') }
  await load()
}

async function changeType(f: Format, p: PrintType) {
  const r = await api.put<string>(`${base}/${f.id}`, { printType: p, version: f.version })
  if (!r.ok) return fail(r.code)
  await load()
}

async function change(f: Format, what: 'activate' | 'deactivate' | 'delete') {
  if (what === 'delete' && !window.confirm(t('این نوع چک حذف شود؟', 'Delete this cheque type?'))) return
  const r = await api.post<string>(`${base}/${f.id}/${what}`, { version: f.version })
  if (!r.ok) return fail(r.code)
  message.value = { kind: 'ok', text: what === 'delete' ? t('حذف شد.', 'Deleted.') : what === 'activate' ? t('فعال شد.', 'Switched on.') : t('غیرفعال شد.', 'Switched off.') }
  await load()
}

async function chooseAccount(id: string) {
  account.value = id
  newType.value = freeTypes.value[0]?.value ?? 'NewLarge'
  await Promise.all(shown.value.map(loadImage))
}

onMounted(async () => {
  await loadLists()
  await load()
  newType.value = freeTypes.value[0]?.value ?? 'NewLarge'
})
onBeforeUnmount(() => Object.values(images.value).forEach(u => URL.revokeObjectURL(u)))
</script>

<template>
  <div>
    <h3>{{ t('انواع چک و چاپ آن‌ها', 'Cheque types and print layout') }}</h3>
    <p v-if="!accounts.length" class="muted">{{ t('ابتدا در «حساب‌های بانکی» حساب بانکی شرکت را تعریف کنید.', 'Define the company\'s bank accounts first.') }}</p>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <div v-if="accounts.length" class="tr-search">
      <label class="acc">{{ t('شماره حساب بانکی', 'Bank account') }}
        <SearchSelect :model-value="account" :options="accountChoices" :aria-label="t('شماره حساب بانکی', 'Bank account')" @update:model-value="chooseAccount" />
      </label>
      <label>{{ t('نوع چاپ', 'Print type') }}
        <select v-model="newType" :disabled="!freeTypes.length"><option v-for="p in freeTypes" :key="p.value" :value="p.value">{{ fa ? p.fa : p.en }}</option></select>
      </label>
      <button type="button" :disabled="busy || !freeTypes.length || !account" @click="add">{{ t('افزودن نوع چک', 'Add cheque type') }}</button>
    </div>

    <div class="tr-cards">
      <article v-for="f in shown" :key="f.id" :class="['tr-card', { inactive: !f.isActive }]">
        <header>
          <strong>{{ t(`شماره چاپ ${num(f.number)}`, `Print no. ${f.number}`) }}</strong>
          <select :value="f.printType" :aria-label="t('نوع چاپ', 'Print type')" @change="changeType(f, ($event.target as HTMLSelectElement).value as PrintType)">
            <option v-for="p in PRINT_TYPES" :key="p.value" :value="p.value" :disabled="p.value !== f.printType && shown.some(x => x.printType === p.value)">{{ fa ? p.fa : p.en }}</option>
          </select>
        </header>
        <div class="scan">
          <img v-if="images[f.id]" :src="images[f.id]" :alt="t('تصویر اسکن چک', 'Cheque scan') + ' – ' + typeLabel(f.printType)" />
          <span v-else class="muted">{{ t('هنوز تصویری بارگذاری نشده است.', 'No scan yet.') }}</span>
        </div>
        <footer>
          <label class="tr-upload">{{ f.hasImage ? t('تعویض تصویر', 'Replace scan') : t('بارگذاری تصویر اسکن', 'Upload scan') }}
            <input type="file" accept="image/png,image/jpeg" @change="upload(f, $event)" />
          </label>
          <small class="muted">{{ t('PNG یا JPEG، حداکثر ۲ مگابایت', 'PNG or JPEG, at most 2 MB') }}</small>
          <span class="tr-actions">
            <button type="button" class="tr-link" @click="change(f, f.isActive ? 'deactivate' : 'activate')">{{ f.isActive ? t('غیرفعال کردن', 'Switch off') : t('فعال کردن', 'Switch on') }}</button>
            <button type="button" class="tr-link danger" @click="change(f, 'delete')">{{ t('حذف', 'Delete') }}</button>
          </span>
        </footer>
      </article>
      <p v-if="account && !shown.length" class="muted">{{ t('برای این حساب هنوز نوع چکی تعریف نشده است.', 'No cheque type for this account yet.') }}</p>
    </div>
  </div>
</template>

<style scoped src="../organization/organization.css"></style>
<style scoped>
.acc { width: 24rem !important; }
.tr-cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(20rem, 1fr)); gap: 1rem; margin-top: 1rem; }
.tr-card { border: 1px solid #e4e7eb; border-radius: 8px; padding: .75rem; display: flex; flex-direction: column; gap: .5rem; background: #fff; }
.tr-card.inactive { opacity: .6; }
.tr-card header { display: flex; justify-content: space-between; align-items: center; gap: .5rem; }
.scan { min-height: 8rem; display: flex; align-items: center; justify-content: center; background: #f5f7fa; border-radius: 6px; overflow: hidden; }
.scan img { max-width: 100%; max-height: 14rem; }
.tr-card footer { display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; }
.tr-upload { flex-direction: row; cursor: pointer; color: #1d4ed8; font-weight: 600; }
.tr-upload input { display: none; }
</style>
