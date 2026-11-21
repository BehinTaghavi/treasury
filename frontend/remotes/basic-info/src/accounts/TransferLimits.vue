<script setup lang="ts">
/**
 * «سقف‌های انتقال الکترونیکی» (owner's report no. 7 of 20 Nov 2026, «نقل و انتقال بین بانکی»): the amount limits of the four electronic
 * ways of moving money between the company's own bank accounts — انتقال داخلی (حساب به حساب)، پل، پایا، ساتنا. The owner: «لازم است
 * فیلد محدودیت مبالغ را قابل کانفیگ نگهداشت و در کد ثابت نباشد» — so the figures are the company's data, typed here and changed
 * whenever the banks or the Central Bank change them. A company starts with the figures of that report: Pol 3 milliard Rial a day at
 * the branch and 2 milliard remotely, at most 5 orders a day; Paya 5 milliard an order at the branch and 3 milliard remotely, at most
 * 5 orders a day; Satna at least 2 milliard an order; the internal transfer none («هر بانکی ضوابط خود را دارد»). An empty box means
 * «بدون سقف». A bank whose rules differ gets limits of its own («سقف ویژهٔ یک بانک»), which then replace the general ones for transfers
 * out of its accounts. Saved by cashflow-svc, which checks every transfer against them.
 */
import { computed, onMounted, reactive, ref } from 'vue'
import { createApi, formatAmount, parseAmount, toLatinDigits, translate, type Locale } from '@treasury/ui-kit'
import AmountInput from '@treasury/ui-kit/AmountInput.vue'
import SearchSelect, { type Choice } from '@treasury/ui-kit/SearchSelect.vue'

type Method = 'Internal' | 'Pol' | 'Paya' | 'Satna'
interface Limit {
  method: Method; bankId: string | null; minPerOrder: number | null; maxPerOrderInPerson: number | null; maxPerOrderRemote: number | null
  maxPerDayInPerson: number | null; maxPerDayRemote: number | null; maxCountPerDay: number | null
}
/** The six boxes of a row, as typed. */
interface Boxes { min: string; orderIn: string; orderRemote: string; dayIn: string; dayRemote: string; count: string }
interface Bank { id: string; name: string; isActive?: boolean }

const props = withDefaults(defineProps<{ apiBase?: string; locale?: Locale; token?: () => string | undefined }>(), { apiBase: '', locale: 'fa', token: undefined })
const api = createApi({ baseUrl: props.apiBase, token: props.token })
const t = (f: string, e: string) => (props.locale === 'fa' ? f : e)
const fa = computed(() => props.locale === 'fa')
const money = (n: number | null) => (n === null ? t('بدون سقف', 'no limit') : formatAmount(n, props.locale))

const methods: { id: Method; title: [string, string]; use: [string, string] }[] = [
  { id: 'Internal', title: ['انتقال داخلی (حساب به حساب)', 'Internal (account to account)'], use: ['دو حساب شرکت در یک بانک', 'two accounts in the same bank'] },
  { id: 'Pol', title: ['پل', 'Pol'], use: ['دو بانک متفاوت – مبالغ کم و فوری', 'two banks – small, urgent amounts'] },
  { id: 'Paya', title: ['پایا', 'Paya'], use: ['دو بانک متفاوت – مبالغ متوسط', 'two banks – middle amounts'] },
  { id: 'Satna', title: ['ساتنا', 'Satna'], use: ['دو بانک متفاوت – مبالغ بالا', 'two banks – large amounts'] },
]
const methodTitle = (m: string) => { const x = methods.find(y => y.id === m); return x ? (fa.value ? x.title[0] : x.title[1]) : m }

const limits = ref<Limit[]>([])
const banks = ref<Bank[]>([])
const bankName = (id: string | null) => banks.value.find(b => b.id === id)?.name ?? ''
const blank = (): Boxes => ({ min: '', orderIn: '', orderRemote: '', dayIn: '', dayRemote: '', count: '' })
const text = (n: number | null) => (n === null ? '' : String(n))
const boxesOf = (l: Limit | undefined): Boxes => (l
  ? { min: text(l.minPerOrder), orderIn: text(l.maxPerOrderInPerson), orderRemote: text(l.maxPerOrderRemote), dayIn: text(l.maxPerDayInPerson), dayRemote: text(l.maxPerDayRemote), count: text(l.maxCountPerDay) }
  : blank())
