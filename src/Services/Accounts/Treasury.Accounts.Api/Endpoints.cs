using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Application.BankAccounts;

namespace Treasury.Accounts.Api;

public sealed record RegisterBankAccountRequest(BankAccountForm Form, Guid? MainAccountId);

public sealed record ChangeBankAccountRequest(BankAccountForm Form, long Version);

public sealed record VersionRequest(long Version);

public static class Endpoints
{
    public static void MapBankAccountEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/accounts/bank-accounts");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        // Receipt and payment forms ask without includeInactive; the Basic Information screen asks for all.
        api.MapGet("/", async (bool? includeInactive, BankAccountUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.ListAsync(includeInactive ?? false, ct).ConfigureAwait(false)));

        api.MapPost("/", async (RegisterBankAccountRequest r, HttpRequest http, BankAccountUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.RegisterAsync(new RegisterBankAccountCommand(Key(http), r.Form, r.MainAccountId), ct).ConfigureAwait(false)));

        api.MapPut("/{id:guid}", async (Guid id, ChangeBankAccountRequest r, HttpRequest http, BankAccountUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.ChangeAsync(new ChangeBankAccountCommand(Key(http), id, r.Form, r.Version), ct).ConfigureAwait(false)));

        api.MapPost("/{id:guid}/{change:regex(^(activate|deactivate)$)}", async (Guid id, string change, VersionRequest r, HttpRequest http, BankAccountUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.SetActiveAsync(new SetBankAccountActiveCommand(Key(http), id, change == "activate", r.Version), ct).ConfigureAwait(false)));
    }

    private static string Key(HttpRequest http) => http.Headers["Idempotency-Key"].ToString();

    /// <summary>Rule violations become 422 with the stable code (503 when reference-svc cannot answer); never exception text for users.</summary>
    private static IResult ToHttp(Result<Guid> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(new { value = result.Value });
        }

        var status = result.ErrorCode switch
        {
            "Request.IdempotencyKeyRequired" => StatusCodes.Status400BadRequest,
            "BankAccount.NotFound" => StatusCodes.Status404NotFound,
            "Concurrency.Conflict" => StatusCodes.Status409Conflict,
            "Reference.Unavailable" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        return Results.Problem(statusCode: status, title: result.ErrorCode, detail: result.ErrorMessage, extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
    }
}
