using System.Security.Claims;
using Treasury.BuildingBlocks.Application;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.ChequeBooks;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Application.Receipts;
using Treasury.Cashflow.Application.Transfers;
using Treasury.Cashflow.Domain.Transfers;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Infrastructure.InMemory;

namespace Treasury.Cashflow.Api;

public sealed record ReceiveRequest(InstrumentKind Kind, string Number, string BankName, MoneyInput Amount, string ReceivedOn, string? DueDate, Guid DrawerId, string DrawerName, string Purpose);

/// <param name="Reference">Bounce notice no., management's order no. (cancellation) or bank reference.</param>
/// <param name="DocumentDate">Cancellation of a received cheque: the date of management's order. Legal action: the date of the certificate of non-payment.</param>
/// <param name="TrackingCode">Bounce and legal action: the tracking code of the certificate of non-payment.</param>
/// <param name="OriginalStamped">Cancellation of a received cheque: «اصل چک ممهور به مهر ابطال شد» was ticked (answer to Q62).</param>
public sealed record ActionRequest(string Date, string? Destination, string? Note, string? Reference, string? DocumentDate = null, string? TrackingCode = null,
    bool OriginalStamped = false);

public sealed record ReplaceRequest(ReceiptHeaderInput Header, IReadOnlyList<ReceivedInstrumentInput> Instruments);

public sealed record EndorseRequest(string Date, Guid PaymentRequestId, string SayadTracking, string? Note, IReadOnlyList<Guid>? SignerIds = null);

public sealed record EndorsementReturnedRequest(string Date, string? NoticeNumber, string? Reason, string? TrackingCode = null);

public sealed record DepositRequest(Guid BankAccountId, string Date, string? SlipNumber);

public sealed record TemplateAccountRequest(string? Code);

public sealed record ChequeBookRequest(Guid BankAccountId, string Serial, long FirstSheet, long LastSheet, Guid? FormatId = null, string? SayadMiddle = null, string? SayadFirstSerial = null, bool PartlyUsed = false, int? SerialDigits = null);

public sealed record ChequeBookCorrection(string Serial, long FirstSheet, long LastSheet, long Version, Guid? FormatId = null, string? SayadMiddle = null, string? SayadFirstSerial = null, bool? PartlyUsed = null, int? SerialDigits = null);

/// <param name="BankAccountId">Null: the standard cheque type for all accounts (owner's report no. 3, item 3).</param>
public sealed record ChequeFormatRequest(Guid? BankAccountId, Treasury.Cashflow.Domain.ChequeBooks.ChequePrintType PrintType);

public sealed record ChequeFormatChange(Treasury.Cashflow.Domain.ChequeBooks.ChequePrintType PrintType, long Version);

public sealed record ChequeBookVersion(long Version);

public sealed record IssueRequest(Guid PaymentId, Guid BankAccountId, Guid ChequeBookId, long? SheetNumber, InstrumentKind Kind, MoneyInput Amount, string IssueDate, string? DueDate, Guid PayeeId, string PayeeName, string Purpose);

public sealed record CashReceiptRequest(ReceiptHeaderInput Header, Guid CashDeskId, decimal Amount, decimal? RateToRial);

/// <param name="PayerSheba">Paya and Satna transfers: the Sheba the money came from.</param>
public sealed record BankReceiptRequest(ReceiptHeaderInput Header, Guid BankAccountId, DepositMethod Method, string? TrackingNumber, decimal Amount, decimal? RateToRial,
    string? PayerSheba = null);

/// <summary>«واریز به کارت بانک»: <paramref name="CardKind"/> is PettyCashCard (then <paramref name="CardId"/> is the petty cash) or CompanyCard.</summary>
public sealed record CardReceiptRequest(ReceiptHeaderInput Header, ReceiptAccountKind CardKind, Guid CardId, DepositMethod Method, string? TrackingNumber, decimal Amount);

public sealed record InstrumentReceiptRequest(ReceiptHeaderInput Header, IReadOnlyList<ReceivedInstrumentInput> Instruments);

/// <summary>The note receipt form (owner's form «فرم دریافت سفته», 16 Nov 2026).</summary>
public sealed record NoteReceiptRequest(
    ReceiptHeaderInput Header, NoteTermsInput Terms, IReadOnlyList<NoteLineInput> Notes, IReadOnlyList<NoteGuarantorInput>? Guarantors = null, IReadOnlyList<Guid>? SignerIds = null);

