using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Organization;

namespace Treasury.Reference.Api;

public sealed record DepartmentEditRequest(DepartmentForm Form, long Version);

public sealed record RequesterEditRequest(RequesterForm Form, long Version);

public sealed record BeneficiaryEditRequest(BeneficiaryForm Form, long Version);

/// <summary>Departments, requesters and beneficiaries (manual items 12–14) under /api/reference.</summary>
public static class OrganizationEndpoints
{
    private const string StateChange = "{change:regex(^(activate|deactivate|delete)$)}";

    public static void MapOrganizationEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/reference");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        api.MapGet("/departments", async (bool? includeInactive, OrganizationUseCases u, CancellationToken ct) =>
            Results.Ok(await u.ListDepartmentsAsync(includeInactive ?? false, ct).ConfigureAwait(false)));
        api.MapPost("/departments", async (DepartmentForm form, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.AddDepartmentAsync(Key(http), form, ct).ConfigureAwait(false)));
        api.MapPut("/departments/{id:guid}", async (Guid id, DepartmentEditRequest r, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.EditDepartmentAsync(Key(http), id, r.Form, r.Version, ct).ConfigureAwait(false)));
        api.MapPost($"/departments/{{id:guid}}/{StateChange}", async (Guid id, string change, VersionRequest r, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.ChangeDepartmentAsync(new(Key(http), id, change, r.Version), ct).ConfigureAwait(false)));

        api.MapGet("/requesters", async (Guid? departmentId, bool? includeInactive, OrganizationUseCases u, CancellationToken ct) =>
            Results.Ok(await u.ListRequestersAsync(departmentId, includeInactive ?? false, ct).ConfigureAwait(false)));
        api.MapPost("/requesters", async (RequesterForm form, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.AddRequesterAsync(Key(http), form, ct).ConfigureAwait(false)));
        api.MapPut("/requesters/{id:guid}", async (Guid id, RequesterEditRequest r, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.EditRequesterAsync(Key(http), id, r.Form, r.Version, ct).ConfigureAwait(false)));
        api.MapPost($"/requesters/{{id:guid}}/{StateChange}", async (Guid id, string change, VersionRequest r, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.ChangeRequesterAsync(new(Key(http), id, change, r.Version), ct).ConfigureAwait(false)));
        api.MapPost("/requesters/{id:guid}/in-use", async (Guid id, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.MarkInUseAsync(Key(http), id, ct).ConfigureAwait(false)));

        api.MapGet("/beneficiaries", async (bool? includeInactive, OrganizationUseCases u, CancellationToken ct) =>
            Results.Ok(await u.ListBeneficiariesAsync(includeInactive ?? false, ct).ConfigureAwait(false)));
        api.MapPost("/beneficiaries", async (BeneficiaryForm form, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.AddBeneficiaryAsync(Key(http), form, ct).ConfigureAwait(false)));
        api.MapPut("/beneficiaries/{id:guid}", async (Guid id, BeneficiaryEditRequest r, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.EditBeneficiaryAsync(Key(http), id, r.Form, r.Version, ct).ConfigureAwait(false)));
        api.MapPost($"/beneficiaries/{{id:guid}}/{StateChange}", async (Guid id, string change, VersionRequest r, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.ChangeBeneficiaryAsync(new(Key(http), id, change, r.Version), ct).ConfigureAwait(false)));
        api.MapPost("/beneficiaries/{id:guid}/in-use", async (Guid id, HttpRequest http, OrganizationUseCases u, CancellationToken ct) =>
            ToHttp(await u.MarkInUseAsync(Key(http), id, ct).ConfigureAwait(false)));
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
            "Department.NotFound" or "Requester.NotFound" or "Beneficiary.NotFound" or "Organization.NotFound" => StatusCodes.Status404NotFound,
            "Concurrency.Conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        return Results.Problem(statusCode: status, title: result.ErrorCode, detail: result.ErrorMessage, extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
    }
}
