using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByNumberAsync(AccountNumber number, CancellationToken ct = default);

    Task AddAsync(Account account, CancellationToken ct = default);
}