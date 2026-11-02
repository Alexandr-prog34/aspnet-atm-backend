using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateAccountService>();
        services.AddScoped<LinkAccountOwnerService>();
        services.AddScoped<GetBalanceService>();
        services.AddScoped<BankingService>();
        services.AddScoped<GetHistoryService>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
