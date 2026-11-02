using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Mapping;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;

public sealed class CreateAccountService
{
    private readonly IAccountRepository _accounts;
    private readonly TimeProvider _clock;

    public CreateAccountService(IAccountRepository accounts, TimeProvider clock)
    {
        _accounts = accounts;
        _clock = clock;
    }

    public async Task<AccountCreationResult> CreateAsync(
        string rawAccountNumber,
        string rawPin,
        decimal initialBalance,
        string ownerSubject,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawAccountNumber) || rawAccountNumber.Length > 64 ||
            string.IsNullOrWhiteSpace(rawPin) || rawPin.Length > 128 ||
            string.IsNullOrWhiteSpace(ownerSubject) || ownerSubject.Length > 200 ||
            initialBalance < 0 || initialBalance > Money.Maximum || !Money.HasKopeckPrecision(initialBalance))
            return new(AccountCreationStatus.InvalidInput);

        var number = new AccountNumber(rawAccountNumber);
        var pin = new PinCode(rawPin);
        var balance = new Money(initialBalance);
        DateTime now = _clock.GetUtcNow().UtcDateTime;

        var account = new Account(
            key: Guid.NewGuid(),
            number: number,
            pinCode: pin,
            initialBalance: balance,
            createdAtUtc: now,
            ownerSubject: ownerSubject);

        Transaction? opening = initialBalance > 0
            ? new Transaction(Guid.NewGuid(), Guid.NewGuid(), account.Key, account.Number,
                new Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType.TransactionType.Opening(),
                balance, now, balance)
            : null;

        bool created = await _accounts.CreateAsync(account, opening, ct);
        return created
            ? new(AccountCreationStatus.Success, account.AsDto())
            : new(AccountCreationStatus.DuplicateNumber);
    }
}
