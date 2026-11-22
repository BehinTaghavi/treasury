using Treasury.Cashflow.Application.Receipts;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Cashflow.Application.Tests;

/// <summary>
/// The note receipt (owner's form «فرم دریافت سفته», 16 Nov 2026; report no. 4, item 13): notes of one issuer saved with their
/// receipt, numbered in their own series, with guarantors, the date written on each note and the voucher of the owner's table.
/// </summary>
public sealed class NoteReceiptUseCaseTests : IDisposable
{
    private readonly InMemoryDatabase _db = new();
    private readonly FakeReceiptReference _reference = new();
    private readonly InMemoryVoucherStore _vouchers = new();
    private readonly ReceiptUseCases _receipts;

    public NoteReceiptUseCaseTests()
    {
        _receipts = new ReceiptUseCases(
            new InMemoryUnitOfWork(_db), new InMemoryReceiptRepository(_db), new InMemoryInstrumentRepository(_db), new FakeReceiptAccounts(), new FakeBankAccounts(),
            _reference, new DevelopmentPolicies().OpenYear(1405), new InMemoryIdempotencyStore(_db), new User(),
            new InMemoryVoucherTemplateRepository(_vouchers), new InMemoryConfirmedVoucherRepository(_vouchers), new FakePaymentRequests());
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static ReceiptHeaderInput Header(string date = "1405/08/25") => new(date, "200015", FakeReceiptReference.SalesGroup, "بابت بدهی فاکتور ۱۲۳", Reference1: "قرارداد ۴۵");

    private static NoteTermsInput Terms(NoteReason reason = NoteReason.Debt, NoteMaturity maturity = NoteMaturity.Fixed) => new(reason, NoteForm.Paper, maturity);

    private static NoteLineInput Note(string number, string? due = "1405/11/30", decimal amount = 500_000_000, string? issued = "1405/08/20") =>
        new(number, issued, due, amount, new NoteChecks(true, true, true, true, PaymentDate: due is not null));

    [Fact]
    public async Task Notes_are_saved_with_their_receipt_and_numbered_in_their_own_series()
    {
        await _receipts.RecordCashAsync(new(Key(), Header(), FakeReceiptAccounts.Desk, 1_000), CancellationToken.None);
        var terms = Terms() with { IssuerNationalCode = "0123456789", CustodyPlace = "صندوق شرکت", OriginalReceived = true, OriginalReceivedOn = "1405/08/26",
            Attachments = new NoteAttachments(NoteImage: true), DeliveredBy = "علی رضایی" };

        var first = await _receipts.RecordNotesAsync(new(Key(), Header(), terms, [Note("123456"), Note("123457", "1405/12/25", 250_000_000)]), CancellationToken.None);
        var second = await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note("200")]), CancellationToken.None);

        Assert.True(first.IsSuccess, first.ErrorCode);
        var v = (await _receipts.GetAsync(first.Value, CancellationToken.None))!;
        Assert.Equal(("Notes", 1, 2, 750_000_000m), (v.Kind, v.Number, v.InstrumentCount, v.TotalRial));
        Assert.Equal(2, (await _receipts.GetAsync(second.Value, CancellationToken.None))!.Number);
        Assert.Equal(("Debt", "Paper", "Fixed", "0123456789", "صندوق شرکت", true, "1405/08/26", "علی رضایی"),
            (v.NoteTerms!.Reason, v.NoteTerms.Form, v.NoteTerms.Maturity, v.NoteTerms.IssuerNationalCode, v.NoteTerms.CustodyPlace, v.NoteTerms.OriginalReceived,
                v.NoteTerms.OriginalReceivedOn, v.NoteTerms.DeliveredBy));
        Assert.Empty(v.NoteTerms.Signers); // nobody holds a permission in the receipt of notes: nobody to choose
        Assert.True(v.NoteTerms.Attachments.NoteImage);
        var note = v.Instruments!.Single(i => i.Number == "123456");
        Assert.Equal(("PromissoryNote", "InCashDesk", "1405/08/25", "1405/08/20", "1405/11/30", 500_000_000m, true),
            (note.Kind, note.State, note.IssueDate, note.NoteIssueDate, note.DueDate, note.Amount, note.IsPostDated));
        Assert.True(note.Details!.Checks!.IsComplete(onDemand: false));
        Assert.Contains(("purpose-groups", FakeReceiptReference.SalesGroup), _reference.Marked);
        Assert.Equal(2, (await _receipts.ListAsync(1405, ReceiptKind.Notes, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task Guarantee_notes_payable_on_demand_have_no_due_date()
    {
        var r = await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(NoteReason.Guarantee, NoteMaturity.OnDemand) with { CommitmentSubject = "حسن انجام قرارداد ۴۵" },
            [Note("G-1", due: null)]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _receipts.GetAsync(r.Value, CancellationToken.None))!;
        var note = Assert.Single(v.Instruments!);
        Assert.Equal(("GuaranteeNote", (string?)null, true), (note.Kind, note.DueDate, note.Details!.OnDemand));
        Assert.Equal("حسن انجام قرارداد ۴۵", v.NoteTerms!.CommitmentSubject);

        var dated = await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(maturity: NoteMaturity.OnDemand), [Note("G-2")]), CancellationToken.None);
        Assert.Equal("Instrument.OnDemandHasDueDate", dated.ErrorCode);
        var undated = await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note("G-3", due: null)]), CancellationToken.None);
        Assert.Equal("Instrument.DueDateRequired", undated.ErrorCode);
    }

    [Fact]
    public async Task A_guarantor_guarantees_one_note_or_all_and_comes_from_the_list_of_guarantors_owner_answer_Q54()
    {
        var r = await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note("1"), Note("2")],
        [
            new(null, null, "200016"),                                        // every note; national code and phone as kept in Basic Information
            new("نام تایپ‌شده", "10101010103", "300001", "021-88990011", "2"),  // note 2 only; what is typed for code and phone wins, the name does not
        ]), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var notes = (await _receipts.GetAsync(r.Value, CancellationToken.None))!.Instruments!;
        var one = Assert.Single(notes.Single(n => n.Number == "1").Details!.GuarantorList);
        Assert.Equal(("مشتری دیگر", "200016", "0123456789", "09121112233"), (one.Name, one.DetailCode, one.NationalCode, one.Phone));
        var two = notes.Single(n => n.Number == "2").Details!.GuarantorList;
        Assert.Equal(["مشتری دیگر", "کارمند"], two.Select(g => g.Name));
        Assert.Equal(("10101010103", "021-88990011"), (two[1].NationalCode, two[1].Phone));
    }

    [Fact]
    public async Task Guarantors_are_checked()
    {
        async Task<string?> Code(params NoteGuarantorInput[] guarantors) =>
            (await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note("1")], guarantors), CancellationToken.None)).ErrorCode;
        var ok = new NoteGuarantorInput(null, DetailCode: "200016");
        // Owner's answer to Q54 (19 Nov 2026): «متعهد و ضامن فقط از این فهرست انتخاب شوند».
        Assert.Equal("Receipt.GuarantorNotRegistered", await Code(new NoteGuarantorInput("الف", DetailCode: "999999")));  // a تفصیل that is not in the list
        Assert.Equal("Receipt.GuarantorNotRegistered", await Code(new NoteGuarantorInput("شرکت ضامن", "10101010103"))); // a typed name
        Assert.Equal("Receipt.GuarantorNotRegistered", await Code(new NoteGuarantorInput(null, DetailCode: "200015")));  // in the list, but an obligor only
        Assert.Equal("Receipt.NotePartyInactive", await Code(new NoteGuarantorInput(null, DetailCode: "200017")));
        Assert.Equal("Receipt.GuarantorNoteNotFound", await Code(ok with { NoteNumber = "77" }));
        Assert.Equal("Instrument.TooManyGuarantors", await Code(ok, ok, ok, ok));
        Assert.Null(await Code(ok, ok, ok)); // three are allowed (answer to Q39)
        Assert.Single(await _receipts.ListAsync(1405, ReceiptKind.Notes, CancellationToken.None));
    }

    [Fact]
    public async Task The_obligor_of_a_note_receipt_is_one_of_the_obligors_of_basic_information_owner_answer_Q54()
    {
        async Task<Treasury.BuildingBlocks.Application.Result<Guid>> Save(string issuer, string number) =>
            await _receipts.RecordNotesAsync(new(Key(), Header() with { PayerDetailCode = issuer }, Terms(), [Note(number)]), CancellationToken.None);
        Assert.Equal("Receipt.IssuerNotObligor", (await Save("200016", "1")).ErrorCode); // a guarantor only
        _reference.Parties.Remove("200015");
        Assert.Equal("Receipt.IssuerNotObligor", (await Save("200015", "1")).ErrorCode); // a customer that is not in the list
        _reference.Parties["200015"] = new("200015", "شرکت نمونه بازرگانی", false, IsObligor: true, IsGuarantor: false);
        Assert.Equal("Receipt.NotePartyInactive", (await Save("200015", "1")).ErrorCode);
        Assert.Equal("Receipt.PayerNotFound", (await Save("999999", "1")).ErrorCode);

        // The list, not the type of the تفصیل (Q22), decides: a member of staff defined as an obligor hands a note over…
        var staff = await Save("300001", "1");
        Assert.True(staff.IsSuccess, staff.ErrorCode);
        // …while the other receipts keep the rule of Q22.
        Assert.Equal("Receipt.PayerTypeNotAllowed",
            (await _receipts.RecordCashAsync(new(Key(), Header() with { PayerDetailCode = "300001" }, FakeReceiptAccounts.Desk, 1_000), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_note_not_fully_checked_is_refused_owner_answer_Q40()
    {
        var unchecked1 = Note("1") with { Checks = new NoteChecks(Signature: true, Date: true, AmountInWords: true, Payee: false, PaymentDate: true) };
        Assert.Equal("Receipt.NoteChecksIncomplete", (await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note("2"), unchecked1]), CancellationToken.None)).ErrorCode);
        Assert.Equal("Receipt.NoteChecksIncomplete", (await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note("1") with { Checks = null }]), CancellationToken.None)).ErrorCode);
        Assert.Empty(await _receipts.ListAsync(1405, ReceiptKind.Notes, CancellationToken.None));
    }

    [Fact]
    public async Task Who_signs_the_note_receipt_is_chosen_among_the_permission_holders_of_its_operation_owner_report_8()
    {
        var confirmer = new SignerFact(Guid.NewGuid(), "علی رضایی", "تایید کننده (اول) – رئیس حسابداری");
        var approver = new SignerFact(Guid.NewGuid(), "بهرام کریمی", "تصویب کننده (اول) – مدیر مالی");
        var second = new SignerFact(Guid.NewGuid(), "سارا احمدی", "تایید کننده (دوم) – کارشناس خزانه");
        _reference.NoteReceiptSigners = new FormSigners([confirmer, second, approver]);
        async Task<Treasury.BuildingBlocks.Application.Result<Guid>> Save(params Guid[] signers) =>
            await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note(Guid.NewGuid().ToString("N")[..6])], SignerIds: signers), CancellationToken.None);

        Assert.Equal("Receipt.SignerNotAuthorised", (await Save(confirmer.Id, Guid.NewGuid())).ErrorCode);

        var saved = await Save(approver.Id, confirmer.Id);
        Assert.True(saved.IsSuccess, saved.ErrorCode);
        var signers = (await _receipts.GetAsync(saved.Value, CancellationToken.None))!.NoteTerms!.Signers;
        Assert.Equal([("علی رضایی", "تایید کننده (اول) – رئیس حسابداری"), ("بهرام کریمی", "تصویب کننده (اول) – مدیر مالی")], signers.Select(s => (s.Name, s.Position!)));
        // The signature rule is gone (report no. 8): nobody is compulsory and no number is asked for.
        Assert.True((await Save()).IsSuccess);
        Assert.True((await Save(second.Id)).IsSuccess);
    }

    [Fact]
    public async Task One_bad_note_saves_nothing_and_takes_no_number()
    {
        var twice = await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note("1"), Note(" 1 ")]), CancellationToken.None);
        Assert.Equal("Receipt.SameInstrumentTwice", twice.ErrorCode);
        Assert.Equal("Receipt.NoInstruments", (await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), []), CancellationToken.None)).ErrorCode);
        Assert.Equal("Money.NotPositive", (await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note("1", amount: 0)]), CancellationToken.None)).ErrorCode);
        Assert.Equal("FiscalDate.Invalid", (await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note("1", issued: "1405/13/01")]), CancellationToken.None)).ErrorCode);
        Assert.Equal("FiscalPeriod.Closed", (await _receipts.RecordNotesAsync(new(Key(), Header("1404/08/25"), Terms(), [Note("1")]), CancellationToken.None)).ErrorCode);

        var ok = await _receipts.RecordNotesAsync(new(Key(), Header(), Terms(), [Note("1")]), CancellationToken.None);
        Assert.Equal(1, (await _receipts.GetAsync(ok.Value, CancellationToken.None))!.Number);
    }

    [Fact]
    public async Task The_same_request_twice_saves_one_receipt()
    {
        var key = Key();
        var command = new RecordNoteReceiptCommand(key, Header(), Terms(), [Note("1")]);
        var a = await _receipts.RecordNotesAsync(command, CancellationToken.None);
        var b = await _receipts.RecordNotesAsync(command, CancellationToken.None);
        Assert.Equal(a.Value, b.Value);
        Assert.Single(await _receipts.ListAsync(1405, ReceiptKind.Notes, CancellationToken.None));
    }

    [Fact]
    public async Task The_form_previews_its_voucher_without_saving_owner_table()
    {
        var debt = await _receipts.PreviewNotesAsync(new("preview", Header(), Terms(), [Note("1")]), CancellationToken.None);
        Assert.True(debt.IsSuccess, debt.ErrorCode);
        Assert.Equal("دریافت سفته", debt.Value!.Operation);
        Assert.Equal([(true, (string?)"ChequesInCashDesk"), (false, (string?)null)], debt.Value.Lines.Select(l => (l.IsDebit, l.Missing)));
        Assert.Equal("4101", debt.Value.Lines[1].AccountCode);

        var guarantee = await _receipts.PreviewNotesAsync(new("preview", Header(), Terms(NoteReason.Trust), [Note("1")]), CancellationToken.None);
        Assert.Equal(["GuaranteeNotesForCompany", "GuaranteeCounterpart"], guarantee.Value!.Lines.Select(l => l.Missing!));

        Assert.Equal("Receipt.OriginalDateRequired",
            (await _receipts.PreviewNotesAsync(new("preview", Header(), Terms() with { OriginalReceived = true }, [Note("1")]), CancellationToken.None)).ErrorCode);
        Assert.Empty(await _receipts.ListAsync(1405, null, CancellationToken.None));
    }

    private sealed class User : Treasury.Cashflow.Application.Abstractions.ICurrentUser
    {
        public string UserId => "treasurer-1";
    }
}
