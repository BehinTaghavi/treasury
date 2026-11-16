<script setup lang="ts">
/**
 * «اطلاعات شرکت» (owner, 16 Nov 2026): the company's name and its logo (آرم), scanned by the user and kept in Basic Information.
 * Both are shown at the top of every screen by the shell, which is told about a change with COMPANY_CHANGED_EVENT.
 * The official details (owner's answer to Q38: شناسه ملی, کد اقتصادی, شماره ثبت, نشانی, کد پستی, تلفن, آدرس سایت, ایمیل) are optional
 * and go on printed forms with the name and logo (Q37). Saved by reference-svc: one profile per client database.
 */
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import {
  COMPANY_CHANGED_EVENT, COMPANY_DETAIL_FIELDS, COMPANY_LOGO_PATH, COMPANY_PATH, createApi, logoFileProblem, translate,
  type CompanyDetails, type CompanyInfo, type Locale,
} from '@treasury/ui-kit'

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)

const noDetails = (): CompanyDetails => ({ nationalId: null, economicCode: null, registrationNumber: null, address: null, postalCode: null, phone: null, website: null, email: null })
const company = ref<CompanyInfo>({ name: '', hasLogo: false, version: 0, ...noDetails() })
const name = ref('')
/** The official details as typed; an empty box is "not entered". */
const details = reactive<Record<keyof CompanyDetails, string>>({ nationalId: '', economicCode: '', registrationNumber: '', address: '', postalCode: '', phone: '', website: '', email: '' })
/** The logo as stored; shown until another file is chosen. */
const storedLogo = ref('')
/** A file chosen but not saved yet, with its preview. */
const chosen = ref<File | null>(null)
const chosenPreview = ref('')
const loaded = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)

const preview = computed(() => chosenPreview.value || storedLogo.value)
const formChanged = computed(() => name.value.trim() !== company.value.name || COMPANY_DETAIL_FIELDS.some(f => details[f].trim() !== (company.value[f] ?? '')))
const changed = computed(() => formChanged.value || chosen.value !== null)
const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }

function headers(): Record<string, string> {
  const h: Record<string, string> = { 'Idempotency-Key': crypto.randomUUID() }
  const token = props.token?.()
  if (token) h.Authorization = `Bearer ${token}`
  return h
}

function forget(url: string) {
  if (url) URL.revokeObjectURL(url)
}

function dropChosen() {
  forget(chosenPreview.value)
  chosenPreview.value = ''
  chosen.value = null
}

/** The logo is fetched with the same credentials as the API (an <img> cannot send them) and shown from memory. */
async function loadLogo() {
  forget(storedLogo.value)
  storedLogo.value = ''
  if (!company.value.hasLogo) return
  try {
    const res = await fetch(`${props.apiBase}${COMPANY_LOGO_PATH}`, { headers: headers(), cache: 'no-store' })
    if (res.ok) storedLogo.value = URL.createObjectURL(await res.blob())
  } catch { /* the name is still shown */ }
}

async function load(): Promise<boolean> {
  const r = await api.get<CompanyInfo>(COMPANY_PATH)
  if (!r.ok) { fail(r.code); return false }
  company.value = { ...noDetails(), ...r.value }
  name.value = r.value.name
  for (const f of COMPANY_DETAIL_FIELDS) details[f] = r.value[f] ?? ''
  loaded.value = true
  await loadLogo()
  return true
}

function choose(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return
  const problem = logoFileProblem(file)
  if (problem) return fail(problem)
  dropChosen()
  chosen.value = file
  chosenPreview.value = URL.createObjectURL(file)
  message.value = null
}

async function save() {
  if (busy.value || !changed.value) return
  if (!name.value.trim()) return fail('Company.NameRequired')
  busy.value = true
  try {
    // The name and details first (a logo belongs to a named company), then the logo with the profile's new version.
    if (formChanged.value) {
      const form = { name: name.value, ...Object.fromEntries(COMPANY_DETAIL_FIELDS.map(f => [f, details[f].trim() || null])) }
      const r = await api.put<string>(COMPANY_PATH, { form, version: company.value.version })
      if (!r.ok) return fail(r.code)
      if (!(await load())) return
    }
    if (chosen.value) {
      const body = new FormData()
      body.append('file', chosen.value)
      body.append('version', String(company.value.version))
      const res = await fetch(`${props.apiBase}${COMPANY_LOGO_PATH}`, { method: 'POST', headers: headers(), body }).catch(() => null)
      if (!res) return fail('Network.Error')
      if (!res.ok) {
        const problem = await res.json().catch(() => ({}))
        return fail((problem as { code?: string }).code ?? 'Unknown')
      }
      dropChosen()
      if (!(await load())) return
    }
    message.value = { kind: 'ok', text: t('اطلاعات شرکت ثبت شد و در بالای همهٔ صفحه‌ها نمایش داده می‌شود.', 'Company information saved; it is shown at the top of every screen.') }
  } finally {
    busy.value = false
    // Also after a half-finished save (name saved, logo refused): the bar shows what is stored.
    window.dispatchEvent(new CustomEvent(COMPANY_CHANGED_EVENT))
  }
}

