using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Sessions;

public sealed class CreateUserSessionService
{
    private readonly IAccountRepository _accounts;
    private readonly ISessionRepository _sessions;

    public CreateUserSessionService(
        IAccountRepository accounts,
        ISessionRepository sessions)
    {
        _accounts = accounts;
        _sessions = sessions;
    }

    public async Task<CreateUserSessionResultt> CreateAsync(
        string rawAccountNumber,
        string rawPin,
        CancellationToken ct = default)
    {
        var number = new AccountNumber(rawAccountNumber);

        Account? account = await _accounts.GetByNumberAsync(number, ct);
        if (account is null)
            return new CreateUserSessionResultt.AccountNotFound();

        if (!account.CheckPin(rawPin))
            return new CreateUserSessionResultt.InvalidPin();

        var session = new Session(
            key: Guid.NewGuid(),
            type: new SessionType.User(),
            accountNumber: number,
            accountKey: account.Key,
            createdAtUtc: DateTime.UtcNow);

        await _sessions.SaveAsync(session, ct);

        return new CreateUserSessionResultt.Success(session.Key);
    }
}
