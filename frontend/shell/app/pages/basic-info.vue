<script setup lang="ts">
const { locale } = useShell()
const { $apiBase } = useNuxtApp()
</script>

<template>
  <!-- Basic Information is a separate micro-frontend (frontend/remotes/basic-info), loaded at run time. -->
  <RemoteArea module="basicinfo/BasicInfo" :component-props="{ locale, apiBase: $apiBase }" :key="locale" />
</template>
