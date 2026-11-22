using System.Text.Json.Serialization;
using Treasury.BuildingBlocks.Application;
using Treasury.Requests.Api;
using Treasury.Requests.Application.Abstractions;
using Treasury.Requests.Application.PaymentRequests;
using Treasury.Requests.Infrastructure;
using Treasury.Requests.Infrastructure.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();

// Storage: PostgreSQL when ConnectionStrings:Treasury is set (role requests_svc), otherwise in-memory (local runs, demos).
var connectionString = builder.Configuration.GetConnectionString("Treasury");
var usePostgres = false;
#if WITH_PACKAGES
if (!string.IsNullOrWhiteSpace(connectionString))
{
    Treasury.Requests.Infrastructure.Postgres.PostgresRegistration.AddRequestsPostgres(builder.Services, connectionString);
    usePostgres = true;
}
#endif
if (!usePostgres)
{
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("A database connection string is set, but this build has no PostgreSQL support (built with -p:Offline=true).");
    }

    builder.Services.AddSingleton<InMemoryRequestsDatabase>();
    builder.Services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
    builder.Services.AddSingleton<IPaymentRequestRepository, InMemoryPaymentRequestRepository>();
    builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
}

// reference-svc answers about requesters, beneficiaries and purpose groups; accounts-svc about the account paid from; cashflow-svc about
// the received cheque of a request of the kind «ثبت انتقال (خرج چک)» (ADR 0001).
static Uri BaseAddress(string url) => new(url.EndsWith('/') ? url : url + "/");
var referenceUrl = builder.Configuration["Services:Reference"] ?? throw new InvalidOperationException("Services:Reference (reference-svc address) is not configured.");
var accountsUrl = builder.Configuration["Services:Accounts"] ?? throw new InvalidOperationException("Services:Accounts (accounts-svc address) is not configured.");
builder.Services.AddHttpClient<IRequestReferenceLookup, HttpRequestReferenceLookup>(c => { c.BaseAddress = BaseAddress(referenceUrl); c.Timeout = TimeSpan.FromSeconds(10); });
builder.Services.AddHttpClient<IPayFromLookup, HttpPayFromLookup>(c => { c.BaseAddress = BaseAddress(accountsUrl); c.Timeout = TimeSpan.FromSeconds(10); });
var cashflowUrl = builder.Configuration["Services:Cashflow"] ?? throw new InvalidOperationException("Services:Cashflow (cashflow-svc address) is not configured.");
builder.Services.AddHttpClient<IReceivedChequeLookup, HttpReceivedChequeLookup>(c => { c.BaseAddress = BaseAddress(cashflowUrl); c.Timeout = TimeSpan.FromSeconds(10); });

// Identity: Keycloak (OpenID Connect), same rules as the other services.
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

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<PaymentRequestUseCases>();

var app = builder.Build();

app.UseExceptionHandler();
if (!string.IsNullOrWhiteSpace(keycloakAuthority))
{
    app.UseAuthentication();
}

app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "requests" }));
app.MapPaymentRequestEndpoints(requireAuthorization: !string.IsNullOrWhiteSpace(keycloakAuthority));

await app.RunAsync().ConfigureAwait(false);

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
