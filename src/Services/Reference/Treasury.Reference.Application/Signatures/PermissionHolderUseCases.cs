using System.Globalization;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Application.Signatures;

/// <summary>The form «دارندگان مجوزها (فرم‌ها)» as the screen sends it.</summary>
/// <param name="JobTitle">«سمت سازمانی / شغل».</param>
/// <param name="Roles">The ticked roles, each with the treasury operations ticked under it.</param>
public sealed record PermissionHolderForm(
    string? FullName, string? JobTitle, IReadOnlyList<PermissionRoleForm>? Roles, string? DetailCode = null, string? NationalCode = null, string? Mobile = null,
    string? Phone = null, string? Extension = null, string? Email = null)
{
    public PermissionHolderData ToData() => new(new PersonForm(FullName, DetailCode, NationalCode, Mobile, Phone, Extension, Email), JobTitle, Roles);
}

/// <param name="Place">«سایر» only: where the role stands among the fixed steps of the approval flow.</param>
/// <param name="Step">The role's order in the flow: 10, 20, 30, 40 for the fixed roles, 5–45 for «سایر».</param>
public sealed record PermissionRoleView(string Kind, string Title, IReadOnlyList<string> Operations, string? Place = null, int Step = 0)
{
    public static PermissionRoleView From(PermissionRole r) => new(r.Kind.ToString(), r.Title, r.Operations.Select(o => o.ToString()).ToList(), r.Place?.ToString(), r.Step);
}

public sealed record PermissionHolderView(
    Guid Id, string FullName, string JobTitle, IReadOnlyList<PermissionRoleView> Roles, string? DetailCode, string? NationalCode, string? Mobile, string? Phone,
    string? Extension, string? Email, bool HasSignature, bool IsActive, long Version)
{
    public static PermissionHolderView From(PermissionHolder h) => new(
        h.Id, h.FullName, h.JobTitle, h.Roles.Select(PermissionRoleView.From).ToList(), h.Person.DetailCode, h.Person.NationalCode, h.Person.Mobile, h.Person.Phone,
        h.Person.Extension, h.Person.Email, h.HasSignature, h.IsActive, h.Version);
}

/// <summary>A holder as the form of one operation shows and prints it: the name, the roles held in that operation and the job title.</summary>
/// <param name="Position">What is printed over the signature box: the roles, then the job title.</param>
public sealed record OperationHolderView(Guid Id, string FullName, string JobTitle, IReadOnlyList<string> Roles, string Position, bool HasSignature);

/// <summary>
/// «دارندگان مجوزها (فرم‌ها)» (owner's report no. 8, 21 Nov 2026): the staff who confirm or approve treasury operations, with their
/// roles and, per role, the operations. The forms of an operation ask reference-svc who holds a permission in it.
/// </summary>
public sealed class PermissionHolderUseCases(IUnitOfWork unitOfWork, IPermissionHolderRepository holders, ILedgerQueries ledger, IIdempotencyStore idempotency)
{
    private static readonly StringComparer PersianOrder = StringComparer.Create(CultureInfo.GetCultureInfo("fa-IR"), ignoreCase: true);
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> AddAsync(string key, PermissionHolderForm form, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var holder = PermissionHolder.Register(Guid.NewGuid(), form.ToData(), await PersonLookup.DetailAsync(ledger, form.DetailCode, ct).ConfigureAwait(false));
            if (await TakenAsync(holder, ct).ConfigureAwait(false) is { } refused)
            {
                return refused;
            }

            await holders.AddAsync(holder, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(holder.Id);
        }, cancellationToken);

