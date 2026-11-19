<script setup lang="ts">
/**
 * The printed payment sheet of a cheque transfer («برگ پرداخت – رسید واگذاری چک», owner, 19 Nov 2026): the sheet as a preview on the
 * screen, and a copy placed directly under <body> that is the only thing the browser prints (as NoteReceiptPrint does for the
 * receipt of notes).
 */
import type { Locale } from '@treasury/ui-kit'
import ChequeTransferSheet from './ChequeTransferSheet.vue'
import type { TransferSheet } from './transferSheet'

withDefaults(defineProps<{ sheet: TransferSheet; locale?: Locale }>(), { locale: 'fa' })
</script>

<template>
  <ChequeTransferSheet v-bind="$props" />
  <Teleport to="body">
    <div class="tr-print-root"><ChequeTransferSheet v-bind="$props" /></div>
  </Teleport>
</template>

<!-- Not scoped: these rules reach <body> and its children while the browser prints (the same rules as NoteReceiptPrint). -->
<style>
.tr-print-root { display: none; }
@media print {
  html, body { background: #fff !important; }
  body > *:not(.tr-print-root) { display: none !important; }
  .tr-print-root { display: block; }
  .tr-print-root .tr-print-sheet { width: auto; max-width: none; min-height: 0; margin: 0; padding: 0; border: 0; }
  /* A hair of room at the sides, so the tables' outer lines are not cut at the edge of the printable area. */
  .tr-print-root .tr-transfer-sheet { padding: 0 .4mm; }
  @page { size: A4; margin: 11mm 12mm; }
}
</style>
