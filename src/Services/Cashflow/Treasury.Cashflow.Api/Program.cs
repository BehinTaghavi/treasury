using System.Text.Json.Serialization;
using Treasury.BuildingBlocks.Application;
using Treasury.Cashflow.Api;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Infrastructure.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

// Storage: PostgreSQL when ConnectionStrings:Treasury is set, otherwise in-memory (local runs, demos).
var connectionString = builder.Configuration.GetConnectionString("Treasury");
var usePostgres = false;
#if WITH_PACKAGES
if (!string.IsNullOrWhiteSpace(connectionString))
{
    Treasury.Cashflow.Infrastructure.Postgres.PostgresRegistration.AddCashflowPostgres(builder.Services, connectionString);
    usePostgres = true;
}
#endif
if (!usePostgres)
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("A database connection string is set, but this build has no PostgreSQL support (built with -p:Offline=true).");
    }

    builder.Services.AddSingleton<InMemoryDatabase>();
    builder.Services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
    builder.Services.AddSingleton<IInstrumentRepository, InMemoryInstrumentRepository>();
    builder.Services.AddSingleton<IChequeBookRepository, InMemoryChequeBookRepository>();
    builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
}

// Identity: Keycloak (OpenID Connect). Every API call needs a valid token unless the app runs in Development.
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

// Answers owned by other contexts. Development stand-ins until those services exist.
var policies = new DevelopmentPolicies();
foreach (var year in builder.Configuration.GetSection("Development:OpenFiscalYears").Get<int[]>() ?? [])
{
    policies.OpenYear(year);
}

builder.Services.AddSingleton(policies);
builder.Services.AddSingleton<IFiscalPeriodPolicy>(policies);
builder.Services.AddSingleton<IBankBalanceQuery>(policies);
builder.Services.AddSingleton<IPaymentApprovalQuery>(policies);

builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<InstrumentUseCases>();
builder.Services.AddScoped<Treasury.Cashflow.Application.ChequeBooks.ChequeBookUseCases>();

// accounts-svc answers whether a bank account exists and is active (ADR 0001: through its API, never its tables).
var accountsUrl = builder.Configuration["Services:Accounts"] ?? throw new InvalidOperationException("Services:Accounts (accounts-svc address) is not configured.");
builder.Services.AddHttpClient<IBankAccountLookup, Treasury.Cashflow.Infrastructure.HttpBankAccountLookup>(c =>
{
    c.BaseAddress = new Uri(accountsUrl.EndsWith('/') ? accountsUrl : accountsUrl + "/");
    c.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.UseExceptionHandler();
if (!string.IsNullOrWhiteSpace(keycloakAuthority))
{
    app.UseAuthentication();
}

app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "cashflow" }));
app.MapInstrumentEndpoints(requireAuthorization: !string.IsNullOrWhiteSpace(keycloakAuthority));

if (app.Environment.IsDevelopment() && !usePostgres)
{
    app.MapDevelopmentEndpoints(policies);
}

app.Run();

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
