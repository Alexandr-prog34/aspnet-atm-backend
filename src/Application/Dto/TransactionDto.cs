namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;

public sealed record TransactionDto(Guid Id,
    string AccountNumber,
    string Type,
    decimal Amount,
    DateTime OccurredAtUtc,
    decimal BalanceAfter);