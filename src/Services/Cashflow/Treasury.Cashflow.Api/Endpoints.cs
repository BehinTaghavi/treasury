using System.Security.Claims;
using Treasury.BuildingBlocks.Application;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Infrastructure.InMemory;

namespace Treasury.Cashflow.Api;

public sealed record ReceiveRequest(InstrumentKind Kind, string Number, string BankName, MoneyInput Amount, string ReceivedOn, string? DueDate, Guid DrawerId, string DrawerName, string Purpose);

public sealed record ActionRequest(string Date, string? Destination, string? Note, string? Reference);

public sealed record ChequeBookRequest(Guid BankAccountId, string Serial, long FirstSheet, long LastSheet);

public sealed record IssueRequest(Guid PaymentId, Guid BankAccountId, Guid ChequeBookId, long? SheetNumber, InstrumentKind Kind, MoneyInput Amount, string IssueDate, string? DueDate, Guid PayeeId, string PayeeName, string Purpose);

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

        api.MapPost("/instruments/{id:guid}/actions/{action}", async (Guid id, InstrumentAction action, ActionRequest r, HttpRequest http, InstrumentUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.ChangeStateAsync(
                new ChangeInstrumentStateCommand(Key(http), id, action, r.Date, r.Destination, r.Note, r.Reference), ct).ConfigureAwait(false),
                _ => $"/api/cashflow/instruments/{id}"));

        api.MapPost("/cheque-books", async (ChequeBookRequest r, HttpRequest http, InstrumentUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.RegisterChequeBookAsync(
                new RegisterChequeBookCommand(Key(http), r.BankAccountId, r.Serial, r.FirstSheet, r.LastSheet), ct).ConfigureAwait(false),
                id => $"/api/cashflow/cheque-books/{id}"));

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

    /// <summary>Rule violations become 422 with the stable error code; the UI shows the translated text, never exception details.</summary>
    private static IResult ToHttp<T>(Result<T> result, Func<T, string> location)
    {
        if (result.IsSuccess)
        {
            return Results.Created(location(result.Value!), new { value = result.Value, warnings = result.Warnings });
        }

        var status = result.ErrorCode switch
        {
            "Request.IdempotencyKeyRequired" => StatusCodes.Status400BadRequest,
            "Instrument.NotFound" or "ChequeBook.NotFound" => StatusCodes.Status404NotFound,
            "Concurrency.Conflict" => StatusCodes.Status409Conflict,
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
