using System.Data;
using Dapper;
using Itmo.ObjectOrientedProgramming.Lab5.Application;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Npgsql;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;

public sealed class PostgresFinancialTransactionRunner(NpgsqlDataSource dataSource, TimeProvider clock) :
    IFinancialTransactionRunner
{
    public async Task<T> ExecuteAsync<T>(Func<IFinancialTransaction, CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var work = new FinancialTransaction(connection, transaction, clock);
        try
        {
            T result = await action(work, ct);
            await work.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private sealed class FinancialTransaction(
        NpgsqlConnection connection, NpgsqlTransaction transaction, TimeProvider clock) : IFinancialTransaction
    {
        private readonly Dictionary<Guid, Account> _accounts = [];
        private readonly List<Transaction> _entries = [];

        public async Task<IdempotencyClaim> ClaimAsync(string key, Guid accountKey, string requestHash,
            CancellationToken ct = default)
        {
            int inserted = await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO idempotency_keys
                    (idempotency_key, account_id, request_hash, status, created_at_utc)
                VALUES (@Key, @AccountId, @RequestHash, 'pending', @CreatedAtUtc)
                ON CONFLICT (idempotency_key) DO NOTHING
                """,
                new
                {
                    Key = key,
                    AccountId = accountKey,
                    RequestHash = requestHash,
                    CreatedAtUtc = clock.GetUtcNow().UtcDateTime
                }, transaction, cancellationToken: ct));
            if (inserted == 1) return new IdempotencyClaim(true, null, null);

            IdempotencyRow existing = await connection.QuerySingleAsync<IdempotencyRow>(new CommandDefinition(
                """
                SELECT idempotency_key AS "Key", account_id, request_hash, status,
                       balance_after, operation_id, created_at_utc
                FROM idempotency_keys WHERE idempotency_key = @Key
                """, new { Key = key }, transaction, cancellationToken: ct));
            MoneyOperationResult? result = Enum.TryParse(existing.Status, out MoneyOperationStatus status)
                ? new MoneyOperationResult(status, existing.BalanceAfter, existing.OperationId)
                : null;
            return new IdempotencyClaim(false, existing.RequestHash, result);
        }

        public async Task ReleaseClaimAsync(string key, CancellationToken ct = default)
        {
            int deleted = await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM idempotency_keys WHERE idempotency_key = @Key AND status = 'pending'",
                new { Key = key }, transaction, cancellationToken: ct));
            if (deleted != 1) throw new InvalidOperationException("Pending idempotency claim was not found.");
        }

        public async Task<Account?> FindByNumberAsync(string number, CancellationToken ct = default)
        {
            AccountRow? row = await connection.QuerySingleOrDefaultAsync<AccountRow>(new CommandDefinition(
                """
                SELECT id, number, pin_hash, balance, created_at_utc, owner_sub,
                    failed_pin_attempts, pin_locked_until_utc
                FROM accounts WHERE number = @Number
                """, new { Number = number }, transaction, cancellationToken: ct));
            return row?.ToDomain();
        }

        public async Task<Account?> LockAccountAsync(Guid key, CancellationToken ct = default)
        {
            AccountRow? row = await connection.QuerySingleOrDefaultAsync<AccountRow>(new CommandDefinition(
                """
                SELECT id, number, pin_hash, balance, created_at_utc, owner_sub,
                    failed_pin_attempts, pin_locked_until_utc
                FROM accounts WHERE id = @Id FOR UPDATE
                """, new { Id = key }, transaction, cancellationToken: ct));
            return row?.ToDomain();
        }

        public void SaveAccount(Account account) => _accounts[account.Key] = account;

        public void AddTransaction(Transaction entry) => _entries.Add(entry);

        public async Task CompleteAsync(string key, MoneyOperationResult result, CancellationToken ct = default)
        {
            int updated = await connection.ExecuteAsync(new CommandDefinition(
                """
                UPDATE idempotency_keys
                SET status = @Status, balance_after = @BalanceAfter, operation_id = @OperationId
                WHERE idempotency_key = @Key
                """, new
                {
                    Key = key,
                    Status = result.Status.ToString(),
                    result.BalanceAfter,
                    result.OperationId
                }, transaction, cancellationToken: ct));
            if (updated != 1) throw new InvalidOperationException("Idempotency claim was not found.");
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            foreach (Account account in _accounts.Values)
            {
                int updated = await connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE accounts SET balance = @Balance, failed_pin_attempts = @FailedPinAttempts, " +
                    "pin_locked_until_utc = @PinLockedUntilUtc WHERE id = @Id",
                    new
                    {
                        Id = account.Key,
                        Balance = account.Balance.Amount,
                        account.FailedPinAttempts,
                        account.PinLockedUntilUtc
                    }, transaction, cancellationToken: ct));
                if (updated != 1) throw new InvalidOperationException("Locked account was not found.");
            }

            foreach (Transaction entry in _entries)
                await FinancialEntrySql.InsertAsync(connection, transaction, entry, ct);
        }
    }
}
