using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.Authentication;
using Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
        services.AddSingleton<ITransactionRepository, InMemoryTransactionRepository>();
        services.AddSingleton<ISessionRepository, InMemorySessionRepository>();

        services.AddSingleton<IAdminPasswordProvider>(
            new AdminPasswordProvider());

        return services;
    }
}