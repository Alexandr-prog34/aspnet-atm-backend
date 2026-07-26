using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Mapping;

public static class TransactionMapping
{
    public static TransactionDto AsDto(this Transaction transaction)
    {
        string typeText = transaction.Type switch
        {
            TransactionType.Deposit => "Deposit",
            TransactionType.Withdrawal => "Withdrawal",
            _ => "Unknown",
        };
        return new TransactionDto(
            transaction.Id,
            transaction.AccountNumber.Value,
            typeText,
            transaction.Amount.Amount,
            transaction.OccurredAtUtc,
            transaction.BalanceAfter.Amount);
    }
}
