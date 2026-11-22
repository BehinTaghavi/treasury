<script setup lang="ts">
/**
 * «محل درج اسکن نمونه امضاء» (owner's report no. 8, item 3-ج): shows the stored scan of a person's signature, lets the user choose
 * another file (PNG or JPEG, at most 1 MB) and remove it. The file is sent by the page after the form is saved.
 */
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import type { Locale } from '@treasury/ui-kit'
import { fetchSignature, signatureFileProblem, type ScanAccess } from './signatureScan'

const props = defineProps<{ access: ScanAccess; locale: Locale; /** Where the stored scan is read; empty: nothing stored yet. */ storedPath: string; chosen: File | null }>()
const emit = defineEmits<{ (e: 'update:chosen', file: File | null): void; (e: 'problem', code: string): void; (e: 'remove'): void }>()
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)

const stored = ref('')
const chosenPreview = ref('')
const preview = computed(() => chosenPreview.value || stored.value)
const forget = (url: string) => { if (url) URL.revokeObjectURL(url) }

watch(() => props.storedPath, async path => {
  forget(stored.value)
  stored.value = path ? await fetchSignature(props.access, path) : ''
}, { immediate: true })
watch(() => props.chosen, file => {
  forget(chosenPreview.value)
  chosenPreview.value = file ? URL.createObjectURL(file) : ''
}, { immediate: true })
onBeforeUnmount(() => { forget(stored.value); forget(chosenPreview.value) })

function choose(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  const problem = signatureFileProblem(file)
  if (problem) return emit('problem', problem)
  emit('update:chosen', file)
}
</script>

<template>
  <div class="tr-scan">
    <span class="tr-scan-title">{{ t('اسکن نمونه امضا', 'Scan of the specimen signature') }}</span>
    <div class="tr-scan-box">
      <img v-if="preview" :src="preview" :alt="t('نمونه امضا', 'Specimen signature')" />
      <span v-else class="muted">{{ t('هنوز اسکنی بارگذاری نشده است.', 'No scan yet.') }}</span>
    </div>
    <div class="tr-scan-actions">
      <label class="tr-upload">{{ preview ? t('تعویض اسکن', 'Replace scan') : t('انتخاب فایل اسکن امضا', 'Choose the scan file') }}
        <input type="file" accept="image/png,image/jpeg" @change="choose" />
      </label>
      <button v-if="chosen" type="button" class="tr-link" @click="emit('update:chosen', null)">{{ t('انصراف از این فایل', 'Discard this file') }}</button>
      <button v-else-if="stored" type="button" class="tr-link danger" @click="emit('remove')">{{ t('حذف اسکن', 'Remove scan') }}</button>
    </div>
    <small class="muted">{{ t('PNG یا JPEG، حداکثر ۱ مگابایت. پس از «ذخیره» ثبت می‌شود.', 'PNG or JPEG, at most 1 MB. Stored when you press Save.') }}</small>
  </div>
</template>

<style scoped>
.tr-scan { display: flex; flex-direction: column; gap: .4rem; font-size: .85rem; }
.tr-scan-box { width: 16rem; max-width: 100%; height: 7rem; display: flex; align-items: center; justify-content: center; background: #f5f7fa; border: 1px dashed #cbd2d9; border-radius: 8px; overflow: hidden; }
.tr-scan-box img { max-width: 100%; max-height: 100%; object-fit: contain; }
.tr-scan-actions { display: flex; flex-wrap: wrap; align-items: center; gap: .75rem; }
.tr-upload { cursor: pointer; color: #1d4ed8; font-weight: 600; }
.tr-upload input { display: none; }
.tr-link { background: transparent; color: #1d4ed8; border: 0; padding: .2rem .4rem; font: inherit; cursor: pointer; }
.tr-link.danger { color: #b42318; }
.muted { color: #7b8794; }
</style>
