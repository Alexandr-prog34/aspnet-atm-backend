namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;

public abstract record WithdrawResult
{
    private WithdrawResult() { }

    public sealed record Success(decimal BalanceAfter) : WithdrawResult;

    public sealed record InsufficientFunds : WithdrawResult;

    public sealed record InvalidAmount : WithdrawResult;
}