async function removeLogo() {
  if (chosen.value) return dropChosen()
  if (busy.value || !company.value.hasLogo) return
  if (!window.confirm(t('آرم شرکت حذف شود؟', "Remove the company's logo?"))) return
  busy.value = true
  const r = await api.post<string>(`${COMPANY_LOGO_PATH}/remove`, { version: company.value.version })
  busy.value = false
  if (!r.ok) return fail(r.code)
  // What is typed but not saved yet stays in the boxes.
  const typed = { name: name.value, ...details }
  await load()
  name.value = typed.name
  for (const f of COMPANY_DETAIL_FIELDS) details[f] = typed[f]
  message.value = { kind: 'ok', text: t('آرم شرکت حذف شد.', 'Logo removed.') }
  window.dispatchEvent(new CustomEvent(COMPANY_CHANGED_EVENT))
}

onMounted(load)
onBeforeUnmount(() => { forget(storedLogo.value); forget(chosenPreview.value) })
</script>

<template>
  <div>
    <h3>{{ t('اطلاعات شرکت', 'Company information') }}</h3>
    <p class="muted">{{ t('نام و آرم شرکت در بالای همهٔ صفحه‌های سیستم نمایش داده می‌شود؛ مشخصات شرکت همراه با نام و آرم در بالای فرم‌های چاپی می‌آید.',
      "The company's name and logo are shown at the top of every screen; its details go with them at the top of printed forms.") }}</p>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <form v-if="loaded" class="tr-form" @submit.prevent="save">
      <fieldset>
        <legend>{{ t('نام و آرم', 'Name and logo') }}</legend>
        <label class="req wide"><span>{{ t('نام شرکت', 'Company name') }} <span class="star">*</span></span>
          <input v-model="name" maxlength="100" autocomplete="organization" />
        </label>
        <div class="wide tr-logo">
          <span class="tr-logo-title">{{ t('آرم شرکت', 'Company logo') }}</span>
          <div class="tr-logo-box">
            <img v-if="preview" :src="preview" :alt="t('آرم شرکت', 'Company logo')" />
            <span v-else class="muted">{{ t('هنوز آرمی بارگذاری نشده است.', 'No logo yet.') }}</span>
          </div>
          <div class="tr-logo-actions">
            <label class="tr-upload">{{ preview ? t('تعویض آرم', 'Replace logo') : t('انتخاب فایل آرم', 'Choose the logo file') }}
              <input type="file" accept="image/png,image/jpeg" @change="choose" />
            </label>
            <button v-if="chosen || company.hasLogo" type="button" class="tr-link danger" :disabled="busy" @click="removeLogo">
              {{ chosen ? t('انصراف از این فایل', 'Discard this file') : t('حذف آرم', 'Remove logo') }}
            </button>
          </div>
          <small class="muted">{{ t('فایل اسکن شدهٔ آرم: PNG یا JPEG، حداکثر ۱ مگابایت.', 'The scanned logo: PNG or JPEG, at most 1 MB.') }}</small>
          <small v-if="chosen" class="tr-warn">{{ t('این فایل هنوز ثبت نشده است؛ «ثبت» را بزنید.', 'This file is not saved yet; press Save.') }}</small>
        </div>
      </fieldset>
      <fieldset>
        <legend>{{ t('مشخصات شرکت (اختیاری)', 'Company details (optional)') }}</legend>
        <label>{{ t('شناسه ملی (۱۱ رقم)', 'National ID (11 digits)') }}<input v-model="details.nationalId" dir="ltr" inputmode="numeric" maxlength="11" autocomplete="off" /></label>
        <label>{{ t('کد اقتصادی (۱۱ یا ۱۲ رقم)', 'Economic code (11 or 12 digits)') }}<input v-model="details.economicCode" dir="ltr" inputmode="numeric" maxlength="14" autocomplete="off" /></label>
        <label>{{ t('شماره ثبت', 'Registration number') }}<input v-model="details.registrationNumber" dir="ltr" inputmode="numeric" maxlength="10" autocomplete="off" /></label>
        <label>{{ t('کد پستی (۱۰ رقم)', 'Postal code (10 digits)') }}<input v-model="details.postalCode" dir="ltr" inputmode="numeric" maxlength="11" autocomplete="postal-code" /></label>
        <label class="wide">{{ t('نشانی', 'Address') }}<input v-model="details.address" maxlength="300" autocomplete="street-address" /></label>
        <label>{{ t('تلفن', 'Telephone') }}<input v-model="details.phone" dir="ltr" inputmode="tel" maxlength="24" autocomplete="tel" /></label>
        <label>{{ t('آدرس سایت', 'Website') }}<input v-model="details.website" dir="ltr" inputmode="url" maxlength="100" autocomplete="url" placeholder="www.example.ir" /></label>
        <label>{{ t('ایمیل', 'E-mail') }}<input v-model="details.email" dir="ltr" inputmode="email" maxlength="100" autocomplete="email" placeholder="info@example.ir" /></label>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy || !changed">{{ t('ثبت', 'Save') }}</button>
      </div>
    </form>
  </div>
</template>

<style scoped src="./organization.css"></style>
<style scoped>
fieldset { grid-template-columns: repeat(2, minmax(13rem, 22rem)); }
.tr-logo { display: flex; flex-direction: column; gap: .4rem; font-size: .85rem; }
.tr-logo-box { width: 16rem; max-width: 100%; height: 9rem; display: flex; align-items: center; justify-content: center; background: #f5f7fa; border: 1px dashed #cbd2d9; border-radius: 8px; overflow: hidden; }
.tr-logo-box img { max-width: 100%; max-height: 100%; object-fit: contain; }
.tr-logo-actions { display: flex; align-items: center; gap: .75rem; }
.tr-upload { flex-direction: row; cursor: pointer; color: #1d4ed8; font-weight: 600; }
.tr-upload input { display: none; }
</style>
