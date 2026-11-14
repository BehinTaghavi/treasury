<script setup lang="ts">
const { locale, fiscalYear } = useShell()
const { $apiBase } = useNuxtApp()
</script>

<template>
  <!-- The receipts area is a separate micro-frontend (frontend/remotes/receipts), loaded at run time. -->
  <RemoteArea module="receipts/Receipts" :component-props="{ locale, apiBase: $apiBase, fiscalYear }" :key="locale + fiscalYear" />
</template>
