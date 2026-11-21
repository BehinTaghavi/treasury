using Treasury.Reference.Application.Company;

namespace Treasury.Reference.Api;

/// <param name="Version">The version the user saw; 0 when the profile is entered for the first time.</param>
public sealed record CompanyProfileRequest(CompanyProfileForm Form, long Version);

/// <summary>«مشخصات شرکت» (owner, 18 Nov 2026) under /api/reference/company; cashflow-svc reads the company's city from it.</summary>
public static class CompanyEndpoints
{
    public static void MapCompanyEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/reference/company");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        api.MapGet("/", async (CompanyUseCases u, CancellationToken ct) => Results.Ok(await u.GetAsync(ct).ConfigureAwait(false)));

        api.MapPut("/", async (CompanyProfileRequest r, HttpRequest http, CompanyUseCases u, CancellationToken ct) =>
        {
            var result = await u.SetAsync(http.Headers["Idempotency-Key"].ToString(), r.Form, r.Version, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return Results.Ok(new { value = result.Value });
            }

            var status = result.ErrorCode switch
            {
                "Request.IdempotencyKeyRequired" => StatusCodes.Status400BadRequest,
                "Concurrency.Conflict" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status422UnprocessableEntity,
            };
            return Results.Problem(statusCode: status, title: result.ErrorCode, detail: result.ErrorMessage, extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
        });
    }
}
