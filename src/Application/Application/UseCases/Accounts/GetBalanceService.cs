using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Mapping;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;

public sealed class GetBalanceService
{
    private readonly IAccountRepository _accounts;

    public GetBalanceService(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<AccountDto?> GetByNumberAsync(
        string rawAccountNumber,
        CancellationToken ct = default)
    {
        var number = new AccountNumber(rawAccountNumber);

        Account? account = await _accounts.GetByNumberAsync(number, ct);
        if (account is null) return null;

        return account.AsDto();
    }
}
