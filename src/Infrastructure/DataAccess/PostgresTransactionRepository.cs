using Dapper;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;
using Npgsql;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;

public sealed class PostgresTransactionRepository(NpgsqlDataSource dataSource) : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> GetByAccountAsync(Guid accountKey, int offset, int limit,
        CancellationToken ct = default)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        string? number = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT number FROM accounts WHERE id = @AccountId",
            new { AccountId = accountKey }, cancellationToken: ct));
        if (number is null) return [];

        IEnumerable<FinancialEntryRow> rows = await connection.QueryAsync<FinancialEntryRow>(new CommandDefinition(
            """
            SELECT id, operation_id, account_id, type, amount, balance_after, occurred_at_utc
            FROM financial_transactions
            WHERE account_id = @AccountId
            ORDER BY occurred_at_utc DESC, id DESC
            OFFSET @Offset LIMIT @Limit
            """, new { AccountId = accountKey, Offset = offset, Limit = limit }, cancellationToken: ct));
        var accountNumber = new AccountNumber(number);
        return rows.Select(row => row.ToDomain(accountNumber)).ToArray();
    }
}
