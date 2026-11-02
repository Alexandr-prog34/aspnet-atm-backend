using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByNumberAsync(AccountNumber number, CancellationToken ct = default);

    Task<Account?> GetByKeyAsync(Guid key, CancellationToken ct = default);

    Task<Account?> GetByOwnerSubjectAsync(string subject, CancellationToken ct = default);

    Task<bool> SetOwnerAsync(Guid key, string subject, CancellationToken ct = default);

    Task<bool> CreateAsync(Account account, Transaction? openingTransaction, CancellationToken ct = default);
}
