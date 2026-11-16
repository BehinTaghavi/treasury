using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Company;
using Treasury.Reference.Domain.Company;

namespace Treasury.Reference.Api;

public sealed record CompanyEditRequest(CompanyForm Form, long Version);

/// <summary>«اطلاعات شرکت» (owner, 16 Nov 2026): the company's name, official details and logo under /api/reference/company.</summary>
public static class CompanyEndpoints
{
    public static void MapCompanyEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var api = app.MapGroup("/api/reference/company");
        if (requireAuthorization)
        {
            api.RequireAuthorization();
        }

        api.MapGet(string.Empty, async (CompanyUseCases u, CancellationToken ct) => Results.Ok(await u.GetAsync(ct).ConfigureAwait(false)));

        api.MapPut(string.Empty, async (CompanyEditRequest r, HttpRequest http, CompanyUseCases u, CancellationToken ct) =>
            ToHttp(await u.SaveAsync(Key(http), r.Form, r.Version, ct).ConfigureAwait(false)));

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
