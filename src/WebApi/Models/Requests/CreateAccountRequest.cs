namespace Itmo.ObjectOrientedProgramming.Lab5.WebApi.Models.Requests;

public record CreateAccountRequest(
    string AccountNumber,
    string Pin,
    decimal InitialBalance);