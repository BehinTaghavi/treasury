<script setup lang="ts">
/**
 * «مشخصات شرکت»: one record per client database, kept by reference-svc. Two requests of the owner on one page:
 * - 16 Nov 2026: the company's name and its logo (آرم), scanned by the user. Both are shown at the top of every screen by the shell,
 *   which is told about a change with COMPANY_CHANGED_EVENT. The official details (شناسه ملی, کد اقتصادی, شماره ثبت, تلفن, آدرس سایت,
 *   ایمیل) are optional and go on printed forms with the name and logo.
 * - 18 Nov 2026: the company's address, and its legal address («آدرس قانونی») with the city in a field of its own and the postal
 *   code. The receipt screen ticks «خارج از مرکز» on a cheque whose bank is in another city than this city.
 */
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import {
  COMPANY_CHANGED_EVENT, COMPANY_DETAIL_FIELDS, COMPANY_LOGO_PATH, COMPANY_PATH, createApi, logoFileProblem, translate,
  type CompanyDetails, type CompanyProfile, type Locale,
} from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'

interface Entry { id: string; name: string; isActive: boolean; parentId: string | null }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)

const provinces = ref<Entry[]>([])
const cities = ref<Entry[]>([])
const form = reactive({ name: '', address: '', legalAddress: '', provinceId: '', cityId: '', postalCode: '' })
/** The official details as typed; an empty box is "not entered". */
const details = reactive<Record<keyof CompanyDetails, string>>({ nationalId: '', economicCode: '', registrationNumber: '', phone: '', website: '', email: '' })
/** What is stored, to tell whether the form was changed. */
const stored = ref<CompanyProfile | null>(null)
const version = ref(0)
const isSet = ref(false)
const hasLogo = ref(false)
/** The logo as stored; shown until another file is chosen. */
const storedLogo = ref('')
/** A file chosen but not saved yet, with its preview. */
const chosen = ref<File | null>(null)
const chosenPreview = ref('')
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)

const provinceChoices = computed<Choice[]>(() => provinces.value.filter(p => p.isActive).map(p => ({ value: p.id, label: p.name })))
/** Cities of the chosen province (all cities when none is chosen); the saved city stays in the list even if switched off since. */
const cityChoices = computed<Choice[]>(() => cities.value
  .filter(c => (c.isActive || c.id === form.cityId) && (!form.provinceId || c.parentId === form.provinceId))
  .map(c => ({ value: c.id, label: c.name, muted: !c.isActive })))
const preview = computed(() => chosenPreview.value || storedLogo.value)
const formChanged = computed(() => {
  const s = stored.value
  if (!s || !s.isSet) return true
  return form.name.trim() !== (s.name ?? '') || form.address.trim() !== (s.address ?? '') || form.legalAddress.trim() !== (s.legalAddress ?? '')
    || form.cityId !== (s.cityId ?? '') || form.postalCode.trim() !== (s.postalCode ?? '')
    || COMPANY_DETAIL_FIELDS.some(f => details[f].trim() !== (s[f] ?? ''))
})
const fail = (code: string) => { message.value = { kind: 'error', text: translate(code, props.locale) } }

function onProvince(id: string) {
  form.provinceId = id
  if (form.cityId && cities.value.find(c => c.id === form.cityId)?.parentId !== id && id) form.cityId = ''
}

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
  if (!hasLogo.value) return
  try {
    const res = await fetch(`${props.apiBase}${COMPANY_LOGO_PATH}`, { headers: headers(), cache: 'no-store' })
    if (res.ok) storedLogo.value = URL.createObjectURL(await res.blob())
  } catch { /* the name is still shown */ }
}

/** Reads what is stored. With `keepTyped` the boxes keep what the user typed but has not saved yet. */
async function loadProfile(keepTyped = false): Promise<boolean> {
  const v = await api.get<CompanyProfile>(COMPANY_PATH)
  if (!v.ok) { fail(v.code); return false }
  stored.value = v.value
  isSet.value = v.value.isSet
  version.value = v.value.version
  hasLogo.value = v.value.hasLogo
  if (!keepTyped) {
    Object.assign(form, {
      name: v.value.name ?? '', address: v.value.address ?? '', legalAddress: v.value.legalAddress ?? '', cityId: v.value.cityId ?? '',
      postalCode: v.value.postalCode ?? '', provinceId: cities.value.find(x => x.id === v.value.cityId)?.parentId ?? '',
    })
    for (const f of COMPANY_DETAIL_FIELDS) details[f] = v.value[f] ?? ''
  }
  await loadLogo()
  return true
}

