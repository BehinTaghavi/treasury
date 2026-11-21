using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Company;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Api;

/// <param name="Version">The version the user saw; 0 when the profile is entered for the first time.</param>
public sealed record CompanyProfileRequest(CompanyProfileForm Form, long Version);

/// <summary>
/// «مشخصات شرکت» under /api/reference/company: the company's name, addresses, official details and logo (owner, 16 and 18 Nov 2026).
/// The shell reads the name and logo for the top of every screen; the receipt screen reads the company's city.
/// </summary>
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
            ToHttp(await u.SetAsync(Key(http), r.Form, r.Version, ct).ConfigureAwait(false)));

        // The logo: multipart form with "file" and "version"; at most 1 MB (checked again by the domain).
        api.MapPost("/logo", async (IFormFile file, [Microsoft.AspNetCore.Mvc.FromForm] long version, HttpRequest http, CompanyUseCases u, CancellationToken ct) =>
        {
            if (file.Length > CompanyProfile.MaxLogoBytes)
            {
                return ToHttp(Result<Guid>.Fail("Company.LogoTooLarge", "The logo file is larger than 1 MB."));
            }

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct).ConfigureAwait(false);
            return ToHttp(await u.AttachLogoAsync(Key(http), buffer.ToArray(), version, ct).ConfigureAwait(false));
        }).DisableAntiforgery();

        api.MapPost("/logo/remove", async (VersionRequest r, HttpRequest http, CompanyUseCases u, CancellationToken ct) =>
            ToHttp(await u.RemoveLogoAsync(Key(http), r.Version, ct).ConfigureAwait(false)));

        // The logo changes rarely but must change at once when replaced: the browser asks again every time.
        api.MapGet("/logo", async (HttpResponse response, CompanyUseCases u, CancellationToken ct) =>
        {
            response.Headers.CacheControl = "no-cache";
            return await u.GetLogoAsync(ct).ConfigureAwait(false) is { } logo ? Results.File(logo.Data, logo.ContentType) : Results.NotFound();
        });
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
            "Request.IdempotencyKeyRequired" => StatusCodes.Status400BadRequest,
            "Concurrency.Conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };
        return Results.Problem(statusCode: status, title: result.ErrorCode, detail: result.ErrorMessage, extensions: new Dictionary<string, object?> { ["code"] = result.ErrorCode });
    }
}
