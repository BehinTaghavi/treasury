using System.Net;
using System.Net.Http.Json;
using Treasury.BuildingBlocks.Domain;
using Treasury.Requests.Application.Abstractions;
using Treasury.Requests.Domain.PaymentRequests;

namespace Treasury.Requests.Infrastructure;

/// <summary>Reads JSON from another service; 404 means "unknown"; failing to reach it becomes <paramref name="code"/>.</summary>
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
/// Requesters, beneficiaries and purpose groups, asked from reference-svc over its API (ADR 0001). Base address: Services:Reference;
/// in Docker Compose http://reference-api:8080. reference-svc lists requesters and beneficiaries (a few hundred) without a route for one.
/// </summary>
public sealed class HttpRequestReferenceLookup(HttpClient http) : IRequestReferenceLookup
{
    private sealed record Requester(Guid Id, string FullName, Guid? DepartmentId, string? CostCenterCode, bool IsActive);

    private sealed record Beneficiary(Guid Id, string DetailCode, string LegalName, string? Sheba, bool IsActive);

    private sealed record Group(Guid Id, string Name, string Direction, bool IsActive);

    private sealed record Department(Guid Id, string Name, string[]? CostCenterCodes, bool IsActive);

    private sealed record Detail(string Code, string Name, bool IsCostCenter, string? Role, bool IsActive);

    public async Task<RequesterFact?> RequesterAsync(Guid id, CancellationToken cancellationToken) =>
        (await ServiceHttp.GetAsync<Requester[]>(http, "api/reference/requesters?includeInactive=true", "Reference.Unavailable", cancellationToken).ConfigureAwait(false) ?? [])
            .Where(r => r.Id == id).Select(r => new RequesterFact(r.Id, r.FullName, r.DepartmentId, r.CostCenterCode, r.IsActive)).FirstOrDefault();

    public async Task<BeneficiaryFact?> BeneficiaryAsync(Guid id, CancellationToken cancellationToken) =>
        (await ServiceHttp.GetAsync<Beneficiary[]>(http, "api/reference/beneficiaries?includeInactive=true", "Reference.Unavailable", cancellationToken).ConfigureAwait(false) ?? [])
            .Where(b => b.Id == id).Select(b => new BeneficiaryFact(b.Id, b.DetailCode, b.LegalName, b.Sheba, b.IsActive)).FirstOrDefault();

    public async Task<PurposeGroupFact?> PurposeGroupAsync(Guid id, CancellationToken cancellationToken) =>
        await ServiceHttp.GetAsync<Group>(http, $"api/reference/purpose-groups/{id}", "Reference.Unavailable", cancellationToken).ConfigureAwait(false) is { } g
            ? new PurposeGroupFact(g.Id, g.Name, g.Direction == "Payment", g.IsActive)
            : null;

    public async Task<DepartmentFact?> DepartmentAsync(Guid id, CancellationToken cancellationToken) =>
        (await ServiceHttp.GetAsync<Department[]>(http, "api/reference/departments?includeInactive=true", "Reference.Unavailable", cancellationToken).ConfigureAwait(false) ?? [])
            .Where(d => d.Id == id).Select(d => new DepartmentFact(d.Id, d.Name, d.CostCenterCodes ?? [], d.IsActive)).FirstOrDefault();

    /// <summary>A cost centre is a تفصیل marked so or of a cost-centre type (owner, 13 Nov 2026), as reference-svc's role says.</summary>
    public async Task<CostCenterFact?> CostCenterAsync(string code, CancellationToken cancellationToken) =>
        await ServiceHttp.GetAsync<Detail>(http, $"api/reference/ledger/details/{Uri.EscapeDataString(code)}", "Reference.Unavailable", cancellationToken).ConfigureAwait(false) is { } d
            ? new CostCenterFact(d.Code, d.Name, d.IsCostCenter || d.Role == "CostCentre", d.IsActive)
            : null;

    public async Task MarkInUseAsync(string list, Guid id, CancellationToken cancellationToken)
    {
        var path = $"api/reference/{list}/{id}/in-use";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative));
            // Marking is the same every time, so one fixed key per entry is enough.
            request.Headers.Add("Idempotency-Key", $"requests-in-use:{list}:{id}");
            using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new DomainException("Reference.Unavailable", $"reference-svc could not answer ({path}): {ex.Message}");
        }
    }
}

/// <summary>The bank account or cash desk a request is to be paid from, asked from accounts-svc (ADR 0001). Base address: Services:Accounts.</summary>
public sealed class HttpPayFromLookup(HttpClient http) : IPayFromLookup
{
    private sealed record Account(Guid Id, string AccountNumber, string CurrencyCode, bool IsActive);

    private sealed record Desk(Guid Id, string Name, string CurrencyCode, bool IsActive);

    public async Task<PayFromFact?> BankAccountAsync(Guid id, CancellationToken cancellationToken) =>
        await ServiceHttp.GetAsync<Account>(http, $"api/accounts/bank-accounts/{id}", "Accounts.Unavailable", cancellationToken).ConfigureAwait(false) is { } a
            ? new PayFromFact(PayFromKind.BankAccount, a.Id, a.AccountNumber, a.CurrencyCode, a.IsActive)
            : null;

    public async Task<PayFromFact?> CashDeskAsync(Guid id, CancellationToken cancellationToken) =>
        (await ServiceHttp.GetAsync<Desk[]>(http, "api/accounts/cash-desks?includeInactive=true", "Accounts.Unavailable", cancellationToken).ConfigureAwait(false) ?? [])
            .Where(d => d.Id == id).Select(d => new PayFromFact(PayFromKind.CashDesk, d.Id, d.Name, d.CurrencyCode, d.IsActive)).FirstOrDefault();
}
