using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Sessions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Requests;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly CreateAccountService _createAccount;
    private readonly CheckSessionService _checkSession;

    public AccountsController(
        CreateAccountService createAccount,
        CheckSessionService checkSession)
    {
        _createAccount = createAccount;
        _checkSession = checkSession;
    }

    public CancellationToken CancellationToken => HttpContext.RequestAborted;

    [HttpPost("create")]
    public async Task<ActionResult<CreateAccountResponse>> CreateAsync(
        [FromBody] CreateAccountRequest request,
        [FromHeader(Name = "X-Session-Key")] string? sessionKeyHeader)
    {
        if (string.IsNullOrWhiteSpace(sessionKeyHeader))
            return Unauthorized(new { error = "X-Session-Key header is required" });

        if (!Guid.TryParse(sessionKeyHeader, out Guid sessionKey))
            return BadRequest(new { error = "Invalid session key format" });

        CheckSessionService.CheckSessionResult adminCheck =
            await _checkSession.ValidateAdminSessionAsync(sessionKey, CancellationToken);

        switch (adminCheck)
        {
            case CheckSessionService.CheckSessionResult.InvalidSession:
                return Unauthorized(new { error = "Invalid session" });

            case CheckSessionService.CheckSessionResult.AccessDenied:
                return Forbid();

            case CheckSessionService.CheckSessionResult.Success:
                break;
        }

        AccountDto accountDto = await _createAccount.CreateAsync(
            request.AccountNumber,
            request.Pin,
            request.InitialBalance,
            CancellationToken);

        var response = new CreateAccountResponse(
            accountDto.Key,
            accountDto.AccountNumber,
            accountDto.Balance);

        return Ok(response);
    }
}