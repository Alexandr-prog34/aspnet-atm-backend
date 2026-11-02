using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Mapping;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Operations;

public sealed class GetHistoryService
{
    private readonly ITransactionRepository _transactions;
    private readonly IAccountRepository _accounts;

    public GetHistoryService(ITransactionRepository transactions, IAccountRepository accounts)
    {
        _transactions = transactions;
        _accounts = accounts;
    }

    public async Task<AccountAccessResult<IReadOnlyList<TransactionDto>>> GetAsync(
        string ownerSubject, int offset, int limit,
        CancellationToken ct = default)
    {
        Account? account = await _accounts.GetByOwnerSubjectAsync(ownerSubject, ct);
        if (account is null) return new(AccountAccessStatus.AccountNotFound);
        IReadOnlyList<Transaction> list = await _transactions.GetByAccountAsync(account.Key, offset, limit, ct);

        return new(AccountAccessStatus.Success, list
            .Select(tx => tx.AsDto())
            .ToArray());
    }
}
