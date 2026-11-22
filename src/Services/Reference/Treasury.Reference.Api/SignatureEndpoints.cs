using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Signatures;
using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Api;

public sealed record SignatoryEditRequest(SignatoryForm Form, long Version);

public sealed record PermissionHolderEditRequest(PermissionHolderForm Form, long Version);

/// <summary>
/// «دارندگان امضای مجاز (اسناد بهادار)» and «دارندگان مجوزها (فرم‌ها)» (owner, 16 Nov 2026; his report no. 8, 21 Nov 2026) under
/// /api/reference.
/// </summary>
public static class SignatureEndpoints
{
    private const string StateChange = "{change:regex(^(activate|deactivate|delete)$)}";

    public static void MapSignatureEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/reference");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        // ---- authorised signatories ----
        api.MapGet("/signatories", async (bool? includeInactive, SignatureUseCases u, CancellationToken ct) =>
            Results.Ok(await u.ListSignatoriesAsync(includeInactive ?? false, ct).ConfigureAwait(false)));
        // One signatory, asked by accounts-svc when a bank account names it.
        api.MapGet("/signatories/{id:guid}", async (Guid id, SignatureUseCases u, CancellationToken ct) =>
            await u.GetSignatoryAsync(id, ct).ConfigureAwait(false) is { } s ? Results.Ok(s) : Results.NotFound());
        api.MapPost("/signatories", async (SignatoryForm form, HttpRequest http, SignatureUseCases u, CancellationToken ct) =>
            ToHttp(await u.AddSignatoryAsync(Key(http), form, ct).ConfigureAwait(false)));
        api.MapPut("/signatories/{id:guid}", async (Guid id, SignatoryEditRequest r, HttpRequest http, SignatureUseCases u, CancellationToken ct) =>
            ToHttp(await u.EditSignatoryAsync(Key(http), id, r.Form, r.Version, ct).ConfigureAwait(false)));
        api.MapPost($"/signatories/{{id:guid}}/{StateChange}", async (Guid id, string change, VersionRequest r, HttpRequest http, SignatureUseCases u, CancellationToken ct) =>
            ToHttp(await u.ChangeSignatoryAsync(new(Key(http), id, change, r.Version), ct).ConfigureAwait(false)));
        api.MapPost("/signatories/{id:guid}/in-use", async (Guid id, HttpRequest http, SignatureUseCases u, CancellationToken ct) =>
            ToHttp(await u.MarkInUseAsync(Key(http), id, ct).ConfigureAwait(false)));

        // The scan of the specimen signature: multipart form with "file" and "version"; at most 1 MB (checked again by the domain).
        api.MapPost("/signatories/{id:guid}/signature", async (Guid id, IFormFile file, [Microsoft.AspNetCore.Mvc.FromForm] long version, HttpRequest http, SignatureUseCases u, CancellationToken ct) =>
            await ReadScanAsync(file, ct).ConfigureAwait(false) is { } scan
                ? ToHttp(await u.AttachSignatureAsync(Key(http), id, scan, version, ct).ConfigureAwait(false))
                : ToHttp(TooLarge)).DisableAntiforgery();
        api.MapPost("/signatories/{id:guid}/signature/remove", async (Guid id, VersionRequest r, HttpRequest http, SignatureUseCases u, CancellationToken ct) =>
            ToHttp(await u.RemoveSignatureAsync(Key(http), id, r.Version, ct).ConfigureAwait(false)));
        api.MapGet("/signatories/{id:guid}/signature", async (Guid id, HttpResponse response, SignatureUseCases u, CancellationToken ct) =>
            Picture(response, await u.GetSignatureAsync(id, ct).ConfigureAwait(false)));

