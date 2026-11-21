<script setup lang="ts">
const { locale, fiscalYear, t } = useShell()
// The version running (commit and date, written into public/version.txt when the ZIP is made), so the owner can see it is the newest.
const version = ref('')
onMounted(async () => {
  try {
    const text = (await (await fetch('/version.txt', { cache: 'no-store' })).text()).trim()
    if (text && !text.startsWith('$Format')) version.value = text
  } catch { /* no stamp in a development run */ }
})
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
      <strong>{{ t('سیستم خزانه‌داری', 'Treasury') }}</strong>
      <label>{{ t('سال مالی', 'Fiscal year') }}
        <select v-model.number="fiscalYear"><option v-for="y in years" :key="y" :value="y">{{ y }}</option></select>
      </label>
      <span v-if="version" class="version" :title="t('نسخهٔ در حال اجرا', 'Version running')">{{ t('نسخه', 'Version') }} {{ version }}</span>
      <button type="button" class="lang" @click="locale = locale === 'fa' ? 'en' : 'fa'">{{ locale === 'fa' ? 'English' : 'فارسی' }}</button>
    </header>
    <nav class="menu">
      <NuxtLink v-for="m in menu" :key="m.to" :to="m.to">{{ m.label }}</NuxtLink>
    </nav>
    <main class="content"><slot /></main>
  </div>
</template>
