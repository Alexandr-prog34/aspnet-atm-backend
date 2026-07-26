namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.ResultType;

public abstract record CreateUserSessionResultt
{
    public record Success(Guid SessionKey) : CreateUserSessionResultt;

    public record AccountNotFound : CreateUserSessionResultt;

    public record InvalidPin : CreateUserSessionResultt;
}
