using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Operations;

public sealed class DepositService
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;

    public DepositService(IAccountRepository accounts, ITransactionRepository transactions)
    {
        _accounts = accounts;
        _transactions = transactions;
    }

    public async Task<DepositResultDto> DepositAsync(
        string rawAccountNumber,
        decimal amount,
        CancellationToken ct = default)
    {
        var number = new AccountNumber(rawAccountNumber);
        var money = new Money(amount);

        Account? account = await _accounts.GetByNumberAsync(number, ct);
        if (account is null)
        {
            return new DepositResultDto.AccountNotFound();
        }

        DepositResult result = account.Deposit(money);

        if (result is DepositResult.Success success)
        {
            await _accounts.AddAsync(account, ct);

            var transaction = new Transaction(
                id: Guid.NewGuid(),
                accountNumber: number,
                type: new TransactionType.Deposit(),
                amount: money,
                occurredAtUtc: DateTime.UtcNow,
                balanceAfter: new Money(success.BalanceAfter));

            await _transactions.AddAsync(transaction, ct);

            return new DepositResultDto.Success(success.BalanceAfter);
        }

        return result switch
        {
            DepositResult.InvalidAmount => new DepositResultDto.InvalidAmount(),
            _ => new DepositResultDto.InvalidAmount(),
        };
    }
}