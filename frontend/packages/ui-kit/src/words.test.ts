import { describe, expect, it } from 'vitest'
import { amountInWords } from './index'

describe('amount in Persian words (مبلغ به حروف)', () => {
  it('writes groups of three digits joined with و', () => {
    expect(amountInWords(12_500_000)).toBe('دوازده میلیون و پانصد هزار')
    expect(amountInWords(1_000)).toBe('یک هزار')
    expect(amountInWords(105)).toBe('یکصد و پنج')
    expect(amountInWords(219)).toBe('دویست و نوزده')
    expect(amountInWords(7_040_000_013)).toBe('هفت میلیارد و چهل میلیون و سیزده')
    expect(amountInWords(2_000_000_000_000)).toBe('دو هزار میلیارد')
    expect(amountInWords(0)).toBe('صفر')
    expect(amountInWords(1_500.75)).toBe('یک هزار و پانصد')
  })
})

import { groupAmount, parseAmount } from './index'

describe('amounts typed with separators (report no. 4, item 10)', () => {
  it('groups by three and keeps decimals and Persian digits readable', () => {
    expect(groupAmount('12500000')).toBe('12,500,000')
    expect(groupAmount('۱۲۵۰۰۰۰')).toBe('1,250,000')
    expect(groupAmount('1,2345.678')).toBe('12,345.67')
    expect(groupAmount('1500.', 2)).toBe('1,500.')
    expect(groupAmount('1500.5', 0)).toBe('1,500')
    expect(groupAmount('-12500', 0, true)).toBe('-12,500')
    expect(groupAmount('-12500', 0)).toBe('12,500')
    expect(groupAmount('00012')).toBe('12')
    expect(groupAmount('\u200e\u2212۱۲٬۵۰۰', 0, true)).toBe('-12,500')
    expect(parseAmount(groupAmount('12500000'))).toBe(12_500_000)
  })
})

import { amountTextInWords } from './index'

describe('words under an amount box (report no. 6, items 1 and 5)', () => {
  it('reads the text as typed, with separators and Persian digits', () => {
    expect(amountTextInWords('12,500,000', 'ریال')).toBe('دوازده میلیون و پانصد هزار ریال')
    expect(amountTextInWords('۱۲٬۵۰۰')).toBe('دوازده هزار و پانصد')
    expect(amountTextInWords('0')).toBe('صفر')
  })
  it('gives nothing for an empty or unreadable box', () => {
    expect(amountTextInWords('')).toBe('')
    expect(amountTextInWords('-')).toBe('')
    expect(amountTextInWords('abc', 'ریال')).toBe('')
  })
  it('reads decimals digit by digit after ممیز and a minus as منفی', () => {
    expect(amountTextInWords('1,250.05', 'دلار')).toBe('یک هزار و دویست و پنجاه ممیز صفر پنج دلار')
    expect(amountTextInWords('42.50')).toBe('چهل و دو ممیز پنج')
    expect(amountTextInWords('12.')).toBe('دوازده')
    expect(amountTextInWords('-3,000', 'ریال')).toBe('منفی سه هزار ریال')
  })
})
