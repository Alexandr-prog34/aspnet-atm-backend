using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.DataAccess;

public sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly Dictionary<Guid, Session> _storage = new();

    public Task<Session?> GetAsync(Guid key, CancellationToken ct = default)
    {
        _storage.TryGetValue(key, out Session? session);
        return Task.FromResult(session);
    }

    public Task SaveAsync(Session session, CancellationToken ct = default)
    {
        _storage[session.Key] = session;
        return Task.CompletedTask;
    }
}