public sealed record CancelReceiptRequest(string Date, string Reason, long Version);

/// <summary>Deletion («حذف») of a receipt recorded by mistake, with the version the user saw (owner's answer to Q59).</summary>
public sealed record DeleteReceiptRequest(long Version);

/// <summary>
/// Correction («ویرایش») of a saved receipt (owner's report no. 6, items 2 and 6): the receipt form as it now is, with the version the
/// user saw. One body per kind of receipt, as for recording.
/// </summary>
public sealed record CashReceiptCorrection(ReceiptHeaderInput Header, Guid CashDeskId, decimal Amount, decimal? RateToRial, long Version);

/// <inheritdoc cref="CashReceiptCorrection"/>
public sealed record BankReceiptCorrection(ReceiptHeaderInput Header, Guid BankAccountId, DepositMethod Method, string? TrackingNumber, decimal Amount, decimal? RateToRial,
    long Version, string? PayerSheba = null);

/// <inheritdoc cref="CashReceiptCorrection"/>
public sealed record CardReceiptCorrection(ReceiptHeaderInput Header, ReceiptAccountKind CardKind, Guid CardId, DepositMethod Method, string? TrackingNumber, decimal Amount, long Version);

/// <summary>A line with an id corrects that cheque, a line without one adds a cheque, a cheque left out is taken off the receipt.</summary>
public sealed record InstrumentReceiptCorrection(ReceiptHeaderInput Header, IReadOnlyList<ReceivedInstrumentInput> Instruments, long Version);

/// <inheritdoc cref="InstrumentReceiptCorrection"/>
public sealed record NoteReceiptCorrection(
    ReceiptHeaderInput Header, NoteTermsInput Terms, IReadOnlyList<NoteLineInput> Notes, long Version, IReadOnlyList<NoteGuarantorInput>? Guarantors = null,
    IReadOnlyList<Guid>? SignerIds = null);

/// <summary>Correction of an electronic transfer, with the version the user saw.</summary>
public sealed record ElectronicTransferCorrection(ElectronicTransferInput Form, long Version);

/// <summary>Deletion of a transfer or taking back its receipts-side record, with the version the user saw.</summary>
public sealed record TransferVersion(long Version);

/// <summary>A dated step of a transfer's cheque on the payment side («ثبت صیادی پرداخت», «سررسید»); empty date: today.</summary>
public sealed record TransferChequeStepRequest(long Version, string? Date = null);

/// <summary>«ابطال چک نقل و انتقال» with its reason; empty date: today.</summary>
public sealed record CancelTransferChequeRequest(long Version, string? Date, string? Reason);

/// <param name="Date">The day the cheque is received; empty: the day it was issued.</param>
public sealed record ReceiveTransferChequeRequest(long Version, string? Date = null);

/// <summary>The limits of one electronic way of transfer, for every bank or (with <paramref name="BankId"/>) for one bank; empty: no limit.</summary>
public sealed record TransferLimitRequest(
    Guid? BankId, decimal? MinPerOrder, decimal? MaxPerOrderInPerson, decimal? MaxPerOrderRemote, decimal? MaxPerDayInPerson, decimal? MaxPerDayRemote, int? MaxCountPerDay);

public static class Endpoints
{
    private const string IdempotencyHeader = "Idempotency-Key";

