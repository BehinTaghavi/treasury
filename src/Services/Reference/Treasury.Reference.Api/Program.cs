using System.Text.Json.Serialization;
using Treasury.Reference.Api;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Infrastructure.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

// Storage: PostgreSQL when ConnectionStrings:Treasury is set (role reference_svc), otherwise in-memory (local runs, demos).
var connectionString = builder.Configuration.GetConnectionString("Treasury");
var usePostgres = false;
#if WITH_PACKAGES
if (!string.IsNullOrWhiteSpace(connectionString))
{
    Treasury.Reference.Infrastructure.Postgres.PostgresRegistration.AddReferencePostgres(builder.Services, connectionString);
    usePostgres = true;
}
#endif
if (!usePostgres)
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("A database connection string is set, but this build has no PostgreSQL support (built with -p:Offline=true).");
    }

    builder.Services.AddSingleton<InMemoryReferenceDatabase>();
    builder.Services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
    builder.Services.AddSingleton<ICatalogueRepository, InMemoryCatalogueRepository>();
    builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
    builder.Services.AddSingleton<InMemoryLedgerRepository>();
    builder.Services.AddSingleton<ILedgerRepository>(sp => sp.GetRequiredService<InMemoryLedgerRepository>());
    builder.Services.AddSingleton<ILedgerQueries>(sp => sp.GetRequiredService<InMemoryLedgerRepository>());
}

// Reading the accounting system's Excel exports (needs the ExcelDataReader package; offline builds refuse imports with a clear code).
#if WITH_PACKAGES
builder.Services.AddSingleton<ILedgerExportReader, Treasury.Reference.Infrastructure.Excel.ExcelLedgerExportReader>();
#else
builder.Services.AddSingleton<ILedgerExportReader, UnavailableExportReader>();
#endif

// Identity: Keycloak (OpenID Connect), same rules as cashflow-svc. Login comes after Basic Information (docs/decisions.md).
var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
#if WITH_PACKAGES
if (!string.IsNullOrWhiteSpace(keycloakAuthority))
{
    Microsoft.Extensions.DependencyInjection.JwtBearerExtensions.AddJwtBearer(
        builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme),
        o =>
        {
            o.Authority = keycloakAuthority;
            o.Audience = builder.Configuration["Keycloak:Audience"] ?? "treasury";
            o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        });
}
#endif
builder.Services.AddAuthorization();
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(keycloakAuthority))
{
    throw new InvalidOperationException("Keycloak:Authority must be configured outside Development.");
}

builder.Services.AddSingleton<CatalogueUseCases>();
builder.Services.AddSingleton<Treasury.Reference.Application.Ledger.LedgerUseCases>();

var app = builder.Build();

app.UseExceptionHandler();
if (!string.IsNullOrWhiteSpace(keycloakAuthority))
{
    app.UseAuthentication();
}

app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "reference" }));
app.MapCatalogueEndpoints(requireAuthorization: !string.IsNullOrWhiteSpace(keycloakAuthority));
app.MapLedgerEndpoints(requireAuthorization: !string.IsNullOrWhiteSpace(keycloakAuthority));

// A new client database starts with usable lists (currencies, provinces and capitals, banks); see DefaultLists.
await DefaultLists.SeedAsync(app.Services.GetRequiredService<CatalogueUseCases>(), CancellationToken.None).ConfigureAwait(false);

await app.RunAsync().ConfigureAwait(false);

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