/** The general rows as typed, one per way. */
const general = reactive<Record<Method, Boxes>>({ Internal: blank(), Pol: blank(), Paya: blank(), Satna: blank() })
const generalRow = (m: Method) => limits.value.find(l => l.method === m && l.bankId === null)
const bankRows = computed(() => limits.value.filter(l => l.bankId !== null).sort((a, b) => bankName(a.bankId).localeCompare(bankName(b.bankId), 'fa') || a.method.localeCompare(b.method)))

/** The boxes as the server takes them: an empty box is «بدون سقف» (null); unreadable text is refused here. */
function body(b: Boxes, bankId: string | null): Omit<Limit, 'method'> | { error: string } {
  const amount = (s: string) => (s.trim() === '' ? null : parseAmount(s))
  const values = [b.min, b.orderIn, b.orderRemote, b.dayIn, b.dayRemote].map(amount)
  if ([b.min, b.orderIn, b.orderRemote, b.dayIn, b.dayRemote].some((s, i) => s.trim() !== '' && (values[i] === null || values[i]! <= 0))) return { error: 'TransferLimit.AmountInvalid' }
  const countText = toLatinDigits(b.count).trim()
  if (countText !== '' && !/^[1-9]\d*$/.test(countText)) return { error: 'TransferLimit.CountInvalid' }
  return { bankId, minPerOrder: values[0]!, maxPerOrderInPerson: values[1]!, maxPerOrderRemote: values[2]!, maxPerDayInPerson: values[3]!, maxPerDayRemote: values[4]!,
    maxCountPerDay: countText === '' ? null : Number(countText) }
}
const same = (a: Boxes, l: Limit | undefined) => {
  const x = body(a, null)
  const o = boxesOf(l)
  return !('error' in x) && (['min', 'orderIn', 'orderRemote', 'dayIn', 'dayRemote'] as const).every(k => (parseAmount(a[k]) ?? null) === (parseAmount(o[k]) ?? null))
    && toLatinDigits(a.count).trim() === o.count
}

const busy = ref('')
const message = ref<{ kind: 'ok' | 'error'; text: string } | null>(null)
async function load() {
  const [l, b] = await Promise.all([api.get<Limit[]>('/api/cashflow/transfer-limits'), api.get<Bank[]>('/api/reference/banks')])
  if (b.ok) banks.value = b.value
  if (!l.ok) { message.value = { kind: 'error', text: translate(l.code, props.locale) }; return }
  limits.value = l.value
  for (const m of methods) Object.assign(general[m.id], boxesOf(generalRow(m.id)))
}

async function saveGeneral(m: Method) {
  if (busy.value) return
  const b = body(general[m], null)
  if ('error' in b) { message.value = { kind: 'error', text: translate(b.error, props.locale) }; return }
  busy.value = m
  const r = await api.put<string>(`/api/cashflow/transfer-limits/${m}`, b)
  busy.value = ''
  if (!r.ok) { message.value = { kind: 'error', text: `${methodTitle(m)}: ${translate(r.code, props.locale)}` }; return }
  message.value = { kind: 'ok', text: t(`سقف‌های «${methodTitle(m)}» ثبت شد و از همین حالا روی نقل و انتقال‌های تازه اعمال می‌شود.`, `The limits of "${methodTitle(m)}" are saved and apply to new transfers from now on.`) }
  await load()
}

