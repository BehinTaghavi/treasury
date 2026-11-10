<script setup lang="ts">
const { locale } = useShell()
const { $apiBase } = useNuxtApp()
</script>

<template>
  <!-- The receipts area is a separate micro-frontend (frontend/remotes/receipts), loaded at run time. -->
  <RemoteArea module="receipts/ReceivedCheques" :component-props="{ locale, apiBase: $apiBase }" :key="locale" />
</template>
