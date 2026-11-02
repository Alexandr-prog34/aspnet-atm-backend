using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Mapping;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;

public sealed class GetBalanceService
{
    private readonly IAccountRepository _accounts;
    public GetBalanceService(IAccountRepository accounts) => _accounts = accounts;

    public async Task<AccountAccessResult<AccountDto>> GetAsync(
        string ownerSubject,
        CancellationToken ct = default)
    {
        Account? account = await _accounts.GetByOwnerSubjectAsync(ownerSubject, ct);
        if (account is null) return new(AccountAccessStatus.AccountNotFound);

        return new(AccountAccessStatus.Success, account.AsDto());
    }
}
