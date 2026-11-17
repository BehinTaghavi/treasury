<script setup lang="ts">
import { COMPANY_CHANGED_EVENT } from '@treasury/ui-kit'

const { locale, fiscalYear, t } = useShell()
// Owner, 16 Nov 2026: the company's name and logo (Basic Information → «اطلاعات شرکت») at the top of every screen.
const company = useCompany()
onMounted(() => {
  void company.load()
  window.addEventListener(COMPANY_CHANGED_EVENT, company.load)
})
onBeforeUnmount(() => window.removeEventListener(COMPANY_CHANGED_EVENT, company.load))
useHead({ title: () => (company.name.value ? `${company.name.value} – ${t('خزانه‌داری', 'Treasury')}` : t('خزانه‌داری', 'Treasury')) })
const years = [1406, 1405, 1404, 1403]
watchEffect(() => {
  if (import.meta.client) {
    document.documentElement.lang = locale.value
    document.documentElement.dir = locale.value === 'fa' ? 'rtl' : 'ltr'
  }
})
// Order set by the product owner (docs/decisions.md, 10 Nov 2026): basic information first, payment requests before payments.
const menu = computed(() => [
  { to: '/', label: t('داشبورد', 'Dashboard') },
  { to: '/basic-info', label: t('اطلاعات پایه', 'Basic info') },
  { to: '/payment-requests', label: t('درخواست وجه', 'Payment requests') },
  { to: '/payments', label: t('پرداخت‌ها', 'Payments') },
  // Owner, 14 Nov 2026: opening balances get their own item, above receipts.
  { to: '/opening-balances', label: t('موجودی اول دوره', 'Opening balances') },
  { to: '/receipts', label: t('دریافت‌ها', 'Receipts') },
  { to: '/reports', label: t('گزارشات', 'Reports') },
])
</script>

<template>
  <div class="shell">
    <header class="bar">
      <div class="brand">
        <img v-if="company.logoUrl.value" class="logo" :src="company.logoUrl.value" :alt="t('آرم شرکت', 'Company logo')" />
        <div class="names">
          <strong v-if="company.name.value" class="company">{{ company.name.value }}</strong>
          <span :class="company.name.value ? 'system' : 'system alone'">{{ t('سیستم خزانه‌داری', 'Treasury') }}</span>
        </div>
      </div>
      <label>{{ t('سال مالی', 'Fiscal year') }}
        <select v-model.number="fiscalYear"><option v-for="y in years" :key="y" :value="y">{{ y }}</option></select>
      </label>
      <button type="button" class="lang" @click="locale = locale === 'fa' ? 'en' : 'fa'">{{ locale === 'fa' ? 'English' : 'فارسی' }}</button>
    </header>
    <nav class="menu">
      <NuxtLink v-for="m in menu" :key="m.to" :to="m.to">{{ m.label }}</NuxtLink>
    </nav>
    <main class="content"><slot /></main>
  </div>
</template>
