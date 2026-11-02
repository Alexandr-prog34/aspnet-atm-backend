namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;

public record TransactionHistoryItemResponse(
    Guid Id,
    Guid OperationId,
    string AccountNumber,
    string Type,
    decimal Amount,
    DateTime OccurredAtUtc,
    decimal BalanceAfter);
