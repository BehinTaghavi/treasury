import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { describe, expect, it } from 'vitest'
import { chequeActionName, chequeStateName, dueMark, dueWaitsFor, groupChequeStatuses } from './index'

/** Names between two markers of the C# enum file, e.g. the received states or the actions. */
function enumNames(start: string, end: string): string[] {
  const file = join(__dirname, '../../../../src/Services/Cashflow/Treasury.Cashflow.Domain/Instruments/InstrumentTypes.cs')
  const text = readFileSync(file, 'utf8')
  const block = text.slice(text.indexOf(start) + start.length, text.indexOf(end, text.indexOf(start)))
  return [...block.matchAll(/^\s*([A-Z][A-Za-z]+),/gm)].map(m => m[1]!)
}

describe('latest status of a received cheque (owner, 18 Nov 2026)', () => {
  it('uses the owner\'s words', () => {
    expect(['InCashDesk', 'SayadRegistered', 'DepositedForCollection', 'Cleared', 'ReturnedToIssuer', 'ReturnedFromBank', 'Bounced', 'InLegal', 'Replaced']
      .map(s => chequeStateName(s, 'fa')))
      .toEqual(['نزد صندوق', 'ثبت صیادی شد', 'در جریان وصول', 'وصول چک', 'عودت از صندوق', 'عودت چک از بانک', 'برگشت چک', 'اقدام حقوقی', 'تعویض چک'])
    expect(dueMark('fa')).toBe('سررسید چک')
  })

  it('names every received state and every step of the domain in both languages', () => {
    const states = enumNames('// Received side', '// Issued side').concat('Cancelled')
    const actions = enumNames('public enum InstrumentAction', '}')
    expect(states).toContain('InLegal')
    expect(actions).toContain('RegisterSayad')
    const issuedOnly = ['PrepareIssue', 'Issue', 'Deliver', 'RecordMatured', 'RecordPaidByBank', 'RecordBouncedAtBank']
    // An unknown key is shown as it is; a Persian name never equals its English key.
    expect(states.filter(s => chequeStateName(s, 'fa') === s)).toEqual([])
    expect(actions.filter(a => !issuedOnly.includes(a) && chequeActionName(a, 'fa') === a)).toEqual([])
    expect(chequeStateName('InLegal', 'en')).toBe('Legal action')
    expect(chequeActionName('RegisterSayad', 'en')).toBe('Register in Sayad')
  })

  it('says what a cheque that reached its due date waits for', () => {
    expect(dueWaitsFor('InCashDesk', 'fa')).toBe('در انتظار واگذاری به بانک یا تعویض چک')
    expect(dueWaitsFor('SayadRegistered', 'fa')).toBe(dueWaitsFor('InCashDesk', 'fa'))
    expect(dueWaitsFor('DepositedForCollection', 'fa')).toBe('در انتظار وصول چک، عودت چک از بانک یا برگشت چک')
    expect(dueWaitsFor('Cleared', 'fa')).toBe('')
  })

  it('groups the cheques of a receipt by status for the receipts list', () => {
    const groups = groupChequeStatuses([
      { state: 'InCashDesk', reachedDueDate: true }, { state: 'Cleared', reachedDueDate: false }, { state: 'InCashDesk', reachedDueDate: false },
    ], 'fa')
    expect(groups).toEqual([
      { state: 'InCashDesk', name: 'نزد صندوق', count: 2, due: 1 },
      { state: 'Cleared', name: 'وصول چک', count: 1, due: 0 },
    ])
    expect(groupChequeStatuses(null, 'fa')).toEqual([])
  })
})