async function load() {
  const [p, c] = await Promise.all([
    api.get<Entry[]>('/api/reference/provinces'),
    api.get<Entry[]>('/api/reference/cities?includeInactive=true'),
  ])
  if (p.ok) provinces.value = p.value
  if (c.ok) cities.value = c.value
  await loadProfile()
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
  if (busy.value) return
  busy.value = true
  try {
    // The form first (a logo belongs to an entered company), then the logo with the profile's new version.
    if (formChanged.value || !chosen.value) {
      const r = await api.put<string>(COMPANY_PATH, {
        form: {
          name: form.name, address: form.address || null, legalAddress: form.legalAddress || null, cityId: form.cityId || null,
          postalCode: form.postalCode || null, ...Object.fromEntries(COMPANY_DETAIL_FIELDS.map(f => [f, details[f].trim() || null])),
        },
        version: version.value,
      })
      if (!r.ok) return fail(r.code)
      if (!(await loadProfile())) return
    }
    if (chosen.value) {
      const body = new FormData()
      body.append('file', chosen.value)
      body.append('version', String(version.value))
      const res = await fetch(`${props.apiBase}${COMPANY_LOGO_PATH}`, { method: 'POST', headers: headers(), body }).catch(() => null)
      if (!res) return fail('Network.Error')
      if (!res.ok) {
        const problem = await res.json().catch(() => ({}))
        return fail((problem as { code?: string }).code ?? 'Unknown')
      }
      dropChosen()
      if (!(await loadProfile())) return
    }
    message.value = { kind: 'ok', text: t('مشخصات شرکت ثبت شد؛ نام و آرم در بالای همهٔ صفحه‌ها نمایش داده می‌شود.',
      'The company\'s particulars were saved; the name and logo are shown at the top of every screen.') }
  } finally {
    busy.value = false
    // Also after a half-finished save (form saved, logo refused): the bar shows what is stored.
    window.dispatchEvent(new CustomEvent(COMPANY_CHANGED_EVENT))
  }
}

async function removeLogo() {
  if (chosen.value) return dropChosen()
  if (busy.value || !hasLogo.value) return
  if (!window.confirm(t('آرم شرکت حذف شود؟', "Remove the company's logo?"))) return
  busy.value = true
  const r = await api.post<string>(`${COMPANY_LOGO_PATH}/remove`, { version: version.value })
  busy.value = false
  if (!r.ok) return fail(r.code)
  // What is typed but not saved yet stays in the boxes.
  await loadProfile(true)
  message.value = { kind: 'ok', text: t('آرم شرکت حذف شد.', 'Logo removed.') }
  window.dispatchEvent(new CustomEvent(COMPANY_CHANGED_EVENT))
}

onMounted(load)
onBeforeUnmount(() => { forget(storedLogo.value); forget(chosenPreview.value) })
</script>

