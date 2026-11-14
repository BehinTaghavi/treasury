<script setup lang="ts">
const { locale, fiscalYear } = useShell()
const { $apiBase } = useNuxtApp()
</script>

<template>
  <!-- Opening balances (موجودی اول دوره) belong to the Basic Information micro-frontend (frontend/remotes/basic-info), loaded at run time. -->
  <RemoteArea module="basicinfo/OpeningBalances" :component-props="{ locale, apiBase: $apiBase, fiscalYear }" :key="locale + fiscalYear" />
</template>
