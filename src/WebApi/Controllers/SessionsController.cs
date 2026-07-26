using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Sessions;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Requests;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly CreateAdminSessionService _createAdminSession;
    private readonly CreateUserSessionService _createUserSession;

    public SessionsController(
        CreateAdminSessionService createAdminSession,
        CreateUserSessionService createUserSession)
    {
        _createAdminSession = createAdminSession;
        _createUserSession = createUserSession;
    }

    public CancellationToken CancellationToken => HttpContext.RequestAborted;

    [HttpPost("admin")]
    public async Task<ActionResult<AdminSessionResponse>> CreateAdminSessionAsync(
        [FromBody] AdminSessionRequest request)
    {
        CreateAdminSessionResultt result = await _createAdminSession.CreateAsync(request.Password, CancellationToken);

        return result switch
        {
            CreateAdminSessionResultt.Success success =>
                Ok(new AdminSessionResponse(success.SessionKey)),
            CreateAdminSessionResultt.InvalidPassword =>
                Unauthorized(new { error = "Invalid password" }),
            _ => BadRequest(),
        };
    }

    [HttpPost("user")]
    public async Task<ActionResult<UserSessionResponse>> CreateUserSessionAsync(
    [FromBody] UserSessionRequest request)
    {
        CreateUserSessionResultt result =
            await _createUserSession.CreateAsync(
                request.AccountNumber,
                request.Pin,
                CancellationToken);

        return result switch
        {
            CreateUserSessionResultt.Success success =>
                Ok(new UserSessionResponse(success.SessionKey)),
            CreateUserSessionResultt.AccountNotFound =>
                NotFound(new { error = "Account not found" }),
            CreateUserSessionResultt.InvalidPin =>
                Unauthorized(new { error = "Invalid PIN" }),
            _ => BadRequest(),
        };
    }
}
