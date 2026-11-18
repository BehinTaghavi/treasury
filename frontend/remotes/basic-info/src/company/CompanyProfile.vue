<script setup lang="ts">
/**
 * «مشخصات شرکت» (owner, 18 Nov 2026): the company's name, its address, and its legal address («آدرس قانونی») with the city in a field
 * of its own and the postal code. The receipt screen ticks «خارج از مرکز» on a cheque whose bank is in another city than this city.
 * One record per client database, kept by reference-svc.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, translate, type Locale } from '@treasury/ui-kit'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'

interface Entry { id: string; name: string; isActive: boolean; parentId: string | null }
interface Profile { isSet: boolean; name: string | null; address: string | null; legalAddress: string | null; cityId: string | null; cityName: string | null
  postalCode: string | null; version: number }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)

const provinces = ref<Entry[]>([])
const cities = ref<Entry[]>([])
const form = reactive({ name: '', address: '', legalAddress: '', provinceId: '', cityId: '', postalCode: '' })
const version = ref(0)
const isSet = ref(false)
const busy = ref(false)
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)

const provinceChoices = computed<Choice[]>(() => provinces.value.filter(p => p.isActive).map(p => ({ value: p.id, label: p.name })))
/** Cities of the chosen province (all cities when none is chosen); the saved city stays in the list even if switched off since. */
const cityChoices = computed<Choice[]>(() => cities.value
  .filter(c => (c.isActive || c.id === form.cityId) && (!form.provinceId || c.parentId === form.provinceId))
  .map(c => ({ value: c.id, label: c.name, muted: !c.isActive })))

function onProvince(id: string) {
  form.provinceId = id
  if (form.cityId && cities.value.find(c => c.id === form.cityId)?.parentId !== id && id) form.cityId = ''
}

async function load() {
  const [p, c, v] = await Promise.all([
    api.get<Entry[]>('/api/reference/provinces'),
    api.get<Entry[]>('/api/reference/cities?includeInactive=true'),
    api.get<Profile>('/api/reference/company'),
  ])
  if (p.ok) provinces.value = p.value
  if (c.ok) cities.value = c.value
  if (!v.ok) { message.value = { kind: 'error', text: translate(v.code, props.locale) }; return }
  isSet.value = v.value.isSet
  version.value = v.value.version
  Object.assign(form, {
    name: v.value.name ?? '', address: v.value.address ?? '', legalAddress: v.value.legalAddress ?? '', cityId: v.value.cityId ?? '',
    postalCode: v.value.postalCode ?? '', provinceId: cities.value.find(x => x.id === v.value.cityId)?.parentId ?? '',
  })
}

async function save() {
  if (busy.value) return
  busy.value = true
  const r = await api.put<string>('/api/reference/company', {
    form: { name: form.name, address: form.address || null, legalAddress: form.legalAddress || null, cityId: form.cityId || null, postalCode: form.postalCode || null },
    version: version.value,
  })
  busy.value = false
  if (!r.ok) { message.value = { kind: 'error', text: translate(r.code, props.locale) }; return }
  await load()
  message.value = { kind: 'ok', text: t('مشخصات شرکت ثبت شد.', 'The company\'s particulars were saved.') }
}

onMounted(load)
</script>

<template>
  <form class="tr-company" @submit.prevent="save">
    <h3>{{ t('مشخصات شرکت', 'Company particulars') }}</h3>
    <p class="muted">{{ t('نام و نشانی شرکت. «شهر» آدرس قانونی، مرکز شرکت به حساب می‌آید: در دریافت چک، اگر شهر بانک چک با این شهر یکی نباشد، «خارج از مرکز» خودکار تیک می‌خورد.',
      'The company\'s name and addresses. The city of the legal address is the company\'s centre: a received cheque whose bank is in another city is ticked "out of town" automatically.') }}</p>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>
    <p v-if="!isSet" class="tr-msg warn">{{ t('مشخصات شرکت هنوز ثبت نشده است.', 'The company\'s particulars are not entered yet.') }}</p>

    <label class="req">{{ t('نام شرکت', 'Company name') }} <input v-model="form.name" maxlength="200" /></label>
    <label>{{ t('آدرس', 'Address') }} <textarea v-model="form.address" rows="2" maxlength="300" /></label>

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
      <label>{{ t('کد پستی (۱۰ رقم)', 'Postal code (10 digits)') }} <input v-model="form.postalCode" dir="ltr" inputmode="numeric" maxlength="11" /></label>
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
input, textarea { padding: .45rem .6rem; border: 1px solid #cbd2d9; border-radius: 6px; font: inherit; }
label.req input { border-color: #d92d20; }
fieldset { border: 1px solid #e4e7eb; border-radius: 8px; padding: .75rem; display: grid; grid-template-columns: 1fr 1fr 1fr; gap: .75rem; }
fieldset .full { grid-column: 1 / -1; }
legend { font-weight: 600; padding: 0 .4rem; }
.tr-msg { padding: .5rem .75rem; border-radius: 6px; margin: 0; }
.tr-msg.error { background: #fdecea; color: #8a1c1c; }
.tr-msg.ok { background: #e6f4ea; color: #1e5631; }
.tr-msg.warn { background: #fff4e5; color: #8a5300; }
button { justify-self: start; padding: .45rem 1.2rem; border-radius: 6px; border: 1px solid #1d4ed8; background: #2563eb; color: #fff; font: inherit; cursor: pointer; }
button:disabled { opacity: .5; cursor: default; }
@media (max-width: 720px) { fieldset { grid-template-columns: 1fr; } }
</style>
