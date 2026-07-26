using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Mapping;

public static class AccountMapping
{
    public static AccountDto AsDto(this Account account)
    {
        return new(account.Number.Value, account.Balance.Amount, account.Key);
    }
}
