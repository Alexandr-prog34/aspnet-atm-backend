using FluentMigrator;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess.FluentMigrations;

// The existing EF Core schema is deliberately left in place. IF NOT EXISTS makes this
// migration a baseline for databases already created by the original EF migration.
[Migration(202610280001)]
public sealed class M00001_BaselineAtmSchema : Migration
{
    public override void Up()
    {
        Execute.Sql("""
            CREATE TABLE IF NOT EXISTS accounts (
                id uuid NOT NULL,
                number varchar(64) NOT NULL,
                pin_hash varchar(200) NOT NULL,
                balance numeric(18,2) NOT NULL,
                created_at_utc timestamptz NOT NULL,
                CONSTRAINT "PK_accounts" PRIMARY KEY (id),
                CONSTRAINT ck_accounts_balance CHECK (balance >= 0)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_accounts_number" ON accounts (number);

            CREATE TABLE IF NOT EXISTS financial_transactions (
                id uuid NOT NULL,
                operation_id uuid NOT NULL,
                account_id uuid NOT NULL,
                type varchar(20) NOT NULL,
                amount numeric(18,2) NOT NULL,
                balance_after numeric(18,2) NOT NULL,
                occurred_at_utc timestamptz NOT NULL,
                CONSTRAINT "PK_financial_transactions" PRIMARY KEY (id),
                CONSTRAINT ck_transactions_amount CHECK (amount > 0),
                CONSTRAINT ck_transactions_balance CHECK (balance_after >= 0),
                CONSTRAINT ck_transactions_type CHECK
                    (type IN ('deposit', 'withdrawal', 'opening', 'transfer_out', 'transfer_in')),
                CONSTRAINT "FK_financial_transactions_accounts_account_id"
                    FOREIGN KEY (account_id) REFERENCES accounts (id) ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_financial_transactions_account_id_occurred_at_utc_id"
                ON financial_transactions (account_id, occurred_at_utc, id);
            CREATE INDEX IF NOT EXISTS "IX_financial_transactions_operation_id"
                ON financial_transactions (operation_id);
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_financial_transactions_operation_id_account_id"
                ON financial_transactions (operation_id, account_id);

            CREATE TABLE IF NOT EXISTS idempotency_keys (
                idempotency_key varchar(200) NOT NULL,
                account_id uuid NOT NULL,
                request_hash varchar(64) NOT NULL,
                status varchar(40) NOT NULL,
                balance_after numeric(18,2),
                operation_id uuid,
                created_at_utc timestamptz NOT NULL,
                CONSTRAINT "PK_idempotency_keys" PRIMARY KEY (idempotency_key),
                CONSTRAINT "FK_idempotency_keys_accounts_account_id"
                    FOREIGN KEY (account_id) REFERENCES accounts (id) ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS "IX_idempotency_keys_account_id"
                ON idempotency_keys (account_id);

            """);
    }

    public override void Down() =>
        throw new NotSupportedException("The baseline migration must not drop financial data.");
}
