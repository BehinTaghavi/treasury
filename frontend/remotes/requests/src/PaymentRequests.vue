<script setup lang="ts">
/**
 * Payment requests (درخواست وجه), owner 15 Nov 2026. New-version manual «ب-صدور درخواست وجه»: a department enters the request,
 * a person allowed to approve ticks «تایید پرداخت», and treasury pays from «فهرست درخواست وجه های تایید شده جهت پرداخت».
 * Paying them (cheque, cash, transfer…) comes with the payments screens.
 */
import { computed, ref } from 'vue'
import '@treasury/ui-kit/required.css'
import type { Locale } from '@treasury/ui-kit'
import RequestForm from './RequestForm.vue'
import RequestList from './RequestList.vue'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined; fiscalYear?: number }>(),
  { apiBase: '', locale: 'fa', token: undefined, fiscalYear: undefined })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')

const tabs = [
  { id: 'new', title: ['درخواست وجه جدید', 'New request'] },
  { id: 'list', title: ['فهرست درخواست‌ها و تایید', 'Requests and approval'] },
  { id: 'approved', title: ['تایید شده جهت پرداخت', 'Approved for payment'] },
] as const
type Tab = (typeof tabs)[number]['id'] | 'edit'
const tab = ref<Tab>('new')
const editId = ref<string | null>(null)
function edit(id: string) { editId.value = id; tab.value = 'edit' }
function backToList() { editId.value = null; tab.value = 'list' }
</script>

<template>
  <section class="tr-requests" :dir="fa ? 'rtl' : 'ltr'">
    <h2>{{ t('درخواست وجه', 'Payment requests') }}</h2>
    <nav class="tr-tabs" role="tablist">
      <button v-for="x in tabs" :key="x.id" type="button" role="tab" :aria-selected="tab === x.id" :class="{ active: tab === x.id }" @click="tab = x.id; editId = null">
        {{ fa ? x.title[0] : x.title[1] }}
      </button>
    </nav>
    <p v-if="tab === 'new'" class="hint">{{ t('هر درخواست یک ذینفع و یک نوع پرداخت دارد؛ برای پرداخت به یک ذینفع به چند روش (نقد، چک…) درخواست‌های جدا ثبت کنید. درخواست پس از تایید در فهرست «تایید شده جهت پرداخت» قرار می‌گیرد.',
      'One beneficiary and one payment method per request; to pay one beneficiary in several ways, enter separate requests. Once approved, a request appears under "Approved for payment".') }}</p>
    <RequestForm v-if="tab === 'new'" :key="'new' + locale" :api-base="apiBase" :locale="locale" :token="token" />
    <RequestForm v-else-if="tab === 'edit'" :key="'edit' + editId + locale" :edit-id="editId" :api-base="apiBase" :locale="locale" :token="token" @closed="backToList" />
    <RequestList v-else-if="tab === 'list'" :key="'list' + locale" :api-base="apiBase" :locale="locale" :token="token" :fiscal-year="fiscalYear" @edit="edit" />
    <RequestList v-else :key="'approved' + locale" approved :api-base="apiBase" :locale="locale" :token="token" :fiscal-year="fiscalYear" @edit="edit" />
  </section>
</template>

<style scoped>
.tr-requests { font-family: Vazirmatn, Tahoma, sans-serif; color: #1f2933; }
.tr-tabs { display: flex; flex-wrap: wrap; gap: .25rem; border-bottom: 1px solid #e4e7eb; margin-bottom: .75rem; }
.tr-tabs button { padding: .55rem .9rem; border: 0; border-bottom: 3px solid transparent; background: transparent; color: #1f2933; font: inherit; cursor: pointer; }
.tr-tabs button.active { border-bottom-color: #2563eb; color: #1d4ed8; font-weight: 600; }
.hint { color: #52606d; font-size: .85rem; margin: .25rem 0 0; }
</style>
