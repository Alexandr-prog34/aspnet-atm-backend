namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;

public abstract record CreateAdminSessionResult
{
    private CreateAdminSessionResult() { }

    public sealed record Success(Guid SessionKey) : CreateAdminSessionResult;

    public sealed record InvalidPassword : CreateAdminSessionResult;
}
