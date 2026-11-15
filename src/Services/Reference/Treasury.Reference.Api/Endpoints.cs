using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Domain.Catalogue;

namespace Treasury.Reference.Api;

public sealed record AddItemRequest(string Name, Guid? ParentId, string? Code);

public sealed record RenameItemRequest(string Name, long Version);

public sealed record VersionRequest(long Version);

public static class Endpoints
{
    private const string IdempotencyHeader = "Idempotency-Key";

    /// <summary>URL name of each list: /api/reference/banks, /api/reference/cities?parentId=…</summary>
    public static readonly IReadOnlyDictionary<string, CatalogueKind> Lists = new Dictionary<string, CatalogueKind>
    {
        ["currencies"] = CatalogueKind.Currency,
        ["countries"] = CatalogueKind.Country,
        ["provinces"] = CatalogueKind.Province,
        ["cities"] = CatalogueKind.City,
        ["banks"] = CatalogueKind.Bank,
    };

    public static void MapCatalogueEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/reference");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        // Forms ask without includeInactive (active entries only); the Basic Information screen asks for all.
        api.MapGet("/{list}", async (string list, Guid? parentId, bool? includeInactive, CatalogueUseCases useCases, CancellationToken ct) =>
            Lists.TryGetValue(list, out var kind)
                ? Results.Ok(await useCases.ListAsync(kind, parentId, includeInactive ?? false, ct).ConfigureAwait(false))
                : Results.NotFound());

        // One entry by id; 404 when it is not in that list. reference-svc's own ledger routes (/ledger/...) are matched first.
        api.MapGet("/{list}/{id:guid}", async (string list, Guid id, CatalogueUseCases useCases, CancellationToken ct) =>
            Lists.TryGetValue(list, out var kind) && await useCases.GetAsync(id, ct).ConfigureAwait(false) is { } item && item.Kind == kind.ToString()
                ? Results.Ok(item)
                : Results.NotFound());

        api.MapPost("/{list}", async (string list, AddItemRequest r, HttpRequest http, CatalogueUseCases useCases, CancellationToken ct) =>
            Lists.TryGetValue(list, out var kind)
                ? ToHttp(await useCases.AddAsync(new AddCatalogueItemCommand(Key(http), kind, r.Name, r.ParentId, r.Code), ct).ConfigureAwait(false), list)
                : Results.NotFound());

        api.MapPut("/{list}/{id:guid}", async (string list, Guid id, RenameItemRequest r, HttpRequest http, CatalogueUseCases useCases, CancellationToken ct) =>
            Lists.ContainsKey(list)
                ? ToHttp(await useCases.RenameAsync(new RenameCatalogueItemCommand(Key(http), id, r.Name, r.Version), ct).ConfigureAwait(false), list)
                : Results.NotFound());

        api.MapPost("/{list}/{id:guid}/{change:regex(^(activate|deactivate)$)}", async (string list, Guid id, string change, VersionRequest r, HttpRequest http, CatalogueUseCases useCases, CancellationToken ct) =>
            Lists.ContainsKey(list)
                ? ToHttp(await useCases.SetActiveAsync(new SetCatalogueItemActiveCommand(Key(http), id, change == "activate", r.Version), ct).ConfigureAwait(false), list)
                : Results.NotFound());
    }

    private static string Key(HttpRequest http) => http.Headers[IdempotencyHeader].ToString();

    /// <summary>Rule violations become 422 with the stable error code; the UI shows the translated text, never exception details.</summary>
    private static IResult ToHttp(Result<Guid> result, string list)
    {
        if (result.IsSuccess)
        {
            return Results.Created($"/api/reference/{list}/{result.Value}", new { value = result.Value });
        }

        var status = result.ErrorCode switch
        {
            "Request.IdempotencyKeyRequired" => StatusCodes.Status400BadRequest,
            "Catalogue.NotFound" => StatusCodes.Status404NotFound,
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
