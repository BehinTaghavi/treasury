using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Application.Ledger;
using Treasury.Reference.Domain.Ledger;

namespace Treasury.Reference.Api;

public sealed record CodeFormatRequest(int Group, int General, int Subsidiary, int SubSubsidiary, int Detail);

public sealed record DetailTypesRequest(IReadOnlyList<string>? Detail1Types, IReadOnlyList<string>? Detail2Types);

/// <summary>
/// The copy of the accounting system's accounts under /api/reference/ledger: code lengths, Excel import (with
/// ?preview=true first), the chart, the تفصیل list and the تفصیل types each account allows.
/// </summary>
public static class LedgerEndpoints
{
    /// <summary>Largest accepted export (Iran Nazho Co.'s تفصیل list with 9,600 rows is about 1 MB).</summary>
    public const long MaxUploadBytes = 20 * 1024 * 1024;

    public static void MapLedgerEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/reference/ledger");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        api.MapGet("/format", async (LedgerUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.GetCodeFormatAsync(ct).ConfigureAwait(false)));

        api.MapPut("/format", async (CodeFormatRequest r, HttpRequest http, LedgerUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.SetCodeFormatAsync(new SetCodeFormatCommand(Key(http), r.Group, r.General, r.Subsidiary, r.SubSubsidiary, r.Detail), ct).ConfigureAwait(false)));

        api.MapPost("/chart/import", (IFormFile file, bool? preview, HttpRequest http, ILedgerExportReader reader, LedgerUseCases useCases, CancellationToken ct) =>
            ImportAsync(file, s => reader.ReadChart(s), rows => preview == true
                ? useCases.PreviewChartAsync(rows, ct)
                : useCases.ImportChartAsync(Key(http), rows, ct)))
            .DisableAntiforgery();

        api.MapPost("/details/import", (IFormFile file, bool? preview, HttpRequest http, ILedgerExportReader reader, LedgerUseCases useCases, CancellationToken ct) =>
            ImportAsync(file, s => reader.ReadDetails(s), rows => preview == true
                ? useCases.PreviewDetailsAsync(rows, ct)
                : useCases.ImportDetailsAsync(Key(http), rows, ct)))
            .DisableAntiforgery();

        api.MapGet("/accounts", async (bool? postableOnly, bool? includeInactive, LedgerUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.AccountsAsync(postableOnly ?? false, includeInactive ?? false, ct).ConfigureAwait(false)));

        api.MapPut("/accounts/{code}/detail-types", async (string code, DetailTypesRequest r, HttpRequest http, LedgerUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.SetDetailTypesAsync(new SetDetailTypesCommand(Key(http), code, r.Detail1Types ?? [], r.Detail2Types ?? []), ct).ConfigureAwait(false)));

        api.MapGet("/details", async (string? search, string? type, bool? includeInactive, int? take, LedgerUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.SearchDetailsAsync(search, type, includeInactive ?? false, take ?? 100, ct).ConfigureAwait(false)));

        api.MapGet("/accounts/{code}", async (string code, LedgerUseCases useCases, CancellationToken ct) =>
            await useCases.AccountAsync(code, ct).ConfigureAwait(false) is { } a ? Results.Ok(a) : Results.NotFound());

        api.MapGet("/details/{code}", async (string code, LedgerUseCases useCases, CancellationToken ct) =>
            await useCases.DetailAsync(code, ct).ConfigureAwait(false) is { } d ? Results.Ok(d) : Results.NotFound());

        api.MapGet("/detail-types", async (LedgerUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.DetailTypesAsync(ct).ConfigureAwait(false)));
    }

    private static async Task<IResult> ImportAsync<TRow>(IFormFile file, Func<Stream, IReadOnlyList<TRow>> read, Func<IReadOnlyList<TRow>, Task<Result<ImportReport>>> run)
    {
        if (file.Length == 0 || file.Length > MaxUploadBytes)
        {
            return Problem("Excel.Unreadable", "The file is empty or larger than 20 MB.", StatusCodes.Status422UnprocessableEntity);
        }

        IReadOnlyList<TRow> rows;
        try
        {
            await using var stream = file.OpenReadStream();
            rows = read(stream);
        }
        catch (DomainException ex)
        {
            return Problem(ex.Code, ex.Message, StatusCodes.Status422UnprocessableEntity);
        }

        return ToHttp(await run(rows).ConfigureAwait(false));
    }

    private static string Key(HttpRequest http) => http.Headers["Idempotency-Key"].ToString();

    private static IResult ToHttp<T>(Result<T> result) => result.IsSuccess
        ? Results.Ok(new { value = result.Value })
        : Problem(result.ErrorCode!, result.ErrorMessage, result.ErrorCode switch
        {
            "Request.IdempotencyKeyRequired" => StatusCodes.Status400BadRequest,
            "Ledger.AccountNotFound" => StatusCodes.Status404NotFound,
            "Concurrency.Conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        });

    private static IResult Problem(string code, string? detail, int status) =>
        Results.Problem(statusCode: status, title: code, detail: detail, extensions: new Dictionary<string, object?> { ["code"] = code });
}

/// <summary>Offline builds have no Excel package: importing is refused with a clear code instead of failing at start-up.</summary>
public sealed class UnavailableExportReader : ILedgerExportReader
{
    public IReadOnlyList<ChartRow> ReadChart(Stream file) => throw NotAvailable();

    public IReadOnlyList<DetailRow> ReadDetails(Stream file) => throw NotAvailable();

    private static DomainException NotAvailable() => new("Excel.NotAvailable", "This build cannot read Excel files (built with -p:Offline=true).");
}