<template>
  <form class="tr-company" @submit.prevent="save">
    <h3>{{ t('مشخصات شرکت', 'Company particulars') }}</h3>
    <p class="muted">{{ t('نام و آرم شرکت در بالای همهٔ صفحه‌های سیستم نمایش داده می‌شود و همراه با مشخصات رسمی در بالای فرم‌های چاپی می‌آید. «شهر» آدرس قانونی، مرکز شرکت به حساب می‌آید: در دریافت چک، اگر شهر بانک چک با این شهر یکی نباشد، «خارج از مرکز» خودکار تیک می‌خورد.',
      'The company\'s name and logo are shown at the top of every screen and, with the official details, at the top of printed forms. The city of the legal address is the company\'s centre: a received cheque whose bank is in another city is ticked "out of town" automatically.') }}</p>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
    <p v-if="!isSet" class="tr-msg warn">{{ t('مشخصات شرکت هنوز ثبت نشده است.', 'The company\'s particulars are not entered yet.') }}</p>

    <label class="req">{{ t('نام شرکت', 'Company name') }} <input v-model="form.name" maxlength="100" autocomplete="organization" /></label>

    <div class="tr-logo">
      <span class="tr-logo-title">{{ t('آرم شرکت', 'Company logo') }}</span>
      <div class="tr-logo-box">
        <img v-if="preview" :src="preview" :alt="t('آرم شرکت', 'Company logo')" />
        <span v-else class="muted">{{ t('هنوز آرمی بارگذاری نشده است.', 'No logo yet.') }}</span>
      </div>
      <div class="tr-logo-actions">
        <label class="tr-upload">{{ preview ? t('تعویض آرم', 'Replace logo') : t('انتخاب فایل آرم', 'Choose the logo file') }}
          <input type="file" accept="image/png,image/jpeg" @change="choose" />
        </label>
        <button v-if="chosen || hasLogo" type="button" class="tr-link danger" :disabled="busy" @click="removeLogo">
          {{ chosen ? t('انصراف از این فایل', 'Discard this file') : t('حذف آرم', 'Remove logo') }}
        </button>
      </div>
      <small class="muted">{{ t('فایل اسکن شدهٔ آرم: PNG یا JPEG، حداکثر ۱ مگابایت.', 'The scanned logo: PNG or JPEG, at most 1 MB.') }}</small>
      <small v-if="chosen" class="tr-warn">{{ t('این فایل هنوز ثبت نشده است؛ «ثبت» را بزنید.', 'This file is not saved yet; press Save.') }}</small>
    </div>

    <label>{{ t('آدرس', 'Address') }} <textarea v-model="form.address" rows="2" maxlength="300" autocomplete="street-address" /></label>

    <fieldset>
      <legend>{{ t('آدرس قانونی شرکت', 'Legal address') }}</legend>
      <label class="full">{{ t('آدرس قانونی', 'Legal address') }} <textarea v-model="form.legalAddress" rows="2" maxlength="300" /></label>
      <label>{{ t('استان', 'Province') }}
        <SearchSelect :model-value="form.provinceId" :options="provinceChoices" :empty-label="t('— همه —', '— all —')" :aria-label="t('استان', 'Province')"
                      @update:model-value="onProvince" />
      </label>
      <label>{{ t('شهر', 'City') }}
        <SearchSelect v-model="form.cityId" :options="cityChoices" required :aria-label="t('شهر', 'City')"
                      :none-text="t('شهری پیدا نشد (اطلاعات پایه ← شهر)', 'No city found (Basic information → Cities)')" />
      </label>
      <label>{{ t('کد پستی (۱۰ رقم)', 'Postal code (10 digits)') }} <input v-model="form.postalCode" dir="ltr" inputmode="numeric" maxlength="11" autocomplete="postal-code" /></label>
    </fieldset>

    <fieldset>
      <legend>{{ t('مشخصات رسمی (اختیاری)', 'Official details (optional)') }}</legend>
      <label>{{ t('شناسه ملی (۱۱ رقم)', 'National ID (11 digits)') }} <input v-model="details.nationalId" dir="ltr" inputmode="numeric" maxlength="11" autocomplete="off" /></label>
      <label>{{ t('کد اقتصادی (۱۱ یا ۱۲ رقم)', 'Economic code (11 or 12 digits)') }} <input v-model="details.economicCode" dir="ltr" inputmode="numeric" maxlength="14" autocomplete="off" /></label>
      <label>{{ t('شماره ثبت', 'Registration number') }} <input v-model="details.registrationNumber" dir="ltr" inputmode="numeric" maxlength="10" autocomplete="off" /></label>
      <label>{{ t('تلفن', 'Telephone') }} <input v-model="details.phone" dir="ltr" inputmode="tel" maxlength="24" autocomplete="tel" /></label>
      <label>{{ t('آدرس سایت', 'Website') }} <input v-model="details.website" dir="ltr" inputmode="url" maxlength="100" autocomplete="url" placeholder="www.example.ir" /></label>
      <label>{{ t('ایمیل', 'E-mail') }} <input v-model="details.email" dir="ltr" inputmode="email" maxlength="100" autocomplete="email" placeholder="info@example.ir" /></label>
    </fieldset>

    <button type="submit" :disabled="busy">{{ t('ثبت', 'Save') }}</button>
  </form>
</template>

<style scoped>
.tr-company { display: grid; gap: .75rem; max-width: 46rem; }
h3 { margin: 0; }
.muted { color: #7b8794; margin: 0; }
label { display: grid; gap: .25rem; font-size: .9rem; }
label.req::after { content: none; }
input, textarea { padding: .45rem .6rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; min-width: 0; }
label.req input { border-color: #d92d20; }
fieldset { border: 1px solid #e4e7eb; border-radius: 8px; padding: .75rem; display: grid; grid-template-columns: 1fr 1fr 1fr; gap: .75rem; min-width: 0; }
fieldset .full { grid-column: 1 / -1; }
legend { font-weight: 600; padding: 0 .4rem; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; margin: 0; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-msg.warn { background: #fff4e5; color: #8a5300; }
button[type=submit] { justify-self: start; padding: .45rem 1.2rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .5; cursor: default; }
.tr-logo { display: flex; flex-direction: column; gap: .4rem; font-size: .9rem; }
.tr-logo-box { width: 16rem; max-width: 100%; height: 9rem; display: flex; align-items: center; justify-content: center; background: #f5f7fa; border: 1px dashed #cbd2d9; border-radius: 8px; overflow: hidden; }
.tr-logo-box img { max-width: 100%; max-height: 100%; object-fit: contain; }
.tr-logo-actions { display: flex; flex-wrap: wrap; align-items: center; gap: .75rem; }
.tr-upload { display: inline-flex; cursor: pointer; color: #1d4ed8; font-weight: 600; }
.tr-upload input { display: none; }
.tr-link { background: transparent; color: #1d4ed8; border: 0; padding: .2rem .4rem; font: inherit; cursor: pointer; }
.tr-link.danger { color: #b42318; }
.tr-warn { color: #b42318; }
/* Phones (ADR 0006): one column. */
@media (max-width: 720px) { fieldset { grid-template-columns: 1fr; } }
</style>
