namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;

public abstract record SessionType
{
    private SessionType() { }

    public sealed record User : SessionType;

    public sealed record Admin : SessionType;
}