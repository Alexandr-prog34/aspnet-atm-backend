using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;

public interface IFinancialTransactionRunner
{
    Task<T> ExecuteAsync<T>(Func<IFinancialTransaction, CancellationToken, Task<T>> action,
        CancellationToken ct = default);
}

public interface IFinancialTransaction
{
    Task<IdempotencyClaim> ClaimAsync(string key, Guid accountKey, string requestHash,
        CancellationToken ct = default);

    Task ReleaseClaimAsync(string key, CancellationToken ct = default);

    Task<Account?> FindByNumberAsync(string number, CancellationToken ct = default);

    Task<Account?> LockAccountAsync(Guid key, CancellationToken ct = default);

    void SaveAccount(Account account);

    void AddTransaction(Transaction transaction);

    Task CompleteAsync(string key, MoneyOperationResult result, CancellationToken ct = default);
}

public sealed record IdempotencyClaim(bool IsNew, string? RequestHash, MoneyOperationResult? Result);
