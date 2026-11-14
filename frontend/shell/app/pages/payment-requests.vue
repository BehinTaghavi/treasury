<script setup lang="ts">
const { locale, fiscalYear } = useShell()
const { $apiBase } = useNuxtApp()
</script>

<template>
  <!-- Payment requests (درخواست وجه) are a separate micro-frontend (frontend/remotes/requests), loaded at run time. -->
  <RemoteArea module="requests/PaymentRequests" :component-props="{ locale, apiBase: $apiBase, fiscalYear }" :key="locale + fiscalYear" />
</template>
