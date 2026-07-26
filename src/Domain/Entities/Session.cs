using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;

public sealed class Session
{
    public Guid Key { get; }

    public SessionType Type { get; }

    public AccountNumber? AccountNumber { get; }

    public Guid? AccountKey { get; }

    public DateTime CreatedAtUtc { get; }

    public bool IsUser()
    {
        return Type is SessionType.User;
    }

    public bool IsAdmin()
    {
        return Type is SessionType.Admin;
    }

    public Session(Guid key, SessionType type, AccountNumber? accountNumber, Guid? accountKey, DateTime createdAtUtc)
    {
        Key = key;
        Type = type;
        AccountNumber = accountNumber;
        AccountKey = accountKey;
        CreatedAtUtc = createdAtUtc;
    }
}