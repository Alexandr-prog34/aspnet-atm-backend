namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;

public abstract record WithDrawResultDto
{
    public sealed record Success(decimal BalanceAfter) : WithDrawResultDto;

    public sealed record InvalidAmount : WithDrawResultDto;

    public sealed record AccountNotFound : WithDrawResultDto;
}