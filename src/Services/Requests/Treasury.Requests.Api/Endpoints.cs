using System.Security.Claims;
using Treasury.BuildingBlocks.Application;
using Treasury.Requests.Application.Abstractions;
using Treasury.Requests.Application.PaymentRequests;
using Treasury.Requests.Domain.PaymentRequests;

namespace Treasury.Requests.Api;

public sealed record EditRequest(PaymentRequestInput Input, long Version);

public sealed record ChangeRequest(long Version, string? Reason = null);

/// <summary>A payment against an approved request, sent by cashflow-svc when a received cheque is endorsed (answer to Q36).</summary>
public sealed record PaymentInput(string Key, string Kind, Guid? InstrumentId, string? Reference, decimal Amount, string Currency, string Date);

public sealed record ReversePaymentInput(string Key);

public static class Endpoints
{
    public static void MapPaymentRequestEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/requests/payment-requests");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        // ?state=Approved gives «فهرست درخواست وجه های تایید شده جهت پرداخت», sorted by payment method and priority.
        api.MapGet("/", async (int year, PaymentRequestState? state, PaymentRequestUseCases u, CancellationToken ct) =>
            Results.Ok(await u.ListAsync(year, state, ct).ConfigureAwait(false)));

        api.MapGet("/{id:guid}", async (Guid id, PaymentRequestUseCases u, CancellationToken ct) =>
            await u.GetAsync(id, ct).ConfigureAwait(false) is { } v ? Results.Ok(v) : Results.NotFound());

        api.MapPost("/", async (PaymentRequestInput input, HttpRequest http, PaymentRequestUseCases u, CancellationToken ct) =>
            ToHttp(await u.RegisterAsync(new(Key(http), input), ct).ConfigureAwait(false)));

        api.MapPut("/{id:guid}", async (Guid id, EditRequest r, HttpRequest http, PaymentRequestUseCases u, CancellationToken ct) =>
            ToHttp(await u.EditAsync(new(Key(http), id, r.Input, r.Version), ct).ConfigureAwait(false)));

        // Report no. 4, item 12: scans of the supporting documents. Multipart form with "file" and "version"; at most 5 MB.
        api.MapPost("/{id:guid}/files", async (Guid id, IFormFile file, [Microsoft.AspNetCore.Mvc.FromForm] long version, HttpRequest http, PaymentRequestUseCases u, CancellationToken ct) =>
        {
            if (file.Length > PaymentRequest.MaxAttachmentBytes)
            {
                return ToHttp(Result<Guid>.Fail("PaymentRequest.AttachmentTooLarge", "A scan is at most 5 MB."));
            }

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct).ConfigureAwait(false);
            return ToHttp(await u.AttachFileAsync(Key(http), id, file.FileName, buffer.ToArray(), version, ct).ConfigureAwait(false));
        }).DisableAntiforgery();

        api.MapGet("/{id:guid}/files/{fileId:guid}", async (Guid id, Guid fileId, PaymentRequestUseCases u, CancellationToken ct) =>
            await u.GetFileAsync(id, fileId, ct).ConfigureAwait(false) is { } f ? Results.File(f.Content, f.File.ContentType, f.File.FileName) : Results.NotFound());

        api.MapPost("/{id:guid}/files/{fileId:guid}/delete", async (Guid id, Guid fileId, ChangeRequest r, HttpRequest http, PaymentRequestUseCases u, CancellationToken ct) =>
            ToHttp(await u.RemoveFileAsync(Key(http), id, fileId, r.Version, ct).ConfigureAwait(false)));

        api.MapPost("/{id:guid}/{change:regex(^(approve|withdraw-approval|cancel)$)}", async (Guid id, string change, ChangeRequest r, HttpRequest http, PaymentRequestUseCases u, CancellationToken ct) =>
            ToHttp(await u.ChangeAsync(new(Key(http), id, change, r.Version, r.Reason), ct).ConfigureAwait(false)));

        // Payments against approved requests (answer to Q36): an endorsed cheque pays the request; a returned one takes the payment back.
        api.MapPost("/{id:guid}/payments", async (Guid id, PaymentInput p, HttpRequest http, PaymentRequestUseCases u, CancellationToken ct) =>
            ToHttp(await u.RecordPaymentAsync(new(Key(http), id, p.Key, p.Kind, p.InstrumentId, p.Reference, p.Amount, p.Currency, p.Date), ct).ConfigureAwait(false)));

        api.MapPost("/{id:guid}/payments/reverse", async (Guid id, ReversePaymentInput p, HttpRequest http, PaymentRequestUseCases u, CancellationToken ct) =>
            ToHttp(await u.ReversePaymentAsync(Key(http), id, p.Key, ct).ConfigureAwait(false)));
    }

    private static string Key(HttpRequest http) => http.Headers["Idempotency-Key"].ToString();

    /// <summary>Rule violations become 422 with the stable error code; the UI shows the translated text, never exception details.</summary>
    private static IResult ToHttp(Result<Guid> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(new { value = result.Value });
        }

        var status = result.ErrorCode switch
        {
            "Request.IdempotencyKeyRequired" or "Request.UnknownChange" => StatusCodes.Status400BadRequest,
            "PaymentRequest.NotFound" or "PaymentRequest.AttachmentNotFound" => StatusCodes.Status404NotFound,
            "Concurrency.Conflict" => StatusCodes.Status409Conflict,
            "Reference.Unavailable" or "Accounts.Unavailable" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        return Results.Problem(statusCode: status, title: result.ErrorCode, detail: result.ErrorMessage, extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
    }
}

/// <summary>
/// The signed-in user. In production from a validated Keycloak token; in Development only a missing identity falls back to "dev-user".
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

            return environment.IsDevelopment() ? "dev-user" : throw new UnauthorizedAccessException("No authenticated user.");
        }
    }
}
