using Dapper;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;
using Npgsql;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;

public sealed class PostgresAccountRepository(NpgsqlDataSource dataSource) : IAccountRepository
{
    private const string AccountColumns =
        "id, number, pin_hash, balance, created_at_utc, owner_sub, failed_pin_attempts, pin_locked_until_utc";

    public async Task<Account?> GetByNumberAsync(AccountNumber number, CancellationToken ct = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        AccountRow? row = await connection.QuerySingleOrDefaultAsync<AccountRow>(
            new CommandDefinition($"SELECT {AccountColumns} FROM accounts WHERE number = @Number",
                new { Number = number.Value }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<Account?> GetByKeyAsync(Guid key, CancellationToken ct = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        AccountRow? row = await connection.QuerySingleOrDefaultAsync<AccountRow>(
            new CommandDefinition($"SELECT {AccountColumns} FROM accounts WHERE id = @Id",
                new { Id = key }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<Account?> GetByOwnerSubjectAsync(string subject, CancellationToken ct = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        AccountRow? row = await connection.QuerySingleOrDefaultAsync<AccountRow>(new CommandDefinition(
            $"SELECT {AccountColumns} FROM accounts WHERE owner_sub = @Subject",
            new { Subject = subject }, cancellationToken: ct));
        return row?.ToDomain();
    }

    public async Task<bool> SetOwnerAsync(Guid key, string subject, CancellationToken ct = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        try
        {
            int updated = await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE accounts SET owner_sub = @Subject WHERE id = @Id AND owner_sub IS NULL",
                new { Id = key, Subject = subject }, cancellationToken: ct));
            return updated == 1;
        }
        catch (PostgresException exception) when
            (exception.SqlState == PostgresErrorCodes.UniqueViolation &&
             exception.ConstraintName == "IX_accounts_owner_sub")
        {
            return false;
        }
    }

    public async Task<bool> CreateAsync(Account account, Transaction? openingTransaction,
        CancellationToken ct = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            AccountRow row = account.ToRow();
            await connection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO accounts (id, number, pin_hash, balance, created_at_utc, owner_sub,
                    failed_pin_attempts, pin_locked_until_utc)
                VALUES (@Id, @Number, @PinHash, @Balance, @CreatedAtUtc, @OwnerSub,
                    @FailedPinAttempts, @PinLockedUntilUtc)
                """, row, transaction, cancellationToken: ct));

            if (openingTransaction is not null)
                await FinancialEntrySql.InsertAsync(connection, transaction, openingTransaction, ct);

            await transaction.CommitAsync(ct);
            return true;
        }
        catch (PostgresException exception) when
            (exception.SqlState == PostgresErrorCodes.UniqueViolation &&
             exception.ConstraintName is "IX_accounts_number" or "IX_accounts_owner_sub")
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return false;
        }
    }
}
