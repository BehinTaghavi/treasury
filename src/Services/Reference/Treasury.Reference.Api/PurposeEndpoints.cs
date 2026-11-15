using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Purposes;
using Treasury.Reference.Domain.Purposes;

namespace Treasury.Reference.Api;

public sealed record PurposeGroupEditRequest(PurposeGroupForm Form, long Version);

public sealed record PurposeRequest(Guid GroupId, string Name);

public sealed record PurposeEditRequest(Guid GroupId, string Name, long Version);

/// <summary>
/// Purpose groups and purposes under /api/reference/purpose-groups and /api/reference/purposes (manual item 9).
/// Literal route segments win over the catalogue's /api/reference/{list}.
/// </summary>
public static class PurposeEndpoints
{
    private const string StateChange = "{change:regex(^(activate|deactivate|delete)$)}";

    public static void MapPurposeEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/reference");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        // Forms ask without includeInactive and with the direction they need (receipts: Receipt).
        api.MapGet("/purpose-groups", async (PurposeDirection? direction, bool? includeInactive, PurposeUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.ListGroupsAsync(direction, includeInactive ?? false, ct).ConfigureAwait(false)));

        api.MapGet("/purpose-groups/{id:guid}", async (Guid id, PurposeUseCases useCases, CancellationToken ct) =>
            await useCases.GetGroupAsync(id, ct).ConfigureAwait(false) is { } g ? Results.Ok(g) : Results.NotFound());

        api.MapPost("/purpose-groups", async (PurposeGroupForm form, HttpRequest http, PurposeUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.AddGroupAsync(new AddPurposeGroupCommand(Key(http), form), ct).ConfigureAwait(false)));

        api.MapPut("/purpose-groups/{id:guid}", async (Guid id, PurposeGroupEditRequest r, HttpRequest http, PurposeUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.EditGroupAsync(new EditPurposeGroupCommand(Key(http), id, r.Form, r.Version), ct).ConfigureAwait(false)));

        api.MapPost($"/purpose-groups/{{id:guid}}/{StateChange}", async (Guid id, string change, VersionRequest r, HttpRequest http, PurposeUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.ChangeGroupStateAsync(new ChangePurposeStateCommand(Key(http), id, change, r.Version), ct).ConfigureAwait(false)));

        // Called by cashflow-svc before it saves a receipt or payment naming the group or purpose.
        api.MapPost("/purpose-groups/{id:guid}/in-use", async (Guid id, HttpRequest http, PurposeUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.MarkGroupInUseAsync(Key(http), id, ct).ConfigureAwait(false)));

        api.MapGet("/purposes", async (Guid? groupId, bool? includeInactive, PurposeUseCases useCases, CancellationToken ct) =>
            Results.Ok(await useCases.ListPurposesAsync(groupId, includeInactive ?? false, ct).ConfigureAwait(false)));

        api.MapPost("/purposes", async (PurposeRequest r, HttpRequest http, PurposeUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.AddPurposeAsync(new AddPurposeCommand(Key(http), r.GroupId, r.Name), ct).ConfigureAwait(false)));

        api.MapPut("/purposes/{id:guid}", async (Guid id, PurposeEditRequest r, HttpRequest http, PurposeUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.EditPurposeAsync(new EditPurposeCommand(Key(http), id, r.GroupId, r.Name, r.Version), ct).ConfigureAwait(false)));

        api.MapPost($"/purposes/{{id:guid}}/{StateChange}", async (Guid id, string change, VersionRequest r, HttpRequest http, PurposeUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.ChangePurposeStateAsync(new ChangePurposeStateCommand(Key(http), id, change, r.Version), ct).ConfigureAwait(false)));

        api.MapPost("/purposes/{id:guid}/in-use", async (Guid id, HttpRequest http, PurposeUseCases useCases, CancellationToken ct) =>
            ToHttp(await useCases.MarkPurposeInUseAsync(Key(http), id, ct).ConfigureAwait(false)));
    }

    private static string Key(HttpRequest http) => http.Headers["Idempotency-Key"].ToString();

    /// <summary>Rule violations become 422 with the stable code; the UI shows the translated text.</summary>
    private static IResult ToHttp(Result<Guid> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(new { value = result.Value });
        }

        var status = result.ErrorCode switch
        {
            "Request.IdempotencyKeyRequired" or "Request.UnknownChange" => StatusCodes.Status400BadRequest,
            "PurposeGroup.NotFound" or "Purpose.NotFound" => StatusCodes.Status404NotFound,
            "Concurrency.Conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        return Results.Problem(statusCode: status, title: result.ErrorCode, detail: result.ErrorMessage, extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
    }
}
