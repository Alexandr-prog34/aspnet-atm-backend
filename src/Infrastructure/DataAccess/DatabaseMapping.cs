using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;

internal static class DatabaseMapping
{
    public static Account ToDomain(this AccountRow row) =>
        new(row.Id, new AccountNumber(row.Number), PinCode.FromStoredHash(row.PinHash),
            new Money(row.Balance), row.CreatedAtUtc, row.OwnerSub, row.FailedPinAttempts, row.PinLockedUntilUtc);

    public static AccountRow ToRow(this Account account) => new()
    {
        Id = account.Key,
        Number = account.Number.Value,
        PinHash = account.PinCode.StoredHash,
        Balance = account.Balance.Amount,
        CreatedAtUtc = account.CreatedAtUtc,
        OwnerSub = account.OwnerSubject,
        FailedPinAttempts = account.FailedPinAttempts,
        PinLockedUntilUtc = account.PinLockedUntilUtc,
    };

    public static FinancialEntryRow ToRow(this Transaction transaction) => new()
    {
        Id = transaction.Id,
        OperationId = transaction.OperationId,
        AccountId = transaction.AccountKey,
        Type = transaction.Type switch
        {
            TransactionType.Deposit => "deposit",
            TransactionType.Withdrawal => "withdrawal",
            TransactionType.Opening => "opening",
            TransactionType.TransferOut => "transfer_out",
            TransactionType.TransferIn => "transfer_in",
            _ => throw new ArgumentOutOfRangeException(nameof(transaction)),
        },
        Amount = transaction.Amount.Amount,
        BalanceAfter = transaction.BalanceAfter.Amount,
        OccurredAtUtc = transaction.OccurredAtUtc,
    };

    public static Transaction ToDomain(this FinancialEntryRow row, AccountNumber accountNumber) =>
        new(row.Id, row.OperationId, row.AccountId, accountNumber, row.Type switch
        {
            "deposit" => new TransactionType.Deposit(),
            "withdrawal" => new TransactionType.Withdrawal(),
            "opening" => new TransactionType.Opening(),
            "transfer_out" => new TransactionType.TransferOut(),
            "transfer_in" => new TransactionType.TransferIn(),
            _ => throw new InvalidOperationException("Unknown transaction type in database."),
        }, new Money(row.Amount), row.OccurredAtUtc, new Money(row.BalanceAfter));
}
