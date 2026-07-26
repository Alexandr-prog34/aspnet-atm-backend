using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Sessions;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;

public sealed class UserSessionAuthorizationService
{
    private readonly CheckSessionService _checkSession;

    public UserSessionAuthorizationService(CheckSessionService checkSession)
    {
        _checkSession = checkSession;
    }

    public async Task<CheckSessionService.CheckSessionResult> AuthorizeAsync(
        Guid sessionKey,
        CancellationToken ct = default)
    {
        return await _checkSession.ValidateUserSessionAsync(sessionKey, ct);
    }
}