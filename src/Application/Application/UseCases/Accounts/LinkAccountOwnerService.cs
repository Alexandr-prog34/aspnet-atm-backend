using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;

public enum LinkOwnerStatus { Success, NotFound, Conflict, InvalidInput }

public sealed class LinkAccountOwnerService(IAccountRepository accounts)
{
    public async Task<LinkOwnerStatus> LinkAsync(Guid accountId, string ownerSubject, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ownerSubject) || ownerSubject.Length > 200)
            return LinkOwnerStatus.InvalidInput;
        if (await accounts.GetByKeyAsync(accountId, ct) is null) return LinkOwnerStatus.NotFound;
        return await accounts.SetOwnerAsync(accountId, ownerSubject, ct)
            ? LinkOwnerStatus.Success : LinkOwnerStatus.Conflict;
    }
}
