using System.Text.Json.Serialization;
using Treasury.Accounts.Api;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Application.BankAccounts;
using Treasury.Accounts.Infrastructure;
using Treasury.Accounts.Infrastructure.InMemory;
using Treasury.BuildingBlocks.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();

// Storage: PostgreSQL when ConnectionStrings:Treasury is set (role accounts_svc), otherwise in-memory (local runs, demos).
var connectionString = builder.Configuration.GetConnectionString("Treasury");
var usePostgres = false;
#if WITH_PACKAGES
if (!string.IsNullOrWhiteSpace(connectionString))
{
    Treasury.Accounts.Infrastructure.Postgres.PostgresRegistration.AddAccountsPostgres(builder.Services, connectionString);
    usePostgres = true;
}
#endif
if (!usePostgres)
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("A database connection string is set, but this build has no PostgreSQL support (built with -p:Offline=true).");
    }

    builder.Services.AddSingleton<InMemoryAccountsDatabase>();
    builder.Services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
    builder.Services.AddSingleton<IBankAccountRepository, InMemoryBankAccountRepository>();
    builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
}

// reference-svc answers about banks, cities, currencies and ledger accounts (ADR 0001: through its API, never its tables).
var referenceUrl = builder.Configuration["Services:Reference"] ?? throw new InvalidOperationException("Services:Reference (reference-svc address) is not configured.");
builder.Services.AddHttpClient<IReferenceLookup, HttpReferenceLookup>(c =>
{
    c.BaseAddress = new Uri(referenceUrl.EndsWith('/') ? referenceUrl : referenceUrl + "/");
    c.Timeout = TimeSpan.FromSeconds(10);
});

// Identity: Keycloak (OpenID Connect), same rules as the other services. Login comes after Basic Information.
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

builder.Services.AddScoped<BankAccountUseCases>();

var app = builder.Build();

app.UseExceptionHandler();
if (!string.IsNullOrWhiteSpace(keycloakAuthority))
{
    app.UseAuthentication();
}

app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "accounts" }));
app.MapBankAccountEndpoints(requireAuthorization: !string.IsNullOrWhiteSpace(keycloakAuthority));

await app.RunAsync().ConfigureAwait(false);

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
