<script setup lang="ts">
/**
 * Opening balances (موجودی اول دوره) of bank accounts, cash desks and petty cash, as the Access form Form_Accamounts: choose the year,
 * then each account's balance on one line, with the total. Owner, 14 Nov 2026: the first time on the start date (mid-year is fine);
 * later years start on 1 Farvardin with the balance carried from the year before, and may be corrected by entering it; a
 * foreign-currency account also has its Rial equivalent. Saved by accounts-svc; no voucher is sent.
 */
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { createApi, formatAmount, normaliseJalali, parseAmount, todayJalali, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect from '../components/SearchSelect.vue'

type Kind = 'BankAccount' | 'CashDesk' | 'PettyCash'
interface Entry { id: string; date: string; amount: number; rialEquivalent: number | null; version: number }
interface Carried { fromYear: number; amount: number; rialEquivalent: number | null }
interface Balance { kind: Kind; accountId: string; entry: Entry | null; carried: Carried | null }
interface BankAccount { id: string; accountNumber: string; bankId: string; branchName: string | null; currencyCode: string; isActive: boolean }
interface CashDesk { id: string; name: string; currencyCode: string; isActive: boolean }
interface Petty { id: string; holderName: string | null; holderDetailCode: string; isActive: boolean }
interface Named { id: string; name: string }
interface Row { kind: Kind; id: string; title: string; hint: string; currency: string; isActive: boolean }
interface Draft { date: string; amount: string; rial: string }

/** fiscalYear: the year chosen in the shell's top bar, shown first (owner, 14 Nov 2026: opening balances in the main menu). */
const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const money = (n: number) => formatAmount(n, props.locale)
const num = (n: number) => n.toLocaleString(fa.value ? 'fa-IR' : 'en-US', { useGrouping: false })
const base = '/api/accounts/opening-balances'

const thisYear = Number(todayJalali().slice(0, 4))
const year = ref(String(props.fiscalYear ?? thisYear))
const yearChoices = computed(() => Array.from({ length: 8 }, (_, i) => thisYear - 4 + i).map(y => ({ value: String(y), label: num(y) })))

const banks = ref<Named[]>([])
const bankAccounts = ref<BankAccount[]>([])
const desks = ref<CashDesk[]>([])
const petty = ref<Petty[]>([])
const balances = ref<Balance[]>([])
const drafts = reactive<Record<string, Draft>>({})
const busy = ref('')
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)

const key = (r: { kind: Kind; id: string }) => `${r.kind}:${r.id}`
const balanceOf = (r: Row) => balances.value.find(b => b.kind === r.kind && b.accountId === r.id)
const bankName = (id: string) => banks.value.find(b => b.id === id)?.name ?? ''

const groups = computed<{ kind: Kind; title: string; rows: Row[] }[]>(() => [
  { kind: 'BankAccount', title: t('حساب‌های بانکی', 'Bank accounts'), rows: bankAccounts.value.map(a => ({
    kind: 'BankAccount' as Kind, id: a.id, title: a.accountNumber, hint: `${bankName(a.bankId)}${a.branchName ? ' – ' + a.branchName : ''}`, currency: a.currencyCode, isActive: a.isActive })) },
  { kind: 'CashDesk', title: t('صندوق‌ها', 'Cash desks'), rows: desks.value.map(d => ({ kind: 'CashDesk' as Kind, id: d.id, title: d.name, hint: '', currency: d.currencyCode, isActive: d.isActive })) },
  { kind: 'PettyCash', title: t('تنخواه گردان‌ها', 'Petty cash'), rows: petty.value.map(p => ({
    kind: 'PettyCash' as Kind, id: p.id, title: p.holderName ?? p.holderDetailCode, hint: p.holderDetailCode, currency: 'IRR', isActive: p.isActive })) },
])

/** The first year anything is entered is the start (any date); every later year starts on 1 Farvardin. */
const firstYear = computed(() => !balances.value.some(b => b.carried))
const defaultDate = computed(() => (firstYear.value && Number(year.value) === thisYear ? todayJalali() : `${year.value}/01/01`))

