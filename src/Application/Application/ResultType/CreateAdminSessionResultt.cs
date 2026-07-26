namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.ResultType;

public abstract record CreateAdminSessionResultt
{
    public record Success(Guid SessionKey) : CreateAdminSessionResultt;

    public record InvalidPassword : CreateAdminSessionResultt;
}
