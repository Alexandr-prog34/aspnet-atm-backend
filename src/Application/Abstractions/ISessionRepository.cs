using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;

public interface ISessionRepository
{
    Task<Session?> GetAsync(Guid key, CancellationToken ct = default);

    Task SaveAsync(Session session, CancellationToken ct = default);
}