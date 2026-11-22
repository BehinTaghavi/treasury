import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { describe, expect, it } from 'vitest'
import { OPERATIONS, OPERATION_GROUPS, OTHER_PLACES, POSITIONS, ROLES, inFlowOrder, operationTitle, placeTitle, positionTitle, roleTitle, rolesToEdit, rolesToSend } from './persons'

const domain = join(__dirname, '../../../../../src/Services/Reference/Treasury.Reference.Domain/Signatures')
/** The members of a C# enum, in order. */
function enumNames(file: string, name: string): string[] {
  const body = new RegExp(`public enum ${name}\\s*\\{([^}]*)\\}`).exec(readFileSync(join(domain, file), 'utf8'))![1]!
  return body.split(',').map(x => x.trim()).filter(Boolean)
}

describe('lists of signatories and permission holders (owner\'s report no. 8)', () => {
  it('offers the positions of the report, in its order, ending with «سایر»', () => {
    expect(POSITIONS.map(p => p.fa)).toEqual([
      'مدیر عامل', 'مدیر عامل و رییس هیئت مدیره', 'رییس هیئت مدیره', 'عضو هیئت مدیره (موظف)', 'عضو هیئت مدیره (غیر موظف)', 'مدیر و مسئول غیر عضو هیئت مدیره', 'سایر',
    ])
  })
  it('offers the five roles of the report', () => {
    expect(ROLES.map(r => r.fa)).toEqual(['تایید کننده (اول)', 'تایید کننده (دوم)', 'تصویب کننده (اول)', 'تصویب کننده (دوم)', 'سایر'])
  })
  it('keeps the same codes, in the same order, as reference-svc stores', () => {
    expect(POSITIONS.map(p => p.code)).toEqual(enumNames('Signatures.cs', 'SignatoryPosition'))
    expect(ROLES.map(r => r.code)).toEqual(enumNames('PermissionHolders.cs', 'PermissionRoleKind'))
    expect(OPERATIONS.map(o => o.code)).toEqual(enumNames('PermissionHolders.cs', 'TreasuryOperation'))
    expect(OTHER_PLACES.map(o => o.code)).toEqual(enumNames('PermissionHolders.cs', 'OtherRolePlace'))
  })
  it('names every operation and group in both languages, each operation once', () => {
    expect(new Set(OPERATIONS.map(o => o.code)).size).toBe(OPERATIONS.length)
    for (const g of OPERATION_GROUPS) expect(g.fa.length > 0 && g.en.length > 0 && g.operations.length > 0).toBe(true)
    for (const o of OPERATIONS) expect(o.fa.length > 0 && o.en.length > 0).toBe(true)
    expect(operationTitle('NoteReceipt', true)).toBe('دریافت سفته')
    expect(roleTitle('FirstApprover', false)).toBe('Approver (first)')
    expect(positionTitle('Chairman', true)).toBe('رییس هیئت مدیره')
    expect(operationTitle('SomethingNew', true)).toBe('SomethingNew')
  })
  it('sends the ticked roles in the order of the tick boxes with their operations in the order of the list', () => {
    const drafts = rolesToEdit([])
    expect(rolesToSend(drafts)).toEqual([])
    drafts.FirstApprover = { ticked: true, title: '', operations: ['ChequePayment', 'PaymentRequest'], place: 'AfterFirstConfirmer' }
    drafts.Other = { ticked: true, title: ' ناظر ', operations: ['PettyCash'], place: 'AfterSecondConfirmer' }
    drafts.FirstConfirmer = { ticked: false, title: '', operations: ['CashReceipt'], place: '' }
    // Only «سایر» has a place in the flow; a fixed role sends none.
    expect(rolesToSend(drafts)).toEqual([
      { kind: 'FirstApprover', title: null, operations: ['PaymentRequest', 'ChequePayment'], place: null },
      { kind: 'Other', title: 'ناظر', operations: ['PettyCash'], place: 'AfterSecondConfirmer' },
    ])
  })
  it('opens a stored holder with its roles ticked and the typed title of «سایر»', () => {
    const drafts = rolesToEdit([{ kind: 'SecondConfirmer', title: 'تایید کننده (دوم)', operations: ['NoteReceipt'] }, { kind: 'Other', title: 'ناظر', operations: ['PettyCash'], place: 'AfterFirstApprover' }])
    expect(drafts.SecondConfirmer).toEqual({ ticked: true, title: '', operations: ['NoteReceipt'], place: 'AfterSecondApprover' })
    expect(drafts.Other).toEqual({ ticked: true, title: 'ناظر', operations: ['PettyCash'], place: 'AfterFirstApprover' })
    expect(drafts.FirstConfirmer).toEqual({ ticked: false, title: '', operations: [], place: 'AfterSecondApprover' })
  })
  it('puts «سایر» last unless a place is chosen, and lists the roles of a holder in the order of the flow (owner, 22 Nov 2026)', () => {
    expect(OTHER_PLACES.map(x => x.fa)).toEqual(['پیش از تایید کننده (اول)', 'پس از تایید کننده (اول)', 'پس از تایید کننده (دوم) – پیش از تصویب', 'پس از تصویب کننده (اول)',
      'پس از تصویب کننده (دوم) – آخرین مرحله'])
    expect(placeTitle('AfterFirstApprover', false)).toBe('After the first approver')
    const sent = rolesToSend({ ...rolesToEdit([]), Other: { ticked: true, title: 'ناظر', operations: ['PettyCash'], place: '' } })
    expect(sent[0]!.place).toBe('AfterSecondApprover')
    expect(inFlowOrder([{ kind: 'SecondApprover', step: 40 }, { kind: 'Other', step: 25 }, { kind: 'FirstConfirmer', step: 10 }]).map(r => r.kind)).toEqual(['FirstConfirmer', 'Other', 'SecondApprover'])
  })
})