        // ---- permission holders ----
        api.MapGet("/permission-holders", async (bool? includeInactive, PermissionHolderUseCases u, CancellationToken ct) =>
            Results.Ok(await u.ListAsync(includeInactive ?? false, ct).ConfigureAwait(false)));
        // «فهرست عملیات خزانه داری», in the order the screens show it.
        api.MapGet("/permission-holders/operations", () => Results.Ok(PermissionHolderUseCases.Operations));
        // Who holds a permission in one operation (asked by the forms of that operation and by the other services).
        api.MapGet("/permission-holders/for/{operation}", async (TreasuryOperation operation, PermissionHolderUseCases u, CancellationToken ct) =>
            Enum.IsDefined(operation) ? Results.Ok(await u.ListForOperationAsync(operation, ct).ConfigureAwait(false)) : Results.NotFound());
        api.MapPost("/permission-holders", async (PermissionHolderForm form, HttpRequest http, PermissionHolderUseCases u, CancellationToken ct) =>
            ToHttp(await u.AddAsync(Key(http), form, ct).ConfigureAwait(false)));
        api.MapPut("/permission-holders/{id:guid}", async (Guid id, PermissionHolderEditRequest r, HttpRequest http, PermissionHolderUseCases u, CancellationToken ct) =>
            ToHttp(await u.EditAsync(Key(http), id, r.Form, r.Version, ct).ConfigureAwait(false)));
        api.MapPost($"/permission-holders/{{id:guid}}/{StateChange}", async (Guid id, string change, VersionRequest r, HttpRequest http, PermissionHolderUseCases u, CancellationToken ct) =>
            ToHttp(await u.ChangeAsync(new(Key(http), id, change, r.Version), ct).ConfigureAwait(false)));
        api.MapPost("/permission-holders/{id:guid}/signature", async (Guid id, IFormFile file, [Microsoft.AspNetCore.Mvc.FromForm] long version, HttpRequest http, PermissionHolderUseCases u, CancellationToken ct) =>
            await ReadScanAsync(file, ct).ConfigureAwait(false) is { } scan
                ? ToHttp(await u.AttachSignatureAsync(Key(http), id, scan, version, ct).ConfigureAwait(false))
                : ToHttp(TooLarge)).DisableAntiforgery();
        api.MapPost("/permission-holders/{id:guid}/signature/remove", async (Guid id, VersionRequest r, HttpRequest http, PermissionHolderUseCases u, CancellationToken ct) =>
            ToHttp(await u.RemoveSignatureAsync(Key(http), id, r.Version, ct).ConfigureAwait(false)));
        api.MapGet("/permission-holders/{id:guid}/signature", async (Guid id, HttpResponse response, PermissionHolderUseCases u, CancellationToken ct) =>
            Picture(response, await u.GetSignatureAsync(id, ct).ConfigureAwait(false)));
    }

    private static Result<Guid> TooLarge => Result<Guid>.Fail("Person.SignatureTooLarge", "The signature file is larger than 1 MB.");

    /// <summary>The uploaded file; null when it is larger than a scan may be (it is then not read at all).</summary>
    private static async Task<byte[]?> ReadScanAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length > SignatureScan.MaxBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>A scan changes rarely but must change at once when replaced: the browser asks again every time.</summary>
    private static IResult Picture(HttpResponse response, (string ContentType, byte[] Data)? scan)
    {
        response.Headers.CacheControl = "no-cache";
        return scan is { } s ? Results.File(s.Data, s.ContentType) : Results.NotFound();
    }

    private static string Key(HttpRequest http) => http.Headers["Idempotency-Key"].ToString();

    private static IResult ToHttp(Result<Guid> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(new { value = result.Value });
        }

        var status = result.ErrorCode switch
        {
            "Request.IdempotencyKeyRequired" or "Request.UnknownChange" => StatusCodes.Status400BadRequest,
            "Signatory.NotFound" or "PermissionHolder.NotFound" => StatusCodes.Status404NotFound,
            "Concurrency.Conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        return Results.Problem(statusCode: status, title: result.ErrorCode, detail: result.ErrorMessage, extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
    }
}
