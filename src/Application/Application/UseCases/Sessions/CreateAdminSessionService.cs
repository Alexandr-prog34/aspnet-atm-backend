using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Sessions;

public sealed class CreateAdminSessionService
{
    private readonly ISessionRepository _sessions;
    private readonly IAdminPasswordProvider _passwordProvider;

    public CreateAdminSessionService(
        ISessionRepository sessions,
        IAdminPasswordProvider passwordProvider)
    {
        _sessions = sessions;
        _passwordProvider = passwordProvider;
    }

    public async Task<CreateAdminSessionResultt> CreateAsync(
        string rawPassword,
        CancellationToken ct = default)
    {
        if (!_passwordProvider.IsValid(rawPassword))
            return new CreateAdminSessionResultt.InvalidPassword();

        var session = new Session(
            key: Guid.NewGuid(),
            type: new SessionType.Admin(),
            accountNumber: null,
            accountKey: null,
            createdAtUtc: DateTime.UtcNow);

        await _sessions.SaveAsync(session, ct);

        return new CreateAdminSessionResultt.Success(session.Key);
    }
}
