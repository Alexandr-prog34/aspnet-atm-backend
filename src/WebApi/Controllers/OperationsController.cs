using Itmo.ObjectOrientedProgramming.Lab5.Application;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Operations;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Requests;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "user")]
[Route("api/accounts/me")]
public sealed class OperationsController(
    BankingService banking,
    GetBalanceService balance,
    GetHistoryService history,
    ILogger<OperationsController> logger) : ControllerBase
{
    private string Subject => User.FindFirst("sub")!.Value;

    [HttpGet("balance")]
    public async Task<ActionResult<BalanceResponse>> GetBalanceAsync(CancellationToken ct)
    {
        AccountAccessResult<AccountDto> result = await balance.GetAsync(Subject, ct);
        return result.Status == AccountAccessStatus.Success
            ? Ok(new BalanceResponse(result.Value!.Balance))
            : AccessProblem(result.Status);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IReadOnlyList<TransactionHistoryItemResponse>>> GetHistoryAsync(
        [FromQuery] int offset = 0, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        if (offset < 0 || limit is < 1 or > 100)
            return Problem(statusCode: 400, title: "Invalid pagination parameters");

        AccountAccessResult<IReadOnlyList<TransactionDto>> result = await history.GetAsync(Subject, offset, limit, ct);
        if (result.Status != AccountAccessStatus.Success) return AccessProblem(result.Status);

        return Ok(result.Value!.Select(x => new TransactionHistoryItemResponse(
            x.Id, x.OperationId, x.AccountNumber, x.Type, x.Amount, x.OccurredAtUtc, x.BalanceAfter)).ToArray());
    }

    [HttpPost("deposits")]
    public Task<ActionResult<MoneyOperationResponse>> DepositAsync([FromBody] DepositRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) =>
        ExecuteAsync("deposit", () => banking.DepositAsync(Subject, request.Amount, key, ct));

    [HttpPost("withdrawals")]
    public Task<ActionResult<MoneyOperationResponse>> WithdrawAsync([FromBody] WithdrawRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) =>
        ExecuteAsync("withdrawal", () => banking.WithdrawAsync(Subject, request.Amount, key, request.Pin, ct));

    [HttpPost("transfers")]
    public Task<ActionResult<MoneyOperationResponse>> TransferAsync([FromBody] TransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct) =>
        ExecuteAsync("transfer", () => banking.TransferAsync(Subject, request.TargetAccountNumber,
            request.Amount, key, request.Pin, ct));

    private async Task<ActionResult<MoneyOperationResponse>> ExecuteAsync(string kind,
        Func<Task<MoneyOperationResult>> action)
    {
        MoneyOperationResult result = await action();
        logger.LogInformation("Financial operation {Kind} result {Status} {OperationId}",
            kind, result.Status, result.OperationId);
        if (result.Status == MoneyOperationStatus.Success)
        {
            return Ok(new MoneyOperationResponse(result.OperationId!.Value, result.BalanceAfter!.Value));
        }

        return result.Status switch
        {
            MoneyOperationStatus.InvalidAmount => Problem(statusCode: 400, title: "Invalid amount"),
            MoneyOperationStatus.InvalidIdempotencyKey => Problem(statusCode: 400, title: "Idempotency-Key is required"),
            MoneyOperationStatus.InsufficientFunds => Problem(statusCode: 409, title: "Insufficient funds"),
            MoneyOperationStatus.SameAccount => Problem(statusCode: 400, title: "Cannot transfer to the same account"),
            MoneyOperationStatus.IdempotencyConflict => Problem(statusCode: 409, title: "Idempotency key conflict"),
            MoneyOperationStatus.AccountNotFound => Problem(statusCode: 404, title: "Account not found"),
            MoneyOperationStatus.AccessDenied => Problem(statusCode: 403, title: "Access denied"),
            MoneyOperationStatus.InvalidPin => Problem(statusCode: 403, title: "Invalid PIN"),
            MoneyOperationStatus.PinLocked => Problem(statusCode: 429, title: "PIN temporarily locked"),
            _ => Problem(statusCode: 500, title: "Unexpected operation result"),
        };
    }

    private ObjectResult AccessProblem(AccountAccessStatus status) => status switch
    {
        AccountAccessStatus.AccessDenied => Problem(statusCode: 403, title: "Access denied"),
        AccountAccessStatus.AccountNotFound => Problem(statusCode: 404, title: "Account not found"),
        _ => Problem(statusCode: 500, title: "Unexpected account result"),
    };
}
