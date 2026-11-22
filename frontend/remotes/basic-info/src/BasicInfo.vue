<script setup lang="ts">
/**
 * Basic Information (اطلاعات پایه), in the order of the new-version manual.
 * The simple lists share one editor (CatalogueList). All 14 items of the manual's Basic Information are here (owner, 12 Nov 2026:
 * finish Basic Information so it is reviewed at once), after «اطلاعات شرکت» (the company's name and logo; owner, 16 Nov 2026).
 * Owner, 20 Nov 2026 (the design he approved): the list of screens is the white column beside the main menu, grouped under small
 * titles (sections.ts, ui-kit SectionMenu); the order is unchanged.
 */
import { computed, ref } from 'vue'
import type { Locale } from '@treasury/ui-kit'
import SectionMenu from '@treasury/ui-kit/SectionMenu.vue'
import '@treasury/ui-kit/required.css'
import CatalogueList from './CatalogueList.vue'
import ChartAccounts from './ledger/ChartAccounts.vue'
import DetailAccounts from './ledger/DetailAccounts.vue'
import BankAccounts from './accounts/BankAccounts.vue'
import CashDesks from './accounts/CashDesks.vue'
import ChequeBooks from './accounts/ChequeBooks.vue'
import ChequeFormats from './accounts/ChequeFormats.vue'
import PettyCash from './accounts/PettyCash.vue'
import CompanyCards from './accounts/CompanyCards.vue'
import VoucherTemplate from './accounts/VoucherTemplate.vue'
import TransferLimits from './accounts/TransferLimits.vue'
import Beneficiaries from './organization/Beneficiaries.vue'
import CompanyInfo from './organization/CompanyInfo.vue'
import Departments from './organization/Departments.vue'
import Requesters from './organization/Requesters.vue'
import PermissionHolders from './organization/PermissionHolders.vue'
import Signatories from './organization/Signatories.vue'
import PurposeGroups from './purposes/PurposeGroups.vue'
import type { ListSpec } from './types'
import { sections } from './sections'

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

const selected = ref(sections[0]!.items[0]!.id)
const current = computed(() => lists.find(l => l.list === selected.value))
</script>

<template>
  <SectionMenu v-model="selected" class="tr-basic" :dir="fa ? 'rtl' : 'ltr'" :title="t('اطلاعات پایه', 'Basic information')" :groups="sections" :locale="locale" storage-key="basic-info">
    <div class="tr-screen">
      <CatalogueList v-if="current" :key="current.list + locale" :spec="current" :api-base="apiBase" :locale="locale" :token="token" />
      <CompanyInfo v-else-if="selected === 'company'" :key="'company' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <ChartAccounts v-else-if="selected === 'chart'" :key="'chart' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <DetailAccounts v-else-if="selected === 'details'" :key="'details' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <BankAccounts v-else-if="selected === 'bank-accounts'" :key="'bank-accounts' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <ChequeBooks v-else-if="selected === 'cheque-books'" :key="'cheque-books' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <TransferLimits v-else-if="selected === 'transfer-limits'" :key="'transfer-limits' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <PurposeGroups v-else-if="selected === 'purposes'" :key="'purposes' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <CashDesks v-else-if="selected === 'cash-desks'" :key="'cash-desks' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <ChequeFormats v-else-if="selected === 'cheque-formats'" :key="'cheque-formats' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <PettyCash v-else-if="selected === 'petty-cash'" :key="'petty-cash' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <CompanyCards v-else-if="selected === 'company-cards'" :key="'company-cards' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <Departments v-else-if="selected === 'departments'" :key="'departments' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <Requesters v-else-if="selected === 'requesters'" :key="'requesters' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <Beneficiaries v-else-if="selected === 'beneficiaries'" :key="'beneficiaries' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <Signatories v-else-if="selected === 'signatories'" :key="'signatories' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <PermissionHolders v-else-if="selected === 'permission-holders'" :key="'permission-holders' + locale" :api-base="apiBase" :locale="locale" :token="token" />
      <VoucherTemplate v-else-if="selected === 'voucher-template'" :key="'voucher-template' + locale" :api-base="apiBase" :locale="locale" :token="token" />
    </div>
  </SectionMenu>
</template>

<style scoped>
.tr-basic { color: #18233a; }
/* The screens of this area were made for a column of their own beside the old list; they keep a comfortable width. */
.tr-screen { max-width: 76rem; }
</style>
