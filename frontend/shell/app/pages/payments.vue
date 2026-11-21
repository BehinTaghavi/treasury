<script setup lang="ts">
const { locale, fiscalYear } = useShell()
const { $apiBase } = useNuxtApp()
</script>

<template>
  <!-- The payments area (owner's report no. 7: transfers between the company's bank accounts) is exposed by the receipts micro-frontend
       (frontend/remotes/receipts, ./Payments) and loaded at run time; the other payment screens join it in Phase E. -->
  <RemoteArea module="receipts/Payments" :component-props="{ locale, apiBase: $apiBase, fiscalYear }" :key="locale + fiscalYear" />
</template>
