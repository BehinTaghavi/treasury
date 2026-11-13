<script setup lang="ts">
/**
 * Basic Information (اطلاعات پایه), in the order of the new-version manual.
 * The simple lists share one editor (CatalogueList). All 14 items of the manual's Basic Information are here (owner, 12 Nov 2026:
 * finish Basic Information so it is reviewed at once).
 */
import { computed, ref } from 'vue'
import type { Locale } from '@treasury/ui-kit'
import CatalogueList from './CatalogueList.vue'
import ChartAccounts from './ledger/ChartAccounts.vue'
import DetailAccounts from './ledger/DetailAccounts.vue'
import BankAccounts from './accounts/BankAccounts.vue'
import CashDesks from './accounts/CashDesks.vue'
import ChequeBooks from './accounts/ChequeBooks.vue'
import ChequeFormats from './accounts/ChequeFormats.vue'
import PettyCash from './accounts/PettyCash.vue'
import Beneficiaries from './organization/Beneficiaries.vue'
import Departments from './organization/Departments.vue'
import Requesters from './organization/Requesters.vue'
import PurposeGroups from './purposes/PurposeGroups.vue'
import type { ListSpec } from './types'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), {
  apiBase: '',
  locale: 'fa',
  token: undefined,
})

const fa = computed(() => props.locale === 'fa')
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)

const lists: ListSpec[] = [
  { list: 'countries', title: ['کشور', 'Countries'], singular: ['کشور', 'country'] },
  { list: 'provinces', title: ['استان / ایالت', 'Provinces'], singular: ['استان', 'province'], parent: { list: 'countries', label: ['کشور', 'Country'] } },
  {
    list: 'cities', title: ['شهر', 'Cities'], singular: ['شهر', 'city'],
    parent: { list: 'provinces', label: ['استان', 'Province'] }, grandparent: { list: 'countries', label: ['کشور', 'Country'] },
  },
  { list: 'currencies', title: ['نوع ارز', 'Currencies'], singular: ['ارز', 'currency'], hasCode: true },
  { list: 'banks', title: ['نام بانک‌ها', 'Bank names'], singular: ['بانک', 'bank'] },
  { list: 'references', title: ['عطف‌ها', 'References'], singular: ['عطف', 'reference'] },
]

/** The menu, in the order of the new-version manual ("اطلاعات پایه" items 1–10), with the copy of the accounting system's accounts. */
const sections: { id: string; title: [string, string] }[] = [
  ...lists.filter(l => l.list !== 'references').map(l => ({ id: l.list, title: l.title })),
  { id: 'chart', title: ['کدینگ حساب‌ها', 'Chart of accounts'] },
  { id: 'details', title: ['حساب‌های تفصیل', 'Detail accounts'] },
  { id: 'bank-accounts', title: ['حساب‌های بانکی', 'Bank accounts'] },
  // Owner, 14 Nov 2026: cheque types first; cheque books choose from them.
  { id: 'cheque-formats', title: ['انواع چک و چاپ', 'Cheque types and printing'] },
  { id: 'cheque-books', title: ['دسته چک‌ها', 'Cheque books'] },
  { id: 'references', title: ['عطف‌ها', 'References'] },
  { id: 'purposes', title: ['گروه بابت‌ها و بابت‌ها', 'Purpose groups and purposes'] },
  { id: 'cash-desks', title: ['صندوق‌ها', 'Cash desks'] },
  { id: 'petty-cash', title: ['تنخواه گردان‌ها', 'Petty cash'] },
  { id: 'departments', title: ['بخش‌ها (واحد سازمانی)', 'Departments'] },
  { id: 'requesters', title: ['درخواست کنندگان وجوه', 'Requesters of funds'] },
  { id: 'beneficiaries', title: ['ذینفعان', 'Beneficiaries'] },
]

const selected = ref(sections[0]!.id)
const current = computed(() => lists.find(l => l.list === selected.value))
</script>

<template>
  <section class="tr-basic" :dir="fa ? 'rtl' : 'ltr'">
    <h2>{{ t('اطلاعات پایه', 'Basic information') }}</h2>
    <div class="tr-layout">
      <nav class="tr-sections" :aria-label="t('بخش‌های اطلاعات پایه', 'Basic information sections')">
        <button
          v-for="s in sections"
          :key="s.id"
          type="button"
          :class="{ active: s.id === selected }"
          :aria-current="s.id === selected ? 'page' : undefined"
          @click="selected = s.id"
        >{{ fa ? s.title[0] : s.title[1] }}</button>
      </nav>
      <CatalogueList v-if="current" :key="current.list + locale" :spec="current" :api-base="apiBase" :locale="locale" :token="token" />
      <ChartAccounts v-else-if="selected === 'chart'" :key="'chart' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <DetailAccounts v-else-if="selected === 'details'" :key="'details' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <BankAccounts v-else-if="selected === 'bank-accounts'" :key="'bank-accounts' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <ChequeBooks v-else-if="selected === 'cheque-books'" :key="'cheque-books' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <PurposeGroups v-else-if="selected === 'purposes'" :key="'purposes' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <CashDesks v-else-if="selected === 'cash-desks'" :key="'cash-desks' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <ChequeFormats v-else-if="selected === 'cheque-formats'" :key="'cheque-formats' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <PettyCash v-else-if="selected === 'petty-cash'" :key="'petty-cash' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <Departments v-else-if="selected === 'departments'" :key="'departments' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <Requesters v-else-if="selected === 'requesters'" :key="'requesters' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <Beneficiaries v-else-if="selected === 'beneficiaries'" :key="'beneficiaries' + locale" :api-base="apiBase" :locale="locale" :token="token" />
    </div>
  </section>
</template>

<style scoped>
.tr-basic { font-family: Vazirmatn, Tahoma, sans-serif; color: #1f2933; }
.tr-layout { display: grid; grid-template-columns: 13rem 1fr; gap: 1.25rem; align-items: start; }
.tr-sections { display: flex; flex-direction: column; gap: .25rem; }
.tr-sections button { text-align: start; padding: .5rem .75rem; border: 0; border-radius: 6px; background: transparent; color: #1f2933; font: inherit; cursor: pointer; }
.tr-sections button.active { background: #e8effc; color: #1d4ed8; font-weight: 600; }
.tr-sections button:hover { background: #f0f3f8; }
@media (max-width: 720px) { .tr-layout { grid-template-columns: 1fr; } }
</style>