/** Amounts: Persian digits and separators accepted; a leading minus for an overdrawn bank account. */
function amountOf(text: string): number | null {
  // Formatted negatives come with invisible direction marks («‎−۱۲٬۵۰۰»): drop them before reading the sign.
  const s = text.replace(/[\u200e\u200f\u202a-\u202e\u2066-\u2069]/g, '').trim()
  const negative = s.startsWith('-') || s.startsWith('−')
  const n = parseAmount(negative ? s.slice(1) : s)
  return n === null ? null : negative ? -n : n
}

function fillDrafts() {
  for (const g of groups.value) {
    for (const r of g.rows) {
      const b = balanceOf(r)
      const source = b?.entry ?? b?.carried ?? null
      drafts[key(r)] = {
        date: b?.entry?.date ?? defaultDate.value,
        amount: source ? money(source.amount) : '',
        rial: source?.rialEquivalent != null ? money(source.rialEquivalent) : '',
      }
    }
  }
}

/** Rial value of a row for the totals: the amount, or the Rial equivalent of a foreign-currency balance. */
function rialOf(r: Row): number {
  const d = drafts[key(r)]
  if (!d) return 0
  return (r.currency === 'IRR' ? amountOf(d.amount) : amountOf(d.rial)) ?? 0
}
const total = (rows: Row[]) => rows.reduce((s, r) => s + rialOf(r), 0)
const grandTotal = computed(() => groups.value.reduce((s, g) => s + total(g.rows), 0))

async function loadBalances() {
  const r = await api.get<Balance[]>(`${base}?year=${year.value}`)
  if (r.ok) balances.value = r.value
  else message.value = { kind: 'error', text: translate(r.code, props.locale) }
  fillDrafts()
}

async function loadAll() {
  const [b, a, d, p] = await Promise.all([
    api.get<Named[]>('/api/reference/banks?includeInactive=true'),
    api.get<BankAccount[]>('/api/accounts/bank-accounts?includeInactive=true'),
    api.get<CashDesk[]>('/api/accounts/cash-desks?includeInactive=true'),
    api.get<Petty[]>('/api/accounts/petty-cash?includeInactive=true'),
  ])
  if (b.ok) banks.value = b.value
  if (a.ok) bankAccounts.value = a.value
  if (d.ok) desks.value = d.value
  if (p.ok) petty.value = p.value
  await loadBalances()
}

async function save(r: Row) {
  const d = drafts[key(r)]!
  const amount = amountOf(d.amount)
  if (amount === null) return void (message.value = { kind: 'error', text: translate('OpeningBalance.AmountInvalid', props.locale) })
  const rial = r.currency === 'IRR' ? null : amountOf(d.rial)
  busy.value = key(r)
  const res = await api.put<string>(base, {
    kind: r.kind, accountId: r.id, fiscalYear: Number(year.value), date: normaliseJalali(d.date) ?? d.date,
    amount, rialEquivalent: rial, version: balanceOf(r)?.entry?.version ?? null,
  })
  busy.value = ''
  if (!res.ok) return void (message.value = { kind: 'error', text: translate(res.code, props.locale) })
  message.value = { kind: 'ok', text: t(`موجودی اول دورهٔ «${r.title}» ثبت شد.`, `Opening balance of "${r.title}" saved.`) }
  await loadBalances()
}

async function remove(r: Row) {
  const entry = balanceOf(r)?.entry
  if (!entry || !window.confirm(t(`موجودی ثبت‌شدهٔ «${r.title}» برای سال ${num(Number(year.value))} حذف شود؟`, `Remove the balance of "${r.title}" for ${year.value}?`))) return
  const res = await api.post<string>(`${base}/${entry.id}/delete`, { version: entry.version })
  if (!res.ok) return void (message.value = { kind: 'error', text: translate(res.code, props.locale) })
  message.value = { kind: 'ok', text: t('حذف شد.', 'Removed.') }
  await loadBalances()
}

watch(year, () => { message.value = null; void loadBalances() })
onMounted(loadAll)
</script>

