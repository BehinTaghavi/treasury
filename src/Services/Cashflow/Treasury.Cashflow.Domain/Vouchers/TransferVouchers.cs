using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Transfers;

namespace Treasury.Cashflow.Domain.Vouchers;

/// <summary>
/// Vouchers of a transfer between two bank accounts of the company (owner's report no. 7, 20 Nov 2026).
/// By cheque, as the owner wrote them: the payer's side «بدهکار: وجوه در راه / بستانکار: بانک انتقال دهنده», the receiver's side
/// «بدهکار: چک نزد صندوق / بستانکار: وجوه در راه»; the later steps of the cheque (واگذاری به بانک, وصول) have the vouchers of every
/// received cheque, so in the end the destination bank is debited. Electronic: the money moves at once, so the destination bank is
/// debited and the source bank credited directly (proposed; open question Q76).
/// </summary>
public static class TransferVouchers
{
    /// <summary>Step of the payer's voucher (and of the one voucher of an electronic transfer).</summary>
    public const int PayerStep = 0;

    /// <summary>Step of the receiver's voucher of a cheque transfer.</summary>
    public const int ReceiverStep = 1;

    /// <summary>«نقل و انتقال بانکی» by an electronic way: Dr destination bank (its معین and تفصیل) / Cr source bank (its معین and تفصیل).</summary>
    public static Voucher ForElectronic(BankTransfer transfer, LedgerSide source, LedgerSide destination)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        var (foreign, currency) = Foreign(transfer);
        var text = $"{Title(transfer)} از {transfer.Source.Title} به {transfer.Destination.Title}{Tracking(transfer)}{Note(transfer)}";
        return new Voucher($"{OperationName(transfer.Method)} – شماره {transfer.Number}", transfer.Date,
        [
            new VoucherLine(true, destination.AccountCode, destination.DetailCode, transfer.Amount.ToRial(), text, null, foreign, currency),
            new VoucherLine(false, source.AccountCode, source.DetailCode, transfer.Amount.ToRial(), text, null, foreign, currency),
        ]);
    }

    /// <summary>
    /// The payer's side of a cheque transfer («انتقال دهنده»): Dr «وجوه در راه» / Cr the source bank (its معین and تفصیل), on the day
    /// the cheque was issued.
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
            InTransit(true, template, transfer.Amount.ToRial(), text, foreign, currency),
            new VoucherLine(false, source.AccountCode, source.DetailCode, transfer.Amount.ToRial(), text, null, foreign, currency),
        ]);
    }

    /// <summary>
    /// The receiver's side of a cheque transfer («انتقال گیرنده»): Dr «اسناد نزد صندوق» with the source account's تفصیل — the drawer of
    /// the cheque, as the payer's تفصیل is on every received cheque — / Cr «وجوه در راه», on the day the cheque was received.
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
        var cashDesk = template.CodeOf(FixedAccount.ChequesInCashDesk);
        return new Voucher($"دریافت چک – نقل و انتقال (انتقال گیرنده) – شماره {transfer.Number}", receivedOn,
        [
            new VoucherLine(true, cashDesk, drawerDetailCode, transfer.Amount.ToRial(), text, cashDesk is null ? FixedAccount.ChequesInCashDesk : null, foreign, currency),
            InTransit(false, template, transfer.Amount.ToRial(), text, foreign, currency),
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

    private static VoucherLine InTransit(bool debit, VoucherTemplate template, decimal amount, string text, decimal? foreign, string? currency) =>
        template.CodeOf(FixedAccount.FundsInTransit) is { } code
            ? new VoucherLine(debit, code, null, amount, text, null, foreign, currency)
            : new VoucherLine(debit, null, null, amount, text, FixedAccount.FundsInTransit, foreign, currency);

    private static (decimal? Amount, string? Currency) Foreign(BankTransfer t) => t.Amount.IsRial ? (null, null) : (t.Amount.Amount, t.Amount.Currency);

    private static string Title(BankTransfer t) => OperationName(t.Method);

    private static string ChequeName(BankTransfer t) =>
        $"چک {t.Cheque?.Number} {t.Source.BankName} سررسید {t.Cheque?.DueDate.ToJalaliString()}";

    private static string Tracking(BankTransfer t) => t.TrackingNumber is null ? string.Empty : $" – پیگیری {t.TrackingNumber}";

    private static string Note(BankTransfer t) => t.Description is null ? string.Empty : $" – {t.Description}";
}
