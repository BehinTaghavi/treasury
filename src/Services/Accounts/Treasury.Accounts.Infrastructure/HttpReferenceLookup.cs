using System.Net;
using System.Net.Http.Json;
using Treasury.Accounts.Application.Abstractions;
using Treasury.Accounts.Domain.BankAccounts;
using Treasury.Accounts.Domain.PettyCash;
using Treasury.BuildingBlocks.Domain;

namespace Treasury.Accounts.Infrastructure;

/// <summary>
/// Asks reference-svc over its HTTP API (ADR 0001: no reads of another service's tables). The base address comes from
/// configuration (Services:Reference); in Docker Compose it is http://reference-api:8080.
/// </summary>
public sealed class HttpReferenceLookup(HttpClient http) : IReferenceLookup
{
    private sealed record Entry(Guid Id, string Name, string? Code, bool IsActive);

    private sealed record Account(string Code, bool IsActive, bool IsPostable, string[] Detail1Types, string[] Detail2Types);

    private sealed record Group(Guid Id, string Direction, bool IsActive);

    private sealed record Detail(string Code, string? Type, bool IsActive, string? Name = null);

    public Task<ReferenceEntry?> BankAsync(Guid id, CancellationToken cancellationToken) => EntryAsync($"api/reference/banks/{id}", cancellationToken);

    public Task<ReferenceEntry?> CityAsync(Guid id, CancellationToken cancellationToken) => EntryAsync($"api/reference/cities/{id}", cancellationToken);

    public async Task<ReferenceEntry?> CurrencyAsync(string code, CancellationToken cancellationToken)
    {
        var list = await GetAsync<Entry[]>("api/reference/currencies?includeInactive=true", cancellationToken).ConfigureAwait(false) ?? [];
        return list.Where(c => string.Equals(c.Code, code, StringComparison.Ordinal)).Select(c => new ReferenceEntry(c.Id, c.Name, c.Code, c.IsActive)).FirstOrDefault();
    }

    public async Task<LedgerAccountFact?> LedgerAccountAsync(string code, CancellationToken cancellationToken)
    {
        var a = await GetAsync<Account>($"api/reference/ledger/accounts/{Uri.EscapeDataString(code)}", cancellationToken).ConfigureAwait(false);
        return a is null ? null : new LedgerAccountFact(a.Code, a.IsActive, a.IsPostable, a.Detail1Types.ToHashSet(StringComparer.Ordinal), a.Detail2Types.ToHashSet(StringComparer.Ordinal));
    }

    public async Task<DetailFact?> DetailAsync(string code, CancellationToken cancellationToken)
    {
        var d = await GetAsync<Detail>($"api/reference/ledger/details/{Uri.EscapeDataString(code)}", cancellationToken).ConfigureAwait(false);
        return d is null ? null : new DetailFact(d.Code, d.Type, d.IsActive);
    }

    public async Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken) =>
        await GetAsync<Group>($"api/reference/purpose-groups/{id}", cancellationToken).ConfigureAwait(false) is { } g
            ? new PurposeGroupFact(g.Id, g.Direction == "Payment", g.IsActive)
            : null;

    public async Task<string?> DetailNameAsync(string code, CancellationToken cancellationToken) =>
        (await GetAsync<Detail>($"api/reference/ledger/details/{Uri.EscapeDataString(code)}", cancellationToken).ConfigureAwait(false))?.Name;

    public async Task<bool> MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken)
    {
        var path = $"api/reference/{list}/{id}/in-use";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative));
            // Marking is the same every time, so one fixed key per entry is enough.
            request.Headers.Add("Idempotency-Key", $"in-use:{id}");
            using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new DomainException("Reference.Unavailable", $"reference-svc could not answer ({path}): {ex.Message}");
        }
    }

    private async Task<ReferenceEntry?> EntryAsync(string path, CancellationToken ct)
    {
        var e = await GetAsync<Entry>(path, ct).ConfigureAwait(false);
        return e is null ? null : new ReferenceEntry(e.Id, e.Name, e.Code, e.IsActive);
    }

    /// <summary>Null on 404 (not found); anything else that is not a success means reference-svc cannot answer now.</summary>
    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
        where T : class
    {
        try
        {
            using var response = await http.GetAsync(new Uri(path, UriKind.Relative), ct).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            throw new DomainException("Reference.Unavailable", $"reference-svc could not answer ({path}): {ex.Message}");
        }
    }
}