    public Task<Result<Guid>> EditAsync(string key, Guid id, PermissionHolderForm form, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var holder = Check(await holders.GetAsync(id, ct).ConfigureAwait(false), expectedVersion, id);
            holder.Edit(form.ToData(), await PersonLookup.DetailAsync(ledger, form.DetailCode, ct).ConfigureAwait(false));
            if (await TakenAsync(holder, ct).ConfigureAwait(false) is { } refused)
            {
                return refused;
            }

            await holders.SaveAsync(holder, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(holder.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeAsync(SignatureStateCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var holder = Check(await holders.GetAsync(command.Id, ct).ConfigureAwait(false), command.ExpectedVersion, command.Id);
            switch (command.Change)
            {
                case "activate":
                    holder.Activate();
                    break;
                case "deactivate":
                    holder.Deactivate();
                    break;
                case "delete":
                    holder.Delete();
                    await holders.DeleteAsync(holder, ct).ConfigureAwait(false);
                    return Result<Guid>.Ok(holder.Id);
                default:
                    return Result<Guid>.Fail("Request.UnknownChange", $"Unknown change '{command.Change}'.");
            }

            await holders.SaveAsync(holder, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(holder.Id);
        }, cancellationToken);

    public Task<Result<Guid>> AttachSignatureAsync(string key, Guid id, byte[] file, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(file);
            var holder = Check(await holders.GetAsync(id, ct).ConfigureAwait(false), expectedVersion, id);
            holder.AttachSignature(file);
            await holders.SaveAsync(holder, ct, file).ConfigureAwait(false);
            return Result<Guid>.Ok(holder.Id);
        }, cancellationToken);

    public Task<Result<Guid>> RemoveSignatureAsync(string key, Guid id, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            var holder = Check(await holders.GetAsync(id, ct).ConfigureAwait(false), expectedVersion, id);
            holder.RemoveSignature();
            await holders.SaveAsync(holder, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(holder.Id);
        }, cancellationToken);

    public Task<IReadOnlyList<PermissionHolderView>> ListAsync(bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<PermissionHolderView>>(async ct =>
            (await holders.ListAsync(includeInactive, ct).ConfigureAwait(false)).OrderBy(h => h.FullName, PersianOrder).Select(PermissionHolderView.From).ToList(),
            cancellationToken);

    /// <summary>
    /// The active holders of a permission in one operation (asked by the forms of that operation), in the order of the approval flow
    /// (owner, 22 Nov 2026): first confirmers, second confirmers, first approvers, second approvers, with a role under «سایر» at the
    /// place it was given; within a step by name.
    /// </summary>
    public Task<IReadOnlyList<OperationHolderView>> ListForOperationAsync(TreasuryOperation operation, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<OperationHolderView>>(async ct =>
            (await holders.ListAsync(includeInactive: false, ct).ConfigureAwait(false))
                .Select(h => (Holder: h, Roles: h.RolesIn(operation)))
                .Where(x => x.Roles.Count > 0)
                .OrderBy(x => x.Roles[0].Step).ThenBy(x => x.Holder.FullName, PersianOrder)
                .Select(x =>
                {
                    var titles = x.Roles.Select(r => r.Title).ToList();
                    return new OperationHolderView(x.Holder.Id, x.Holder.FullName, x.Holder.JobTitle, titles, $"{string.Join("، ", titles)} – {x.Holder.JobTitle}", x.Holder.HasSignature);
                })
                .ToList(),
            cancellationToken);

    public Task<(string ContentType, byte[] Data)?> GetSignatureAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(ct => holders.GetSignatureAsync(id, ct), cancellationToken);

    /// <summary>«فهرست عملیات خزانه داری» in the order the screens show it.</summary>
    public static IReadOnlyList<string> Operations { get; } = Enum.GetNames<TreasuryOperation>();

    private async Task<Result<Guid>?> TakenAsync(PermissionHolder holder, CancellationToken ct)
    {
        var others = (await holders.ListAsync(includeInactive: true, ct).ConfigureAwait(false)).Where(h => h.Id != holder.Id).ToList();
        if (others.Any(h => h.NameKey == holder.NameKey))
        {
            return Result<Guid>.Fail("PermissionHolder.NameTaken", $"'{holder.FullName}' is already a permission holder.");
        }

        return holder.Person.NationalCode is { } code && others.Any(h => h.Person.NationalCode == code)
            ? Result<Guid>.Fail("PermissionHolder.NationalCodeTaken", $"National code {code} belongs to another permission holder.")
            : null;
    }

    private static PermissionHolder Check(PermissionHolder? holder, long expectedVersion, Guid id) =>
        holder is null ? throw new DomainException("PermissionHolder.NotFound", $"Permission holder {id} was not found.")
        : holder.Version == expectedVersion ? holder
        : throw new ConcurrencyConflictException($"Permission holder {id} was changed by someone else.");

    private Task<Result<Guid>> RunAsync(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
