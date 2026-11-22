import { describe, expect, it, vi } from 'vitest'
import { createApi, formatAmount, hasTranslation, logoFileProblem, normaliseJalali, parseAmount, translate } from './index'

describe('formatting', () => {
  it('accepts Persian digits and separators in amounts', () => {
    expect(parseAmount('۱۲٬۵۰۰٬۰۰۰')).toBe(12_500_000)
    expect(parseAmount('12,500,000')).toBe(12_500_000)
    expect(parseAmount('12a')).toBeNull()
  })
  it('formats amounts for each language', () => {
    expect(formatAmount(12500000, 'en')).toBe('12,500,000')
    expect(formatAmount(12500000, 'fa')).toBe('۱۲٬۵۰۰٬۰۰۰')
  })
  it('normalises Jalali dates typed in different ways', () => {
    expect(normaliseJalali('۱۴۰۵/۸/۱۸')).toBe('1405/08/18')
    expect(normaliseJalali('14050818')).toBe('1405/08/18')
    expect(normaliseJalali('1405/07/31')).toBeNull()
    expect(normaliseJalali('1405/13/01')).toBeNull()
  })
})

describe('translations', () => {
  it('translates API error codes in both languages', () => {
    expect(translate('ChequeBook.SheetAlreadyUsed', 'fa')).toBe('شماره چک تکراری است.')
    expect(translate('No.Such.Code', 'en')).toBe('An unexpected error occurred.')
    expect(hasTranslation('Instrument.InvalidTransition')).toBe(true)
  })
})

describe('api client', () => {
  it('sends an idempotency key on writes and returns the error code of a problem response', async () => {
    const fetchImpl = vi.fn(async (_url: string, init?: RequestInit) => {
      expect((init!.headers as Record<string, string>)['Idempotency-Key']).toMatch(/^[0-9a-f-]{36}$/)
      return new Response(JSON.stringify({ code: 'Instrument.InvalidTransition' }), { status: 422 })
    })
    const api = createApi({ fetchImpl: fetchImpl as unknown as typeof fetch })
    const result = await api.post('/api/x', {})
    expect(result).toEqual({ ok: false, code: 'Instrument.InvalidTransition', status: 422 })
  })
  it('reads a bare number as the value (the تفصیل count, owner report no. 2)', async () => {
    const api = createApi({ fetchImpl: (async () => new Response('250', { status: 200 })) as unknown as typeof fetch })
    expect(await api.get<number>('/x')).toEqual({ ok: true, value: 250, warnings: [] })
  })

  it('reports network failures as a code, not an exception', async () => {
    const api = createApi({ fetchImpl: (async () => { throw new TypeError('offline') }) as unknown as typeof fetch })
    expect(await api.get('/api/x')).toEqual({ ok: false, code: 'Network.Error', status: 0 })
  })

  it('sends an idempotency key on updates too', async () => {
    let seen: RequestInit | undefined
    const fetchImpl = async (_: string, init: RequestInit) => { seen = init; return new Response(JSON.stringify({ value: 'x' }), { status: 201 }) }
    const api = createApi({ fetchImpl: fetchImpl as unknown as typeof fetch })
    const r = await api.put('/api/reference/banks/1', { name: 'ملت', version: 1 })
    expect(r.ok).toBe(true)
    expect(seen?.method).toBe('PUT')
    expect((seen?.headers as Record<string, string>)['Idempotency-Key']).toBeTruthy()
  })

  it('uploads a file as form data with an idempotency key and no JSON content type', async () => {
    let seen: RequestInit | undefined
    const fetchImpl = async (_: string, init: RequestInit) => { seen = init; return new Response(JSON.stringify({ value: { added: 3 } }), { status: 200 }) }
    const api = createApi({ fetchImpl: fetchImpl as unknown as typeof fetch })
    const r = await api.upload<{ added: number }>('/api/reference/ledger/chart/import?preview=true', new Blob(['x']), 'chart.xls')
    expect(r.ok && r.value.added).toBe(3)
    const h = seen?.headers as Record<string, string>
    expect(seen?.body).toBeInstanceOf(FormData)
    expect(h['Idempotency-Key']).toBeTruthy()
    expect(h['Content-Type']).toBeUndefined()
  })
})

