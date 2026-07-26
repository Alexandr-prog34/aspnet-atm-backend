using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Mapping;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;

public sealed class CreateAccountService
{
    private readonly IAccountRepository _accounts;

    public CreateAccountService(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<AccountDto> CreateAsync(
        string rawAccountNumber,
        string rawPin,
        decimal initialBalance,
        CancellationToken ct = default)
    {
        var number = new AccountNumber(rawAccountNumber);
        var pin = new PinCode(rawPin);
        var balance = new Money(initialBalance);

        var account = new Account(
            key: Guid.NewGuid(),
            number: number,
            pinCode: pin,
            initialBalance: balance);

        await _accounts.AddAsync(account, ct);

        return account.AsDto();
    }
}
