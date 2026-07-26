using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;

public sealed class Transaction
{
    public Guid Id { get; }

    public AccountNumber AccountNumber { get; }

    public TransactionType Type { get; }

    public Money Amount { get; }

    public DateTime OccurredAtUtc { get; }

    public Money BalanceAfter { get; }

    public Transaction(
        Guid id,
        AccountNumber accountNumber,
        TransactionType type,
        Money amount,
        DateTime occurredAtUtc,
        Money balanceAfter)
    {
        Id = id;
        AccountNumber = accountNumber;
        Type = type;
        Amount = amount;
        OccurredAtUtc = occurredAtUtc;
        BalanceAfter = balanceAfter;
    }
}
