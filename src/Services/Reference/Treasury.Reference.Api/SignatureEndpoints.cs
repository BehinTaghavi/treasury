using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Signatures;
using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Api;

public sealed record SignatoryEditRequest(SignatoryForm Form, long Version);

/// <summary>«دارندگان امضای مجاز» and the signature rules (owner, 16 Nov 2026) under /api/reference.</summary>
public static class SignatureEndpoints
{
    public static void MapSignatureEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/reference");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        api.MapGet("/signatories", async (bool? includeInactive, SignatureUseCases u, CancellationToken ct) =>
            Results.Ok(await u.ListSignatoriesAsync(includeInactive ?? false, ct).ConfigureAwait(false)));
        api.MapPost("/signatories", async (SignatoryForm form, HttpRequest http, SignatureUseCases u, CancellationToken ct) =>
            ToHttp(await u.AddSignatoryAsync(Key(http), form, ct).ConfigureAwait(false)));
        api.MapPut("/signatories/{id:guid}", async (Guid id, SignatoryEditRequest r, HttpRequest http, SignatureUseCases u, CancellationToken ct) =>
            ToHttp(await u.EditSignatoryAsync(Key(http), id, r.Form, r.Version, ct).ConfigureAwait(false)));
        api.MapPost("/signatories/{id:guid}/{change:regex(^(activate|deactivate|delete)$)}", async (Guid id, string change, VersionRequest r, HttpRequest http, SignatureUseCases u, CancellationToken ct) =>
            ToHttp(await u.ChangeSignatoryAsync(new(Key(http), id, change, r.Version), ct).ConfigureAwait(false)));

        // The rule of every kind of document, and the rule one form follows (asked by the other services).
        api.MapGet("/signature-rules", async (SignatureUseCases u, CancellationToken ct) => Results.Ok(await u.ListRulesAsync(ct).ConfigureAwait(false)));
        api.MapGet("/signature-rules/{document}", async (SignedDocument document, SignatureUseCases u, CancellationToken ct) =>
            Results.Ok(await u.GetRuleAsync(document, ct).ConfigureAwait(false)));
        api.MapPut("/signature-rules/{document}", async (SignedDocument document, SignatureRuleForm form, HttpRequest http, SignatureUseCases u, CancellationToken ct) =>
            ToHttp(await u.SetRuleAsync(Key(http), document, form, ct).ConfigureAwait(false)));
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
            "Signatory.NotFound" => StatusCodes.Status404NotFound,
            "Concurrency.Conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        return Results.Problem(statusCode: status, title: result.ErrorCode, detail: result.ErrorMessage, extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
    }
}
