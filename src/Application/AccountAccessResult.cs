namespace Itmo.ObjectOrientedProgramming.Lab5.Application;

public enum AccountAccessStatus { Success, AccessDenied, AccountNotFound }

public sealed record AccountAccessResult<T>(AccountAccessStatus Status, T? Value = default);
