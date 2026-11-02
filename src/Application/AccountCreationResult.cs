using Itmo.ObjectOrientedProgramming.Lab5.Application.Dto;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application;

public enum AccountCreationStatus { Success, InvalidInput, DuplicateNumber }

public sealed record AccountCreationResult(AccountCreationStatus Status, AccountDto? Account = null);
