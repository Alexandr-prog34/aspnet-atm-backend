using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Operations;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateAccountService>();
        services.AddScoped<GetBalanceService>();
        services.AddScoped<WithdrawService>();
        services.AddScoped<DepositService>();
        services.AddScoped<GetHistoryService>();

        services.AddScoped<CreateAdminSessionService>();
        services.AddScoped<CreateUserSessionService>();
        services.AddScoped<CheckSessionService>();
        services.AddScoped<UserSessionAuthorizationService>();

        return services;
    }
}