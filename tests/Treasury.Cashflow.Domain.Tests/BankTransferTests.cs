using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.ChequeBooks;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Transfers;
using Treasury.Cashflow.Domain.Vouchers;
using Xunit;
using static Treasury.Cashflow.Domain.Tests.TestData;

namespace Treasury.Cashflow.Domain.Tests;

/// <summary>
/// Transfers between the company's own bank accounts («نقل و انتقال بین بانکی»), the owner's report no. 7 of 20 Nov 2026: the four
/// electronic ways with their configurable limits, and the Sayad cheque with its payment side («انتقال دهنده») and receipts side
/// («انتقال گیرنده»). The old program (Lcash.mdb) had no such operation.
/// </summary>
public sealed class BankTransferTests
{
    private const decimal Milliard = 1_000_000_000m;

    private static readonly Guid Mellat = Guid.NewGuid();
    private static readonly Guid Melli = Guid.NewGuid();

    private static readonly TransferAccountFact MellatCurrent = new(Guid.NewGuid(), "0101-1", Mellat, "بانک ملت", "IRR", true, true, "1102", "600001", "مرکزی", "6501");
    private static readonly TransferAccountFact MellatSaving = new(Guid.NewGuid(), "0101-2", Mellat, "بانک ملت", "IRR", true, false, "1102", "600002");
    private static readonly TransferAccountFact MelliCurrent = new(Guid.NewGuid(), "0202-1", Melli, "بانک ملی", "IRR", true, true, "1103", "600003");
    private static readonly TransferAccountFact MellatDollar = new(Guid.NewGuid(), "USD-1", Mellat, "بانک ملت", "USD", true, false, "1104", "600004");
    private static readonly TransferAccountFact MellatDollar2 = new(Guid.NewGuid(), "USD-2", Mellat, "بانک ملت", "USD", true, false, "1104", "600005");

    /// <summary>An approved payment request «نقل و انتقال بین بانک‌های شرکت» for these two accounts (owner's answer to Q82).</summary>
    private static TransferRequestFact Request(TransferAccountFact source, TransferAccountFact destination, decimal remaining = 1000 * Milliard) =>
        new(Guid.NewGuid(), 12, IsApproved: true, IsForOwnTransfer: true, source.Id, destination.Id, remaining);

    private static ElectronicTransferForm Form(
        TransferMethod method, decimal amount, TransferChannel? channel = TransferChannel.Remote, TransferAccountFact? source = null, TransferAccountFact? destination = null,
        string date = "1405/08/29")
    {
        source ??= MellatCurrent;
        destination ??= method == TransferMethod.Internal ? MellatSaving : MelliCurrent;
        return new(D(date), source, destination, method, channel, Money.Rials(amount), "TRK-1", "تأمین موجودی", Request(source, destination));
    }

    private static BankTransfer Record(ElectronicTransferForm form, TransferLimits? limits = null, TransferDayUsage? usage = null) =>
        BankTransfer.RecordElectronic(Guid.NewGuid(), 1, form, limits ?? TransferLimits.OwnerDefaults(), usage ?? TransferDayUsage.None, User);

    private static string Refused(Action act) => Assert.Throws<DomainException>(act).Code;

    // ----- the ways of an electronic transfer (report no. 7: «نحوه انتقال الکترونیکی») ---------------------------------------------

    [Fact]
    public void An_internal_transfer_is_between_two_accounts_of_the_same_bank()
    {
        var t = Record(Form(TransferMethod.Internal, 800_000_000));
        Assert.Equal((TransferState.Recorded, 1405, "بانک ملت – 0101-1", "بانک ملت – 0101-2"), (t.State, t.FiscalYear, t.Source.Title, t.Destination.Title));
        Assert.IsType<BankTransferRecorded>(Assert.Single(t.PendingEvents));
        Assert.Equal("Transfer.InternalNeedsSameBank", Refused(() => Record(Form(TransferMethod.Internal, 1_000, destination: MelliCurrent))));
    }

    [Theory]
    [InlineData(TransferMethod.Pol)]
    [InlineData(TransferMethod.Paya)]
    [InlineData(TransferMethod.Satna)]
    public void Pol_Paya_and_Satna_are_between_two_different_banks(TransferMethod method)
    {
        var amount = method == TransferMethod.Satna ? 2 * Milliard : 500_000_000m;
        Assert.Equal(method, Record(Form(method, amount)).Method);
        Assert.Equal("Transfer.MethodNeedsOtherBank", Refused(() => Record(Form(method, amount, destination: MellatSaving))));
    }

    [Fact]
    public void A_transfer_names_two_different_active_accounts_in_one_currency_and_how_the_order_was_given()
    {
        Assert.Equal("Transfer.SameAccount", Refused(() => Record(Form(TransferMethod.Internal, 1_000, destination: MellatCurrent))));
        Assert.Equal("Transfer.AccountInactive", Refused(() => Record(Form(TransferMethod.Paya, 1_000, destination: MelliCurrent with { IsActive = false }))));
        Assert.Equal("Transfer.CurrencyMismatch", Refused(() => Record(Form(TransferMethod.Internal, 1_000, destination: MellatDollar))));
        Assert.Equal("Transfer.ChannelRequired", Refused(() => Record(Form(TransferMethod.Paya, 1_000, channel: null))));
        Assert.Equal("Transfer.MethodRequired", Refused(() => Record(Form(TransferMethod.Cheque, 1_000))));
        Assert.Equal("Transfer.UserRequired", Refused(() => BankTransfer.RecordElectronic(Guid.NewGuid(), 1, Form(TransferMethod.Paya, 1_000), new TransferLimits(), TransferDayUsage.None, " ")));
    }

