using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Sessions;

public sealed class CheckSessionService
{
    private readonly ISessionRepository _sessions;

    public CheckSessionService(ISessionRepository sessions)
    {
        _sessions = sessions;
    }

    public abstract record CheckSessionResult
    {
        public record Success(Guid AccountKey) : CheckSessionResult;

        public record InvalidSession : CheckSessionResult;

        public record AccessDenied : CheckSessionResult;
    }

    public async Task<CheckSessionResult> ValidateAdminSessionAsync(
        Guid sessionKey,
        CancellationToken ct = default)
    {
        Session? session = await _sessions.GetAsync(sessionKey, ct);
        if (session is null)
            return new CheckSessionResult.InvalidSession();

        if (!session.IsAdmin())
            return new CheckSessionResult.AccessDenied();

        return new CheckSessionResult.Success(Guid.Empty);
    }

    public async Task<CheckSessionResult> ValidateUserSessionAsync(
        Guid sessionKey,
        CancellationToken ct = default)
    {
        Session? session = await _sessions.GetAsync(sessionKey, ct);
        if (session is null)
            return new CheckSessionResult.InvalidSession();

        if (!session.IsUser())
            return new CheckSessionResult.AccessDenied();

        if (session.AccountKey is null)
            return new CheckSessionResult.AccessDenied();

        return new CheckSessionResult.Success(session.AccountKey.Value);
    }
}