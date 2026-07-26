namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;

public record TransactionHistoryItemResponse(
    Guid Id,
    string AccountNumber,
    string Type,
    decimal Amount,
    DateTime OccurredAtUtc,
    decimal BalanceAfter);
