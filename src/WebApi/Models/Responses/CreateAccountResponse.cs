namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Responses;

public record CreateAccountResponse(Guid AccountKey, string AccountNumber, decimal Balance);