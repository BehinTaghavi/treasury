using System.Net;
using System.Net.Http.Json;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;

namespace Treasury.Cashflow.Infrastructure;

/// <summary>
/// Payment requests in requests-svc, over its API (ADR 0001). Base address: Services:Requests. An endorsed received cheque pays an approved
/// request (owner, answer to Q36); a refusal by requests-svc comes back with its own error code (e.g. PaymentRequest.Overpaid).
/// </summary>
public sealed class HttpPaymentRequestLookup(HttpClient http) : IPaymentRequestLookup
{
    private const string Unavailable = "Requests.Unavailable";

    private sealed record Request(Guid Id, int Number, string State, string BeneficiaryDetailCode, string BeneficiaryName, Guid PurposeGroupId, string Purpose,
        decimal Amount, string Currency, decimal PaidAmount);

    private sealed record Problem(string? Code, string? Title);

    public async Task<PaymentRequestFact?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await ServiceHttp.GetAsync<Request>(http, $"api/requests/payment-requests/{id}", Unavailable, cancellationToken).ConfigureAwait(false) is { } r
            ? new PaymentRequestFact(r.Id, r.Number, r.State, r.BeneficiaryDetailCode, r.BeneficiaryName, r.PurposeGroupId, r.Purpose, r.Amount, r.Currency, r.PaidAmount)
            : null;

    public Task RecordPaymentAsync(Guid requestId, RequestPaymentInput payment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payment);
        return PostAsync($"api/requests/payment-requests/{requestId}/payments", $"cashflow:{payment.Key}",
            new { payment.Key, Kind = "EndorsedCheque", payment.InstrumentId, payment.Reference, payment.Amount, payment.Currency, payment.Date }, cancellationToken);
    }

    public Task ReversePaymentAsync(Guid requestId, string paymentKey, CancellationToken cancellationToken) =>
        PostAsync($"api/requests/payment-requests/{requestId}/payments/reverse", $"cashflow-reverse:{paymentKey}", new { Key = paymentKey }, cancellationToken);

    private async Task PostAsync(string path, string idempotencyKey, object body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative)) { Content = JsonContent.Create(body) };
            request.Headers.Add("Idempotency-Key", idempotencyKey);
            using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            if (response.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.NotFound or HttpStatusCode.Conflict
                && await response.Content.ReadFromJsonAsync<Problem>(ct).ConfigureAwait(false) is { } p && (p.Code ?? p.Title) is { } code)
            {
                throw new DomainException(code, $"requests-svc refused the payment: {code}");
            }

            throw new DomainException(Unavailable, $"requests-svc answered {(int)response.StatusCode} ({path}).");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            throw new DomainException(Unavailable, $"requests-svc could not answer ({path}): {ex.Message}");
        }
    }
}
