import { describe, expect, it } from 'vitest'
import { menuItems, menuPath } from '@treasury/ui-kit'
import { sections } from './sections'

describe('menu of «اطلاعات پایه»', () => {
  it('keeps the order of the new-version manual as the owner arranged it; the group titles do not move an item', () => {
    expect(menuItems(sections).map(i => i.id)).toEqual([
      'company', 'countries', 'provinces', 'cities', 'currencies', 'banks', 'chart', 'details', 'bank-accounts', 'cheque-formats', 'cheque-books',
      'references', 'purposes', 'cash-desks', 'petty-cash', 'company-cards', 'departments', 'requesters', 'beneficiaries', 'signatories', 'voucher-template',
    ])
  })
  it('names every group and every item in both languages', () => {
    for (const g of sections) {
      expect(g.title?.every(x => x.length > 0)).toBe(true)
      for (const i of g.items) expect(i.title.every(x => x.length > 0)).toBe(true)
    }
  })
  it('gives the path shown when the menu is closed', () => {
    expect(menuPath('اطلاعات پایه', sections, 'cheque-books', true)).toEqual(['اطلاعات پایه', 'بانک و چک', 'دسته چک‌ها'])
    expect(menuPath('Basic information', sections, 'company', false)).toEqual(['Basic information', 'Company', 'Company particulars'])
  })
})