    public static void MapInstrumentEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/cashflow");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        api.MapGet("/instruments/{id:guid}", async (Guid id, InstrumentUseCases useCases, CancellationToken ct) =>
            await useCases.GetAsync(id, ct).ConfigureAwait(false) is { } view ? Results.Ok(view) : Results.NotFound());

        api.MapPost("/instruments/received", async (ReceiveRequest r, HttpRequest http, InstrumentUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.ReceiveAsync(
                new ReceiveInstrumentCommand(Key(http), r.Kind, r.Number, r.BankName, r.Amount, r.ReceivedOn, r.DueDate, r.DrawerId, r.DrawerName, r.Purpose), ct).ConfigureAwait(false),
                id => $"/api/cashflow/instruments/{id}"));

        // Report no. 4, item 9: received cheques by state, and depositing one at a company bank account for collection.
        api.MapGet("/instruments/received", async (InstrumentState state, InstrumentUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.ListReceivedAsync(state, ct).ConfigureAwait(false)));

        api.MapPost("/instruments/{id:guid}/deposit", async (Guid id, DepositRequest r, HttpRequest http, InstrumentUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.DepositForCollectionAsync(new DepositChequeCommand(Key(http), id, r.BankAccountId, r.Date, r.SlipNumber), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/instruments/{id}"));

        api.MapPost("/instruments/{id:guid}/actions/{action}", async (Guid id, InstrumentAction action, ActionRequest r, HttpRequest http, InstrumentUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.ChangeStateAsync(
                new ChangeInstrumentStateCommand(Key(http), id, action, r.Date, r.Destination, r.Note, r.Reference, r.DocumentDate, r.TrackingCode, r.OriginalStamped), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/instruments/{id}"));

        // Owner's item 14: replacement (a cheque receipt of the new cheques) and endorsement of a received cheque.
        api.MapPost("/instruments/{id:guid}/replace", async (Guid id, ReplaceRequest r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.ReplaceAsync(new ReplaceChequeCommand(Key(http), id, r.Header, r.Instruments), ct).ConfigureAwait(false),
                receiptId => $"/api/cashflow/receipts/{receiptId}"));

        api.MapPost("/instruments/{id:guid}/replace/voucher-preview", async (Guid id, ReplaceRequest r, ReceiptUseCases useCases, CancellationToken ct) =>
            Preview(await useCases.PreviewReplacementAsync(new ReplaceChequeCommand("preview", id, r.Header, r.Instruments), ct).ConfigureAwait(false)));

        api.MapPost("/instruments/{id:guid}/endorse", async (Guid id, EndorseRequest r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.EndorseAsync(new EndorseChequeCommand(Key(http), id, r.Date, r.PaymentRequestId, r.SayadTracking, r.Note, r.SignerIds), ct)
                .ConfigureAwait(false), _ => $"/api/cashflow/instruments/{id}"));

        api.MapPost("/instruments/{id:guid}/endorsement-returned", async (Guid id, EndorsementReturnedRequest r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.EndorsementReturnedAsync(new EndorsementReturnedCommand(Key(http), id, r.Date, r.NoticeNumber, r.Reason, r.TrackingCode), ct)
                .ConfigureAwait(false), _ => $"/api/cashflow/instruments/{id}"));

        // Legal action (owner, 18 Nov 2026): the scans of its papers. The upload is a multipart form with "file"; at most 5 MB (checked
        // again by the domain). The action itself is the step SendToLegal with its date and description.
        api.MapGet("/instruments/{id:guid}/legal-files", async (Guid id, LegalActionUseCases u, CancellationToken ct) =>
            Results.Ok(await u.ListAsync(id, ct).ConfigureAwait(false)));

        api.MapPost("/instruments/{id:guid}/legal-files", async (Guid id, IFormFile file, HttpRequest http, LegalActionUseCases u, CancellationToken ct) =>
        {
            if (file.Length > InstrumentAttachment.MaxBytes)
            {
                return ToHttp(Result<Guid>.Fail("Instrument.AttachmentTooLarge", "A scan is at most 5 MB."), _ => string.Empty);
            }

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct).ConfigureAwait(false);
            return ToHttp(await u.AttachAsync(Key(http), id, file.FileName, buffer.ToArray(), ct).ConfigureAwait(false), fileId => $"/api/cashflow/instruments/{id}/legal-files/{fileId}");
        }).DisableAntiforgery();

        api.MapGet("/instruments/{id:guid}/legal-files/{fileId:guid}", async (Guid id, Guid fileId, LegalActionUseCases u, CancellationToken ct) =>
            await u.GetAsync(id, fileId, ct).ConfigureAwait(false) is { } f ? Results.File(f.Content, f.File.ContentType, f.File.FileName) : Results.NotFound());

        api.MapPost("/instruments/{id:guid}/legal-files/{fileId:guid}/delete", async (Guid id, Guid fileId, HttpRequest http, LegalActionUseCases u, CancellationToken ct) =>
            ToHttp(await u.RemoveAsync(Key(http), id, fileId, ct).ConfigureAwait(false), _ => $"/api/cashflow/instruments/{id}/legal-files"));

        // Receipts (owner, 14 Nov 2026): cash, money paid into a bank account, cheques and notes.
        api.MapGet("/receipts", async (int year, ReceiptKind? kind, ReceiptUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.ListAsync(year, kind, ct).ConfigureAwait(false)));

        api.MapGet("/receipts/{id:guid}", async (Guid id, ReceiptUseCases useCases, CancellationToken ct) =>
            await useCases.GetAsync(id, ct).ConfigureAwait(false) is { } view ? Results.Ok(view) : Results.NotFound());

        api.MapPost("/receipts/cash", async (CashReceiptRequest r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.RecordCashAsync(new RecordCashReceiptCommand(Key(http), r.Header, r.CashDeskId, r.Amount, r.RateToRial), ct).ConfigureAwait(false),
                id => $"/api/cashflow/receipts/{id}"));

        api.MapPost("/receipts/bank", async (BankReceiptRequest r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.RecordBankDepositAsync(
                new RecordBankReceiptCommand(Key(http), r.Header, r.BankAccountId, r.Method, r.TrackingNumber, r.Amount, r.RateToRial, r.PayerSheba), ct).ConfigureAwait(false),
                id => $"/api/cashflow/receipts/{id}"));

        api.MapPost("/receipts/instruments", async (InstrumentReceiptRequest r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.RecordInstrumentsAsync(new RecordInstrumentReceiptCommand(Key(http), r.Header, r.Instruments), ct).ConfigureAwait(false),
                id => $"/api/cashflow/receipts/{id}"));

        // Promissory notes have a receipt of their own (owner's report no. 4, item 13; his form of 16 Nov 2026).
        api.MapPost("/receipts/notes", async (NoteReceiptRequest r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.RecordNotesAsync(new RecordNoteReceiptCommand(Key(http), r.Header, r.Terms, r.Notes, r.Guarantors, r.SignerIds), ct).ConfigureAwait(false),
                id => $"/api/cashflow/receipts/{id}"));

        api.MapPost("/receipts/notes/voucher-preview", async (NoteReceiptRequest r, ReceiptUseCases u, CancellationToken ct) =>
            Preview(await u.PreviewNotesAsync(new RecordNoteReceiptCommand("preview", r.Header, r.Terms, r.Notes, r.Guarantors, r.SignerIds), ct).ConfigureAwait(false)));

        // Correction of a saved receipt while its voucher is not confirmed (owner's report no. 6, items 2 and 6).
        api.MapPut("/receipts/{id:guid}/cash", async (Guid id, CashReceiptCorrection r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.CorrectCashAsync(new CorrectCashReceiptCommand(Key(http), id, r.Version, r.Header, r.CashDeskId, r.Amount, r.RateToRial), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/receipts/{id}"));

        api.MapPost("/receipts/card", async (CardReceiptRequest r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.RecordCardDepositAsync(
                new RecordCardReceiptCommand(Key(http), r.Header, r.CardKind, r.CardId, r.Method, r.TrackingNumber, r.Amount), ct).ConfigureAwait(false),
                id => $"/api/cashflow/receipts/{id}"));

        api.MapPut("/receipts/{id:guid}/card", async (Guid id, CardReceiptCorrection r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.CorrectCardDepositAsync(
                new CorrectCardReceiptCommand(Key(http), id, r.Version, r.Header, r.CardKind, r.CardId, r.Method, r.TrackingNumber, r.Amount), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/receipts/{id}"));

        api.MapPut("/receipts/{id:guid}/bank", async (Guid id, BankReceiptCorrection r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.CorrectBankDepositAsync(
                new CorrectBankReceiptCommand(Key(http), id, r.Version, r.Header, r.BankAccountId, r.Method, r.TrackingNumber, r.Amount, r.RateToRial, r.PayerSheba), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/receipts/{id}"));

        api.MapPut("/receipts/{id:guid}/instruments", async (Guid id, InstrumentReceiptCorrection r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.CorrectInstrumentsAsync(new CorrectInstrumentReceiptCommand(Key(http), id, r.Version, r.Header, r.Instruments), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/receipts/{id}"));

        api.MapPut("/receipts/{id:guid}/notes", async (Guid id, NoteReceiptCorrection r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.CorrectNotesAsync(
                new CorrectNoteReceiptCommand(Key(http), id, r.Version, r.Header, r.Terms, r.Notes, r.Guarantors, r.SignerIds), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/receipts/{id}"));

        // Deletion of a receipt recorded by mistake, while its voucher is not confirmed (owner's answer to Q59, 19 Nov 2026).
        api.MapPost("/receipts/{id:guid}/delete", async (Guid id, DeleteReceiptRequest r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.DeleteAsync(new DeleteReceiptCommand(Key(http), id, r.Version), ct).ConfigureAwait(false), _ => "/api/cashflow/receipts"));

        api.MapPost("/receipts/{id:guid}/cancel", async (Guid id, CancelReceiptRequest r, HttpRequest http, ReceiptUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.CancelAsync(new CancelReceiptCommand(Key(http), id, r.Date, r.Reason, r.Version), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/receipts/{id}"));

        // Vouchers (owner's report no. 4, item 8): the template's fixed accounts, the voucher of a receipt or a cheque step, previews
        // of the receipt forms, and the treasurer's confirmation.
        api.MapGet("/voucher-template", async (Treasury.Cashflow.Application.Vouchers.VoucherUseCases u, CancellationToken ct) =>
            Results.Ok(await u.GetTemplateAsync(ct).ConfigureAwait(false)));

        api.MapPut("/voucher-template/{account}", async (Treasury.Cashflow.Domain.Vouchers.FixedAccount account, TemplateAccountRequest r, HttpRequest http,
            Treasury.Cashflow.Application.Vouchers.VoucherUseCases u, CancellationToken ct) =>
            ToHttp(await u.SetTemplateAccountAsync(Key(http), account, r.Code, ct).ConfigureAwait(false), _ => "/api/cashflow/voucher-template"));

        api.MapGet("/receipts/{id:guid}/voucher", async (Guid id, Treasury.Cashflow.Application.Vouchers.VoucherUseCases u, CancellationToken ct) =>
            await u.ReceiptVoucherAsync(id, ct).ConfigureAwait(false) is { } v ? Results.Ok(v) : Results.NotFound());

        api.MapPost("/receipts/{id:guid}/voucher/confirm", async (Guid id, HttpRequest http, Treasury.Cashflow.Application.Vouchers.VoucherUseCases u, CancellationToken ct) =>
            ToHttp(await u.ConfirmReceiptVoucherAsync(Key(http), id, ct).ConfigureAwait(false), _ => $"/api/cashflow/receipts/{id}/voucher"));

        api.MapGet("/instruments/{id:guid}/vouchers", async (Guid id, Treasury.Cashflow.Application.Vouchers.VoucherUseCases u, CancellationToken ct) =>
            Results.Ok(await u.ChequeVouchersAsync(id, ct).ConfigureAwait(false)));

        api.MapPost("/instruments/{id:guid}/vouchers/{step:int}/confirm", async (Guid id, int step, HttpRequest http, Treasury.Cashflow.Application.Vouchers.VoucherUseCases u, CancellationToken ct) =>
            ToHttp(await u.ConfirmChequeVoucherAsync(Key(http), id, step, ct).ConfigureAwait(false), _ => $"/api/cashflow/instruments/{id}/vouchers"));

        api.MapPost("/receipts/cash/voucher-preview", async (CashReceiptRequest r, ReceiptUseCases u, CancellationToken ct) =>
            Preview(await u.PreviewCashAsync(new RecordCashReceiptCommand("preview", r.Header, r.CashDeskId, r.Amount, r.RateToRial), ct).ConfigureAwait(false)));

        api.MapPost("/receipts/bank/voucher-preview", async (BankReceiptRequest r, ReceiptUseCases u, CancellationToken ct) =>
            Preview(await u.PreviewBankDepositAsync(new RecordBankReceiptCommand("preview", r.Header, r.BankAccountId, r.Method, r.TrackingNumber, r.Amount, r.RateToRial, r.PayerSheba), ct).ConfigureAwait(false)));

        api.MapPost("/receipts/card/voucher-preview", async (CardReceiptRequest r, ReceiptUseCases u, CancellationToken ct) =>
            Preview(await u.PreviewCardDepositAsync(new RecordCardReceiptCommand("preview", r.Header, r.CardKind, r.CardId, r.Method, r.TrackingNumber, r.Amount), ct).ConfigureAwait(false)));

        api.MapPost("/receipts/instruments/voucher-preview", async (InstrumentReceiptRequest r, ReceiptUseCases u, CancellationToken ct) =>
            Preview(await u.PreviewInstrumentsAsync(new RecordInstrumentReceiptCommand("preview", r.Header, r.Instruments), ct).ConfigureAwait(false)));

        // Cheque books screen (Basic Information, manual item 6).
        api.MapGet("/cheque-books", async (Guid? bankAccountId, bool? includeInactive, ChequeBookUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.ListAsync(bankAccountId, includeInactive ?? false, ct).ConfigureAwait(false)));

        api.MapPost("/cheque-books", async (ChequeBookRequest r, HttpRequest http, ChequeBookUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.RegisterAsync(
                new RegisterChequeBookCommand(Key(http), r.BankAccountId, r.Serial, r.FirstSheet, r.LastSheet, r.FormatId, r.SayadMiddle, r.SayadFirstSerial, r.PartlyUsed, r.SerialDigits), ct).ConfigureAwait(false),
                id => $"/api/cashflow/cheque-books/{id}"));

        api.MapPut("/cheque-books/{id:guid}", async (Guid id, ChequeBookCorrection r, HttpRequest http, ChequeBookUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.CorrectAsync(new CorrectChequeBookCommand(Key(http), id, r.Serial, r.FirstSheet, r.LastSheet, r.Version, r.FormatId, r.SayadMiddle, r.SayadFirstSerial, r.PartlyUsed, r.SerialDigits), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/cheque-books/{id}"));

        // Cheque types (Basic Information, manual item 7) with the scan of each cheque.
        api.MapGet("/cheque-formats", async (Guid? bankAccountId, bool? includeInactive, ChequeFormatUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.ListAsync(bankAccountId, includeInactive ?? false, ct).ConfigureAwait(false)));

        api.MapPost("/cheque-formats", async (ChequeFormatRequest r, HttpRequest http, ChequeFormatUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.RegisterAsync(Key(http), r.BankAccountId, r.PrintType, ct).ConfigureAwait(false), id => $"/api/cashflow/cheque-formats/{id}"));

        api.MapPut("/cheque-formats/{id:guid}", async (Guid id, ChequeFormatChange r, HttpRequest http, ChequeFormatUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.ChangePrintTypeAsync(Key(http), id, r.PrintType, r.Version, ct).ConfigureAwait(false), _ => $"/api/cashflow/cheque-formats/{id}"));

        api.MapPost("/cheque-formats/{id:guid}/{change:regex(^(activate|deactivate|delete)$)}", async (Guid id, string change, ChequeBookVersion r, HttpRequest http, ChequeFormatUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.ChangeStateAsync(Key(http), id, change, r.Version, ct).ConfigureAwait(false), _ => $"/api/cashflow/cheque-formats/{id}"));

        // The scan: multipart form with "file" and "version"; at most 2 MB (checked again by the domain).
        api.MapPost("/cheque-formats/{id:guid}/image", async (Guid id, IFormFile file, [Microsoft.AspNetCore.Mvc.FromForm] long version, HttpRequest http, ChequeFormatUseCases useCases, CancellationToken ct) =>
        {
            if (file.Length > Treasury.Cashflow.Domain.ChequeBooks.ChequeFormat.MaxImageBytes)
            {
                return ToHttp(Result<Guid>.Fail("ChequeFormat.ImageTooLarge", "The scan file is larger than 2 MB."), _ => string.Empty);
            }

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct).ConfigureAwait(false);
            return ToHttp(await useCases.AttachImageAsync(Key(http), id, buffer.ToArray(), version, ct).ConfigureAwait(false), _ => $"/api/cashflow/cheque-formats/{id}/image");
        }).DisableAntiforgery();

        api.MapGet("/cheque-formats/{id:guid}/image", async (Guid id, ChequeFormatUseCases useCases, CancellationToken ct) =>
            await useCases.GetImageAsync(id, ct).ConfigureAwait(false) is { } image ? Results.File(image.Data, image.ContentType) : Results.NotFound());

        api.MapPost("/cheque-books/{id:guid}/{change:regex(^(activate|deactivate|delete)$)}", async (Guid id, string change, ChequeBookVersion r, HttpRequest http, ChequeBookUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.ChangeStateAsync(new ChangeChequeBookStateCommand(Key(http), id, change, r.Version), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/cheque-books/{id}"));

        // Transfers between the company's own bank accounts (owner's report no. 7, 20 Nov 2026): electronic within the configurable
        // limits, and by Sayad cheque with its payment side («انتقال دهنده») and receipts side («انتقال گیرنده»).
        api.MapGet("/transfers", async (int year, bool? cheque, TransferUseCases u, CancellationToken ct) =>
            Results.Ok(await u.ListAsync(year, cheque, ct).ConfigureAwait(false)));

        api.MapGet("/transfers/{id:guid}", async (Guid id, TransferUseCases u, CancellationToken ct) =>
            await u.GetAsync(id, ct).ConfigureAwait(false) is { } view ? Results.Ok(view) : Results.NotFound());

        // The owner's answers of 21 Nov 2026 to Q83 and Q84: whether the fee line asks for a تفصیل, and the day's orders of an account.
        api.MapGet("/transfers/fee-account", async (TransferUseCases u, CancellationToken ct) => Results.Ok(await u.FeeAccountAsync(ct).ConfigureAwait(false)));

        api.MapGet("/transfers/day-usage", async (Guid sourceAccountId, string date, TransferMethod method, TransferChannel channel, Guid? except, TransferUseCases u,
            CancellationToken ct) => Results.Ok(await u.DayUsageAsync(sourceAccountId, date, method, channel, except, ct).ConfigureAwait(false)));

        api.MapPost("/transfers/electronic", async (ElectronicTransferInput r, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.RecordElectronicAsync(new RecordElectronicTransferCommand(Key(http), r), ct).ConfigureAwait(false), id => $"/api/cashflow/transfers/{id}"));

        api.MapPut("/transfers/{id:guid}/electronic", async (Guid id, ElectronicTransferCorrection r, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.CorrectElectronicAsync(new CorrectElectronicTransferCommand(Key(http), id, r.Version, r.Form), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/transfers/{id}"));

        api.MapPost("/transfers/electronic/voucher-preview", async (ElectronicTransferInput r, Guid? correcting, TransferUseCases u, CancellationToken ct) =>
            Preview(await u.PreviewElectronicAsync(r, correcting, ct).ConfigureAwait(false)));

        api.MapPost("/transfers/cheque", async (TransferChequeInput r, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.IssueChequeAsync(new IssueTransferChequeCommand(Key(http), r), ct).ConfigureAwait(false), id => $"/api/cashflow/transfers/{id}"));

        api.MapPost("/transfers/cheque/voucher-preview", async (TransferChequeInput r, TransferUseCases u, CancellationToken ct) =>
            Preview(await u.PreviewChequeAsync(r, ct).ConfigureAwait(false)));

        api.MapPost("/transfers/{id:guid}/receive-cheque", async (Guid id, ReceiveTransferChequeRequest r, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.ReceiveChequeAsync(new ReceiveTransferChequeCommand(Key(http), id, r.Version, r.Date), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/transfers/{id}"));

        api.MapPost("/transfers/{id:guid}/undo-receive", async (Guid id, TransferVersion r, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.UndoReceiveAsync(new UndoTransferReceiptCommand(Key(http), id, r.Version), ct).ConfigureAwait(false), _ => $"/api/cashflow/transfers/{id}"));

        // The owner's answers of 21 Nov 2026: the payer's Sayad registration and the cancellation of a cheque not yet registered (Q79),
        // and the maturity of a post-dated cheque (Q81).
        api.MapPost("/transfers/{id:guid}/register-sayad", async (Guid id, TransferChequeStepRequest r, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.RegisterPayerSayadAsync(new TransferChequeStepCommand(Key(http), id, r.Version, r.Date), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/transfers/{id}"));

        api.MapPost("/transfers/{id:guid}/mature", async (Guid id, TransferChequeStepRequest r, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.MatureChequeAsync(new TransferChequeStepCommand(Key(http), id, r.Version, r.Date), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/transfers/{id}"));

        api.MapPost("/transfers/{id:guid}/cancel-cheque", async (Guid id, CancelTransferChequeRequest r, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.CancelChequeAsync(new CancelTransferChequeCommand(Key(http), id, r.Version, r.Date, r.Reason), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/transfers/{id}"));

        api.MapPost("/transfers/{id:guid}/delete", async (Guid id, TransferVersion r, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.DeleteAsync(new DeleteTransferCommand(Key(http), id, r.Version), ct).ConfigureAwait(false), _ => "/api/cashflow/transfers"));

        api.MapGet("/transfers/{id:guid}/vouchers", async (Guid id, TransferUseCases u, CancellationToken ct) =>
            Results.Ok(await u.VouchersAsync(id, ct).ConfigureAwait(false)));

        api.MapPost("/transfers/{id:guid}/vouchers/{step:int}/confirm", async (Guid id, int step, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.ConfirmVoucherAsync(Key(http), id, step, ct).ConfigureAwait(false), _ => $"/api/cashflow/transfers/{id}/vouchers"));

        // The limits of the electronic ways (Basic Information page «سقف‌های انتقال الکترونیکی»): data of the company, not code.
        api.MapGet("/transfer-limits", async (TransferUseCases u, CancellationToken ct) => Results.Ok(await u.GetLimitsAsync(ct).ConfigureAwait(false)));

        api.MapPut("/transfer-limits/{method}", async (TransferMethod method, TransferLimitRequest r, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.SetLimitAsync(new SetTransferLimitCommand(Key(http), method, r.BankId, r.MinPerOrder, r.MaxPerOrderInPerson, r.MaxPerOrderRemote,
                r.MaxPerDayInPerson, r.MaxPerDayRemote, r.MaxCountPerDay), ct).ConfigureAwait(false), _ => "/api/cashflow/transfer-limits"));

        api.MapPost("/transfer-limits/{method}/banks/{bankId:guid}/delete", async (TransferMethod method, Guid bankId, HttpRequest http, TransferUseCases u, CancellationToken ct) =>
            ToHttp(await u.RemoveBankLimitAsync(Key(http), method, bankId, ct).ConfigureAwait(false), _ => "/api/cashflow/transfer-limits"));

        api.MapPost("/payments/issue-cheque", async (IssueRequest r, HttpRequest http, InstrumentUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.IssueChequeAsync(
                new IssueChequeCommand(Key(http), r.PaymentId, r.BankAccountId, r.ChequeBookId, r.SheetNumber, r.Kind, r.Amount, r.IssueDate, r.DueDate, r.PayeeId, r.PayeeName, r.Purpose), ct).ConfigureAwait(false),
                v => $"/api/cashflow/instruments/{v.InstrumentId}"));
    }

    /// <summary>Development only: simulate answers from other services (approvals, balances, open years).</summary>
    public static void MapDevelopmentEndpoints(this IEndpointRouteBuilder app, DevelopmentPolicies policies)
    {
        var dev = app.MapGroup("/dev");
        dev.MapPost("/payments/{id:guid}/approve", (Guid id) => { policies.Approve(id); return Results.NoContent(); });
        dev.MapPost("/bank-accounts/{id:guid}/balance/{rial:decimal}", (Guid id, decimal rial) => { policies.SetBalance(id, rial); return Results.NoContent(); });
        dev.MapPost("/fiscal-years/{year:int}/open", (int year) => { policies.OpenYear(year); return Results.NoContent(); });
        dev.MapGet("/outbox", (InMemoryDatabase db) => Results.Ok(db.OutboxMessages));
    }

    private static string Key(HttpRequest http) => http.Headers[IdempotencyHeader].ToString();

    /// <summary>A preview of an incomplete form answers 422 with the first rule it breaks, like a save would.</summary>
    private static IResult Preview(Result<Treasury.Cashflow.Application.Vouchers.VoucherView> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: result.ErrorCode, detail: result.ErrorMessage,
                extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });

    /// <summary>Rule violations become 422 with the stable error code; the UI shows the translated text, never exception details.</summary>
    private static IResult ToHttp<T>(Result<T> result, Func<T, string> location)
    {
        if (result.IsSuccess)
        {
            return Results.Created(location(result.Value!), new { value = result.Value, warnings = result.Warnings });
        }

        var status = result.ErrorCode switch
        {
            "Request.IdempotencyKeyRequired" or "Request.UnknownChange" => StatusCodes.Status400BadRequest,
            "Instrument.NotFound" or "ChequeBook.NotFound" or "ChequeFormat.NotFound" or "Receipt.NotFound" or "Instrument.AttachmentNotFound" or "Transfer.NotFound"
                => StatusCodes.Status404NotFound,
            "Requests.Unavailable" => StatusCodes.Status503ServiceUnavailable,
            "Concurrency.Conflict" => StatusCodes.Status409Conflict,
            "Accounts.Unavailable" or "Reference.Unavailable" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        return Results.Problem(
            statusCode: status,
            title: result.ErrorCode,
            detail: result.ErrorMessage,
            extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
    }
}

/// <summary>
/// The signed-in user. In production the identity comes from a validated Keycloak token (JWT bearer — added with the
/// Postgres/Keycloak packages). In Development only, a missing identity falls back to "dev-user".
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor, IHostEnvironment environment) : ICurrentUser
{
    public string UserId
    {
        get
        {
            var user = accessor.HttpContext?.User;
            var id = user?.FindFirstValue("preferred_username") ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }

            return environment.IsDevelopment()
                ? "dev-user"
                : throw new UnauthorizedAccessException("No authenticated user.");
        }
    }
}
