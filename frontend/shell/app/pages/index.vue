<script setup lang="ts">
const { t, fiscalYear } = useShell()
</script>

<template>
  <section>
    <h1>{{ t('داشبورد', 'Dashboard') }}</h1>
    <p class="muted">{{ t('موجودی بانک‌ها و صندوق‌ها و پیش‌بینی نقدینگی در فاز ۶ اضافه می‌شود.', 'Bank and cash balances and the liquidity forecast are added in phase 6.') }} ({{ fiscalYear }})</p>
  </section>
</template>
