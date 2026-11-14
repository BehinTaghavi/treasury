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
