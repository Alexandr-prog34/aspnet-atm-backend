using FluentMigrator;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess.FluentMigrations;

[Migration(202610280002)]
public sealed class M00002_KeycloakOwnerAndPinLock : Migration
{
    public override void Up()
    {
        Execute.Sql("""
            ALTER TABLE accounts ADD COLUMN IF NOT EXISTS owner_sub varchar(200);
            ALTER TABLE accounts ADD COLUMN IF NOT EXISTS failed_pin_attempts integer NOT NULL DEFAULT 0;
            ALTER TABLE accounts ADD COLUMN IF NOT EXISTS pin_locked_until_utc timestamptz;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_accounts_owner_sub" ON accounts (owner_sub)
                WHERE owner_sub IS NOT NULL;
            ALTER TABLE accounts ADD CONSTRAINT ck_accounts_failed_pin_attempts
                CHECK (failed_pin_attempts >= 0);
            DROP TABLE IF EXISTS sessions;
            """);
    }

    public override void Down() => throw new NotSupportedException("Cannot reverse security migration safely.");
}
