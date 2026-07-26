namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;

public abstract record CreateUserSessionResult
{
    private CreateUserSessionResult() { }

    public sealed record Success(Guid SessionKey) : CreateUserSessionResult;

    public sealed record AccountNotFound : CreateUserSessionResult;

    public sealed record InvalidPin : CreateUserSessionResult;
}
