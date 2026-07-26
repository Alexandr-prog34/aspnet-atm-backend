using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Mapping;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Operations;

public sealed class GetHistoryService
{
    private readonly ITransactionRepository _transactions;

    public GetHistoryService(ITransactionRepository transactions)
    {
        _transactions = transactions;
    }

    public async Task<IReadOnlyList<TransactionDto>> GetByAccountAsync(
        string rawAccountNumber,
        CancellationToken ct = default)
    {
        var number = new AccountNumber(rawAccountNumber);

        IReadOnlyCollection<Transaction> list = await _transactions.GetByAccountAsync(number, ct);

        return list
            .Select(tx => tx.AsDto())
            .ToArray();
    }
}