    [Fact]
    public void A_foreign_currency_transfer_is_internal_only()
    {
        var dollars = Money.Foreign(1_000, "USD", 600_000);
        var form = new ElectronicTransferForm(D("1405/08/29"), MellatDollar, MellatDollar2, TransferMethod.Internal, TransferChannel.InPerson, dollars,
            Request: Request(MellatDollar, MellatDollar2));
        Assert.Equal(600_000_000m, Record(form).Amount.ToRial());
        Assert.Equal("Transfer.MethodNotForForeignCurrency",
            Refused(() => Record(form with { Method = TransferMethod.Satna, Destination = MellatDollar2 with { BankId = Melli, BankName = "بانک ملی" } })));
    }

    // ----- limits (report no. 7: «فیلد محدودیت مبالغ … قابل کانفیگ … و در کد ثابت نباشد») -------------------------------------------

    [Fact]
    public void A_company_starts_with_the_limits_of_the_owners_report()
    {
        var limits = TransferLimits.OwnerDefaults();
        Assert.True(limits.For(TransferMethod.Internal, Mellat).IsEmpty); // «هر بانکی ضوابط خود را دارد»
        Assert.Equal(new TransferLimit(TransferMethod.Pol, MaxPerDayInPerson: 3 * Milliard, MaxPerDayRemote: 2 * Milliard, MaxCountPerDay: 5), limits.For(TransferMethod.Pol, Mellat));
        Assert.Equal(new TransferLimit(TransferMethod.Paya, MaxPerOrderInPerson: 5 * Milliard, MaxPerOrderRemote: 3 * Milliard, MaxCountPerDay: 5), limits.For(TransferMethod.Paya, Mellat));
        Assert.Equal(new TransferLimit(TransferMethod.Satna, MinPerOrder: 2 * Milliard), limits.For(TransferMethod.Satna, Mellat));
    }

    [Fact]
    public void Pol_orders_of_one_day_stay_within_the_daily_total_of_their_channel_and_five_orders()
    {
        // «روزانه حضوری جمعا ۳ میلیارد ریال و غیر حضوری جمعا ۲ میلیارد ریال و حداکثر ۵ ترانزکشن در روز»
        Record(Form(TransferMethod.Pol, 500_000_000, TransferChannel.InPerson), usage: new TransferDayUsage(2.5m * Milliard, 2));
        Assert.Equal("Transfer.OverDailyLimit", Refused(() => Record(Form(TransferMethod.Pol, 600_000_000, TransferChannel.InPerson), usage: new TransferDayUsage(2.5m * Milliard, 2))));
        Record(Form(TransferMethod.Pol, 2 * Milliard, TransferChannel.Remote));
        Assert.Equal("Transfer.OverDailyLimit", Refused(() => Record(Form(TransferMethod.Pol, 2 * Milliard + 1, TransferChannel.Remote))));
        Assert.Equal("Transfer.OverDailyCount", Refused(() => Record(Form(TransferMethod.Pol, 1_000_000), usage: new TransferDayUsage(0, 5))));
    }

    [Fact]
    public void A_Paya_order_stays_within_the_order_limit_of_its_channel_and_five_orders_a_day()
    {
        // «حضوری هر دستور پرداخت ۵ میلیارد ریال و غیر حضوری هر دستور پرداخت ۳ میلیارد ریال و حداکثر ۵ ترانزکشن در روز»
        Record(Form(TransferMethod.Paya, 5 * Milliard, TransferChannel.InPerson), usage: new TransferDayUsage(20 * Milliard, 4));
        Assert.Equal("Transfer.OverOrderLimit", Refused(() => Record(Form(TransferMethod.Paya, 5 * Milliard + 1, TransferChannel.InPerson))));
        Record(Form(TransferMethod.Paya, 3 * Milliard, TransferChannel.Remote));
        Assert.Equal("Transfer.OverOrderLimit", Refused(() => Record(Form(TransferMethod.Paya, 3 * Milliard + 1, TransferChannel.Remote))));
        Assert.Equal("Transfer.OverDailyCount", Refused(() => Record(Form(TransferMethod.Paya, 1_000_000), usage: new TransferDayUsage(0, 5))));
    }

    [Fact]
    public void A_Satna_order_carries_at_least_the_minimum()
    {
        // «حداقل هر دستور پرداخت ۲ میلیارد ریال»
        Record(Form(TransferMethod.Satna, 2 * Milliard), usage: new TransferDayUsage(900 * Milliard, 40));
        Assert.Equal("Transfer.BelowMinimum", Refused(() => Record(Form(TransferMethod.Satna, 2 * Milliard - 1))));
    }

    [Fact]
    public void The_limits_are_the_companys_data_and_a_bank_may_have_its_own()
    {
        var limits = TransferLimits.OwnerDefaults();
        limits.Set(new TransferLimit(TransferMethod.Paya, MaxPerOrderInPerson: 10 * Milliard, MaxPerOrderRemote: 10 * Milliard)); // the banks raised Paya
        Record(Form(TransferMethod.Paya, 8 * Milliard), limits, new TransferDayUsage(0, 30));

        // «هر بانکی ضوابط خود را دارد»: Mellat's own limit of an internal transfer replaces the general (empty) one for its accounts.
        limits.Set(new TransferLimit(TransferMethod.Internal, Mellat, MaxPerOrderRemote: Milliard));
        Assert.Equal("Transfer.OverOrderLimit", Refused(() => Record(Form(TransferMethod.Internal, Milliard + 1), limits)));
        Assert.True(limits.For(TransferMethod.Internal, Melli).IsEmpty);
        limits.RemoveBankRow(TransferMethod.Internal, Mellat);
        Record(Form(TransferMethod.Internal, 50 * Milliard), limits);
        Assert.Equal("TransferLimit.NotFound", Refused(() => limits.RemoveBankRow(TransferMethod.Internal, Mellat)));
    }

