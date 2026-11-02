using Itmo.ObjectOrientedProgramming.Lab5.Application;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Requests;
using Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(CreateAccountService createAccount, LinkAccountOwnerService linkOwner) : ControllerBase
{
    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<ActionResult<CreateAccountResponse>> CreateAsync(
        [FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        AccountCreationResult result = await createAccount.CreateAsync(
            request.AccountNumber, request.Pin, request.InitialBalance, request.OwnerSubject, ct);
        return result.Status switch
        {
            AccountCreationStatus.Success => StatusCode(StatusCodes.Status201Created,
                new CreateAccountResponse(result.Account!.Key, result.Account.AccountNumber, result.Account.Balance)),
            AccountCreationStatus.DuplicateNumber => Problem(statusCode: 409, title: "Account number or owner already exists"),
            _ => Problem(statusCode: 400, title: "Invalid account data"),
        };
    }

    [Authorize(Roles = "admin")]
    [HttpPut("{id:guid}/owner")]
    public async Task<IActionResult> LinkOwnerAsync(Guid id, [FromBody] LinkOwnerRequest request,
        CancellationToken ct)
    {
        LinkOwnerStatus status = await linkOwner.LinkAsync(id, request.OwnerSubject, ct);
        return status switch
        {
            LinkOwnerStatus.Success => NoContent(),
            LinkOwnerStatus.NotFound => Problem(statusCode: 404, title: "Account not found"),
            LinkOwnerStatus.Conflict => Problem(statusCode: 409, title: "Account already linked or owner in use"),
            _ => Problem(statusCode: 400, title: "Invalid owner subject"),
        };
    }
}
