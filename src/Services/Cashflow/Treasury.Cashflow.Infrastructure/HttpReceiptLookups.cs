using System.Net;
using System.Net.Http.Json;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.Receipts;

namespace Treasury.Cashflow.Infrastructure;

/// <summary>Reads JSON from another service; 404 means "unknown", anything else unreachable becomes <paramref name="code"/>.</summary>
internal static class ServiceHttp
{
    public static async Task<T?> GetAsync<T>(HttpClient http, string path, string code, CancellationToken ct)
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
            throw new DomainException(code, $"{code}: could not answer ({path}): {ex.Message}");
        }
    }
}

/// <summary>
/// Cash desks and bank accounts a receipt is booked to, asked from accounts-svc over its API (ADR 0001). Base address:
/// Services:Accounts.
/// </summary>
public sealed class HttpReceiptAccountLookup(HttpClient http) : IReceiptAccountLookup
{
    private sealed record Desk(Guid Id, string Name, string CurrencyCode, bool IsActive, string? LedgerAccountCode, string? HolderDetailCode);

    private sealed record Account(Guid Id, string AccountNumber, string CurrencyCode, bool IsActive, string? LedgerAccountCode, string? Detail1Code);

    public async Task<ReceiptAccountFact?> CashDeskAsync(Guid id, CancellationToken cancellationToken)
    {
        // accounts-svc lists cash desks (a handful per company); there is no route for one desk.
        var desks = await ServiceHttp.GetAsync<Desk[]>(http, "api/accounts/cash-desks?includeInactive=true", "Accounts.Unavailable", cancellationToken).ConfigureAwait(false) ?? [];
        return desks.Where(d => d.Id == id).Select(d => new ReceiptAccountFact(ReceiptAccountKind.CashDesk, d.Id, d.Name, d.CurrencyCode, d.IsActive, d.LedgerAccountCode, d.HolderDetailCode)).FirstOrDefault();
    }

    public async Task<ReceiptAccountFact?> BankAccountAsync(Guid id, CancellationToken cancellationToken) =>
        await ServiceHttp.GetAsync<Account>(http, $"api/accounts/bank-accounts/{id}", "Accounts.Unavailable", cancellationToken).ConfigureAwait(false) is { } a
            ? new ReceiptAccountFact(ReceiptAccountKind.BankAccount, a.Id, a.AccountNumber, a.CurrencyCode, a.IsActive, a.LedgerAccountCode, a.Detail1Code)
            : null;
}

/// <summary>Payers' تفصیل, purpose groups and bank names, asked from reference-svc over its API (ADR 0001). Base address: Services:Reference.</summary>
public sealed class HttpReceiptReferenceLookup(HttpClient http) : IReceiptReferenceLookup
{
    private sealed record Detail(string Code, string Name, bool IsActive, string? Role = null);

    private sealed record Group(Guid Id, string Name, string Direction, bool IsActive, string? LedgerAccountCode);

    private sealed record Ledger(string Code, string Name, bool IsPostable, bool IsActive);

    public async Task<Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact?> LedgerAccountAsync(string code, CancellationToken cancellationToken) =>
        await ServiceHttp.GetAsync<Ledger>(http, $"api/reference/ledger/accounts/{Uri.EscapeDataString(code)}", "Reference.Unavailable", cancellationToken).ConfigureAwait(false) is { } l
            ? new Treasury.Cashflow.Domain.Vouchers.LedgerAccountFact(l.Code, l.Name, l.IsPostable, l.IsActive)
            : null;

    private sealed record Entry(Guid Id, string Name);

    public async Task<PayerFact?> PayerAsync(string detailCode, CancellationToken cancellationToken) =>
        await ServiceHttp.GetAsync<Detail>(http, $"api/reference/ledger/details/{Uri.EscapeDataString(detailCode)}", "Reference.Unavailable", cancellationToken).ConfigureAwait(false) is { } d
            ? new PayerFact(d.Code, d.Name, d.IsActive, d.Role is "Customer" or "PersonOrCompany")
            : null;

    public async Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken) =>
        await ServiceHttp.GetAsync<Group>(http, $"api/reference/purpose-groups/{id}", "Reference.Unavailable", cancellationToken).ConfigureAwait(false) is { } g
            ? new PurposeGroupFact(g.Id, g.Name, g.Direction == "Receipt", g.IsActive, g.LedgerAccountCode)
            : null;

    public async Task<string?> BankNameAsync(Guid id, CancellationToken cancellationToken) =>
        (await ServiceHttp.GetAsync<Entry>(http, $"api/reference/banks/{id}", "Reference.Unavailable", cancellationToken).ConfigureAwait(false))?.Name;

    private sealed record RuleSignatory(Guid Id, string FullName, string? Position);

    private sealed record Rule(int Required, RuleSignatory[]? Compulsory, RuleSignatory[]? Optional);

    public async Task<SignatureRuleFact?> SignatureRuleAsync(string document, CancellationToken cancellationToken)
    {
        static List<SignerFact> Facts(RuleSignatory[]? list) => (list ?? []).Select(s => new SignerFact(s.Id, s.FullName, s.Position)).ToList();
        return await ServiceHttp.GetAsync<Rule>(http, $"api/reference/signature-rules/{Uri.EscapeDataString(document)}", "Reference.Unavailable", cancellationToken)
            .ConfigureAwait(false) is { } r ? new SignatureRuleFact(r.Required, Facts(r.Compulsory), Facts(r.Optional)) : null;
    }

    public async Task MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken)
    {
        var path = $"api/reference/{list}/{id}/in-use";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative));
            // Marking is the same every time, so one fixed key per entry is enough.
            request.Headers.Add("Idempotency-Key", $"cashflow-in-use:{list}:{id}");
            using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new DomainException("Reference.Unavailable", $"reference-svc could not answer ({path}): {ex.Message}");
        }
    }
}
