using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Transfers;

namespace Treasury.Cashflow.Domain.Vouchers;

/// <summary>
/// Vouchers of a transfer between two bank accounts of the company (owner's report no. 7 of 20 Nov 2026 and his answers to Q76–Q82 of
/// 21 Nov 2026).
/// By cheque, as the owner wrote them: the payer's side «بدهکار: وجوه در راه / بستانکار: بانک انتقال دهنده» — for a post-dated cheque
/// «بستانکار: اسناد پرداختنی» until its due date, and at maturity «بدهکار: اسناد پرداختنی / بستانکار: بانک» (Q81) —, the receiver's side
/// «بدهکار: چک نزد صندوق / بستانکار: وجوه در راه»; the later steps of the cheque (واگذاری به بانک, وصول) have the vouchers of every
/// received cheque, so in the end the destination bank is debited. Electronic: the destination bank is debited and the source bank
/// credited directly (Q76), with the bank's fee debited to «هزینه کارمزد خدمات بانکی» (Q77).
/// </summary>
public static class TransferVouchers
{
    /// <summary>Step of the payer's voucher (and of the one voucher of an electronic transfer).</summary>
    public const int PayerStep = 0;

    /// <summary>Step of the receiver's voucher of a cheque transfer.</summary>
    public const int ReceiverStep = 1;

    /// <summary>Step of the maturity voucher of a post-dated transfer cheque.</summary>
    public const int MaturityStep = 2;

    /// <summary>Steps of the two vouchers of a cancelled transfer cheque: the payer's side and the receiver's side.</summary>
    public const int PayerCancelStep = 3;

    public const int ReceiverCancelStep = 4;

    public static readonly IReadOnlyList<int> Steps = [PayerStep, ReceiverStep, MaturityStep, PayerCancelStep, ReceiverCancelStep];

    /// <summary>
    /// «نقل و انتقال بانکی» by an electronic way (confirmed by the owner, Q76): Dr destination bank (its معین and تفصیل) / Cr source bank
    /// (its معین and تفصیل). With a fee (Q77): also Dr «هزینه کارمزد خدمات بانکی» — with its تفصیل when that account takes one (Q83) — /
    /// Cr source bank.
    /// </summary>
    public static Voucher ForElectronic(BankTransfer transfer, LedgerSide source, LedgerSide destination, VoucherTemplate template)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(template);
        var (foreign, currency) = Foreign(transfer);
        var text = $"{OperationName(transfer.Method)} از {transfer.Source.Title} به {transfer.Destination.Title}{Tracking(transfer)}{Note(transfer)}";
        var lines = new List<VoucherLine>
        {
            new(true, destination.AccountCode, destination.DetailCode, transfer.Amount.ToRial(), text, null, foreign, currency),
            new(false, source.AccountCode, source.DetailCode, transfer.Amount.ToRial(), text, null, foreign, currency),
        };
        if (transfer.FeeRial is { } fee)
        {
            var feeText = $"کارمزد {text}";
            lines.Add(Fixed(true, template, FixedAccount.BankFees, transfer.FeeDetailCode, fee, feeText, null, null));
            lines.Add(new VoucherLine(false, source.AccountCode, source.DetailCode, fee, feeText));
        }