describe('company logo', () => {
  // Owner, 16 Nov 2026: the logo scanned by the user is a PNG or JPEG picture; the domain allows at most 1 MB (CompanyProfile).
  it('accepts a PNG or JPEG picture of at most 1 MB', () => {
    expect(logoFileProblem({ size: 20_000, type: 'image/png' })).toBeNull()
    expect(logoFileProblem({ size: 1024 * 1024, type: 'image/jpeg' })).toBeNull()
  })
  it('names the problem of any other file with an error code that has a text', () => {
    expect(logoFileProblem({ size: 20_000, type: 'application/pdf' })).toBe('Company.LogoInvalid')
    expect(logoFileProblem({ size: 0, type: 'image/png' })).toBe('Company.LogoInvalid')
    expect(logoFileProblem({ size: 1024 * 1024 + 1, type: 'image/png' })).toBe('Company.LogoTooLarge')
    expect(hasTranslation('Company.LogoInvalid') && hasTranslation('Company.LogoTooLarge')).toBe(true)
  })
})

import { receiptNumber } from './index'

describe('the receipt number box of the receipts lists (report no. 6, item 9)', () => {
  it('reads Persian digits and spaces, and tells an empty box from text that is no number', () => {
    expect(receiptNumber('۱۲')).toBe(12)
    expect(receiptNumber(' 0012 ')).toBe(12)
    expect(receiptNumber('')).toBeNull()
    expect(receiptNumber('۱۲الف')).toBeNaN()
  })
})

import { cardCheckDigitFits, cardNumberHint, groupSheba, shebaCheckDigitsFit, shebaDigits } from './index'

describe('Sheba and card numbers (report no. 6, items 16 and 17)', () => {
  it('reads the digits of a Sheba typed or pasted with IR, spaces or Persian digits', () => {
    expect(shebaDigits('IR06 2960 0000 0010 0324 2000 01')).toBe('062960000000100324200001')
    expect(shebaDigits(' ir۰۶۲۹۶۰۰۰۰۰۰۰۱۰۰۳۲۴۲۰۰۰۰۱')).toBe('062960000000100324200001')
    expect(shebaDigits('0629-6000')).toBe('06296000')
    expect(shebaDigits('0629600000001003242000019999')).toHaveLength(24)
    expect(shebaDigits(null)).toBe('')
  })
  it('groups the digits as banks print them after IR', () => {
    expect(groupSheba('062960000000100324200001')).toBe('06 2960 0000 0010 0324 2000 01')
    expect(groupSheba('0629')).toBe('06 29')
    expect(groupSheba('')).toBe('')
  })
  it('tells a complete number with wrong check digits from a right one', () => {
    expect(shebaCheckDigitsFit('062960000000100324200001')).toBe(true)
    expect(shebaCheckDigitsFit('062960000000100324200002')).toBe(false)
    expect(shebaCheckDigitsFit('123456789012345678901234')).toBe(false)
    expect(shebaCheckDigitsFit('0629600000001003242')).toBe(false)
    expect(cardCheckDigitFits('6037991234567893')).toBe(true)
    expect(cardCheckDigitFits('6037991234567890')).toBe(false)
    expect(cardCheckDigitFits('603799123456789')).toBe(false)
  })
  it('says under a card box whether the length or the check digit is wrong', () => {
    expect(cardNumberHint('')).toBeNull()
    expect(cardNumberHint('6037-9912', 'en')).toEqual({ bad: true, text: '8 of 16 digits' })
    expect(cardNumberHint('۶۰۳۷-۹۹۱۲-۳۴۵۶-۷۸۹۳')?.bad).toBe(false)
    expect(cardNumberHint('6037991234567890', 'en')?.text).toContain('check digit does not fit')
  })
})
