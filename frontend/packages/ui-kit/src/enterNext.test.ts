import { describe, expect, it } from 'vitest'
import { canLand, followsRule, isEnterField, nextStop, type EnterStop } from './index'

/** A field or button as the rule sees it. */
function stop(tagName: string, more: Partial<EnterStop> & { hidden?: boolean } = {}): EnterStop {
  const { hidden, ...rest } = more
  return { tagName, tabIndex: 0, getClientRects: () => ({ length: hidden ? 0 : 1 }), ...rest }
}
const box = (more: Partial<EnterStop> & { hidden?: boolean } = {}) => stop('INPUT', { type: 'text', ...more })
const button = (type: string, more: Partial<EnterStop> & { hidden?: boolean } = {}) => stop('BUTTON', { type, ...more })

describe('Enter goes to the next field (owner, 22 Nov 2026)', () => {
  it('means "next field" in boxes, lists, tick boxes and boxes of several lines, not on buttons or file choosers', () => {
    expect(isEnterField(box())).toBe(true)
    expect(isEnterField(stop('SELECT'))).toBe(true)
    expect(isEnterField(stop('TEXTAREA'))).toBe(true)
    expect(isEnterField(box({ type: 'checkbox' }))).toBe(true)
    expect(isEnterField(button('submit'))).toBe(false)
    expect(isEnterField(box({ type: 'file' }))).toBe(false)
    expect(isEnterField(box({ type: 'submit' }))).toBe(false)
  })

  it('goes to the following field of the form', () => {
    const date = box(), account = box(), amount = box()
    expect(nextStop([date, account, amount], date)).toBe(account)
    expect(nextStop([date, account, amount], account)).toBe(amount)
  })

  it('passes over fields that cannot be filled: switched off, only shown, hidden, or left out of the Tab order', () => {
    const date = box(), amount = box()
    const off = box({ disabled: true }), shown = box({ readOnly: true }), hidden = box({ hidden: true }), skipped = box({ tabIndex: -1 })
    expect([off, shown, hidden, skipped].map(canLand)).toEqual([false, false, false, false])
    expect(nextStop([date, off, shown, hidden, skipped, amount], date)).toBe(amount)
  })

  it('passes over the buttons between the fields (add a row, delete, choose a file)', () => {
    const number = box(), remove = button('button'), file = box({ type: 'file' }), note = box()
    expect(nextStop([number, remove, file, note], number)).toBe(note)
  })

  it('goes from the last field to the save button of the form, wherever it is drawn, never to another button', () => {
    const save = button('submit'), date = box(), addRow = button('button'), note = box(), back = button('button')
    expect(nextStop([date, addRow, note, back, save], note)).toBe(save)
    expect(nextStop([save, date, addRow, note, back], note)).toBe(save) // save button drawn above the fields
  })

  it('goes from the last field of a group without a save button (a cheque step, a table row) to the first button after it', () => {
    const tab = button('button'), date = box(), note = box(), record = button('button'), back = button('button')
    expect(nextStop([tab, date, note, record, back], note)).toBe(record)
  })

  it('stays where it is when the save button is switched off or there is nothing after the last field', () => {
    const date = box(), save = button('submit', { disabled: true })
    expect(nextStop([date, save], date)).toBeNull()
    expect(nextStop([date], date)).toBeNull()
    expect(nextStop([], date)).toBeNull()
  })

  it('treats radio buttons of one name as one choice: leaves them together, arrives at the chosen one', () => {
    const name = box()
    const cash = box({ type: 'radio', name: 'kind' }), cheque = box({ type: 'radio', name: 'kind', checked: true })
    const amount = box()
    const stops = [name, cash, cheque, amount]
    expect(nextStop(stops, name)).toBe(cheque)
    expect(nextStop(stops, cash)).toBe(amount)
    expect(nextStop(stops, cheque)).toBe(amount)
    cheque.checked = false
    expect(nextStop(stops, name)).toBe(cash) // nothing chosen yet: the first of them
  })

  it('leaves a form of one field alone: Enter adds the name to its list at once, as before', () => {
    expect(followsRule([box(), button('submit')], true)).toBe(false)
    expect(followsRule([box(), box({ readOnly: true }), button('submit')], true)).toBe(false)
    expect(followsRule([box(), stop('SELECT'), button('submit')], true)).toBe(true)
  })

  it('outside a form applies only where something is typed: the tick box of a row in a list does not lead to its buttons', () => {
    expect(followsRule([box({ type: 'checkbox' }), button('button')], false)).toBe(false)
    expect(followsRule([box(), button('button')], false)).toBe(true) // a row with a date or an amount and its own button
    expect(followsRule([box({ type: 'checkbox' }), stop('TEXTAREA'), button('button')], false)).toBe(true)
  })

  it('goes through unnamed radio buttons and tick boxes one by one, as Tab does', () => {
    const all = box({ type: 'radio' }), some = box({ type: 'radio' }), tick = box({ type: 'checkbox' })
    expect(nextStop([all, some, tick], all)).toBe(some)
    expect(nextStop([all, some, tick], some)).toBe(tick)
  })
})
