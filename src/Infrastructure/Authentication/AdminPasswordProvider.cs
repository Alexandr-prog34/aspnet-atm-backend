using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;

namespace Itmo.ObjectOrientedProgramming.Lab5.Infrastructure.Authentication;

public sealed class AdminPasswordProvider : IAdminPasswordProvider
{
    private const string AdminPassword = "admin123";

    public bool IsValid(string password)
    {
        return password == AdminPassword;
    }
}