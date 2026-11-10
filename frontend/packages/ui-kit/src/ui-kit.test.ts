import { describe, expect, it, vi } from 'vitest'
import { createApi, formatAmount, hasTranslation, normaliseJalali, parseAmount, translate } from './index'

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
  it('reports network failures as a code, not an exception', async () => {
    const api = createApi({ fetchImpl: (async () => { throw new TypeError('offline') }) as unknown as typeof fetch })
    expect(await api.get('/api/x')).toEqual({ ok: false, code: 'Network.Error', status: 0 })
  })
})
