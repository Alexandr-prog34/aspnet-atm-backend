using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetByAccountAsync(Guid accountKey, int offset, int limit,
        CancellationToken ct = default);
}