// ---- «سقف ویژهٔ یک بانک»: a bank whose rules differ from the general limits of a way ----
const own = reactive({ bankId: '', method: '' as Method | '', boxes: blank() })
const bankChoices = computed<Choice[]>(() => banks.value.filter(b => b.isActive !== false || b.id === own.bankId).map(b => ({ value: b.id, label: b.name })))
const methodChoices = computed<Choice[]>(() => methods.map(m => ({ value: m.id, label: fa.value ? m.title[0] : m.title[1] })))
const ownExisting = computed(() => limits.value.find(l => l.bankId !== null && l.bankId === own.bankId && l.method === own.method))
function editOwn(l: Limit) {
  message.value = null
  Object.assign(own, { bankId: l.bankId ?? '', method: l.method, boxes: boxesOf(l) })
}
async function saveOwn() {
  if (busy.value) return
  if (!own.bankId) { message.value = { kind: 'error', text: t('بانک را انتخاب کنید.', 'Choose the bank.') }; return }
  if (!own.method) { message.value = { kind: 'error', text: translate('Transfer.MethodRequired', props.locale) }; return }
  const b = body(own.boxes, own.bankId)
  if ('error' in b) { message.value = { kind: 'error', text: translate(b.error, props.locale) }; return }
  busy.value = 'own'
  const r = await api.put<string>(`/api/cashflow/transfer-limits/${own.method}`, b)
  busy.value = ''
  if (!r.ok) { message.value = { kind: 'error', text: translate(r.code, props.locale) }; return }
  message.value = { kind: 'ok', text: t(`سقف ویژهٔ ${bankName(own.bankId)} برای «${methodTitle(own.method)}» ثبت شد.`, `${bankName(own.bankId)}'s own limits for "${methodTitle(own.method)}" are saved.`) }
  Object.assign(own, { bankId: '', method: '', boxes: blank() })
  await load()
}
async function removeOwn(l: Limit) {
  if (busy.value || l.bankId === null) return
  if (!window.confirm(t(`سقف ویژهٔ ${bankName(l.bankId)} برای «${methodTitle(l.method)}» حذف شود؟ از آن پس سقف عمومی این روش برای این بانک اعمال می‌شود.`,
    `Remove ${bankName(l.bankId)}'s own limits for "${methodTitle(l.method)}"? The general limits of the way then apply to this bank.`))) return
  busy.value = 'own'
  const r = await api.post<string>(`/api/cashflow/transfer-limits/${l.method}/banks/${l.bankId}/delete`, {})
  busy.value = ''
  if (!r.ok) { message.value = { kind: 'error', text: translate(r.code, props.locale) }; return }
  message.value = { kind: 'ok', text: t(`سقف ویژهٔ ${bankName(l.bankId)} برای «${methodTitle(l.method)}» حذف شد.`, `${bankName(l.bankId)}'s own limits for "${methodTitle(l.method)}" were removed.`) }
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <h3>{{ t('سقف‌های انتقال الکترونیکی', 'Electronic transfer limits') }}</h3>
    <p class="muted">{{ t('محدودیت مبلغ روش‌های انتقال الکترونیکی بین حساب‌های بانکی شرکت (به ریال). هر نقل و انتقال هنگام ثبت با این ارقام کنترل می‌شود. کادر خالی یعنی «بدون سقف».',
      "The amount limits of the electronic ways of transfer between the company's bank accounts (in Rial). Every transfer is checked against them when it is saved. An empty box means no limit.") }}</p>
    <p class="muted">{{ t('ارقام اولیه همان ارقام گزارش ۲۹ آبان ۱۴۰۵ مالک محصول است؛ هر زمان بانک‌ها یا بانک مرکزی آن‌ها را تغییر دادند، همین‌جا تغییرشان دهید.',
      "The first figures are those of the owner's report of 29 Aban 1405 (20 Nov 2026); change them here whenever the banks or the Central Bank change them.") }}</p>
    <p v-if="message" :class="['tr-msg', message.kind]" role="status">{{ message.text }}</p>

    <div class="tr-scroll">
      <table class="tr-table tl-table">
        <thead>
          <tr>
            <th rowspan="2">{{ t('روش انتقال', 'Way of transfer') }}</th>
            <th rowspan="2">{{ t('حداقل هر دستور', 'Least of one order') }}</th>
            <th colspan="2">{{ t('سقف هر دستور پرداخت', 'Most of one order') }}</th>
            <th colspan="2">{{ t('سقف روزانه (جمع دستورها)', 'Most of a day (all orders)') }}</th>
            <th rowspan="2">{{ t('حداکثر تعداد دستور در روز', 'Most orders a day') }}</th>
            <th rowspan="2" />
          </tr>
          <tr>
            <th>{{ t('حضوری', 'At the branch') }}</th><th>{{ t('غیرحضوری', 'Remote') }}</th>
            <th>{{ t('حضوری', 'At the branch') }}</th><th>{{ t('غیرحضوری', 'Remote') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="m in methods" :key="m.id">
            <td><strong>{{ fa ? m.title[0] : m.title[1] }}</strong><div class="muted small">{{ fa ? m.use[0] : m.use[1] }}</div></td>
            <td><AmountInput v-model="general[m.id].min" :decimals="0" unit="ریال" :placeholder="t('بدون حداقل', 'none')" :aria-label="`${methodTitle(m.id)} – ${t('حداقل هر دستور', 'least of one order')}`" /></td>
            <td><AmountInput v-model="general[m.id].orderIn" :decimals="0" unit="ریال" :placeholder="t('بدون سقف', 'no limit')" :aria-label="`${methodTitle(m.id)} – ${t('سقف هر دستور حضوری', 'most of one order at the branch')}`" /></td>
            <td><AmountInput v-model="general[m.id].orderRemote" :decimals="0" unit="ریال" :placeholder="t('بدون سقف', 'no limit')" :aria-label="`${methodTitle(m.id)} – ${t('سقف هر دستور غیرحضوری', 'most of one remote order')}`" /></td>
            <td><AmountInput v-model="general[m.id].dayIn" :decimals="0" unit="ریال" :placeholder="t('بدون سقف', 'no limit')" :aria-label="`${methodTitle(m.id)} – ${t('سقف روزانهٔ حضوری', 'most of a day at the branch')}`" /></td>
            <td><AmountInput v-model="general[m.id].dayRemote" :decimals="0" unit="ریال" :placeholder="t('بدون سقف', 'no limit')" :aria-label="`${methodTitle(m.id)} – ${t('سقف روزانهٔ غیرحضوری', 'most of a remote day')}`" /></td>
            <td><input v-model="general[m.id].count" class="tl-count" dir="ltr" inputmode="numeric" maxlength="4" :placeholder="t('ندارد', 'none')" :aria-label="`${methodTitle(m.id)} – ${t('حداکثر تعداد دستور در روز', 'most orders a day')}`" /></td>
            <td><button type="button" :disabled="busy !== '' || same(general[m.id], generalRow(m.id))" @click="saveGeneral(m.id)">{{ t('ذخیره', 'Save') }}</button></td>
          </tr>
        </tbody>
      </table>
    </div>
    <p class="muted small">{{ t('سقف روزانه و تعداد دستور برای هر حساب مبدأ (انتقال دهنده) و هر روش، در همان روز حساب می‌شود؛ نقل و انتقال‌های حذف‌شده شمرده نمی‌شوند.',
      'The daily limit and the number of orders are counted per source account and way, on the same day; deleted transfers do not count.') }}</p>

    <!-- A bank whose rules differ («هر بانکی ضوابط خود را دارد»). -->
    <form class="tr-form" novalidate @submit.prevent="saveOwn">
      <fieldset>
        <legend>{{ t('سقف ویژهٔ یک بانک', "A bank's own limits") }}</legend>
        <label>{{ t('بانک (بانک حساب مبدأ)', "Bank (the source account's bank)") }}
          <SearchSelect v-model="own.bankId" :options="bankChoices" required :aria-label="t('بانک', 'Bank')" :none-text="t('بانکی پیدا نشد', 'No bank found')" />
        </label>
        <label>{{ t('روش انتقال', 'Way of transfer') }}
          <SearchSelect v-model="own.method" :options="methodChoices" required :aria-label="t('روش انتقال', 'Way of transfer')" />
        </label>
        <label>{{ t('حداقل هر دستور', 'Least of one order') }} <AmountInput v-model="own.boxes.min" :decimals="0" unit="ریال" :placeholder="t('بدون حداقل', 'none')" /></label>
        <label>{{ t('سقف هر دستور – حضوری', 'Most of one order – at the branch') }} <AmountInput v-model="own.boxes.orderIn" :decimals="0" unit="ریال" :placeholder="t('بدون سقف', 'no limit')" /></label>
        <label>{{ t('سقف هر دستور – غیرحضوری', 'Most of one order – remote') }} <AmountInput v-model="own.boxes.orderRemote" :decimals="0" unit="ریال" :placeholder="t('بدون سقف', 'no limit')" /></label>
        <label>{{ t('سقف روزانه – حضوری', 'Most of a day – at the branch') }} <AmountInput v-model="own.boxes.dayIn" :decimals="0" unit="ریال" :placeholder="t('بدون سقف', 'no limit')" /></label>
        <label>{{ t('سقف روزانه – غیرحضوری', 'Most of a day – remote') }} <AmountInput v-model="own.boxes.dayRemote" :decimals="0" unit="ریال" :placeholder="t('بدون سقف', 'no limit')" /></label>
        <label>{{ t('حداکثر تعداد دستور در روز', 'Most orders a day') }} <input v-model="own.boxes.count" dir="ltr" inputmode="numeric" maxlength="4" :placeholder="t('بدون سقف', 'no limit')" /></label>
        <p class="muted small tl-wide">{{ t('سقف ویژهٔ یک بانک، برای نقل و انتقال از حساب‌های آن بانک، به‌جای سقف عمومیِ همان روش به‌کار می‌رود (همهٔ شش رقم؛ کادر خالی یعنی برای آن بانک سقفی نیست).',
          "A bank's own limits replace the general limits of the way for transfers out of its accounts (all six figures; an empty box means no limit for that bank).") }}
          <template v-if="ownExisting"> {{ t('برای این بانک و این روش سقف ویژه ثبت شده است؛ با ذخیره، ارقام تازه جایگزین می‌شود.', 'This bank already has limits of its own for this way; saving replaces them.') }}</template></p>
      </fieldset>
      <div class="tr-buttons">
        <button type="submit" :disabled="busy !== ''">{{ ownExisting ? t('ذخیرهٔ تغییر سقف ویژه', 'Save the change') : t('ثبت سقف ویژه', "Save the bank's limits") }}</button>
        <button v-if="own.bankId || own.method" type="button" class="tr-link" @click="Object.assign(own, { bankId: '', method: '', boxes: blank() })">{{ t('انصراف', 'Cancel') }}</button>
      </div>
    </form>

    <table v-if="bankRows.length" class="tr-table">
      <thead>
        <tr>
          <th>{{ t('بانک', 'Bank') }}</th><th>{{ t('روش انتقال', 'Way') }}</th><th>{{ t('حداقل هر دستور', 'Least of one order') }}</th>
          <th>{{ t('هر دستور – حضوری', 'One order – branch') }}</th><th>{{ t('هر دستور – غیرحضوری', 'One order – remote') }}</th>
          <th>{{ t('روزانه – حضوری', 'A day – branch') }}</th><th>{{ t('روزانه – غیرحضوری', 'A day – remote') }}</th><th>{{ t('تعداد در روز', 'Orders a day') }}</th><th />
        </tr>
      </thead>
      <tbody>
        <tr v-for="l in bankRows" :key="l.method + l.bankId">
          <td>{{ bankName(l.bankId) }}</td>
          <td>{{ methodTitle(l.method) }}</td>
          <td class="num">{{ l.minPerOrder === null ? t('بدون حداقل', 'none') : money(l.minPerOrder) }}</td>
          <td class="num">{{ money(l.maxPerOrderInPerson) }}</td>
          <td class="num">{{ money(l.maxPerOrderRemote) }}</td>
          <td class="num">{{ money(l.maxPerDayInPerson) }}</td>
          <td class="num">{{ money(l.maxPerDayRemote) }}</td>
          <td class="num">{{ money(l.maxCountPerDay) }}</td>
          <td class="tr-actions">
            <button type="button" class="tr-link" @click="editOwn(l)">{{ t('ویرایش', 'Edit') }}</button>
            <button type="button" class="tr-link danger" :disabled="busy !== ''" @click="removeOwn(l)">{{ t('حذف', 'Remove') }}</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else class="muted small">{{ t('هنوز برای هیچ بانکی سقف ویژه‌ای ثبت نشده است؛ سقف‌های عمومی بالا برای همهٔ بانک‌ها اعمال می‌شود.', 'No bank has limits of its own yet; the general limits above apply to every bank.') }}</p>
  </div>
</template>

<style scoped src="../organization/organization.css"></style>
<style scoped>
.tr-scroll { overflow-x: auto; }
/* Seven boxes on a line: the boxes are kept narrow so the row and its «ذخیره» fit the screen. */
.tl-table { font-size: .9rem; }
.tl-table th { background: #f3f5f9; font-size: .82rem; font-weight: 600; text-align: center; vertical-align: middle; padding: .35rem .3rem; }
.tl-table td { vertical-align: top; padding: .45rem .3rem; }
.tl-table td:first-child { min-width: 8.5rem; }
.tl-table :deep(.tr-amount) { width: 7.6rem; min-width: 0; }
.tl-table :deep(.tr-amount > input), .tl-count { font-size: .9rem; padding: .4rem .45rem; }
.tl-count { width: 4.6rem; text-align: center; }
.tl-table td:last-child button { padding: .4rem .7rem; }
.tl-wide { grid-column: 1 / -1; margin: 0; }
.num { font-variant-numeric: tabular-nums; white-space: nowrap; }
</style>