        return new Voucher($"{OperationName(transfer.Method)} – شماره {transfer.Number}", transfer.Date, lines);
    }

    /// <summary>
    /// The payer's side of a cheque transfer («انتقال دهنده»), on the day the cheque was issued: Dr «وجوه در راه» / Cr the source bank
    /// (its معین and تفصیل) for a cheque of the day; for a post-dated cheque Cr «اسناد پرداختنی» with the source bank account's تفصیل
    /// (owner's answer to Q81).
    /// </summary>
    public static Voucher ForChequeIssued(BankTransfer transfer, LedgerSide source, VoucherTemplate template)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(template);
        var (foreign, currency) = Foreign(transfer);
        var text = $"صدور {ChequeName(transfer)} – انتقال به {transfer.Destination.Title}{Note(transfer)}";
        return new Voucher($"پرداخت چک – نقل و انتقال (انتقال دهنده) – شماره {transfer.Number}", transfer.Date,
        [
            Fixed(true, template, FixedAccount.FundsInTransit, null, transfer.Amount.ToRial(), text, foreign, currency),
            transfer.IsPostDated
                ? Fixed(false, template, FixedAccount.NotesPayable, source.DetailCode, transfer.Amount.ToRial(), text, foreign, currency)
                : new VoucherLine(false, source.AccountCode, source.DetailCode, transfer.Amount.ToRial(), text, null, foreign, currency),
        ]);
    }

    /// <summary>
    /// «سررسید چک پرداختنی» (owner's answer to Q81): at maturity the post-dated cheque leaves «اسناد پرداختنی» for the bank — Dr «اسناد
    /// پرداختنی» / Cr the source bank, both with the source bank account's تفصیل. Null until the maturity is recorded.
    /// </summary>
    public static Voucher? ForChequeMatured(BankTransfer transfer, LedgerSide source, VoucherTemplate template)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(template);
        if (transfer.Cheque?.MaturedOn is not { } maturedOn)
        {
            return null;
        }

        var (foreign, currency) = Foreign(transfer);
        var text = $"سررسید {ChequeName(transfer)} – انتقال به {transfer.Destination.Title}";
        return new Voucher($"سررسید چک پرداختنی – نقل و انتقال – شماره {transfer.Number}", maturedOn,
        [
            Fixed(true, template, FixedAccount.NotesPayable, source.DetailCode, transfer.Amount.ToRial(), text, foreign, currency),
            new VoucherLine(false, source.AccountCode, source.DetailCode, transfer.Amount.ToRial(), text, null, foreign, currency),
        ]);
    }

    /// <summary>
    /// The receiver's side of a cheque transfer («انتقال گیرنده»): Dr «اسناد نزد صندوق» with the source account's تفصیل — the drawer of
    /// the cheque (confirmed by the owner, Q78) — / Cr «وجوه در راه», on the day the cheque was received.
    /// </summary>
    /// <param name="drawerDetailCode">The تفصیل kept on the received cheque (the source bank account's).</param>
    public static Voucher? ForChequeReceived(BankTransfer transfer, string? drawerDetailCode, VoucherTemplate template)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentNullException.ThrowIfNull(template);
        if (transfer.Cheque?.ReceivedOn is not { } receivedOn)
        {
            return null;
        }

        var (foreign, currency) = Foreign(transfer);
        var text = $"دریافت {ChequeName(transfer)} – انتقال از {transfer.Source.Title}{Note(transfer)}";
        return new Voucher($"دریافت چک – نقل و انتقال (انتقال گیرنده) – شماره {transfer.Number}", receivedOn,
        [
            Fixed(true, template, FixedAccount.ChequesInCashDesk, drawerDetailCode, transfer.Amount.ToRial(), text, foreign, currency),
            Fixed(false, template, FixedAccount.FundsInTransit, null, transfer.Amount.ToRial(), text, foreign, currency),
        ]);
    }

    /// <summary>
    /// The payer's side of a cancelled transfer cheque (answer to Q79), on the day of the cancellation: it takes the payer's voucher
    /// back — Dr what that voucher credited («اسناد پرداختنی» for a post-dated cheque not yet matured, otherwise the source bank) / Cr
    /// «وجوه در راه». Null while the cheque is not cancelled.
    /// </summary>
    public static Voucher? ForChequeCancelledByPayer(BankTransfer transfer, LedgerSide source, VoucherTemplate template)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(template);
        if (transfer.Cheque?.CancelledOn is not { } cancelledOn)
        {
            return null;
        }

        var (foreign, currency) = Foreign(transfer);
        var text = $"ابطال {ChequeName(transfer)} – {transfer.Cheque.CancelReason}";
        return new Voucher($"ابطال چک نقل و انتقال (انتقال دهنده) – شماره {transfer.Number}", cancelledOn,
        [
            transfer.IsPostDated && transfer.Cheque.MaturedOn is null
                ? Fixed(true, template, FixedAccount.NotesPayable, source.DetailCode, transfer.Amount.ToRial(), text, foreign, currency)
                : new VoucherLine(true, source.AccountCode, source.DetailCode, transfer.Amount.ToRial(), text, null, foreign, currency),
            Fixed(false, template, FixedAccount.FundsInTransit, null, transfer.Amount.ToRial(), text, foreign, currency),
        ]);
    }

    /// <summary>
    /// The receiver's side of a cancelled transfer cheque that was already on the receipts side: Dr «وجوه در راه» / Cr «اسناد نزد صندوق»
    /// with the source account's تفصیل. Null when the cheque is not cancelled or never reached the receipts side.
    /// </summary>
    public static Voucher? ForChequeCancelledByReceiver(BankTransfer transfer, string? drawerDetailCode, VoucherTemplate template)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentNullException.ThrowIfNull(template);
        if (transfer.Cheque is not { CancelledOn: { } cancelledOn, ReceivedOn: not null })
        {
            return null;
        }

        var (foreign, currency) = Foreign(transfer);
        var text = $"ابطال {ChequeName(transfer)} – {transfer.Cheque.CancelReason}";
        return new Voucher($"ابطال چک نقل و انتقال (انتقال گیرنده) – شماره {transfer.Number}", cancelledOn,
        [
            Fixed(true, template, FixedAccount.FundsInTransit, null, transfer.Amount.ToRial(), text, foreign, currency),
            Fixed(false, template, FixedAccount.ChequesInCashDesk, drawerDetailCode, transfer.Amount.ToRial(), text, foreign, currency),
        ]);
    }

    public static string OperationName(TransferMethod method) => method switch
    {
        TransferMethod.Internal => "نقل و انتقال بانکی (انتقال داخلی)",
        TransferMethod.Pol => "نقل و انتقال بانکی (پل)",
        TransferMethod.Paya => "نقل و انتقال بانکی (پایا)",
        TransferMethod.Satna => "نقل و انتقال بانکی (ساتنا)",
        _ => "نقل و انتقال بانکی با چک",
    };

    private static VoucherLine Fixed(bool debit, VoucherTemplate template, FixedAccount account, string? detail, decimal amount, string text, decimal? foreign, string? currency) =>
        template.CodeOf(account) is { } code
            ? new VoucherLine(debit, code, detail, amount, text, null, foreign, currency)
            : new VoucherLine(debit, null, detail, amount, text, account, foreign, currency);

    private static (decimal? Amount, string? Currency) Foreign(BankTransfer t) => t.Amount.IsRial ? (null, null) : (t.Amount.Amount, t.Amount.Currency);

    private static string ChequeName(BankTransfer t) =>
        $"چک {t.Cheque?.Number} {t.Source.BankName} سررسید {t.Cheque?.DueDate.ToJalaliString()}";

    private static string Tracking(BankTransfer t) => t.TrackingNumber is null ? string.Empty : $" – پیگیری {t.TrackingNumber}";

    private static string Note(BankTransfer t) => t.Description is null ? string.Empty : $" – {t.Description}";
}
