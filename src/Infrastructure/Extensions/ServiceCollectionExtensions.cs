using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;
using Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess.FluentMigrations;
using Dapper;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Func<IServiceProvider, string> connectionString)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.AddSingleton(provider => NpgsqlDataSource.Create(connectionString(provider)));
        services.AddScoped<IAccountRepository, PostgresAccountRepository>();
        services.AddScoped<ITransactionRepository, PostgresTransactionRepository>();
        services.AddScoped<IFinancialTransactionRunner, PostgresFinancialTransactionRunner>();

        services.AddFluentMigratorCore()
            .ConfigureRunner(runner => runner.AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(M00001_BaselineAtmSchema).Assembly).For.Migrations());

        return services;
    }
}
