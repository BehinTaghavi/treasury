import { describe, expect, it } from 'vitest'
import { chequeTransferStatus, groupSayad, limitFor, limitLines, methodName, sayadParts, sheetNumber, sheetText, shortRial, totalRial, waysFor,
  type TransferLimit } from './transfers'

const none = { minPerOrder: null, maxPerOrderInPerson: null, maxPerOrderRemote: null, maxPerDayInPerson: null, maxPerDayRemote: null, maxCountPerDay: null }
/** The figures of the owner's report no. 7 (20 Nov 2026), as the company's table starts. */
const limits: TransferLimit[] = [
  { method: 'Internal', bankId: null, ...none },
  { method: 'Pol', bankId: null, ...none, maxPerDayInPerson: 3_000_000_000, maxPerDayRemote: 2_000_000_000, maxCountPerDay: 5 },
  { method: 'Paya', bankId: null, ...none, maxPerOrderInPerson: 5_000_000_000, maxPerOrderRemote: 3_000_000_000, maxCountPerDay: 5 },
  { method: 'Satna', bankId: null, ...none, minPerOrder: 2_000_000_000 },
  { method: 'Paya', bankId: 'mellat', ...none, maxPerOrderRemote: 1_000_000_000 },
]

describe('transfers between the company\'s bank accounts (owner\'s report no. 7)', () => {
  it('offers the internal transfer within one bank, and Pol, Paya and Satna between two banks', () => {
    expect(waysFor('mellat', 'mellat')).toEqual(['Internal'])
    expect(waysFor('mellat', 'saderat')).toEqual(['Pol', 'Paya', 'Satna'])
    expect(waysFor('mellat', '')).toEqual(['Internal', 'Pol', 'Paya', 'Satna'])
  })
  it('names the ways as the owner does', () => {
    expect(methodName('Internal', 'fa')).toBe('انتقال داخلی (حساب به حساب)')
    expect(['Pol', 'Paya', 'Satna'].map(m => methodName(m, 'fa'))).toEqual(['پل', 'پایا', 'ساتنا'])
  })
  it('takes a bank\'s own limits before the general limits of the way', () => {
    expect(limitFor(limits, 'Paya', 'mellat')?.maxPerOrderRemote).toBe(1_000_000_000)
    expect(limitFor(limits, 'Paya', 'saderat')?.maxPerOrderRemote).toBe(3_000_000_000)
    expect(limitFor(limits, 'Cheque', 'mellat')).toBeUndefined()
  })
  it('says the limits in plain words, of the chosen channel only', () => {
    expect(limitLines(limitFor(limits, 'Paya', 'saderat'), 'Remote', 'fa')).toEqual(['سقف هر دستور غیرحضوری: ۳ میلیارد ریال', 'حداکثر ۵ دستور در روز'])
    expect(limitLines(limitFor(limits, 'Pol', 'saderat'), 'InPerson', 'fa')).toEqual(['سقف روزانهٔ حضوری (جمع): ۳ میلیارد ریال', 'حداکثر ۵ دستور در روز'])
    expect(limitLines(limitFor(limits, 'Satna', 'saderat'), '', 'fa')).toEqual(['حداقل هر دستور پرداخت: ۲ میلیارد ریال'])
    expect(limitLines(limitFor(limits, 'Internal', 'saderat'), 'Remote', 'fa')).toEqual([])
    expect(limitLines(limitFor(limits, 'Pol', 'x'), '', 'fa')).toHaveLength(3)
  })
  it('writes round limits short', () => {
    expect(shortRial(2_500_000_000, 'fa')).toBe('۲٫۵ میلیارد ریال')
    expect(shortRial(500_000_000, 'fa')).toBe('۵۰۰ میلیون ریال')
    expect(shortRial(1_234_567, 'en')).toBe('1,234,567 Rial')
  })
  it('builds the Sayad ID of a sheet from the book: 8 fixed middle digits and the sheet\'s Sayad serial', () => {
    const book = { firstSheet: 56780, lastSheet: 56804, sayadMiddle: '12345678', sayadFirstSerial: 97 }
    expect(sayadParts(book, 56780)).toEqual(['1234', '5678', '0097'])
    expect(sayadParts(book, 56783)).toEqual(['1234', '5678', '0100'])
    expect(sayadParts(book, 56805)).toBeNull()
    expect(sayadParts({ ...book, sayadMiddle: null }, 56780)).toBeNull()
    expect(groupSayad('9876123456780097')).toBe('9876 1234 5678 0097')
  })
  it('keeps the leading zeros of a serial and reads a typed sheet number', () => {
    expect(sheetText(56780, 6)).toBe('056780')
    expect(sheetNumber('۰۵۶۷۸۰')).toBe(56780)
    expect(sheetNumber('56a')).toBeNull()
  })
  it('names the status of a cheque transfer from the payment side to the collection', () => {
    const cheque = { issuedChequeId: 'i', chequeBookId: 'b', number: '056780', sayadId: '1', dueDate: '1405/09/30', receivedChequeId: 'r', receivedOn: '1405/08/29', collectedOn: null,
      reachedDueDate: false }
    expect(chequeTransferStatus({ state: 'ChequeIssued', cheque: { ...cheque, receivedChequeId: null, receivedState: null } }, 'fa')).toBe('پرداخت ثبت صیاد شد – در انتظار ثبت در دریافت')
    expect(chequeTransferStatus({ state: 'ChequeReceived', cheque: { ...cheque, receivedState: 'SayadRegistered' } }, 'fa')).toBe('دریافت شد – ثبت صیادی شد')
    expect(chequeTransferStatus({ state: 'ChequeReceived', cheque: { ...cheque, receivedState: 'DepositedForCollection' } }, 'fa')).toBe('دریافت شد – در جریان وصول')
    expect(chequeTransferStatus({ state: 'Collected', cheque: { ...cheque, receivedState: 'Cleared' } }, 'fa')).toBe('وصول شد')
    expect(chequeTransferStatus({ state: 'Deleted', cheque: null }, 'fa')).toBe('حذف شده')
  })
  it('leaves deleted transfers out of the total', () => {
    expect(totalRial([{ state: 'Recorded', amountRial: 100 }, { state: 'Deleted', amountRial: 50 }, { state: 'Collected', amountRial: 7 }])).toBe(107)
  })
})
