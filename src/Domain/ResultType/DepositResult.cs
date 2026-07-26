namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;

public abstract record DepositResult
{
    private DepositResult() { }

    public sealed record Success(decimal BalanceAfter) : DepositResult;

    public sealed record InvalidAmount : DepositResult;
}