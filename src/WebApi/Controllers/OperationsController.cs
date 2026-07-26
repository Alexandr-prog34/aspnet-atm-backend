using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Operations;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Sessions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Requests;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperationsController : ControllerBase
{
    private readonly DepositService _deposit;
    private readonly WithdrawService _withdraw;
    private readonly GetHistoryService _history;
    private readonly GetBalanceService _getBalance;
    private readonly CheckSessionService _checkSession;

    public OperationsController(
        DepositService deposit,
        WithdrawService withdraw,
        GetHistoryService history,
        GetBalanceService getBalance,
        CheckSessionService checkSession)
    {
        _deposit = deposit;
        _withdraw = withdraw;
        _history = history;
        _getBalance = getBalance;
        _checkSession = checkSession;
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<BalanceResponse>> DepositAsync(
        [FromBody] DepositRequest request,
        [FromHeader(Name = "X-Session-Key")] string? sessionKeyHeader,
        [FromHeader(Name = "X-Account-Number")] string? accountNumberHeader)
    {
        if (string.IsNullOrWhiteSpace(sessionKeyHeader))
            return Unauthorized(new { error = "X-Session-Key header is required" });

        if (!Guid.TryParse(sessionKeyHeader, out Guid sessionKey))
            return BadRequest(new { error = "Invalid session key format" });

        CheckSessionService.CheckSessionResult sessionCheck =
            await _checkSession.ValidateUserSessionAsync(sessionKey);

        switch (sessionCheck)
        {
            case CheckSessionService.CheckSessionResult.InvalidSession:
                return Unauthorized(new { error = "Invalid session" });
            case CheckSessionService.CheckSessionResult.AccessDenied:
                return Forbid();
            case CheckSessionService.CheckSessionResult.Success:
                break;
        }

        if (string.IsNullOrWhiteSpace(accountNumberHeader))
            return BadRequest(new { error = "X-Account-Number header is required" });

        DepositResultDto result =
            await _deposit.DepositAsync(accountNumberHeader, request.Amount);

        return result switch
        {
            DepositResultDto.Success success =>
                Ok(new BalanceResponse(success.BalanceAfter)),
            DepositResultDto.AccountNotFound =>
                NotFound(new { error = "Account not found" }),
            DepositResultDto.InvalidAmount =>
                BadRequest(new { error = "Invalid amount" }),
            _ => BadRequest(),
        };
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<BalanceResponse>> WithdrawAsync(
        [FromBody] WithdrawRequest request,
        [FromHeader(Name = "X-Session-Key")] string? sessionKeyHeader,
        [FromHeader(Name = "X-Account-Number")] string? accountNumberHeader)
    {
        if (string.IsNullOrWhiteSpace(sessionKeyHeader))
            return Unauthorized(new { error = "X-Session-Key header is required" });

        if (!Guid.TryParse(sessionKeyHeader, out Guid sessionKey))
            return BadRequest(new { error = "Invalid session key format" });

        CheckSessionService.CheckSessionResult sessionCheck =
            await _checkSession.ValidateUserSessionAsync(sessionKey);

        switch (sessionCheck)
        {
            case CheckSessionService.CheckSessionResult.InvalidSession:
                return Unauthorized(new { error = "Invalid session" });
            case CheckSessionService.CheckSessionResult.AccessDenied:
                return Forbid();
            case CheckSessionService.CheckSessionResult.Success:
                break;
        }

        if (string.IsNullOrWhiteSpace(accountNumberHeader))
            return BadRequest(new { error = "X-Account-Number header is required" });

        WithDrawResultDto result =
            await _withdraw.WithdrawAsync(accountNumberHeader, request.Amount);

        return result switch
        {
            WithDrawResultDto.Success success =>
                Ok(new BalanceResponse(success.BalanceAfter)),
            WithDrawResultDto.InvalidAmount =>
                BadRequest(new { error = "Insufficient funds" }),
            _ => BadRequest(),
        };
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<TransactionHistoryItemResponse>>> GetHistoryAsync(
        [FromHeader(Name = "X-Session-Key")] string? sessionKeyHeader,
        [FromHeader(Name = "X-Account-Number")] string? accountNumberHeader)
    {
        if (string.IsNullOrWhiteSpace(sessionKeyHeader))
            return Unauthorized(new { error = "X-Session-Key header is required" });

        if (!Guid.TryParse(sessionKeyHeader, out Guid sessionKey))
            return BadRequest(new { error = "Invalid session key format" });

        CheckSessionService.CheckSessionResult sessionCheck =
            await _checkSession.ValidateUserSessionAsync(sessionKey);

        switch (sessionCheck)
        {
            case CheckSessionService.CheckSessionResult.InvalidSession:
                return Unauthorized(new { error = "Invalid session" });
            case CheckSessionService.CheckSessionResult.AccessDenied:
                return Forbid();
            case CheckSessionService.CheckSessionResult.Success:
                break;
        }

        if (string.IsNullOrWhiteSpace(accountNumberHeader))
            return BadRequest(new { error = "X-Account-Number header is required" });

        IReadOnlyList<TransactionDto> history =
            await _history.GetByAccountAsync(accountNumberHeader);

        TransactionHistoryItemResponse[] response = history
            .Select(tx => new TransactionHistoryItemResponse(
                tx.Id,
                tx.AccountNumber,
                tx.Type,
                tx.Amount,
                tx.OccurredAtUtc,
                tx.BalanceAfter))
            .ToArray();

        return Ok(response);
    }

    [HttpGet("balance")]
    public async Task<ActionResult<BalanceResponse>> GetBalanceAsync(
        [FromHeader(Name = "X-Session-Key")] string? sessionKeyHeader,
        [FromHeader(Name = "X-Account-Number")] string? accountNumberHeader)
    {
        if (string.IsNullOrWhiteSpace(sessionKeyHeader))
            return Unauthorized(new { error = "X-Session-Key header is required" });

        if (!Guid.TryParse(sessionKeyHeader, out Guid sessionKey))
            return BadRequest(new { error = "Invalid session key format" });

        CheckSessionService.CheckSessionResult sessionCheck =
            await _checkSession.ValidateUserSessionAsync(sessionKey);

        switch (sessionCheck)
        {
            case CheckSessionService.CheckSessionResult.InvalidSession:
                return Unauthorized(new { error = "Invalid session" });
            case CheckSessionService.CheckSessionResult.AccessDenied:
                return Forbid();
            case CheckSessionService.CheckSessionResult.Success:
                break;
        }

        if (string.IsNullOrWhiteSpace(accountNumberHeader))
            return BadRequest(new { error = "X-Account-Number header is required" });

        AccountDto? account = await _getBalance.GetByNumberAsync(accountNumberHeader);

        if (account is null)
            return NotFound(new { error = "Account not found" });

        return Ok(new BalanceResponse(account.Balance));
    }
}