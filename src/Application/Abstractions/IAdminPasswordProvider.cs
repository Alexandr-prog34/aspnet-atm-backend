namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;

public interface IAdminPasswordProvider
{
    bool IsValid(string password);
}