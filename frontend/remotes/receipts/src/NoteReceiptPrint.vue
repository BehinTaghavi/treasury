<script setup lang="ts">
/**
 * The printed receipt of promissory notes: the sheet as a preview on the screen, and a copy placed directly under <body> that is
 * the only thing the browser prints (everything else on the page, the shell's menu included, is left out).
 */
import type { Locale } from '@treasury/ui-kit'
import NoteReceiptSheet from './NoteReceiptSheet.vue'
import type { NoteReceipt } from './notes'

withDefaults(defineProps<{
  receipt: NoteReceipt; locale?: Locale; departmentName?: string; ledgerAccount?: string; reference1Title?: string; reference2Title?: string
}>(), { locale: 'fa', departmentName: '', ledgerAccount: '', reference1Title: '', reference2Title: '' })
</script>

<template>
  <NoteReceiptSheet v-bind="$props" />
  <Teleport to="body">
    <div class="tr-print-root"><NoteReceiptSheet v-bind="$props" /></div>
  </Teleport>
</template>

<!-- Not scoped: these rules reach <body> and its children while the browser prints. -->
<style>
.tr-print-root { display: none; }
@media print {
  html, body { background: #fff !important; }
  body > *:not(.tr-print-root) { display: none !important; }
  .tr-print-root { display: block; }
  .tr-print-root .tr-print-sheet { width: auto; max-width: none; min-height: 0; margin: 0; padding: 0; border: 0; }
  @page { size: A4; margin: 11mm 12mm; }
}
</style>
