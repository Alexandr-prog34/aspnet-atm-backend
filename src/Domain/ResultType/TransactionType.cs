namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;

public abstract record TransactionType
{
    private TransactionType() { }

    public sealed record Deposit : TransactionType;

    public sealed record Withdrawal : TransactionType;
}