    [Fact]
    public void A_limit_is_a_whole_positive_rial_amount_and_fits_with_the_others()
    {
        var limits = new TransferLimits();
        Assert.Equal("TransferLimit.MethodInvalid", Refused(() => limits.Set(new TransferLimit(TransferMethod.Cheque))));
        Assert.Equal("TransferLimit.AmountInvalid", Refused(() => limits.Set(new TransferLimit(TransferMethod.Pol, MaxPerDayRemote: 0))));
        Assert.Equal("TransferLimit.AmountInvalid", Refused(() => limits.Set(new TransferLimit(TransferMethod.Pol, MaxPerDayRemote: 10.5m))));
        Assert.Equal("TransferLimit.CountInvalid", Refused(() => limits.Set(new TransferLimit(TransferMethod.Pol, MaxCountPerDay: 0))));
        Assert.Equal("TransferLimit.MinAboveMax", Refused(() => limits.Set(new TransferLimit(TransferMethod.Satna, MinPerOrder: 5, MaxPerOrderRemote: 4))));
        Assert.Equal("TransferLimit.OrderAboveDay", Refused(() => limits.Set(new TransferLimit(TransferMethod.Paya, MaxPerOrderInPerson: 5, MaxPerDayInPerson: 4))));
        limits.Set(new TransferLimit(TransferMethod.Pol)); // every value empty: no limit
        Record(Form(TransferMethod.Pol, 900 * Milliard), limits, new TransferDayUsage(900 * Milliard, 99));
    }

    // ----- correction and deletion before «تایید سند» (as for receipts, report no. 6) ----------------------------------------------

    [Fact]
    public void An_electronic_transfer_is_corrected_until_its_voucher_is_confirmed_and_keeps_its_number_and_year()
    {
        var t = Record(Form(TransferMethod.Paya, Milliard));
        t.CorrectElectronic(Form(TransferMethod.Satna, 4 * Milliard, TransferChannel.InPerson), TransferLimits.OwnerDefaults(), TransferDayUsage.None, User, voucherConfirmed: false);
        Assert.Equal((TransferMethod.Satna, TransferChannel.InPerson, 4 * Milliard, 1), (t.Method, t.Channel, t.Amount.Amount, t.Number));
        Assert.IsType<BankTransferCorrected>(t.PendingEvents[^1]);

        Assert.Equal("Transfer.BelowMinimum",
            Refused(() => t.CorrectElectronic(Form(TransferMethod.Satna, Milliard), TransferLimits.OwnerDefaults(), TransferDayUsage.None, User, false)));
        Assert.Equal("Transfer.CorrectionOtherYear",
            Refused(() => t.CorrectElectronic(Form(TransferMethod.Satna, 4 * Milliard, date: "1404/12/20"), TransferLimits.OwnerDefaults(), TransferDayUsage.None, User, false)));
        Assert.Equal("Transfer.VoucherConfirmed",
            Refused(() => t.CorrectElectronic(Form(TransferMethod.Satna, 4 * Milliard), TransferLimits.OwnerDefaults(), TransferDayUsage.None, User, voucherConfirmed: true)));
    }

    [Fact]
    public void A_transfer_recorded_by_mistake_is_deleted_before_its_voucher_is_confirmed_and_stays_marked()
    {
        var at = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);
        var confirmed = Record(Form(TransferMethod.Paya, Milliard));
        Assert.Equal("Transfer.VoucherConfirmed", Refused(() => confirmed.Delete(User, voucherConfirmed: true, at)));

