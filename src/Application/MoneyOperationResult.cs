namespace Itmo.ObjectOrientedProgramming.Lab5.Application;

public enum MoneyOperationStatus
{
    Success,
    AccessDenied,
    AccountNotFound,
    InvalidAmount,
    InsufficientFunds,
    SameAccount,
    IdempotencyConflict,
    InvalidIdempotencyKey,
    InvalidPin,
    PinLocked,
}

public sealed record MoneyOperationResult(MoneyOperationStatus Status, decimal? BalanceAfter = null,
    Guid? OperationId = null)
{
    public static MoneyOperationResult Success(decimal balance, Guid operationId) =>
        new(MoneyOperationStatus.Success, balance, operationId);
}
