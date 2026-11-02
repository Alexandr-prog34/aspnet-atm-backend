using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;

public sealed class Transaction
{
    public Guid Id { get; }

    public Guid OperationId { get; }

    public Guid AccountKey { get; }

    public AccountNumber AccountNumber { get; }

    public TransactionType Type { get; }

    public Money Amount { get; }

    public DateTime OccurredAtUtc { get; }

    public Money BalanceAfter { get; }

    public Transaction(
        Guid id,
        Guid operationId,
        Guid accountKey,
        AccountNumber accountNumber,
        TransactionType type,
        Money amount,
        DateTime occurredAtUtc,
        Money balanceAfter)
    {
        Id = id;
        OperationId = operationId;
        AccountKey = accountKey;
        AccountNumber = accountNumber;
        Type = type;
        Amount = amount;
        OccurredAtUtc = occurredAtUtc;
        BalanceAfter = balanceAfter;
    }
}
