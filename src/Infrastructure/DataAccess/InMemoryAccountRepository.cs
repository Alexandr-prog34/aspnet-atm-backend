using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;

public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly Dictionary<string, Account> _storage = new();

    public Task<Account?> GetByNumberAsync(AccountNumber number, CancellationToken ct = default)
    {
        _storage.TryGetValue(number.Value, out Account? account);
        return Task.FromResult(account);
    }

    public Task AddAsync(Account account, CancellationToken ct = default)
    {
        _storage[account.Number.Value] = account;
        return Task.CompletedTask;
    }
}