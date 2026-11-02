namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;

public sealed class AccountRow
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string PinHash { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? OwnerSub { get; set; }
    public int FailedPinAttempts { get; set; }
    public DateTime? PinLockedUntilUtc { get; set; }
}

public sealed class FinancialEntryRow
{
    public Guid Id { get; set; }
    public Guid OperationId { get; set; }
    public Guid AccountId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}

public sealed class IdempotencyRow
{
    public string Key { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? BalanceAfter { get; set; }
    public Guid? OperationId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
