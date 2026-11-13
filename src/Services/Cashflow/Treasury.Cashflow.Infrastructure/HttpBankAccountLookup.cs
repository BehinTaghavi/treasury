using System.Net;
using System.Net.Http.Json;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;

namespace Treasury.Cashflow.Infrastructure;

/// <summary>
/// Asks accounts-svc about one of our bank accounts over its HTTP API (ADR 0001: never its tables). The base address comes from
/// configuration (Services:Accounts); in Docker Compose it is http://accounts-api:8080.
/// </summary>
public sealed class HttpBankAccountLookup(HttpClient http) : IBankAccountLookup
{
    private sealed record Account(Guid Id, string AccountNumber, bool IsActive);

    public async Task<BankAccountFact?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var path = $"api/accounts/bank-accounts/{id}";
        try
        {
            using var response = await http.GetAsync(new Uri(path, UriKind.Relative), cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var a = await response.Content.ReadFromJsonAsync<Account>(cancellationToken).ConfigureAwait(false);
            return a is null ? null : new BankAccountFact(a.Id, a.AccountNumber, a.IsActive);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            throw new DomainException("Accounts.Unavailable", $"accounts-svc could not answer ({path}): {ex.Message}");
        }
    }

    public async Task MarkInUseAsync(Guid id, CancellationToken cancellationToken)
    {
        var path = $"api/accounts/bank-accounts/{id}/in-use";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative));
            // Same key for the same account: marking twice is harmless and answered from the idempotency store.
            request.Headers.Add("Idempotency-Key", $"cashflow-in-use:{id}");
            using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new DomainException("Accounts.Unavailable", $"accounts-svc could not answer ({path}): {ex.Message}");
        }
    }
}
