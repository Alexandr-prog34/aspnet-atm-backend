namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;

public sealed record MoneyOperationResponse(Guid OperationId, decimal BalanceAfter);
