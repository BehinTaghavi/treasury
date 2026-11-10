<script setup lang="ts">
const { locale, fiscalYear, t } = useShell()
const years = [1406, 1405, 1404, 1403]
watchEffect(() => {
  if (import.meta.client) {
    document.documentElement.lang = locale.value
    document.documentElement.dir = locale.value === 'fa' ? 'rtl' : 'ltr'
  }
})
const menu = computed(() => [
  { to: '/', label: t('داشبورد', 'Dashboard') },
  { to: '/receipts', label: t('دریافت‌ها', 'Receipts') },
  { to: '/payments', label: t('پرداخت‌ها', 'Payments') },
  { to: '/basic-info', label: t('اطلاعات پایه', 'Basic info') },
  { to: '/reports', label: t('گزارشات', 'Reports') },
])
</script>

<template>
  <div class="shell">
    <header class="bar">
      <strong>{{ t('سیستم خزانه‌داری', 'Treasury') }}</strong>
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