<template>
  <div>
    <h3>{{ t('موجودی اول دوره', 'Opening balances') }}</h3>
    <p class="muted small">{{ t('اولین بار، موجودی هر حساب را در تاریخ شروع کار با سیستم وارد کنید (می‌تواند وسط سال باشد). در سال‌های بعد، موجودی اول فروردین خودکار از سال قبل منتقل می‌شود و در صورت نیاز قابل اصلاح است. برای حساب ارزی، مبلغ هم‌ارز ریالی هم لازم است.',
      'The first time, enter each balance on the date you start (mid-year is fine). Later years start on 1 Farvardin with the balance carried from the year before, which you may correct. A foreign-currency account also needs its Rial equivalent.') }}</p>
    <div class="tr-search">
      <label class="year">{{ t('سال مالی', 'Fiscal year') }}
        <SearchSelect v-model="year" :options="yearChoices" :aria-label="t('سال مالی', 'Fiscal year')" />
      </label>
    </div>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <template v-for="g in groups" :key="g.kind">
      <h4>{{ g.title }}</h4>
      <p v-if="!g.rows.length" class="muted small">{{ t('موردی تعریف نشده است.', 'None defined.') }}</p>
      <div v-else class="tr-scroll">
        <table class="tr-table">
          <thead>
            <tr>
              <th>{{ t('حساب', 'Account') }}</th><th>{{ t('ارز', 'Currency') }}</th><th>{{ t('تاریخ', 'Date') }}</th>
              <th>{{ t('موجودی اول دوره', 'Balance') }}</th><th>{{ t('هم‌ارز ریالی', 'Rial equivalent') }}</th><th>{{ t('وضعیت', 'Status') }}</th><th />
            </tr>
          </thead>
          <tbody>
            <tr v-for="r in g.rows" :key="key(r)" :class="{ inactive: !r.isActive }">
              <td><span dir="auto">{{ r.title }}</span> <small v-if="r.hint" class="muted">{{ r.hint }}</small></td>
              <td dir="ltr">{{ r.currency }}</td>
              <td><input v-model="drafts[key(r)]!.date" dir="ltr" class="date" :aria-label="t('تاریخ', 'Date')" /></td>
              <td><input v-model="drafts[key(r)]!.amount" dir="ltr" inputmode="decimal" class="amount" :aria-label="t('موجودی اول دوره', 'Balance')" /></td>
              <td><input v-if="r.currency !== 'IRR'" v-model="drafts[key(r)]!.rial" dir="ltr" inputmode="numeric" class="amount" :aria-label="t('هم‌ارز ریالی', 'Rial equivalent')" /></td>
              <td class="small">
                <span v-if="balanceOf(r)?.entry" class="tr-tag ok">{{ t('ثبت‌شده', 'Entered') }}</span>
                <span v-else-if="balanceOf(r)?.carried" class="tr-tag">{{ t(`منتقل‌شده از ${num(balanceOf(r)!.carried!.fromYear)}`, `Carried from ${balanceOf(r)!.carried!.fromYear}`) }}</span>
                <span v-else class="muted">{{ t('ندارد', 'None') }}</span>
              </td>
              <td class="tr-actions">
                <button type="button" class="tr-link" :disabled="busy === key(r)" @click="save(r)">{{ balanceOf(r)?.entry ? t('اصلاح', 'Correct') : t('ثبت', 'Save') }}</button>
                <button v-if="balanceOf(r)?.entry" type="button" class="tr-link danger" @click="remove(r)">{{ t('حذف', 'Remove') }}</button>
              </td>
            </tr>
          </tbody>
          <tfoot><tr><td colspan="3">{{ t('جمع (ریال)', 'Total (Rial)') }}</td><td colspan="4" class="total">{{ money(total(g.rows)) }}</td></tr></tfoot>
        </table>
      </div>
    </template>
    <p class="grand">{{ t('جمع کل موجودی‌ها (ریال):', 'Total of all balances (Rial):') }} <strong>{{ money(grandTotal) }}</strong></p>
  </div>
</template>

<style scoped src="../organization/organization.css"></style>
<style scoped>
h4 { margin: 1.25rem 0 .25rem; color: #1f3a5c; }
.year { width: 10rem !important; }
.tr-scroll { overflow-x: auto; }
input.date { width: 7.5rem; }
input.amount { width: 11rem; text-align: left; }
.tr-tag.ok { background: #e6f4ea; color: #1e5631; }
tfoot td { font-weight: 600; border-top: 2px solid #cbd2d9; }
.total { text-align: left; direction: ltr; }
.grand { margin-top: 1rem; font-size: 1rem; }
</style>
