import { describe, expect, it } from 'vitest'
import { lineIncomplete, lineTiming, rialOf, summariseLines, type ChequeLineFacts } from './chequeLines'
import { receiptsMenu } from './receiptsMenu'
import { paymentsMenu } from './paymentsMenu'
import { menuItems } from '@treasury/ui-kit'

const cheque = (over: Partial<ChequeLineFacts> = {}): ChequeLineFacts =>
  ({ kind: 'OrdinaryCheque', number: '458210', dueDate: '1405/09/15', currency: 'IRR', amount: '320,000,000', rate: '', bankId: 'b1', sayadId: '1234567890123456', ...over })

describe('cheque lines of the receipt form (design approved by the owner, 20 Nov 2026)', () => {
  it('a cheque is current when due on or before the receipt date, post-dated after it (manual: «چک روز»)', () => {
    expect(lineTiming('1405/08/29', '1405/08/29')).toBe('current')
    expect(lineTiming('1405/08/01', '1405/08/29')).toBe('current')
    expect(lineTiming('1405/09/15', '1405/08/29')).toBe('later')
    expect(lineTiming('', '1405/08/29')).toBeNull()
    expect(lineTiming('1405/13/01', '1405/08/29')).toBeNull()
  })

  it('a folded line is marked incomplete while a required field is missing', () => {
    expect(lineIncomplete(cheque())).toBe(false)
    expect(lineIncomplete(cheque({ number: ' ' }))).toBe(true)
    expect(lineIncomplete(cheque({ amount: '' }))).toBe(true)
    expect(lineIncomplete(cheque({ dueDate: '' }))).toBe(true)
    expect(lineIncomplete(cheque({ dueDate: '1405/13/01' }))).toBe(true)
    expect(lineIncomplete(cheque({ bankId: '' }))).toBe(true)
    expect(lineIncomplete(cheque({ sayadId: '' }))).toBe(true)
  })

  it('a guarantee cheque may have no due date; a foreign-currency cheque needs its rate', () => {
    expect(lineIncomplete(cheque({ kind: 'GuaranteeCheque', dueDate: '' }))).toBe(false)
    expect(lineIncomplete(cheque({ kind: 'GuaranteeCheque', dueDate: 'x' }))).toBe(true)
    expect(lineIncomplete(cheque({ currency: 'USD', amount: '1,000', rate: '' }))).toBe(true)
    expect(lineIncomplete(cheque({ currency: 'USD', amount: '1,000', rate: '600,000' }))).toBe(false)
  })

  it('the Rial value of a foreign-currency amount is amount × rate', () => {
    expect(rialOf('1,000', 'USD', '600,000')).toBe(600_000_000)
    expect(rialOf('1,000', 'USD', '')).toBeNull()
    expect(rialOf('۱۲٬۵۰۰', 'IRR', '')).toBe(12_500)
  })

  it('the summary counts current and post-dated cheques with their Rial totals', () => {
    const s = summariseLines([
      cheque({ dueDate: '1405/08/29', amount: '320,000,000' }),
      cheque({ dueDate: '1405/10/15', amount: '450,000,000' }),
      cheque({ dueDate: '1405/11/20', amount: '180,000,000' }),
      cheque({ dueDate: '', amount: '' }),
    ], '1405/08/29')
    expect(s.current).toEqual({ count: 1, rial: 320_000_000 })
    expect(s.later).toEqual({ count: 2, rial: 630_000_000 })
    expect(s.undated).toEqual({ count: 1, rial: 0 })
  })
})

describe('menu of «دریافت‌ها»', () => {
  it('keeps the order the owner gave (report no. 6, item 12), the transfer screens of report no. 7 at the end of their groups, and the list last in a coloured box', () => {
    const items = menuItems(receiptsMenu)
    expect(items.map(i => i.title[0])).toEqual([
      'دریافت چک', 'عملیات چک‌های دریافتی', 'دریافت چک – نقل و انتقال (انتقال گیرنده)', 'دریافت نقدی', 'واریز به بانک', 'واریز به کارت بانک',
      'نقل و انتقال بانکی (الکترونیکی)', 'دریافت سفته', 'عملیات سفته‌های دریافتی', 'اقدام حقوقی', 'فهرست جامع دریافت‌ها',
    ])
    expect(items.filter(i => i.boxed).map(i => i.id)).toEqual(['list'])
    expect(new Set(items.map(i => i.id)).size).toBe(items.length)
  })
  it('names the payment side of a transfer cheque and the electronic transfer in «پرداخت‌ها» (report no. 7)', () => {
    expect(menuItems(paymentsMenu).map(i => i.title[0])).toEqual(['پرداخت چک – نقل و انتقال (انتقال دهنده)', 'انتقال الکترونیکی بین بانک‌های شرکت'])
    for (const g of paymentsMenu) expect(g.title?.every(x => x.length > 0)).toBe(true)
  })
  it('names every group but the boxed list in both languages', () => {
    for (const g of receiptsMenu.slice(0, -1)) expect(g.title?.every(x => x.length > 0)).toBe(true)
    expect(receiptsMenu[receiptsMenu.length - 1]!.title).toBeUndefined()
  })
})
