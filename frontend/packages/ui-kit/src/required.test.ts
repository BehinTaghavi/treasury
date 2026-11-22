import { describe, expect, it } from 'vitest'
import { isMissing, markMissing, menuItems, menuPath, type FieldLike, type MenuGroup } from './index'

/** A required field as the rule sees it: a box with a value, and the attributes set on the field. */
function field(value: string | null, disabled = false): FieldLike & { attrs: Record<string, string> } {
  const attrs: Record<string, string> = {}
  return {
    attrs,
    querySelector: () => (value === null ? null : { value, disabled }),
    setAttribute: (n, v) => { attrs[n] = v },
    removeAttribute: n => { delete attrs[n] },
  }
}

describe('required fields (owner, 20 Nov 2026: red only when left empty at a save attempt)', () => {
  it('a field is missing when its box is empty or holds only spaces', () => {
    expect(isMissing(field(''))).toBe(true)
    expect(isMissing(field('   '))).toBe(true)
    expect(isMissing(field('1405/08/29'))).toBe(false)
  })
  it('a field that cannot be typed in, or has no box on the screen, is not missing', () => {
    expect(isMissing(field('', true))).toBe(false)
    expect(isMissing(field(null))).toBe(false)
  })
  it('a save attempt marks the empty required fields and clears the mark of the filled ones', () => {
    const empty = field('')
    const filled = field('x')
    filled.attrs['data-missing'] = ''
    const form = { querySelectorAll: () => [empty, filled] }
    expect(markMissing(form)).toBe(1)
    expect('data-missing' in empty.attrs).toBe(true)
    expect('data-missing' in filled.attrs).toBe(false)
  })
  it('nothing is marked when every required field is filled, or there is no form', () => {
    const a = field('a')
    expect(markMissing({ querySelectorAll: () => [a] })).toBe(0)
    expect(a.attrs).toEqual({})
    expect(markMissing(null)).toBe(0)
  })
})

describe('sub-menu of an area', () => {
  const groups: MenuGroup[] = [
    { title: ['چک', 'Cheques'], items: [{ id: 'a', title: ['دریافت چک', 'Cheque receipt'] }, { id: 'b', title: ['عملیات', 'Steps'] }] },
    { items: [{ id: 'list', title: ['فهرست جامع', 'All'], boxed: true }] },
  ]
  it('lists the items in the order given, across the groups', () => {
    expect(menuItems(groups).map(i => i.id)).toEqual(['a', 'b', 'list'])
  })
  it('gives the path to an item: area, group, item; an item without a group has no group in its path', () => {
    expect(menuPath('دریافت‌ها', groups, 'b', true)).toEqual(['دریافت‌ها', 'چک', 'عملیات'])
    expect(menuPath('Receipts', groups, 'list', false)).toEqual(['Receipts', 'All'])
    expect(menuPath('Receipts', groups, 'nothing', false)).toEqual(['Receipts'])
  })
})
