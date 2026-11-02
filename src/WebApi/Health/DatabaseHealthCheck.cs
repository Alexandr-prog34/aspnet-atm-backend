using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Health;

public sealed class DatabaseHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (NpgsqlException)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
        }
    }
}
