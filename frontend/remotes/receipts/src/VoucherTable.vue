<script setup lang="ts">
/**
 * One treasury voucher (سند حسابداری) as the system proposes it, or as the treasurer confirmed it (owner's report no. 4, item 8; his
 * template table and answers of 15 Nov 2026: "one voucher for each receipt or operation, Treasurer confirms"). A line whose fixed
 * account is not set yet names the account to set in Basic Information → «شابلون اسناد خزانه داری».
 */
import { computed } from 'vue'
import { formatAmount, type Locale } from '@treasury/ui-kit'

export interface VoucherLine {
  isDebit: boolean; accountCode: string | null; detailCode: string | null; amountRial: number; description: string
  missing: string | null; foreignAmount: number | null; currency: string | null
}
export interface Voucher {
  source: string; step: number; operation: string; date: string; lines: VoucherLine[]; totalDebit: number; totalCredit: number; isComplete: boolean
  confirmedBy: string | null; confirmedAt: string | null
}

const props = withDefaults(defineProps<{ voucher: Voucher; locale?: Locale; accountName?: (code: string) => string | undefined; canConfirm?: boolean; busy?: boolean }>(),
  { locale: 'fa', accountName: undefined, canConfirm: false, busy: false })
defineEmits<{ confirm: [] }>()
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const money = (n: number) => formatAmount(n, props.locale)

/** Names of the template's fixed accounts, as on the Basic Information page. */
const fixedNames: Record<string, [string, string]> = {
  ChequesInCashDesk: ['اسناد نزد صندوق (اسناد دریافتنی)', 'Cheques in the cash desk'],
  ChequesInCollection: ['چک‌های در جریان وصول', 'Cheques in collection'],
  ProtestedCheques: ['اسناد واخواست شده (برگشتی)', 'Protested cheques'],
  ProtestedChequesInLegal: ['اسناد واخواست شده حقوقی شده', 'Protested cheques in legal action'],
  GuaranteeChequesInLegal: ['اسناد تضمینی حقوقی شده به نفع شرکت', 'Guarantee items in legal action'],
  GuaranteeChequesForCompany: ['چک انتظامی به نفع شرکت', 'Guarantee cheques for the company'],
  GuaranteeCounterpart: ['طرف حساب انتظامی', 'Guarantee counterpart'],
  BadDebts: ['مطالبات سوخت شده', 'Bad debts written off'],
  GuaranteeNotesForCompany: ['سفته انتظامی به نفع شرکت', 'Guarantee notes for the company'],
  NotesInCollection: ['سفته‌های در جریان وصول', 'Notes in collection'],
  // Owner's report no. 7: a transfer by cheque between two bank accounts of the company.
  FundsInTransit: ['وجوه در راه', 'Funds in transit'],
}
const missingName = (k: string) => (fixedNames[k] ? (props.locale === 'fa' ? fixedNames[k][0] : fixedNames[k][1]) : k)
const confirmed = computed(() => props.voucher.confirmedBy !== null)
const balanced = computed(() => props.voucher.totalDebit === props.voucher.totalCredit)
const at = computed(() => (props.voucher.confirmedAt ? new Date(props.voucher.confirmedAt).toLocaleString(props.locale === 'fa' ? 'fa-IR' : 'en-GB') : ''))
</script>

<template>
  <div class="tr-voucher">
    <div class="tr-buttons">
      <strong>{{ t('سند حسابداری', 'Voucher') }}: {{ voucher.operation }}</strong>
      <span class="num muted">{{ voucher.date }}</span>
      <span v-if="confirmed" class="tr-tag current">{{ t(`تایید شده توسط ${voucher.confirmedBy} – ${at}`, `Confirmed by ${voucher.confirmedBy} – ${at}`) }}</span>
      <span v-else class="tr-tag later">{{ t('پیش‌نویس (تایید نشده)', 'Draft (not confirmed)') }}</span>
      <button v-if="canConfirm && !confirmed" type="button" :disabled="busy || !voucher.isComplete" @click="$emit('confirm')">{{ t('تایید سند', 'Confirm voucher') }}</button>
    </div>
    <div class="tr-scroll">
      <table class="tr-table">
        <thead>
          <tr>
            <th>{{ t('معین', 'Account') }}</th><th>{{ t('تفصیل', 'Detail') }}</th><th>{{ t('شرح', 'Description') }}</th>
            <th>{{ t('بدهکار (ریال)', 'Debit (Rial)') }}</th><th>{{ t('بستانکار (ریال)', 'Credit (Rial)') }}</th><th>{{ t('ارزی', 'Currency') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(l, i) in voucher.lines" :key="i">
            <td>
              <template v-if="l.accountCode"><span class="num">{{ l.accountCode }}</span> <span class="muted small">{{ accountName?.(l.accountCode) }}</span></template>
              <span v-else-if="l.missing" class="tr-missing">{{ t(`تعیین نشده: ${missingName(l.missing)}`, `Not set: ${missingName(l.missing)}`) }}</span>
              <span v-else class="tr-missing">{{ t('حساب معین تعیین نشده', 'No account') }}</span>
            </td>
            <td class="num">{{ l.detailCode }}</td>
            <td class="small">{{ l.description }}</td>
            <td class="num">{{ l.isDebit ? money(l.amountRial) : '' }}</td>
            <td class="num">{{ l.isDebit ? '' : money(l.amountRial) }}</td>
            <td class="num small">{{ l.foreignAmount !== null ? `${money(l.foreignAmount)} ${l.currency}` : '' }}</td>
          </tr>
        </tbody>
        <tfoot>
          <tr>
            <td colspan="3"><strong>{{ t('جمع', 'Total') }}</strong></td>
            <td class="num"><strong>{{ money(voucher.totalDebit) }}</strong></td>
            <td class="num"><strong>{{ money(voucher.totalCredit) }}</strong></td>
            <td />
          </tr>
        </tfoot>
      </table>
    </div>
    <p v-if="!voucher.isComplete && !confirmed" class="tr-msg warning small">
      {{ balanced
        ? t('حساب بعضی ردیف‌ها تعیین نشده است. آن‌ها را در اطلاعات پایه ← «شابلون اسناد خزانه داری» (یا در گروه بابت، صندوق یا حساب بانکی) تعیین کنید؛ سپس سند قابل تایید است.',
            'Some lines have no account. Set them in Basic Information → "Treasury voucher template" (or on the purpose group, cash desk or bank account); then the voucher can be confirmed.')
        : t('جمع بدهکار و بستانکار برابر نیست.', 'Debits and credits differ.') }}
    </p>
  </div>
</template>

<style scoped src="./receipts.css"></style>
<style scoped>
.tr-voucher { border: 1px dashed #9fb3c8; border-radius: 8px; padding: .75rem; background: #fbfcfe; margin-top: .75rem; }
.tr-missing { color: #b42318; font-size: .85rem; }
</style>
