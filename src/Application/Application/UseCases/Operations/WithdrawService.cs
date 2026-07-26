using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Operations;

public sealed class WithdrawService
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;

    public WithdrawService(IAccountRepository accounts, ITransactionRepository transactions)
    {
        _accounts = accounts;
        _transactions = transactions;
    }

    public async Task<WithDrawResultDto> WithdrawAsync(
        string rawAccountNumber,
        decimal amount,
        CancellationToken ct = default)
    {
        var number = new AccountNumber(rawAccountNumber);
        var money = new Money(amount);

        Account? account = await _accounts.GetByNumberAsync(number, ct);
        if (account is null)
        {
            return new WithDrawResultDto.AccountNotFound();
        }

        WithdrawResult result = account.Withdraw(money);

        if (result is WithdrawResult.Success success)
        {
            await _accounts.AddAsync(account, ct);

            var transaction = new Transaction(
                id: Guid.NewGuid(),
                accountNumber: number,
                type: new TransactionType.Withdrawal(),
                amount: money,
                occurredAtUtc: DateTime.UtcNow,
                balanceAfter: new Money(success.BalanceAfter));

            await _transactions.AddAsync(transaction, ct);

            return new WithDrawResultDto.Success(success.BalanceAfter);
        }

        return result switch
        {
            WithdrawResult.InsufficientFunds => new WithDrawResultDto.InvalidAmount(),
            _ => new WithDrawResultDto.InvalidAmount(),
        };
    }
}