        var t = Record(Form(TransferMethod.Paya, Milliard));
        t.Delete(User, false, at);
        Assert.Equal((TransferState.Deleted, User, at), (t.State, t.DeletedBy, t.DeletedAt!.Value));
        Assert.IsType<BankTransferDeleted>(t.PendingEvents[^1]);
        Assert.Equal("Transfer.Deleted", Refused(() => t.Delete(User, false, at)));
        Assert.Equal("Transfer.Deleted", t.WhyNotCorrectable(false));
    }

    // ----- by Sayad cheque (report no. 7: «نحوه انتقال با چک صیادی») ---------------------------------------------------------------

    private static readonly Guid Book = Guid.NewGuid();
    private const string SayadId = "4821" + "12345678" + "0007";

    private static (BankTransfer Transfer, Instrument Issued) ChequeTransfer(string date = "1405/08/29", string due = "1405/10/15", decimal amount = 7_000_000_000,
        TransferAccountFact? source = null, TransferAccountFact? destination = null, bool sayadRegistered = true)
    {
        var id = Guid.NewGuid();
        source ??= MellatCurrent;
        destination ??= MelliCurrent;
        var issued = Instrument.PrepareIssue(Guid.NewGuid(), due == date ? InstrumentKind.OrdinaryCheque : InstrumentKind.PostDatedCheque, Money.Rials(amount), D(date), D(due),
            new CounterpartyRef(destination.Id, "حساب 0202-1 بانک ملی"), "نقل و انتقال بانکی", source.Id, id, User);
        issued.Issue(Book, "000507", D(date), User);
        return (BankTransfer.IssueCheque(id, 3, D(date), source, destination, issued, Book, 507, SayadId, "تأمین موجودی حساب ملی", User, Request(source, destination),
            sayadRegistered), issued);
    }

    private static Instrument Receive(BankTransfer transfer, Instrument issued, string on = "1405/08/29")
    {
        var received = Instrument.ReceiveTransferCheque(Guid.NewGuid(), issued, transfer.Id, transfer.Destination.Id, CounterpartyRef.ForDetail("600001", "حساب 0101-1 بانک ملت"),
            "بانک ملت", new ReceivedInstrumentDetails(Mellat, "مرکزی", "6501", "0101-1", SayadId: SayadId), D(on), User);
        transfer.ReceiveCheque(received, User);
        return received;
    }

    [Fact]
    public void The_payment_side_issues_a_cheque_of_the_source_account_with_its_sayad_id()
    {
        var (t, issued) = ChequeTransfer();
        Assert.Equal((TransferState.ChequeIssued, TransferMethod.Cheque, 7_000_000_000m, "000507", SayadId), (t.State, t.Method, t.Amount.Amount, t.Cheque!.Number, t.Cheque.SayadId));
        Assert.Equal((issued.Id, D("1405/10/15")), (t.Cheque.IssuedChequeId, t.Cheque.DueDate));
        Assert.Null(t.Channel); // a cheque has no channel and no electronic limit: 7 milliard is above every Paya limit
        Assert.IsType<TransferChequeIssued>(Assert.Single(t.PendingEvents));

        Assert.Equal("Transfer.SourceHasNoCheques", Refused(() => ChequeTransfer(source: MellatSaving)));
        Assert.Equal("Transfer.SameAccount", Refused(() => ChequeTransfer(destination: MellatCurrent)));
        Assert.Equal("Instrument.DueBeforeIssue", Refused(() => ChequeTransfer(due: "1405/08/28"))); // old rule R6
    }

    [Fact]
    public void A_cheque_transfer_needs_the_cheque_issued_for_it_and_a_sixteen_digit_sayad_id()
    {
        var (_, other) = ChequeTransfer();
        Assert.Equal("Transfer.ChequeMismatch",
            Refused(() => BankTransfer.IssueCheque(Guid.NewGuid(), 4, D("1405/08/29"), MellatCurrent, MelliCurrent, other, Book, 507, SayadId, null, User,
                Request(MellatCurrent, MelliCurrent))));
        var id = Guid.NewGuid();
        var issued = Instrument.PrepareIssue(Guid.NewGuid(), InstrumentKind.OrdinaryCheque, Money.Rials(1_000), D("1405/08/29"), D("1405/08/29"),
            new CounterpartyRef(MelliCurrent.Id, "حساب"), "نقل و انتقال", MellatCurrent.Id, id, User);
        issued.Issue(Book, "000508", D("1405/08/29"), User);
        Assert.Equal("Transfer.SayadIdRequired",
            Refused(() => BankTransfer.IssueCheque(id, 4, D("1405/08/29"), MellatCurrent, MelliCurrent, issued, Book, 508, "1234", null, User,
                Request(MellatCurrent, MelliCurrent))));
    }

    [Fact]
    public void The_button_records_the_same_cheque_on_the_receipts_side_in_the_cash_desk()
    {
        var (t, issued) = ChequeTransfer();
        var received = Receive(t, issued);

        Assert.Equal((TransferState.ChequeReceived, received.Id, D("1405/08/29")), (t.State, t.Cheque!.ReceivedChequeId!.Value, t.Cheque.ReceivedOn!.Value));
        Assert.Equal((InstrumentDirection.Received, InstrumentState.InCashDesk, InstrumentKind.OrdinaryCheque), (received.Direction, received.State, received.Kind));
        Assert.Equal(("000507", 7_000_000_000m, D("1405/10/15"), "بانک ملت", SayadId), (received.Number, received.Amount.Amount, received.DueDate!.Value, received.BankName, received.Details.SayadId));
        Assert.Equal((t.Id, t.Destination.Id, "600001"), (received.Details.TransferId!.Value, received.Details.TransferAccountId!.Value, received.Counterparty.DetailCode));
        Assert.True(received.IsTransferCheque);
        Assert.True(received.IsPostDated);
        Assert.Null(received.ReceiptId);

        Assert.Equal("Transfer.ChequeAlreadyReceived", Refused(() => t.ReceiveCheque(received, User)));
    }

    [Fact]
    public void The_cheque_is_not_received_before_the_day_it_was_issued_nor_for_another_transfer()
    {
        var (t, issued) = ChequeTransfer();
        Assert.Equal("Transfer.ReceivedBeforeIssue", Refused(() => Receive(t, issued, on: "1405/08/28")));
        var (other, _) = ChequeTransfer();
        Assert.Equal("Transfer.ChequeMismatch", Refused(() => Receive(other, issued)));
    }

    [Fact]
    public void A_transfer_cheque_goes_on_with_sayad_registration_deposit_at_the_destination_account_and_collection()
    {
        // «در مورد چک‌های نقل و انتقال در بخش دریافت مراحل بعدی واگذاری به بانک و ثبت صیادی مانند سایر چک‌های دریافتی انجام می‌گردد»
        var (t, issued) = ChequeTransfer();
        var cheque = Receive(t, issued);
        Assert.Equal([InstrumentAction.RegisterSayad], cheque.AllowedActions());

        var destination = new CollectionAccount(t.Destination.Id, "0202-1", "IRR", true);
        Assert.Equal("Instrument.SayadRegistrationRequired", Refused(() => cheque.DepositForCollection(destination, D("1405/08/30"), User))); // rule N3
        cheque.RegisterSayad(D("1405/08/29"), User, "SYD-77"); // «دریافت ثبت صیاد شد»
        Assert.Equal([InstrumentAction.DepositForCollection], cheque.AllowedActions());

        Assert.Equal("Instrument.TransferChequeOtherAccount",
            Refused(() => cheque.DepositForCollection(new CollectionAccount(Guid.NewGuid(), "0303-1", "IRR", true), D("1405/08/30"), User)));
        Assert.Equal("Instrument.TransferChequeOtherAccount", Refused(() => cheque.DepositForCollection("0303-1", D("1405/08/30"), User)));
        cheque.DepositForCollection(destination, D("1405/08/30"), User, "F-9");
        Assert.Equal([InstrumentAction.RecordCleared, InstrumentAction.ReturnFromBank], cheque.AllowedActions());

        Assert.Equal("Instrument.ClearedBeforeDueDate", Refused(() => cheque.RecordCleared(D("1405/10/14"), User))); // rule N2
        cheque.RecordCleared(D("1405/10/15"), User);
        t.MarkCollected(D("1405/10/15"));
        issued.RecordPaidByBank(D("1405/10/15"), User);
        Assert.Equal((InstrumentState.Cleared, TransferState.Collected, InstrumentState.PaidByBank), (cheque.State, t.State, issued.State));
        Assert.Equal(D("1405/10/15"), t.Cheque!.CollectedOn);
    }

    [Fact]
    public void The_steps_that_settle_with_an_outside_payer_are_not_recorded_on_a_transfer_cheque()
    {
        var (t, issued) = ChequeTransfer();
        var cheque = Receive(t, issued);
        Assert.Equal("Instrument.NotForTransferCheque", Refused(() => cheque.ReturnToIssuer(D("1405/08/30"), User)));
        Assert.Equal("Instrument.NotForTransferCheque", Refused(() => cheque.CancelReceived(D("1405/08/30"), User, "M-1", D("1405/08/30"), "اشتباه")));
        Assert.Equal("Instrument.NotForTransferCheque", Refused(() => cheque.SendToLegal(D("1405/08/30"), User, "اقدام")));

        cheque.RegisterSayad(D("1405/08/29"), User);
        cheque.DepositForCollection(new CollectionAccount(t.Destination.Id, "0202-1", "IRR", true), D("1405/08/30"), User);
        Assert.Equal("Instrument.NotForTransferCheque", Refused(() => cheque.RecordBounced(D("1405/10/16"), User, "کسری", "BN-1")));

        // The bank may hand it back; it is then deposited again at the destination account.
        cheque.ReturnFromBank(D("1405/10/16"), User);
        Assert.Equal([InstrumentAction.DepositForCollection], cheque.AllowedActions());
        cheque.DepositForCollection(new CollectionAccount(t.Destination.Id, "0202-1", "IRR", true), D("1405/10/17"), User);
        Assert.Equal(InstrumentState.DepositedForCollection, cheque.State);

        // An ordinary received cheque keeps every step.
        Assert.Contains(InstrumentAction.ReturnToIssuer, ReceivedCheque().AllowedActions());
    }

    [Fact]
    public void A_receipts_side_record_made_by_mistake_is_taken_back_while_the_cheque_has_no_step_and_no_confirmed_voucher()
    {
        var (t, issued) = ChequeTransfer();
        var cheque = Receive(t, issued);
        Assert.Equal("Transfer.VoucherConfirmed", Refused(() => t.UndoReceive(cheque, receiverVoucherConfirmed: true, User)));
        t.UndoReceive(cheque, false, User);
        Assert.Equal((TransferState.ChequeIssued, (Guid?)null, (FiscalDate?)null), (t.State, t.Cheque!.ReceivedChequeId, t.Cheque.ReceivedOn));
        Assert.IsType<TransferChequeReceiptUndone>(t.PendingEvents[^1]);

        var again = Receive(t, issued);
        again.RegisterSayad(D("1405/08/29"), User);
        Assert.Equal("Transfer.ChequeMovedOn", Refused(() => t.UndoReceive(again, false, User)));
    }

    [Fact]
    public void A_cheque_transfer_is_deleted_only_before_its_cheque_is_on_the_receipts_side_and_is_never_corrected()
    {
        var at = DateTimeOffset.UnixEpoch;
        var (t, issued) = ChequeTransfer();
        Assert.Equal("Transfer.ChequeNotCorrected", t.WhyNotCorrectable(false));

        var cheque = Receive(t, issued);
        Assert.Equal("Transfer.ChequeAlreadyReceived", Refused(() => t.Delete(User, false, at)));
        Assert.Equal("Transfer.ChequeMovedOn", Refused(() => cheque.RemoveIssuedForTransfer(t.Id, User))); // not the issued cheque
        t.UndoReceive(cheque, false, User);

        t.Delete(User, false, at);
        issued.RemoveIssuedForTransfer(t.Id, User);
        Assert.Equal(TransferState.Deleted, t.State);
        Assert.IsType<IssuedInstrumentRemoved>(issued.PendingEvents[^1]);
        Assert.Equal("Transfer.Deleted", Refused(() => Receive(t, issued)));
    }

    [Fact]
    public void The_sheet_of_a_deleted_cheque_transfer_is_freed_and_can_be_typed_again()
    {
        var book = ChequeBook.Register(Guid.NewGuid(), MellatCurrent.Id, "1234", 100500, 100524, ChequeBookSayad.Create("12345678", "1"));
        book.Reserve(100500);
        book.Void(100501, "مخدوش");
        Assert.Equal(100502, book.SuggestNext());
        book.Release(100500);
        Assert.Equal(100500, book.SuggestNext());
        Assert.False(book.UsedSheets.ContainsKey(100500));
        Assert.IsType<ChequeSheetReleased>(book.PendingEvents[^1]);
        Assert.Equal("ChequeBook.SheetNotIssued", Refused(() => book.Release(100501))); // a voided sheet is never freed
        Assert.Equal("ChequeBook.SheetNotIssued", Refused(() => book.Release(100502)));
        book.Reserve(100500);

        // A book switched off because its last sheet was used is switched on again when that sheet is freed.
        var small = ChequeBook.Register(Guid.NewGuid(), MellatCurrent.Id, "1234", 100600, 100601, ChequeBookSayad.Create("12345678", "1"), partlyUsed: true);
        small.Reserve(100600);
        small.Reserve(100601);
        Assert.False(small.IsActive);
        small.Release(100601);
        Assert.True(small.IsActive);
        Assert.Equal(100601, small.SuggestNext());
    }

    // ----- vouchers (report no. 7) ---------------------------------------------------------------------------------------------------

    private static VoucherTemplate Template(bool inTransit = true)
    {
        var accounts = new Dictionary<FixedAccount, string>
        {
            [FixedAccount.ChequesInCashDesk] = "1201", [FixedAccount.ChequesInCollection] = "1202", [FixedAccount.BankFees] = "8205", [FixedAccount.NotesPayable] = "2105",
        };
        if (inTransit)
        {
            accounts[FixedAccount.FundsInTransit] = "1109";
        }

        return new VoucherTemplate(accounts);
    }

    private static (bool, string?, string?, decimal)[] Lines(Voucher? v) => [.. v!.Lines.Select(l => (l.IsDebit, l.AccountCode, l.DetailCode, l.AmountRial))];

    [Fact]
    public void An_electronic_transfer_debits_the_destination_bank_and_credits_the_source_bank()
    {
        var t = Record(Form(TransferMethod.Paya, Milliard));
        var v = TransferVouchers.ForElectronic(t, new LedgerSide("1102", "600001"), new LedgerSide("1103", "600003"), Template());
        Assert.Equal([(true, "1103", "600003", Milliard), (false, "1102", "600001", Milliard)], Lines(v));
        Assert.Equal(("نقل و انتقال بانکی (پایا) – شماره 1", D("1405/08/29"), true), (v.Operation, v.Date, v.IsComplete));
        Assert.Contains("TRK-1", v.Lines[0].Description, StringComparison.Ordinal);
    }

    [Fact]
    public void The_payer_of_a_transfer_cheque_debits_funds_in_transit_and_credits_the_source_bank()
    {
        // «سند حسابداری انتقال دهنده (بدهکار: وجوه در راه، بستانکار: بانک انتقال دهنده)» — a cheque of the day.
        var (t, _) = ChequeTransfer(due: "1405/08/29");
        var v = TransferVouchers.ForChequeIssued(t, new LedgerSide("1102", "600001"), Template());
        Assert.Equal([(true, "1109", null, 7_000_000_000m), (false, "1102", "600001", 7_000_000_000m)], Lines(v));
        Assert.Equal((D("1405/08/29"), true), (v.Date, v.IsComplete));
        Assert.Null(TransferVouchers.ForChequeMatured(t, new LedgerSide("1102", "600001"), Template())); // nothing to mature
        Assert.Equal("Transfer.NotPostDated", Refused(() => t.RecordMatured(D("1405/08/29"), User)));

        var missing = TransferVouchers.ForChequeIssued(t, new LedgerSide("1102", "600001"), Template(inTransit: false));
        Assert.Equal((false, (FixedAccount?)FixedAccount.FundsInTransit), (missing.IsComplete, missing.Lines[0].Missing));
    }

    [Fact]
    public void The_receiver_of_a_transfer_cheque_debits_cheques_in_the_cash_desk_and_credits_funds_in_transit()
    {
        // «سند حسابداری انتقال گیرنده در دریافت (بدهکار: چک نزد صندوق، بستانکار: وجوه در راه)»
        var (t, issued) = ChequeTransfer();
        Assert.Null(TransferVouchers.ForChequeReceived(t, "600001", Template())); // not on the receipts side yet
        var cheque = Receive(t, issued, on: "1405/09/01");
        var v = TransferVouchers.ForChequeReceived(t, cheque.Counterparty.DetailCode, Template());
        Assert.Equal([(true, "1201", "600001", 7_000_000_000m), (false, "1109", null, 7_000_000_000m)], Lines(v));
        Assert.Equal(D("1405/09/01"), v!.Date);

        // Then, as for every received cheque: to the bank, and collected into the destination bank.
        cheque.RegisterSayad(D("1405/09/01"), User);
        cheque.DepositForCollection(new CollectionAccount(t.Destination.Id, "0202-1", "IRR", true), D("1405/09/02"), User);
        cheque.RecordCleared(D("1405/10/15"), User);
        var destination = new LedgerSide("1103", "600003");
        Assert.Equal([(true, "1202", "600003", 7_000_000_000m), (false, "1201", "600001", 7_000_000_000m)], Lines(ReceiptVouchers.ForStep(cheque, 3, destination, null, Template())));
        Assert.Equal([(true, "1103", "600003", 7_000_000_000m), (false, "1202", "600003", 7_000_000_000m)], Lines(ReceiptVouchers.ForStep(cheque, 4, destination, null, Template())));
    }

    // ----- the owner's answers of 21 Nov 2026 to Q76–Q82 ----------------------------------------------------------------------------

    [Fact]
    public void Every_transfer_pays_an_approved_payment_request_written_for_its_two_accounts_Q82()
    {
        // «برای نقل و انتقال از هر نوع لازم است درخواست وجه صادر و مجوز آن اخذ شود»
        var form = Form(TransferMethod.Paya, Milliard);
        var request = form.Request!;
        var t = Record(form);
        Assert.Equal((request.Id, 12, $"transfer:{t.Id}:1"), (t.Request!.Id, t.Request.Number, t.Request.PaymentKey));

        Assert.Equal("Transfer.RequestRequired", Refused(() => Record(form with { Request = null })));
        Assert.Equal("Transfer.RequestNotApproved", Refused(() => Record(form with { Request = request with { IsApproved = false } })));
        Assert.Equal("Transfer.RequestNotForTransfer", Refused(() => Record(form with { Request = request with { IsForOwnTransfer = false } })));
        Assert.Equal("Transfer.RequestOtherAccounts", Refused(() => Record(form with { Request = Request(MelliCurrent, MellatCurrent) }))); // the other way round
        Assert.Equal("Transfer.RequestOverpaid", Refused(() => Record(form with { Request = request with { RemainingAmount = Milliard - 1 } })));
        Record(form with { Request = request with { RemainingAmount = Milliard } }); // in full; a part is allowed too (Q29)

        // The cheque needs its request as well.
        var id = Guid.NewGuid();
        var issued = Instrument.PrepareIssue(Guid.NewGuid(), InstrumentKind.OrdinaryCheque, Money.Rials(1_000), D("1405/08/29"), D("1405/08/29"),
            new CounterpartyRef(MelliCurrent.Id, "حساب"), "نقل و انتقال", MellatCurrent.Id, id, User);
        issued.Issue(Book, "000509", D("1405/08/29"), User);
        Assert.Equal("Transfer.RequestRequired",
            Refused(() => BankTransfer.IssueCheque(id, 5, D("1405/08/29"), MellatCurrent, MelliCurrent, issued, Book, 509, SayadId, null, User, null)));

        // A correction keeps or changes the request and takes a new payment key.
        t.CorrectElectronic(form with { Amount = Money.Rials(2 * Milliard) }, TransferLimits.OwnerDefaults(), TransferDayUsage.None, User, false);
        Assert.Equal($"transfer:{t.Id}:1", t.Request.PaymentKey); // the version changes when the transfer is saved; see the use-case tests
    }

    [Fact]
    public void The_bank_fee_of_an_electronic_transfer_is_debited_to_the_fee_account_and_credited_to_the_source_bank_Q77()
    {
        // «از بابت هزینه‌های کارمزد خدمات بانکی در هنگام صدور سند آن را بدهکار نمود»
        var t = Record(Form(TransferMethod.Paya, Milliard) with { FeeRial = 35_000 });
        Assert.Equal(35_000m, t.FeeRial);
        var source = new LedgerSide("1102", "600001");
        var v = TransferVouchers.ForElectronic(t, source, new LedgerSide("1103", "600003"), Template());
        Assert.Equal([(true, "1103", "600003", Milliard), (false, "1102", "600001", Milliard), (true, "8205", null, 35_000m), (false, "1102", "600001", 35_000m)], Lines(v));
        Assert.True(v.IsComplete);

        var noAccount = TransferVouchers.ForElectronic(t, source, new LedgerSide("1103", "600003"), new VoucherTemplate());
        Assert.Equal((false, (FixedAccount?)FixedAccount.BankFees), (noAccount.IsComplete, noAccount.Lines[2].Missing));

        Assert.Null(Record(Form(TransferMethod.Paya, Milliard) with { FeeRial = 0 }).FeeRial); // no fee, no lines
        Assert.Equal(2, TransferVouchers.ForElectronic(Record(Form(TransferMethod.Paya, Milliard)), source, source, Template()).Lines.Count);
        Assert.Equal("Transfer.FeeInvalid", Refused(() => Record(Form(TransferMethod.Paya, Milliard) with { FeeRial = -1 })));
        Assert.Equal("Transfer.FeeInvalid", Refused(() => Record(Form(TransferMethod.Paya, Milliard) with { FeeRial = 10.5m })));
    }

    [Fact]
    public void A_post_dated_transfer_cheque_is_credited_to_notes_payable_until_its_maturity_is_recorded_Q81()
    {
        // «حساب اسناد پرداختنی بستانکار تا سررسید چک … به محض سررسید شدن از حساب بانک خارج و سند حسابداری مربوط … صادر می‌گردد»
        var (t, issued) = ChequeTransfer(); // issued 1405/08/29, due 1405/10/15
        var source = new LedgerSide("1102", "600001");
        Assert.True(t.IsPostDated);
        Assert.Equal([(true, "1109", null, 7_000_000_000m), (false, "2105", "600001", 7_000_000_000m)], Lines(TransferVouchers.ForChequeIssued(t, source, Template())));
        Assert.Null(TransferVouchers.ForChequeMatured(t, source, Template()));

        Assert.Equal("Transfer.MaturedBeforeDueDate", Refused(() => t.RecordMatured(D("1405/10/14"), User)));
        t.RecordMatured(D("1405/10/15"), User);
        issued.RecordMatured(D("1405/10/15"), User);
        Assert.Equal((D("1405/10/15"), true), (t.Cheque!.MaturedOn!.Value, issued.IsMatured));
        Assert.IsType<TransferChequeMatured>(t.PendingEvents[^1]);
        var matured = TransferVouchers.ForChequeMatured(t, source, Template());
        Assert.Equal([(true, "2105", "600001", 7_000_000_000m), (false, "1102", "600001", 7_000_000_000m)], Lines(matured));
        Assert.Equal(D("1405/10/15"), matured!.Date);
        Assert.Equal("Transfer.AlreadyMatured", Refused(() => t.RecordMatured(D("1405/10/16"), User)));

        // Without the account in the template the voucher names what is missing.
        var missing = TransferVouchers.ForChequeIssued(t, source, new VoucherTemplate());
        Assert.Equal((FixedAccount?)FixedAccount.NotesPayable, missing.Lines[1].Missing);
    }

    [Fact]
    public void The_receipts_side_registers_the_cheque_in_sayad_only_after_the_payer_did_Q79()
    {
        var (t, _) = ChequeTransfer(sayadRegistered: false);
        Assert.Null(t.Cheque!.PayerSayadOn);
        Assert.Equal("Transfer.PayerSayadRequired", Refused(t.EnsureReceiverMayRegisterSayad));
        Assert.Equal("Transfer.StepBeforeIssue", Refused(() => t.RegisterPayerSayad(D("1405/08/28"), User)));
        t.RegisterPayerSayad(D("1405/08/30"), User);
        Assert.Equal(D("1405/08/30"), t.Cheque.PayerSayadOn);
        Assert.IsType<TransferChequeSayadRegistered>(t.PendingEvents[^1]);
        t.EnsureReceiverMayRegisterSayad();
        Assert.Equal("Transfer.PayerSayadAlreadyRegistered", Refused(() => t.RegisterPayerSayad(D("1405/08/30"), User)));

        // Ticked on the form: registered on the day of issue.
        Assert.Equal(D("1405/08/29"), ChequeTransfer().Transfer.Cheque!.PayerSayadOn);
    }

    [Fact]
    public void A_transfer_cheque_not_yet_registered_in_sayad_can_be_cancelled_and_its_vouchers_are_taken_back_Q79()
    {
        // «آن چک اگر ثبت صیادی پرداخت کننده نشده باشد طبعاً ثبت صیادی دریافت کننده هم نشده است و می‌توان آن را باطل نمود»
        var (t, issued) = ChequeTransfer(sayadRegistered: false);
        var received = Receive(t, issued, on: "1405/08/30");
        Assert.Null(t.WhyNotCancellable());
        Assert.Equal("Transfer.CancelReasonRequired", Refused(() => t.CancelCheque(D("1405/09/01"), " ", User)));
        Assert.Equal("Transfer.StepBeforeIssue", Refused(() => t.CancelCheque(D("1405/08/29"), "انصراف", User))); // before it was received

        t.CancelCheque(D("1405/09/01"), "انصراف از انتقال", User);
        issued.Cancel(D("1405/09/01"), User, "انصراف از انتقال");
        received.CancelTransferCheque(t.Id, D("1405/09/01"), User, "انصراف از انتقال");
        Assert.Equal((TransferState.Cancelled, D("1405/09/01"), "انصراف از انتقال"), (t.State, t.Cheque!.CancelledOn!.Value, t.Cheque.CancelReason));
        Assert.Equal((InstrumentState.Cancelled, InstrumentState.Cancelled), (issued.State, received.State));
        Assert.IsType<TransferChequeCancelled>(t.PendingEvents[^1]);
        Assert.Equal("Transfer.ChequeCancelled", t.WhyNotDeletable(false));
        Assert.Equal("Transfer.ChequeNotOpen", Refused(() => t.CancelCheque(D("1405/09/02"), "دوباره", User)));
        Assert.Empty(received.AllowedActions()); // no «تعویض»: a new transfer is a new, independent cheque

        // The two vouchers are taken back: the payer's (post-dated, not matured: «اسناد پرداختنی») and the receiver's.
        var source = new LedgerSide("1102", "600001");
        Assert.Equal([(true, "2105", "600001", 7_000_000_000m), (false, "1109", null, 7_000_000_000m)], Lines(TransferVouchers.ForChequeCancelledByPayer(t, source, Template())));
        Assert.Equal([(true, "1109", null, 7_000_000_000m), (false, "1201", "600001", 7_000_000_000m)],
            Lines(TransferVouchers.ForChequeCancelledByReceiver(t, received.Counterparty.DetailCode, Template())));

        // A cheque of the day that never reached the receipts side: only the payer's side, against the bank.
        var (day, _) = ChequeTransfer(due: "1405/08/29", sayadRegistered: false);
        day.CancelCheque(D("1405/08/29"), "اشتباه در مبلغ", User);
        Assert.Equal([(true, "1102", "600001", 7_000_000_000m), (false, "1109", null, 7_000_000_000m)], Lines(TransferVouchers.ForChequeCancelledByPayer(day, source, Template())));
        Assert.Null(TransferVouchers.ForChequeCancelledByReceiver(day, "600001", Template()));
    }

    [Fact]
    public void A_transfer_cheque_registered_in_sayad_is_not_cancelled_Q79()
    {
        var (t, issued) = ChequeTransfer(); // registered as it was issued
        Assert.Equal("Transfer.SayadRegisteredNotCancelled", t.WhyNotCancellable());
        Assert.Equal("Transfer.SayadRegisteredNotCancelled", Refused(() => t.CancelCheque(D("1405/08/30"), "انصراف", User)));

        // And the received cheque is cancelled only through its transfer, never on the cheque screens.
        var received = Receive(t, issued);
        Assert.Equal("Instrument.NotForTransferCheque", Refused(() => received.CancelReceived(D("1405/08/30"), User, "M-1", D("1405/08/30"), "اشتباه")));
        Assert.Equal("Transfer.ChequeMismatch", Refused(() => received.CancelTransferCheque(Guid.NewGuid(), D("1405/08/30"), User, "اشتباه")));
        Assert.Equal("Transfer.ChequeMismatch", Refused(() => ReceivedCheque().CancelTransferCheque(t.Id, D("1405/08/30"), User, "اشتباه")));
    }

    [Fact]
    public void The_fee_line_names_a_detail_only_when_the_fee_account_takes_one_Q83()
    {
        // Owner, 21 Nov 2026: «هزینه کارمزد خدمات بانکی غالباً فاقد حساب تفصیل است ولیکن در برخی از شرکت‌ها یک مرکز هزینه عمومی امور مالی برای
        // آن ایجاد می‌شود … اگر … خصیصه تفصیل دارد انتخاب شده باشد در هنگام تعیین سند … درخواست می‌گردد».
        var withDetail = new FeeAccountFact(["مراکز هزینه"]);
        var centre = new FeeDetailFact("810001", "مرکز هزینه امور مالی", "مراکز هزینه", true);
        var form = Form(TransferMethod.Paya, Milliard) with { FeeRial = 35_000, FeeAccount = withDetail, FeeDetail = centre };
        var t = Record(form);
        Assert.Equal("810001", t.FeeDetailCode);
        var v = TransferVouchers.ForElectronic(t, new LedgerSide("1102", "600001"), new LedgerSide("1103", "600003"), Template());
        Assert.Equal((true, "8205", "810001", 35_000m), Lines(v)[2]);

        Assert.Equal("Transfer.FeeDetailRequired", Refused(() => Record(form with { FeeDetail = null })));
        Assert.Equal("Transfer.FeeDetailInactive", Refused(() => Record(form with { FeeDetail = centre with { IsActive = false } })));
        Assert.Equal("Transfer.FeeDetailTypeNotAllowed", Refused(() => Record(form with { FeeDetail = centre with { Type = "مشتریان" } })));

        // An account without تفصیل: none is asked, and one that was sent is not kept. Without a fee nothing is asked either.
        Assert.Null(Record(form with { FeeAccount = FeeAccountFact.WithoutDetail }).FeeDetailCode);
        Assert.Null(Record(form with { FeeAccount = null }).FeeDetailCode);
        Assert.Null(Record(form with { FeeRial = null, FeeDetail = null }).FeeDetailCode);
    }
}
