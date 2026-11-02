using Dapper;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Npgsql;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;

internal static class FinancialEntrySql
{
    public static Task<int> InsertAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        Transaction entry, CancellationToken ct) => connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO financial_transactions
                (id, operation_id, account_id, type, amount, balance_after, occurred_at_utc)
            VALUES (@Id, @OperationId, @AccountId, @Type, @Amount, @BalanceAfter, @OccurredAtUtc)
            """, entry.ToRow(), transaction, cancellationToken: ct));
}
