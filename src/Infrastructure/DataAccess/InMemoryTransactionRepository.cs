using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;
using System.Collections.ObjectModel;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;

public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> _storage = new();

    public Task AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        _storage.Add(transaction);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Transaction>> GetByAccountAsync(
        AccountNumber number,
        CancellationToken ct = default)
    {
        ReadOnlyCollection<Transaction> transactions = _storage
            .Where(tx => tx.AccountNumber.Value == number.Value)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyCollection<Transaction>>(transactions);
    }
}