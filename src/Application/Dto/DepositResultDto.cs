namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;

public abstract record DepositResultDto
{
    public sealed record Success(decimal BalanceAfter) : DepositResultDto;

    public sealed record InvalidAmount : DepositResultDto;

    public sealed record AccountNotFound : DepositResultDto;
}