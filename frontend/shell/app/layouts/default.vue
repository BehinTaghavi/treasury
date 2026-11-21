<script setup lang="ts">
import { COMPANY_CHANGED_EVENT } from '@treasury/ui-kit'

const { locale, fiscalYear, t } = useShell()
// Owner, 16 Nov 2026: the company's name and logo (Basic Information → «مشخصات شرکت») at the top of every screen.
const company = useCompany()
onMounted(() => {
  void company.load()
  window.addEventListener(COMPANY_CHANGED_EVENT, company.load)
})
onBeforeUnmount(() => window.removeEventListener(COMPANY_CHANGED_EVENT, company.load))
useHead({ title: () => (company.name.value ? `${company.name.value} – ${t('خزانه‌داری', 'Treasury')}` : t('خزانه‌داری', 'Treasury')) })
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
// Owner, 20 Nov 2026 (the design he approved): the menu is a narrow rail, each item an icon with its name under it.
const menu = computed(() => [
  { to: '/', label: t('داشبورد', 'Dashboard'), icon: 'M4 4h7v7H4zM13 4h7v4h-7zM13 12h7v8h-7zM4 15h7v5H4z' },
  { to: '/basic-info', label: t('اطلاعات پایه', 'Basic info'), icon: 'M4 6c0-1.7 3.6-3 8-3s8 1.3 8 3-3.6 3-8 3-8-1.3-8-3zM4 6v6c0 1.7 3.6 3 8 3s8-1.3 8-3V6M4 12v6c0 1.7 3.6 3 8 3s8-1.3 8-3v-6' },
  { to: '/payment-requests', label: t('درخواست وجه', 'Payment requests'), icon: 'M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8zM14 3v5h5M9 13h6M9 17h6' },
  { to: '/payments', label: t('پرداخت‌ها', 'Payments'), icon: 'M12 15V4M7 9l5-5 5 5M4 15v3a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-3' },
  // Owner, 14 Nov 2026: opening balances get their own item, above receipts.
  { to: '/opening-balances', label: t('موجودی اول دوره', 'Opening balances'), icon: 'M3 5h6a3 3 0 0 1 3 3v13a3 3 0 0 0-3-3H3zM21 5h-6a3 3 0 0 0-3 3v13a3 3 0 0 1 3-3h6z' },
  { to: '/receipts', label: t('دریافت‌ها', 'Receipts'), icon: 'M12 4v11M7 10l5 5 5-5M4 15v3a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-3' },
  { to: '/reports', label: t('گزارشات', 'Reports'), icon: 'M4 20V4M4 20h16M9 16v-5M13 16V8M17 16v-8' },
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
      <label class="year">{{ t('سال مالی', 'Fiscal year') }}
        <select v-model.number="fiscalYear"><option v-for="y in years" :key="y" :value="y">{{ y }}</option></select>
      </label>
      <span v-if="version" class="version" :title="t('نسخهٔ در حال اجرا', 'Version running')">{{ t('نسخه', 'Version') }} {{ version }}</span>
      <button type="button" class="lang" @click="locale = locale === 'fa' ? 'en' : 'fa'">{{ locale === 'fa' ? 'English' : 'فارسی' }}</button>
    </header>
    <nav class="menu" :aria-label="t('منوی اصلی', 'Main menu')">
      <div class="menu-in">
        <NuxtLink v-for="m in menu" :key="m.to" :to="m.to">
          <svg aria-hidden="true" viewBox="0 0 24 24"><path :d="m.icon" /></svg>
          <span>{{ m.label }}</span>
        </NuxtLink>
      </div>
    </nav>
    <main class="content"><slot /></main>
  </div>
</template>
