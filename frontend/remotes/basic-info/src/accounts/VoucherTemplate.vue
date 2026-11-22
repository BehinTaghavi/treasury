<script setup lang="ts">
/**
 * «شابلون اسناد خزانه داری» (owner's report no. 4, item 8; his template table, docs/domain/posting-rules.md): the company's accounts
 * for the fixed lines of treasury vouchers. The other lines take their account from the purpose group (گروه بابت), the cash desk or
 * the bank account. «چک‌های در جریان وصول» is one account for all banks, with the bank account's تفصیل (owner, 15 Nov 2026).
 * Saved by cashflow-svc, which checks each account against the chart again (active, takes entries).
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'

interface Row { account: string; code: string | null }
interface LedgerAccount { code: string; name: string; isPostable: boolean; isActive: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')

const names: Record<string, { title: [string, string]; use: [string, string] }> = {
  ChequesInCashDesk: {
    title: ['اسناد نزد صندوق (اسناد دریافتنی)', 'Cheques in the cash desk (notes receivable)'],
    use: ['بدهکار هنگام دریافت چک، بستانکار هنگام واگذاری به بانک؛ تفصیل: پرداخت کننده', 'Debited when a cheque is received, credited when it goes to the bank; detail: the payer'],
  },
  ChequesInCollection: {
    title: ['چک‌های در جریان وصول', 'Cheques in collection'],
    use: ['یک حساب برای همهٔ بانک‌ها؛ تفصیل: تفصیل حساب بانکی', 'One account for all banks; detail: the bank account\'s detail'],
  },
  ProtestedCheques: {
    title: ['اسناد واخواست شده (چک برگشتی)', 'Protested (bounced) cheques'],
    use: ['بدهکار هنگام برگشت چک از بانک؛ تفصیل: پرداخت کننده', 'Debited when the bank returns a cheque; detail: the payer'],
  },
  // Owner's answer of 18 Nov 2026: the voucher of the legal action («اقدام حقوقی»).
  ProtestedChequesInLegal: {
    title: ['اسناد واخواست شده حقوقی شده', 'Protested cheques in legal action'],
    use: ['بدهکار هنگام اقدام حقوقی، در مقابل بستانکار «اسناد واخواست شده» (یا «اسناد نزد صندوق» برای چکی که هنوز نزد صندوق است)؛ تفصیل: پرداخت کننده',
      'Debited at the legal action, against a credit of «Protested cheques» (or «Cheques in the cash desk» for a cheque still there); detail: the payer'],
  },
  // Owner's answer of 18 Nov 2026 (Q42): the legal action on guarantee cheques and notes.
  GuaranteeChequesInLegal: {
    title: ['اسناد تضمینی حقوقی شده به نفع شرکت', 'Guarantee items in legal action'],
    use: ['بدهکار هنگام اقدام حقوقی چک یا سفتهٔ تضمینی، در مقابل بستانکار «چک انتظامی به نفع شرکت»؛ تفصیل: پرداخت کننده',
      'Debited at the legal action on a guarantee cheque or note, against a credit of «Guarantee cheques for the company»; detail: the payer'],
  },
  GuaranteeChequesForCompany: {
    title: ['چک انتظامی به نفع شرکت', 'Guarantee cheques for the company'],
    use: ['بدهکار هنگام دریافت چک تضمینی', 'Debited when a guarantee cheque is received'],
  },
  GuaranteeCounterpart: {
    title: ['طرف حساب انتظامی', 'Guarantee counterpart'],
    use: ['بستانکار هنگام دریافت چک تضمینی', 'Credited when a guarantee cheque is received'],
  },
  BadDebts: {
    title: ['مطالبات سوخت شده', 'Bad debts written off'],
    use: ['بدهکار هنگام ابطال چک دریافتی به دستور مدیریت؛ تفصیل: پرداخت کننده', "Debited when a received cheque is cancelled by management's order; detail: the payer"],
  },
  GuaranteeNotesForCompany: {
    title: ['سفته انتظامی به نفع شرکت', 'Guarantee notes for the company'],
    use: ['بدهکار هنگام دریافت سفتهٔ تضمینی یا امانی؛ بستانکار آن «طرف حساب انتظامی» است', 'Debited when a guarantee or trust note is received; credited to the guarantee counterpart'],
  },
  NotesInCollection: {
    title: ['سفته‌های در جریان وصول', 'Notes in collection'],
    use: ['بدهکار هنگام واگذاری سفته به بانک، بستانکار هنگام وصول، واخواست یا عودت؛ تفصیل: تفصیل حساب بانکی', "Debited when a note goes to the bank, credited when collected, protested or returned; detail: the bank account's detail"],
  },
}

const rows = ref<Row[]>([])
const ledger = ref<LedgerAccount[]>([])
const edit = reactive<Record<string, string>>({})
const busy = ref('')
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
const choices = computed<Choice[]>(() => ledger.value.filter(a => a.isPostable && a.isActive).map(a => ({ value: a.code, label: a.name, hint: a.code })))
const ledgerName = (code: string | null) => ledger.value.find(a => a.code === code)?.name ?? ''
const title = (k: string) => (names[k] ? (fa.value ? names[k].title[0] : names[k].title[1]) : k)

async function load() {
  const [r, l] = await Promise.all([
    api.get<Row[]>('/api/cashflow/voucher-template'),
    api.get<LedgerAccount[]>('/api/reference/ledger/accounts?postableOnly=true'),
  ])
  if (r.ok) {
    rows.value = r.value
    for (const x of r.value) edit[x.account] = x.code ?? ''
  } else message.value = { kind: 'error', text: translate(r.code, props.locale) }
  if (l.ok) ledger.value = l.value
}

async function save(row: Row) {
  if (busy.value) return
  busy.value = row.account
  const r = await api.put<string>(`/api/cashflow/voucher-template/${row.account}`, { code: edit[row.account] || null })
  busy.value = ''
  if (!r.ok) { message.value = { kind: 'error', text: translate(r.code, props.locale) }; return }
  message.value = { kind: 'ok', text: t(`«${title(row.account)}» ثبت شد.`, `"${title(row.account)}" saved.`) }
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <h3>{{ t('شابلون اسناد خزانه داری', 'Treasury voucher template') }}</h3>
    <p class="muted">{{ t('حساب‌های ثابت اسناد دریافت و مراحل چک. سایر ردیف‌های سند، حساب خود را از گروه بابت، صندوق یا حساب بانکی می‌گیرند. سندهای تایید شده با تغییر این حساب‌ها عوض نمی‌شوند.',
      'The fixed accounts of receipt and cheque-step vouchers. The other lines take their account from the purpose group, cash desk or bank account. Confirmed vouchers do not change when these accounts change.') }}</p>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
    <table class="tr-table">
      <thead>
        <tr><th>{{ t('حساب', 'Account') }}</th><th>{{ t('کاربرد', 'Used for') }}</th><th style="width: 22rem">{{ t('حساب معین', 'Ledger account') }}</th><th /></tr>
      </thead>
      <tbody>
        <tr v-for="row in rows" :key="row.account">
          <td><strong>{{ title(row.account) }}</strong>
            <div v-if="row.code" class="muted small">{{ row.code }} – {{ ledgerName(row.code) }}</div>
            <div v-else class="tr-missing small">{{ t('تعیین نشده', 'Not set') }}</div>
          </td>
          <td class="small">{{ names[row.account] ? (fa ? names[row.account]!.use[0] : names[row.account]!.use[1]) : '' }}</td>
          <td>
            <SearchSelect v-model="edit[row.account]" :options="choices" :empty-label="t('— تعیین نشده —', '— not set —')" :aria-label="title(row.account)"
                          :none-text="t('حساب معین پیدا نشد (کدینگ حساب‌ها را وارد کنید)', 'No account found (import the chart of accounts)')" />
          </td>
          <td><button type="button" :disabled="busy !== '' || (edit[row.account] ?? '') === (row.code ?? '')" @click="save(row)">{{ t('ثبت', 'Save') }}</button></td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
h3 { margin: 0 0 .5rem; }
.muted { color: #7b8794; }
.small { font-size: .8rem; }
.tr-missing { color: #b42318; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; margin: .5rem 0; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-table { width: 100%; border-collapse: collapse; margin-top: .75rem; }
.tr-table th, .tr-table td { padding: .45rem .5rem; border-bottom: 1px solid #e4e7eb; text-align: start; vertical-align: middle; }
button { padding: .45rem .9rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .5; cursor: default; }
</style>
